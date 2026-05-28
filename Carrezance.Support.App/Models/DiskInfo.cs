namespace Carrezance.Support.App.Models;

public sealed class DiskInfo
{
    public string DriveName { get; init; } = "C:";
    public long TotalBytes { get; init; }
    public long FreeBytes { get; init; }
    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    public double FreePercent => TotalBytes <= 0 ? 0 : FreeBytes * 100d / TotalBytes;
    public string Status => FreePercent < 10 ? "Critique" : FreePercent < 20 ? "Attention" : "OK";
}
