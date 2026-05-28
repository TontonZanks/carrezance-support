namespace Carrezance.Support.App.Models;

public sealed class PrinterInfo
{
    public string Name { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string Status { get; init; } = "Disponible";
}
