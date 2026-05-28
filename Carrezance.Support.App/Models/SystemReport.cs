using System.Collections.ObjectModel;

namespace Carrezance.Support.App.Models;

public sealed class SystemReport
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string ComputerName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string WindowsVersion { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string DomainOrWorkgroup { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = "Utilisateur standard";
    public NetworkInfo Network { get; set; } = new();
    public DiskInfo DiskC { get; set; } = new();
    public long TotalMemoryBytes { get; set; }
    public long UsedMemoryBytes { get; set; }
    public DateTime LastBootTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public string DiagnosticType { get; set; } = "Diagnostic rapide";
    public string AntivirusStatus { get; set; } = "Non disponible";
    public string BitLockerStatus { get; set; } = "Non disponible";
    public string OneDriveStatus { get; set; } = "Non disponible";
    public string ActiveDirectoryStatus { get; set; } = "Non disponible";
    public string AzureAdJoinStatus { get; set; } = "Non disponible";
    public ObservableCollection<DiagnosticResult> NetworkTests { get; set; } = new();
    public ObservableCollection<SoftwareDetectionResult> ImportantSoftware { get; set; } = new();
    public ObservableCollection<LogEntry> RecentActions { get; set; } = new();
}
