using System.Diagnostics;
using System.Globalization;
using System.Text;
using Carrezance.Support.App.Models;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;

namespace Carrezance.Support.App.Services;

public sealed class ProcessService
{
    private readonly LogService _logService;

    public ProcessService(LogService logService)
    {
        _logService = logService;
    }

    public async Task<SupportActionResult> RunAsync(string fileName, string arguments, string actionName, bool useShellExecute = false)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                CreateNoWindow = !useShellExecute,
                RedirectStandardOutput = !useShellExecute,
                RedirectStandardError = !useShellExecute
            };
            if (!useShellExecute)
            {
                startInfo.StandardOutputEncoding = GetConsoleOutputEncoding();
                startInfo.StandardErrorEncoding = GetConsoleOutputEncoding();
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logService.Log(actionName, "Erreur", "Le processus n'a pas pu être démarré.");
                return SupportActionResult.Fail("L'action n'a pas pu être lancée.");
            }

            if (useShellExecute)
            {
                _logService.Log(actionName, "OK");
                return SupportActionResult.Ok("Action lancée.");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var success = process.ExitCode == 0;
            _logService.Log(actionName, success ? "OK" : "Erreur", success ? null : error);
            return success
                ? SupportActionResult.Ok("Action terminée.", output)
                : SupportActionResult.Fail("L'action a rencontré une erreur.", error);
        }
        catch (Exception ex)
        {
            _logService.Log(actionName, "Erreur", ex.Message);
            return SupportActionResult.Fail("Une erreur est survenue.", ex.Message);
        }
    }

    private static Encoding GetConsoleOutputEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        }
        catch
        {
            return Console.OutputEncoding;
        }
    }

    public SupportActionResult Open(string target, string actionName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            _logService.Log(actionName, "OK");
            return SupportActionResult.Ok("Ouverture effectuée.");
        }
        catch (Exception ex)
        {
            _logService.Log(actionName, "Erreur", ex.Message);
            return SupportActionResult.Fail("Impossible d'ouvrir l'élément demandé.", ex.Message);
        }
    }
}
