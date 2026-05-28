using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using SupportActionResult = Carrezance.Support.App.Models.ActionResult;

namespace Carrezance.Support.App.Services;

public sealed class ReportService
{
    private readonly LogService _logService;
    private readonly RepairActionHistoryService _historyService;
    private string? _lastReportPath;
    private string? _lastHtmlReportPath;

    public ReportService(LogService logService, RepairActionHistoryService historyService)
    {
        _logService = logService;
        _historyService = historyService;
    }

    public string BuildReportText(SystemReport report, ObservableCollection<LogEntry>? logs = null)
    {
        EnsureReportId(report);
        var builder = new StringBuilder();
        builder.AppendLine(AppInfo.DisplayName);
        builder.AppendLine("Rapport support");
        builder.AppendLine($"ID de rapport : {report.ReportId}");
        builder.AppendLine($"Version Carrezance Support : v{AppInfo.Version}");
        builder.AppendLine($"Date : {report.GeneratedAt:dd/MM/yyyy HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(_lastHtmlReportPath))
        {
            builder.AppendLine($"Dernier rapport HTML : {_lastHtmlReportPath}");
        }
        builder.AppendLine();
        builder.AppendLine("Informations système");
        builder.AppendLine($"Nom du poste : {ValueOrFallback(report.ComputerName)}");
        builder.AppendLine($"Utilisateur : {ValueOrFallback(report.UserName)}");
        builder.AppendLine($"Windows : {ValueOrFallback(report.WindowsVersion)}");
        builder.AppendLine($"Architecture : {ValueOrFallback(report.Architecture)}");
        builder.AppendLine($"Domaine / Workgroup : {ValueOrFallback(report.DomainOrWorkgroup)}");
        builder.AppendLine($"Mode d'exécution : {ValueOrFallback(report.ExecutionMode)}");
        builder.AppendLine($"Dernier démarrage : {FormatDate(report.LastBootTime)}");
        builder.AppendLine($"Uptime : {DateTimeHelper.FormatDuration(report.Uptime)}");
        builder.AppendLine();
        builder.AppendLine("Réseau");
        builder.AppendLine($"Carte : {ValueOrFallback(report.Network.AdapterName)}");
        builder.AppendLine($"IP locale : {ValueOrFallback(report.Network.LocalIpAddress)}");
        builder.AppendLine($"Passerelle : {ValueOrFallback(report.Network.Gateway)}");
        builder.AppendLine($"DNS : {FormatList(report.Network.DnsServers)}");
        builder.AppendLine($"MAC : {ValueOrFallback(report.Network.MacAddress)}");
        builder.AppendLine();
        builder.AppendLine("Disque");
        builder.AppendLine($"Disque {ValueOrFallback(report.DiskC.DriveName)} total : {FileSizeHelper.Format(report.DiskC.TotalBytes)}");
        builder.AppendLine($"Utilisé : {FileSizeHelper.Format(report.DiskC.UsedBytes)}");
        builder.AppendLine($"Libre : {FileSizeHelper.Format(report.DiskC.FreeBytes)} ({report.DiskC.FreePercent:0.#}%)");
        builder.AppendLine($"Statut : {ValueOrFallback(report.DiskC.Status)}");
        builder.AppendLine();
        builder.AppendLine("Mémoire");
        builder.AppendLine($"RAM totale : {FileSizeHelper.Format(report.TotalMemoryBytes)}");
        builder.AppendLine($"RAM utilisée : {FileSizeHelper.Format(report.UsedMemoryBytes)}");
        builder.AppendLine();
        builder.AppendLine("Sécurité et identité");
        builder.AppendLine($"Antivirus : {ValueOrFallback(report.AntivirusStatus)}");
        builder.AppendLine($"BitLocker C: : {ValueOrFallback(report.BitLockerStatus)}");
        builder.AppendLine($"OneDrive : {ValueOrFallback(report.OneDriveStatus)}");
        builder.AppendLine($"Domaine AD / Workgroup : {ValueOrFallback(report.ActiveDirectoryStatus)}");
        builder.AppendLine($"Azure AD Join : {ValueOrFallback(report.AzureAdJoinStatus)}");

        if (report.NetworkTests.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Tests réseau");
            foreach (var test in report.NetworkTests)
            {
                builder.AppendLine($"- {test.Name} : {test.Status} - {test.Message}");
            }
        }

        if (report.ImportantSoftware.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Logiciels importants");
            foreach (var software in report.ImportantSoftware)
            {
                builder.AppendLine($"- {software.Name} : {software.DisplayStatus}");
                if (software.Status == "Detected")
                {
                    builder.AppendLine($"  Source : {ValueOrFallback(software.DetectionSource)}");
                    builder.AppendLine($"  Chemin/ID : {ValueOrFallback(software.DetectionPath)}");
                    builder.AppendLine($"  Confiance : {ValueOrFallback(software.Confidence)}");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("Historique des actions");
        if (_historyService.Items.Count == 0)
        {
            builder.AppendLine("Aucune action de réparation exécutée.");
        }
        else
        {
            foreach (var action in _historyService.Items.Take(30))
            {
                builder.AppendLine($"- {action.DateTime:dd/MM/yyyy HH:mm:ss} | {action.Category} | {action.ActionName} | {action.Status} | {action.Message} | {action.DurationMs} ms");
            }
        }

        var recentLogs = logs ?? report.RecentActions;
        if (recentLogs.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Actions récentes");
            foreach (var log in recentLogs.Take(25))
            {
                builder.AppendLine(log.DisplayText);
            }
        }

        return builder.ToString();
    }

    public void LogAction(string action, string result = "OK", string? error = null)
    {
        _logService.Log(action, result, error);
    }

    public SupportActionResult Export(SystemReport report, ObservableCollection<LogEntry>? logs = null)
    {
        try
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var targetFolder = Directory.Exists(documents) ? documents : AppContext.BaseDirectory;
            report.GeneratedAt = DateTime.Now;
            AssignNewReportId(report);
            var fileName = $"CarrezanceSupport_Rapport_{SafeFileNamePart(report.ComputerName)}_{SafeFileNamePart(report.UserName)}_{report.GeneratedAt:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(targetFolder, fileName);
            File.WriteAllText(path, BuildReportText(report, logs), Encoding.UTF8);
            _lastReportPath = path;
            _logService.Log("[Rapport]", $"Export TXT OK - ID : {report.ReportId}");
            return SupportActionResult.Ok("Le rapport support a été exporté.", path);
        }
        catch (Exception ex)
        {
            _logService.Log("[Rapport]", "Export TXT erreur", ex.Message);
            return SupportActionResult.Fail("Le rapport n'a pas pu être exporté. Vérifiez que le dossier Documents est accessible.", ex.Message);
        }
    }

    public SupportActionResult ExportHtml(SystemReport report, ObservableCollection<LogEntry>? logs = null)
    {
        try
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var targetFolder = Directory.Exists(documents) ? documents : AppContext.BaseDirectory;
            report.GeneratedAt = DateTime.Now;
            AssignNewReportId(report);
            var fileName = $"CarrezanceSupport_Rapport_{SafeFileNamePart(report.ComputerName)}_{SafeFileNamePart(report.UserName)}_{report.GeneratedAt:yyyyMMdd_HHmmss}.html";
            var path = Path.Combine(targetFolder, fileName);
            File.WriteAllText(path, BuildReportHtml(report, logs), Encoding.UTF8);
            _lastHtmlReportPath = path;
            _logService.Log("[Rapport]", $"Export HTML OK - ID : {report.ReportId} - {path}");
            return SupportActionResult.Ok("Rapport HTML généré avec succès.", path);
        }
        catch (Exception ex)
        {
            _logService.Log("[Rapport]", "Export HTML erreur", ex.Message);
            return SupportActionResult.Fail("Le rapport HTML n'a pas pu être exporté. Vérifiez que le dossier Documents est accessible.", ex.Message);
        }
    }

    public string BuildReportHtml(SystemReport report, ObservableCollection<LogEntry>? logs = null)
    {
        EnsureReportId(report);
        var recentLogs = logs ?? report.RecentActions;
        var summary = BuildReportSummary(report);
        _logService.Log("[Rapport]", $"Synthèse générée - État global : {summary.GlobalStatus} - OK : {summary.OkCount} - Attention : {summary.WarningCount} - Critique : {summary.CriticalCount}");

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"fr\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("<title>Rapport de diagnostic - Carrezance Support</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("""
body{margin:0;background:#f4f7fb;color:#1e293b;font-family:Segoe UI,Arial,sans-serif}
.top{background:#0b1f3a;color:#fff;padding:28px 34px}
.top h1{margin:0;font-size:28px;font-weight:650}
.top p{margin:6px 0 0;color:#b7c7da}
.top-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:10px;margin-top:18px}
.top-item{background:rgba(255,255,255,.08);border:1px solid rgba(255,255,255,.16);border-radius:8px;padding:10px}
.top-item span{display:block;color:#b7c7da;font-size:12px}
.top-item strong{display:block;color:#fff;font-size:14px;margin-top:3px;word-break:break-word}
.wrap{max-width:1120px;margin:0 auto;padding:28px}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:16px}
.card{background:#fff;border:1px solid #dde5ef;border-radius:8px;padding:18px;margin-bottom:16px}
h2{font-size:18px;margin:0 0 14px;color:#0b1f3a}
table{width:100%;border-collapse:collapse}
td,th{padding:8px 0;border-bottom:1px solid #edf2f7;vertical-align:top;text-align:left}
th{color:#64748b;font-weight:600;width:42%}
.badge{display:inline-block;border-radius:999px;padding:4px 10px;font-size:12px;font-weight:700}
.badge-lg{font-size:14px;padding:7px 14px}
.report-meta{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:16px}
.summary-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:12px;margin-top:12px}
.summary-box{border:1px solid #edf2f7;border-radius:8px;padding:12px;background:#f8fafc}
.summary-box strong{display:block;font-size:22px;color:#0b1f3a}
.summary-list{margin:12px 0 0;padding-left:18px}
.table-scroll{width:100%;overflow-x:auto}
.software-table{width:100%;min-width:860px;table-layout:fixed;border-collapse:collapse}
.software-table td,.software-table th{padding:9px 8px;white-space:normal;vertical-align:top;word-break:break-word;overflow-wrap:anywhere}
.software-table th{width:auto}
.software-table th:nth-child(1),.software-table td:nth-child(1){width:18%}
.software-table th:nth-child(2),.software-table td:nth-child(2){width:120px}
.software-table th:nth-child(3),.software-table td:nth-child(3){width:180px}
.software-table th:nth-child(5),.software-table td:nth-child(5){width:95px;text-align:center}
.repair-table th:nth-child(1),.repair-table td:nth-child(1){width:140px}
.repair-table th:nth-child(2),.repair-table td:nth-child(2){width:140px}
.repair-table th:nth-child(4),.repair-table td:nth-child(4){width:90px;text-align:center}
.repair-table th:nth-child(6),.repair-table td:nth-child(6){width:80px;text-align:center}
.ok{background:#dcfce7;color:#166534}
.warn{background:#fef3c7;color:#92400e}
.crit{background:#fee2e2;color:#991b1b}
.neutral{background:#e7eef7;color:#0b1f3a}
.muted{color:#64748b}
.footer{color:#64748b;font-size:12px;margin-top:20px}
@media print{
body{background:#fff;color:#111827}
.top{background:#fff;color:#0b1f3a;border-bottom:2px solid #0b1f3a;padding:18px 0}
.top p,.top-item span,.muted,.footer{color:#475569}
.top-item{background:#fff;border:1px solid #cbd5e1}
.top-item strong{color:#0b1f3a}
.wrap{max-width:none;padding:18px 0}
.card{break-inside:avoid;page-break-inside:avoid;border-color:#cbd5e1}
.badge{border:1px solid currentColor;background:#fff}
.table-scroll{overflow:visible}
.software-table{min-width:0;font-size:11px}
.software-table td,.software-table th{padding:6px 5px}
.software-table th:nth-child(2),.software-table td:nth-child(2){width:82px}
.software-table th:nth-child(3),.software-table td:nth-child(3){width:130px}
.software-table th:nth-child(5),.software-table td:nth-child(5),.repair-table th:nth-child(6),.repair-table td:nth-child(6){width:70px}
}
""");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<header class=\"top\">");
        builder.AppendLine("<h1>Rapport de diagnostic - Carrezance Support</h1>");
        builder.AppendLine($"<p>{Html(AppInfo.Description)}</p>");
        builder.AppendLine("<div class=\"top-grid\">");
        AppendHeaderItem(builder, "Version", $"v{AppInfo.Version}");
        AppendHeaderItem(builder, "ID de rapport", report.ReportId);
        AppendHeaderItem(builder, "Type de diagnostic", report.DiagnosticType);
        AppendHeaderItem(builder, "Date de génération", report.GeneratedAt.ToString("dd/MM/yyyy HH:mm:ss"));
        AppendHeaderItem(builder, "Nom du poste", report.ComputerName);
        AppendHeaderItem(builder, "Utilisateur", report.UserName);
        builder.AppendLine("</div>");
        builder.AppendLine("</header>");
        builder.AppendLine("<main class=\"wrap\">");
        AppendSummarySection(builder, summary);
        builder.AppendLine("<div class=\"report-meta\">");
        builder.AppendLine("<span class=\"badge neutral\">Rapport généré localement depuis Carrezance Support.</span>");
        builder.AppendLine($"<span class=\"badge neutral\">{Html(report.DiagnosticType)}</span>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"grid\">");
        AppendCard(builder, "Informations système", new[]
        {
            ("Date du rapport", report.GeneratedAt.ToString("dd/MM/yyyy HH:mm:ss")),
            ("Nom du poste", report.ComputerName),
            ("Utilisateur", report.UserName),
            ("Windows", report.WindowsVersion),
            ("Architecture", report.Architecture),
            ("Domaine / Workgroup", report.DomainOrWorkgroup),
            ("Mode d'exécution", report.ExecutionMode),
            ("Dernier démarrage", FormatDate(report.LastBootTime)),
            ("Uptime", DateTimeHelper.FormatDuration(report.Uptime))
        });
        AppendCard(builder, "Réseau", new[]
        {
            ("Carte", report.Network.AdapterName),
            ("IP locale", report.Network.LocalIpAddress),
            ("Passerelle", report.Network.Gateway),
            ("DNS", FormatList(report.Network.DnsServers)),
            ("MAC", report.Network.MacAddress)
        });
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"grid\">");
        AppendCard(builder, "Disque et mémoire", new[]
        {
            ($"Disque {report.DiskC.DriveName} total", FileSizeHelper.Format(report.DiskC.TotalBytes)),
            ("Utilisé", FileSizeHelper.Format(report.DiskC.UsedBytes)),
            ("Libre", $"{FileSizeHelper.Format(report.DiskC.FreeBytes)} ({report.DiskC.FreePercent:0.#}%)"),
            ("Statut disque", Badge(GetDiskStatus(report.DiskC.FreeBytes))),
            ("RAM totale", FileSizeHelper.Format(report.TotalMemoryBytes)),
            ("RAM utilisée", FileSizeHelper.Format(report.UsedMemoryBytes))
        }, valuesContainHtml: true);
        AppendCard(builder, "Sécurité et identité", new[]
        {
            ("Antivirus", report.AntivirusStatus),
            ("BitLocker C:", report.BitLockerStatus),
            ("OneDrive", report.OneDriveStatus),
            ("Domaine AD", report.ActiveDirectoryStatus),
            ("Azure AD Join", report.AzureAdJoinStatus)
        });
        builder.AppendLine("</div>");

        AppendDiagnosticSection(builder, "Tests Internet", report.NetworkTests);
        AppendRepairHistorySection(builder, _historyService.Items);
        AppendSoftwareSection(builder, report.ImportantSoftware);

        if (recentLogs.Count > 0)
        {
            builder.AppendLine("<section class=\"card\"><h2>Actions récentes</h2><table>");
            foreach (var log in recentLogs.Take(25))
            {
                builder.AppendLine($"<tr><td>{Html(log.DisplayText)}</td></tr>");
            }
            builder.AppendLine("</table></section>");
        }

        builder.AppendLine("<section class=\"card\"><h2>Conclusion technicien</h2>");
        builder.AppendLine("<p>À compléter par le technicien après analyse du rapport.</p>");
        builder.AppendLine("</section>");

        builder.AppendLine($"<p class=\"footer\">Rapport généré localement par {Html(AppInfo.DisplayName)}. Aucune donnée sensible ni mot de passe n'est stocké par l'application.</p>");
        builder.AppendLine("</main></body></html>");
        return builder.ToString();
    }

