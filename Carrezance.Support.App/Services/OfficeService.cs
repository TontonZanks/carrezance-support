using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;
using Carrezance.Support.App.Models;

namespace Carrezance.Support.App.Services;

public sealed class OfficeService
{
    private readonly LogService _logService;
    private readonly ProcessService _processService;
    private readonly RepairActionHistoryService _historyService;

    public OfficeService(LogService logService, ProcessService processService, RepairActionHistoryService historyService)
    {
        _logService = logService;
        _processService = processService;
        _historyService = historyService;
    }

    public async Task<SupportActionResult> CloseOutlookAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        const string actionName = "Fermer Outlook bloqué";
        _logService.Log("[Action]", $"{actionName} démarrée");
        try
        {
            var processes = Process.GetProcessesByName("OUTLOOK");
            if (processes.Length == 0)
            {
                stopwatch.Stop();
                RecordOfficeAction(actionName, "Non exécuté", "Outlook n'est pas en cours d'exécution.", stopwatch.ElapsedMilliseconds);
                _logService.Log("[Action]", $"{actionName} : aucun processus Outlook en cours d'exécution en {stopwatch.ElapsedMilliseconds} ms");
                return SupportActionResult.Ok("Outlook n'est pas en cours d'exécution.");
            }

            foreach (var process in processes)
            {
                try
                {
                    process.CloseMainWindow();
                }
                catch
                {
                    // Continue with the fallback kill below.
                }
            }

            await Task.Delay(1500);

            var killed = 0;
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        killed++;
                    }
                }
                catch
                {
                    // The process may have exited between checks.
                }
            }

            stopwatch.Stop();
            var message = $"Outlook a été fermé ({processes.Length} processus détecté(s), {killed} forcé(s)).";
            RecordOfficeAction(actionName, "Succès", message, stopwatch.ElapsedMilliseconds);
            _logService.Log("[Action]", $"{actionName} succès en {stopwatch.ElapsedMilliseconds} ms - {processes.Length} processus");
            return SupportActionResult.Ok(message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordOfficeAction(actionName, "Échec", "Impossible de fermer Outlook.", stopwatch.ElapsedMilliseconds);
            _logService.Log("Fermer Outlook bloqué", "Erreur", ex.ToString());
            return SupportActionResult.Fail("Impossible de fermer Outlook. Vous pouvez réessayer ou redémarrer le poste si Outlook reste bloqué.", ex.Message);
        }
    }

    public SupportActionResult StartOutlookSafeMode()
    {
        try
        {
            var outlookPath = FindClassicOutlookPath();
            if (string.IsNullOrWhiteSpace(outlookPath))
            {
                _logService.Log("Outlook mode sans échec", "Non trouvé");
                return SupportActionResult.Fail("Outlook classique n'a pas été trouvé sur ce poste.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = outlookPath,
                Arguments = "/safe",
                UseShellExecute = true
            });
            _logService.Log("Outlook mode sans échec", "OK");
            return SupportActionResult.Ok("Outlook a été lancé en mode sans échec.");
        }
        catch (Exception ex)
        {
            _logService.Log("Outlook mode sans échec", "Erreur", ex.Message);
            return SupportActionResult.Fail("Outlook n'a pas pu être lancé. Vérifiez qu'Outlook est installé sur ce poste.", ex.Message);
        }
    }

    public SupportActionResult OpenOutlookWeb()
    {
        return _processService.Open("https://outlook.office.com", "Ouvrir Outlook Web");
    }

    public SupportActionResult OpenOfficePortal()
    {
        return _processService.Open("https://www.office.com", "Ouvrir portail Microsoft 365");
    }

    private void RecordOfficeAction(string actionName, string status, string message, long durationMs)
    {
        _historyService.Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = actionName,
            Category = "Outlook & Office",
            Status = status,
            Message = message,
            DurationMs = durationMs
        });
    }

    private static string? FindClassicOutlookPath()
    {
        foreach (var registryPath in new[]
                 {
                     @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE",
                     @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE"
                 })
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(registryPath);
                var value = key?.GetValue(null)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch
            {
                // Best effort.
            }
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        return new[]
        {
            Path.Combine(programFiles, @"Microsoft Office\root\Office16\OUTLOOK.EXE"),
            Path.Combine(programFilesX86, @"Microsoft Office\root\Office16\OUTLOOK.EXE"),
            Path.Combine(programFiles, @"Microsoft Office\Office16\OUTLOOK.EXE"),
            Path.Combine(programFilesX86, @"Microsoft Office\Office16\OUTLOOK.EXE")
        }.FirstOrDefault(File.Exists);
    }
}
