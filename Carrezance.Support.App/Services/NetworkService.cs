using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Carrezance.Support.App.Models;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;

namespace Carrezance.Support.App.Services;

public sealed class NetworkService
{
    private readonly LogService _logService;
    private readonly ProcessService _processService;
    private readonly RepairActionHistoryService _historyService;

    public NetworkService(LogService logService, ProcessService processService, RepairActionHistoryService historyService)
    {
        _logService = logService;
        _processService = processService;
        _historyService = historyService;
    }

    public async Task<ObservableCollection<DiagnosticResult>> TestInternetAsync(bool logResult = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ObservableCollection<DiagnosticResult>
        {
            await PingHostAsync("1.1.1.1", "Ping Cloudflare"),
            await PingHostAsync("8.8.8.8", "Ping Google DNS"),
            await TestDnsAsync("microsoft.com")
        };

        stopwatch.Stop();
        if (logResult)
        {
            _logService.Log("[Diagnostic]", $"Test connexion Internet : {(results.Any(r => r.Status == "Erreur") ? "Attention" : "OK")}");
        }

        return results;
    }

    public Task<SupportActionResult> FlushDnsAsync()
    {
        return RunFlushDnsAsync();
    }

    private async Task<SupportActionResult> RunFlushDnsAsync()
    {
        const string actionName = "Réparer l'accès aux sites web";
        var stopwatch = Stopwatch.StartNew();
        _logService.Log("[Action]", $"{actionName} démarrée");

        try
        {
            var result = await _processService.RunAsync("ipconfig.exe", "/flushdns", actionName);
            stopwatch.Stop();
            var status = result.Success ? "Succès" : "Échec";
            var message = result.Success
                ? "Cache DNS vidé. L'accès aux sites web a été réparé."
                : result.Message;

            _historyService.Add(new RepairActionHistoryItem
            {
                DateTime = DateTime.Now,
                ActionName = actionName,
                Category = "Internet & Réseau",
                Status = status,
                Message = message,
                DurationMs = stopwatch.ElapsedMilliseconds
            });

            _logService.Log("[Action]", $"{actionName} {status.ToLowerInvariant()} en {stopwatch.ElapsedMilliseconds} ms", result.Details);
            return result.Success ? SupportActionResult.Ok(message, result.Details) : result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _historyService.Add(new RepairActionHistoryItem
            {
                DateTime = DateTime.Now,
                ActionName = actionName,
                Category = "Internet & Réseau",
                Status = "Échec",
                Message = "La réparation DNS n'a pas pu être exécutée.",
                DurationMs = stopwatch.ElapsedMilliseconds
            });
            _logService.Log("[Action]", $"{actionName} échec en {stopwatch.ElapsedMilliseconds} ms", ex.ToString());
            return SupportActionResult.Fail("La réparation DNS n'a pas pu être exécutée.", ex.Message);
        }
    }

    private static async Task<DiagnosticResult> PingHostAsync(string host, string name)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 1000);
            return reply.Status == IPStatus.Success
                ? new DiagnosticResult { Name = name, Status = "OK", Message = $"{reply.RoundtripTime} ms" }
                : new DiagnosticResult { Name = name, Status = "Erreur", Message = reply.Status.ToString() };
        }
        catch (Exception ex)
        {
            return new DiagnosticResult { Name = name, Status = "Erreur", Message = ex.Message };
        }
    }

    private static async Task<DiagnosticResult> TestDnsAsync(string host)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host).WaitAsync(TimeSpan.FromSeconds(1));
            return addresses.Length > 0
                ? new DiagnosticResult { Name = "Résolution DNS", Status = "OK", Message = $"{host} résolu" }
                : new DiagnosticResult { Name = "Résolution DNS", Status = "Erreur", Message = "Aucune adresse trouvée" };
        }
        catch (Exception ex)
        {
            var message = ex is TimeoutException ? "Timeout après 1000 ms" : ex.Message;
            return new DiagnosticResult { Name = "Résolution DNS", Status = "Erreur", Message = message };
        }
    }
}