    public string? LastReportPath => _lastReportPath;
    public string? LastHtmlReportPath => _lastHtmlReportPath;

    public string? LastReportFolder => string.IsNullOrWhiteSpace(_lastReportPath)
        ? null
        : Path.GetDirectoryName(_lastReportPath);

    public string? LastHtmlReportFolder => string.IsNullOrWhiteSpace(_lastHtmlReportPath)
        ? null
        : Path.GetDirectoryName(_lastHtmlReportPath);

    public SupportActionResult OpenLastHtmlReport(ProcessService processService)
    {
        if (string.IsNullOrWhiteSpace(_lastHtmlReportPath) || !File.Exists(_lastHtmlReportPath))
        {
            return SupportActionResult.Fail("Aucun rapport HTML n'a encore été généré. Lancez un diagnostic puis exportez le rapport.");
        }

        var result = processService.Open(_lastHtmlReportPath, "Ouvrir dernier rapport HTML");
        if (!result.Success)
        {
            _logService.Log("[Rapport]", "Ouvrir dernier rapport HTML erreur", result.Details);
            return SupportActionResult.Fail("Le rapport HTML est inaccessible ou ne peut pas être ouvert.", result.Details);
        }

        return SupportActionResult.Ok("Rapport HTML ouvert.");
    }

