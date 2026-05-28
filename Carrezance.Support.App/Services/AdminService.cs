using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Carrezance.Support.App.Models;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;

namespace Carrezance.Support.App.Services;

public sealed class AdminService
{
    private readonly LogService _logService;

    public AdminService(LogService logService)
    {
        _logService = logService;
    }

    public bool IsRunningAsAdministrator()
    {
        return IsAdministrator();
    }

    public bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public string ExecutionMode => IsRunningAsAdministrator() ? "Administrateur" : "Utilisateur standard";

    public void LogAdminRequired(string actionName)
    {
        _logService.Log("[Admin]", $"{actionName} demandee sans droits administrateur");
    }

    public void LogAdminRestartProposed(string actionName)
    {
        _logService.Log("[Admin]", $"Relance administrateur proposee pour {actionName}");
    }

    public void LogAdminRestartCanceled(string actionName)
    {
        _logService.Log("[Admin]", $"Relance administrateur annulee par l'utilisateur pour {actionName}");
    }

    public void LogAdminDialogFailed(string actionName, Exception exception)
    {
        _logService.Log("[Admin]", $"Impossible d'ouvrir la demande d'elevation administrateur pour {actionName}", exception.ToString());
    }

    public SupportActionResult RestartAsAdministrator()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                _logService.Log("[Admin]", "Relance administrateur impossible", "Chemin de l'executable introuvable");
                return SupportActionResult.Fail("La relance en administrateur n'a pas pu etre preparee.");
            }

            _logService.Log("[Admin]", $"Relance administrateur demandee : {executablePath}");
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = AppContext.BaseDirectory
            });

            _logService.Log("[Admin]", "Relance administrateur lancee avec succes");
            Application.Current?.Dispatcher.BeginInvoke(() => Application.Current.Shutdown());
            return SupportActionResult.Ok("Relance en administrateur demandee.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _logService.Log("[Admin]", "Relance administrateur refusee ou annulee", ex.Message);
            return SupportActionResult.Fail("Relance administrateur refusee ou annulee.", ex.Message);
        }
        catch (Exception ex)
        {
            _logService.Log("[Admin]", "Erreur relance administrateur", ex.ToString());
            return SupportActionResult.Fail("La relance en administrateur n'a pas pu etre effectuee.", ex.Message);
        }
    }
}
