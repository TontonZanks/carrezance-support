using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Printing;
using Carrezance.Support.App.Models;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;

namespace Carrezance.Support.App.Services;

public sealed class PrinterService
{
    private const string AdminMessage = "Cette action nécessite les droits administrateur. Veuillez relancer Carrezance Support en tant qu'administrateur.";
    private readonly LogService _logService;
    private readonly ProcessService _processService;
    private readonly AdminService _adminService;
    private readonly RepairActionHistoryService _historyService;

    public PrinterService(LogService logService, ProcessService processService, AdminService adminService, RepairActionHistoryService historyService)
    {
        _logService = logService;
        _processService = processService;
        _adminService = adminService;
        _historyService = historyService;
    }

    public string? LastError { get; private set; }

    public async Task<SupportActionResult> RestartSpoolerAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        const string actionName = "Réparer l'impression";
        _logService.Log("[Action]", $"{actionName} démarrée");
        var isAdmin = _adminService.IsAdministrator();
        if (!_adminService.IsAdministrator())
        {
            stopwatch.Stop();
            _historyService.Add(new RepairActionHistoryItem
            {
                DateTime = DateTime.Now,
                ActionName = actionName,
                Category = "Imprimantes",
                Status = "Non exécuté",
                Message = "Droits administrateur requis.",
                RequiresAdmin = true,
                ExecutedAsAdmin = isAdmin,
                DurationMs = stopwatch.ElapsedMilliseconds
            });
            _logService.Log("[Action]", $"{actionName} non exécutée : droits administrateur requis en {stopwatch.ElapsedMilliseconds} ms");
            return SupportActionResult.Fail(AdminMessage);
        }

        var stop = await _processService.RunAsync("sc.exe", "stop spooler", "Arrêt spouleur impression");
        await Task.Delay(1500);
        var start = await _processService.RunAsync("sc.exe", "start spooler", "Démarrage spouleur impression");
        stopwatch.Stop();

        var success = stop.Success || start.Success;
        var message = success ? "Le service d'impression a été relancé." : "Le service d'impression n'a pas pu être relancé.";
        _historyService.Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = actionName,
            Category = "Imprimantes",
            Status = success ? "Succès" : "Échec",
            Message = message,
            RequiresAdmin = true,
            ExecutedAsAdmin = isAdmin,
            DurationMs = stopwatch.ElapsedMilliseconds
        });
        _logService.Log("[Action]", $"{actionName} {(success ? "succès" : "échec")} en {stopwatch.ElapsedMilliseconds} ms");
        return success
            ? SupportActionResult.Ok(message)
            : SupportActionResult.Fail(message, $"{stop.Details}{Environment.NewLine}{start.Details}");
    }

    public async Task<SupportActionResult> ClearPrintQueueAsync()
    {
        return await Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            const string actionName = "Vider la file d'attente impression";
            _logService.Log("[Action]", $"{actionName} démarrée");
            var isAdmin = _adminService.IsAdministrator();
            if (!isAdmin)
            {
                stopwatch.Stop();
                RecordPrinterAction(actionName, "Non exécuté", "Droits administrateur requis.", true, false, stopwatch.ElapsedMilliseconds);
                _logService.Log("[Action]", $"{actionName} non exécutée : droits administrateur requis en {stopwatch.ElapsedMilliseconds} ms");
                return SupportActionResult.Fail(AdminMessage);
            }

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\spool\PRINTERS");
            var deleted = 0;
            var errors = 0;

            await _processService.RunAsync("sc.exe", "stop spooler", "Arrêt spouleur impression");
            await Task.Delay(1500);

            try
            {
                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            File.Delete(file);
                            deleted++;
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            _logService.Log("[Action]", $"{actionName} erreur fichier ignorée", ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors++;
                _logService.Log("[Action]", $"{actionName} erreur lecture dossier", ex.Message);
            }

            await _processService.RunAsync("sc.exe", "start spooler", "Démarrage spouleur impression");
            stopwatch.Stop();

            var message = $"File d'attente vidée : {deleted} fichier(s) supprimé(s), {errors} erreur(s) ignorée(s).";
            RecordPrinterAction(actionName, errors == 0 ? "Succès" : "Succès", message, true, true, stopwatch.ElapsedMilliseconds);
            _logService.Log("[Action]", $"{actionName} terminé : {deleted} fichiers supprimés, {errors} erreurs ignorées en {stopwatch.ElapsedMilliseconds} ms");
            return SupportActionResult.Ok(message);
        });
    }

    private void RecordPrinterAction(string actionName, string status, string message, bool requiresAdmin, bool executedAsAdmin, long durationMs)
    {
        _historyService.Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = actionName,
            Category = "Imprimantes",
            Status = status,
            Message = message,
            RequiresAdmin = requiresAdmin,
            ExecutedAsAdmin = executedAsAdmin,
            DurationMs = durationMs
        });
    }

    public ObservableCollection<PrinterInfo> GetPrinters()
    {
        var printers = new ObservableCollection<PrinterInfo>();
        LastError = null;

        try
        {
            using var server = new LocalPrintServer();
            string? defaultPrinter = null;

            try
            {
                defaultPrinter = server.DefaultPrintQueue?.FullName;
            }
            catch
            {
                defaultPrinter = null;
            }

            foreach (var queue in server.GetPrintQueues())
            {
                try
                {
                    printers.Add(new PrinterInfo
                    {
                        Name = queue.FullName,
                        IsDefault = string.Equals(queue.FullName, defaultPrinter, StringComparison.OrdinalIgnoreCase),
                        Status = queue.QueueStatus.ToString()
                    });
                }
                catch
                {
                    // One inaccessible printer must not block the full list.
                }
            }

            _logService.Log("Afficher imprimantes", "OK");
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logService.Log("Afficher imprimantes", "Erreur", ex.Message);
        }

        return printers;
    }
}