    public SupportActionResult CopyLastHtmlReportPath()
    {
        if (string.IsNullOrWhiteSpace(_lastHtmlReportPath))
        {
            return SupportActionResult.Fail("Aucun rapport HTML n'a encore été généré. Lancez un diagnostic puis exportez le rapport.");
        }

        try
        {
            Clipboard.SetText(_lastHtmlReportPath);
            _logService.Log("[Rapport]", "Copier chemin dernier rapport HTML OK");
            return SupportActionResult.Ok("Le chemin du dernier rapport HTML a été copié.");
        }
        catch (Exception ex)
        {
            _logService.Log("[Rapport]", "Copier chemin dernier rapport HTML erreur", ex.Message);
            return SupportActionResult.Fail("Le chemin du rapport HTML n'a pas pu être copié.", ex.Message);
        }
    }

    private static string ValueOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Non disponible" : value;
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string FormatDate(DateTime value)
    {
        return value == default ? "Non disponible" : value.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private static string FormatList(IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return items.Length == 0 ? "Non disponible" : string.Join(", ", items);
    }

    private static void AppendCard(StringBuilder builder, string title, IEnumerable<(string Label, string Value)> rows, bool valuesContainHtml = false)
    {
        builder.AppendLine($"<section class=\"card\"><h2>{Html(title)}</h2><table>");
        foreach (var (label, value) in rows)
        {
            var displayedValue = valuesContainHtml ? ValueOrFallback(value) : Html(ValueOrFallback(value));
            builder.AppendLine($"<tr><th>{Html(label)}</th><td>{displayedValue}</td></tr>");
        }
        builder.AppendLine("</table></section>");
    }

    private static void AppendHeaderItem(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"<div class=\"top-item\"><span>{Html(label)}</span><strong>{Html(ValueOrFallback(value))}</strong></div>");
    }

