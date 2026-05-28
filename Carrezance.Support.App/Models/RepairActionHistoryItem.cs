namespace Carrezance.Support.App.Models;

public sealed class RepairActionHistoryItem
{
    public DateTime DateTime { get; init; } = DateTime.Now;
    public string ActionName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = "Non exécuté";
    public string Message { get; init; } = string.Empty;
    public bool RequiresAdmin { get; init; }
    public bool ExecutedAsAdmin { get; init; }
    public long DurationMs { get; init; }

    public string DisplayText => $"[{DateTime:dd/MM/yyyy HH:mm:ss}] [{Category}] [{Status}] {ActionName} - {Message}";
}
