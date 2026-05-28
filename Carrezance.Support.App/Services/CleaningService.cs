using System.IO;
using System.Diagnostics;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;

namespace Carrezance.Support.App.Services;

public sealed class CleaningService
{
    private readonly LogService _logService;
    private readonly RepairActionHistoryService _historyService;

    public CleaningService(LogService logService, RepairActionHistoryService historyService)
    {
        _logService = logService;
        _historyService = historyService;
    }

    public async Task<SupportActionResult> CleanUserTempAsync()
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            const string actionName = "Nettoyage simple";
            long cleaned = 0;
            var filesDeleted = 0;
            var foldersDeleted = 0;
            var ignored = 0;
            var errors = 0;
            var tempPath = Path.GetFullPath(Path.GetTempPath());
            _logService.Log("[Action]", $"{actionName} démarrée");

            try
            {
                if (!Directory.Exists(tempPath))
                {
                    stopwatch.Stop();
                    _logService.Log("[Action]", "Nettoyage TEMP utilisateur erreur", "Le dossier TEMP utilisateur est introuvable.");
                    RecordCleaningAction("Échec", "Le dossier temporaire de votre session Windows est introuvable.", stopwatch.ElapsedMilliseconds);
                    return SupportActionResult.Fail("Le dossier temporaire de votre session Windows est introuvable.");
                }

                foreach (var file in Directory.EnumerateFiles(tempPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (!IsInsideTemp(info.FullName, tempPath))
                        {
                            continue;
                        }

                        var length = info.Exists ? info.Length : 0;
                        info.Delete();
                        cleaned += File.Exists(info.FullName) ? 0 : length;
                        filesDeleted++;
                    }
                    catch
                    {
                        errors++;
                        // Locked temp files are normal and can be skipped.
                    }
                }

                foreach (var directory in Directory.EnumerateDirectories(tempPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var info = new DirectoryInfo(directory);
                        if (!IsInsideTemp(info.FullName, tempPath) ||
                            info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            ignored++;
                            continue;
                        }

                        var directorySize = GetDirectorySize(info);
                        Directory.Delete(info.FullName, true);
                        cleaned += Directory.Exists(info.FullName) ? 0 : directorySize;
                        foldersDeleted++;
                    }
                    catch
                    {
                        errors++;
                        // Locked temp directories are skipped.
                    }
                }

                stopwatch.Stop();
                var freedSpace = FormatFreedSpace(cleaned);
                var message = $"Nettoyage TEMP terminé : {filesDeleted} fichier(s) supprimé(s), {foldersDeleted} dossier(s) supprimé(s), espace libéré : {freedSpace}, {ignored} élément(s) ignoré(s), {errors} erreur(s) ignorée(s).";
                RecordCleaningAction("Succès", message, stopwatch.ElapsedMilliseconds);
                _logService.Log("[Action]", $"Nettoyage TEMP terminé : {filesDeleted} fichiers supprimés, {foldersDeleted} dossiers supprimés, espace libéré : {freedSpace}, {ignored} éléments ignorés, {errors} erreurs ignorées");
                return SupportActionResult.Ok(message, cleaned.ToString());
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordCleaningAction("Échec", "Le nettoyage n'a pas pu être terminé.", stopwatch.ElapsedMilliseconds);
                _logService.Log("[Action]", "Nettoyage TEMP utilisateur erreur", ex.ToString());
                return SupportActionResult.Fail("Le nettoyage n'a pas pu être terminé.", ex.Message);
            }
        });
    }

    private void RecordCleaningAction(string status, string message, long durationMs)
    {
        _historyService.Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = "Nettoyage simple",
            Category = "Nettoyage",
            Status = status,
            Message = message,
            DurationMs = durationMs
        });
    }

    private static bool IsInsideTemp(string path, string tempPath)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatFreedSpace(long bytes)
    {
        return bytes switch
        {
            0 => "0 octet",
            1 => "1 octet",
            < 1024 => $"{bytes} octets",
            _ => FileSizeHelper.Format(bytes)
        };
    }

    private static long GetDirectorySize(DirectoryInfo directory)
    {
        try
        {
            long total = 0;
            foreach (var file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (!file.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        total += file.Length;
                    }
                }
                catch
                {
                    // Size estimation is best effort.
                }
            }

            foreach (var child in directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (!child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        total += GetDirectorySize(child);
                    }
                }
                catch
                {
                    // Size estimation is best effort.
                }
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }
}