    private static void AppendSummarySection(StringBuilder builder, ReportSummary summary)
    {
        builder.AppendLine("<section class=\"card\"><h2>Synthèse</h2>");
        builder.AppendLine($"<p><span class=\"muted\">État global :</span> {Badge(summary.GlobalStatus, large: true)}</p>");
        builder.AppendLine($"<p><span class=\"muted\">Type de diagnostic :</span> {Html(summary.DiagnosticType)}</p>");
        if (!string.IsNullOrWhiteSpace(summary.Note))
        {
            builder.AppendLine($"<p class=\"muted\">{Html(summary.Note)}</p>");
        }

        builder.AppendLine("<div class=\"summary-grid\">");
        builder.AppendLine($"<div class=\"summary-box\"><span class=\"muted\">Points OK</span><strong>{summary.OkCount}</strong></div>");
        builder.AppendLine($"<div class=\"summary-box\"><span class=\"muted\">Points d'attention</span><strong>{summary.WarningCount}</strong></div>");
        builder.AppendLine($"<div class=\"summary-box\"><span class=\"muted\">Points critiques</span><strong>{summary.CriticalCount}</strong></div>");
        builder.AppendLine("</div>");

        if (summary.Points.Count > 0)
        {
            builder.AppendLine("<ul class=\"summary-list\">");
            foreach (var point in summary.Points.Take(8))
            {
                builder.AppendLine($"<li>{Html(point)}</li>");
            }
            builder.AppendLine("</ul>");
        }
        else
        {
            builder.AppendLine("<p>Aucun point d'attention majeur détecté.</p>");
        }

        builder.AppendLine("</section>");
    }

