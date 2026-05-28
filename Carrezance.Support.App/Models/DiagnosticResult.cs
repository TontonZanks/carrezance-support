namespace Carrezance.Support.App.Models;

public sealed class DiagnosticResult
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "OK";
    public string Message { get; init; } = string.Empty;
}
