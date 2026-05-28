namespace Carrezance.Support.App.Models;

public sealed class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string UserName { get; init; } = Environment.UserName;
    public string Action { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public string? Error { get; init; }

    public string DisplayText => Error is { Length: > 0 }
        ? $"[{Timestamp:dd/MM/yyyy HH:mm:ss}] [{UserName}] [{Action}] [{Result}] [{Error}]"
        : $"[{Timestamp:dd/MM/yyyy HH:mm:ss}] [{UserName}] [{Action}] [{Result}]";
}