    private static void AppendDiagnosticSection(StringBuilder builder, string title, ObservableCollection<DiagnosticResult> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        builder.AppendLine($"<section class=\"card\"><h2>{Html(title)}</h2><table>");
        foreach (var result in results)
        {
            builder.AppendLine($"<tr><th>{Html(result.Name)}</th><td>{Badge(result.Status)} <span class=\"muted\">{Html(result.Message)}</span></td></tr>");
        }
        builder.AppendLine("</table></section>");
    }

    private static void AppendRepairHistorySection(StringBuilder builder, ObservableCollection<RepairActionHistoryItem> actions)
    {
        builder.AppendLine("<section class=\"card\"><h2>Historique des actions</h2>");
        if (actions.Count == 0)
        {
            builder.AppendLine("<p>Aucune action de réparation exécutée.</p></section>");
            return;
        }

        builder.AppendLine("<div class=\"table-scroll\"><table class=\"software-table repair-table\">");
        builder.AppendLine("<thead><tr><th>Date/heure</th><th>Catégorie</th><th>Action</th><th>Statut</th><th>Message</th><th>Durée</th></tr></thead><tbody>");
        foreach (var action in actions.Take(30))
        {
            builder.AppendLine($"<tr><td>{action.DateTime:dd/MM/yyyy HH:mm:ss}</td><td>{Html(action.Category)}</td><td>{Html(action.ActionName)}</td><td>{NeutralBadge(action.Status)}</td><td>{Html(action.Message)}</td><td>{action.DurationMs} ms</td></tr>");
        }
        builder.AppendLine("</tbody></table></div></section>");
    }

