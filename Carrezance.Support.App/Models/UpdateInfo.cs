namespace Carrezance.Support.App.Models;

public sealed class UpdateInfo
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public bool IsUpdateAvailable { get; set; }
    public string ReleaseName { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetDownloadUrl { get; set; } = string.Empty;
    public string Sha256AssetName { get; set; } = string.Empty;
    public string Sha256AssetDownloadUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime LastCheckedAt { get; set; }
}