    private static void AppendSoftwareSection(StringBuilder builder, ObservableCollection<SoftwareDetectionResult> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        builder.AppendLine("<section class=\"card\"><h2>Logiciels importants</h2><div class=\"table-scroll\"><table class=\"software-table\">");
        builder.AppendLine("<thead><tr><th>Logiciel</th><th>Statut</th><th>Source</th><th>Chemin / ID</th><th>Confiance</th></tr></thead><tbody>");
        foreach (var result in results)
        {
            var source = result.Status == "Detected" ? ValueOrDash(result.DetectionSource) : "-";
            var path = result.Status == "Detected" ? ValueOrDash(result.DetectionPath) : "-";
            var confidence = result.Status == "Detected" ? ValueOrDash(result.Confidence) : "-";
            builder.AppendLine($"<tr><td>{Html(result.Name)}</td><td>{NeutralBadge(result.DisplayStatus)}</td><td>{Html(source)}</td><td>{Html(path)}</td><td>{Html(confidence)}</td></tr>");
        }
        builder.AppendLine("</tbody></table></div></section>");
    }

    private static string Badge(string value, bool large = false)
    {
        var label = ValueOrFallback(value);
        var css = label.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                  label.Equals("Activé", StringComparison.OrdinalIgnoreCase) ||
                  label.Equals("Détecté", StringComparison.OrdinalIgnoreCase)
            ? "ok"
            : label.Equals("Critique", StringComparison.OrdinalIgnoreCase) ||
              label.Equals("Erreur", StringComparison.OrdinalIgnoreCase)
                ? "crit"
                : "warn";

        var sizeClass = large ? " badge-lg" : string.Empty;
        return $"<span class=\"badge {css}{sizeClass}\">{Html(label)}</span>";
    }

    private static string NeutralBadge(string value)
    {
        var label = ValueOrFallback(value);
        var css = label.Equals("Détecté", StringComparison.OrdinalIgnoreCase) ? "ok" : "neutral";
        return $"<span class=\"badge {css}\">{Html(label)}</span>";
    }

    private static string GetDiskStatus(long freeBytes)
    {
        const long tenGigabytes = 10L * 1024 * 1024 * 1024;
        const long twentyGigabytes = 20L * 1024 * 1024 * 1024;

        return freeBytes switch
        {
            > twentyGigabytes => "OK",
            >= tenGigabytes => "Attention",
            _ => "Critique"
        };
    }

    private static ReportSummary BuildReportSummary(SystemReport report)
    {
        var ok = 1; // Rapport généré correctement.
        var warnings = new List<string>();
        var criticals = new List<string>();
        var isFullDiagnostic = report.DiagnosticType.Contains("complet", StringComparison.OrdinalIgnoreCase);

        const long tenGigabytes = 10L * 1024 * 1024 * 1024;
        const long twentyGigabytes = 20L * 1024 * 1024 * 1024;

        if (report.DiskC.FreeBytes < tenGigabytes)
        {
            criticals.Add("Disque C: espace libre inférieur à 10 Go.");
        }
        else if (report.DiskC.FreeBytes <= twentyGigabytes)
        {
            warnings.Add("Disque C: espace libre entre 10 et 20 Go.");
        }
        else
        {
            ok++;
        }

        if (IsUnavailable(report.Network.LocalIpAddress))
        {
            criticals.Add("Aucun réseau détecté.");
        }
        else
        {
            ok++;
        }

        if (report.Network.DnsServers.Count == 0)
        {
            criticals.Add("Aucun DNS détecté.");
        }
        else
        {
            ok++;
        }

        if (report.NetworkTests.Count > 0)
        {
            if (report.NetworkTests.Any(test => test.Status.Equals("OK", StringComparison.OrdinalIgnoreCase)))
            {
                ok++;
            }
            else
            {
                criticals.Add("Test Internet échoué.");
            }
        }

        if (isFullDiagnostic)
        {
            if (IsUnavailable(report.AntivirusStatus) || ContainsAny(report.AntivirusStatus, "non détecté"))
            {
                criticals.Add("Antivirus non détecté ou non disponible.");
            }
            else
            {
                ok++;
            }

            if (report.BitLockerStatus.Equals("Non analysé", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("BitLocker non analysé.");
            }
            else if (report.BitLockerStatus.Equals("Non disponible", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("État BitLocker non disponible.");
            }
            else if (ContainsAny(report.BitLockerStatus, "désactivé", "desactive"))
            {
                warnings.Add("BitLocker désactivé.");
            }

            if (ContainsAny(report.DomainOrWorkgroup, "Workgroup") || ContainsAny(report.ActiveDirectoryStatus, "Domaine AD : non"))
            {
                warnings.Add("Poste en Workgroup.");
            }
            else
            {
                ok++;
            }

            if (ContainsAny(report.AzureAdJoinStatus, "Azure AD Join : non"))
            {
                warnings.Add("Azure AD Join non actif.");
            }

            if (!IsSoftwareDetected(report, "Microsoft Office / Microsoft 365"))
            {
                warnings.Add("Microsoft Office / Microsoft 365 non détecté.");
            }
            else
            {
                ok++;
            }

            if (!IsSoftwareDetected(report, "AnyDesk") && !IsSoftwareDetected(report, "TeamViewer"))
            {
                warnings.Add("Aucun outil de prise en main détecté.");
            }
        }

        if (IsSoftwareDetected(report, "Google Chrome") ||
            IsSoftwareDetected(report, "Microsoft Edge") ||
            IsSoftwareDetected(report, "Mozilla Firefox") ||
            IsSoftwareDetected(report, "Opera"))
        {
            ok++;
        }

        var globalStatus = criticals.Count > 0 ? "Critique" : warnings.Count > 0 ? "Attention" : "OK";
        var note = isFullDiagnostic
            ? string.Empty
            : "Diagnostic rapide : certaines vérifications avancées n'ont pas été lancées.";

        return new ReportSummary(
            report.DiagnosticType,
            globalStatus,
            ok,
            warnings.Count,
            criticals.Count,
            criticals.Concat(warnings).ToList(),
            note);
    }

    private static bool IsSoftwareDetected(SystemReport report, string name)
    {
        return report.ImportantSoftware.Any(software =>
            software.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            software.Status.Equals("Detected", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUnavailable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
               value.Equals("Non disponible", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Non analysé", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string? value, params string[] expectedValues)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               expectedValues.Any(expected => value.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureReportId(SystemReport report)
    {
        if (!string.IsNullOrWhiteSpace(report.ReportId))
        {
            return;
        }

        report.ReportId = $"CRZ-{report.GeneratedAt:yyyyMMdd-HHmmss}-{SafeReportIdPart(report.ComputerName)}";
    }

    private static void AssignNewReportId(SystemReport report)
    {
        report.ReportId = $"CRZ-{report.GeneratedAt:yyyyMMdd-HHmmss}-{SafeReportIdPart(report.ComputerName)}";
    }

    private static string SafeFileNamePart(string? value)
    {
        var safe = string.IsNullOrWhiteSpace(value) ? "NonDisponible" : value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(invalid, '_');
        }

        safe = safe.Replace(' ', '_');
        return string.IsNullOrWhiteSpace(safe) ? "NonDisponible" : safe;
    }

    private static string SafeReportIdPart(string? value)
    {
        var safe = SafeFileNamePart(value).ToUpperInvariant();
        var builder = new StringBuilder();
        foreach (var character in safe)
        {
            if (char.IsLetterOrDigit(character) || character == '-')
            {
                builder.Append(character);
            }
        }

        return builder.Length == 0 ? "POSTE" : builder.ToString();
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(ValueOrFallback(value));
    }

    private sealed record ReportSummary(
        string DiagnosticType,
        string GlobalStatus,
        int OkCount,
        int WarningCount,
        int CriticalCount,
        List<string> Points,
        string Note);
}
