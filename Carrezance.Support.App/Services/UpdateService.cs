using System.Net.Http;
using System.Text.Json;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;

namespace Carrezance.Support.App.Services;

public sealed class UpdateService
{
    private const string GitHubOwner = "TontonZanks";
    private const string GitHubRepository = "carrezance-support";
    private const string GitHubLatestReleaseApi = "https://api.github.com/repos/TontonZanks/carrezance-support/releases/latest";
    private readonly LogService _logService;

    public UpdateService(LogService logService)
    {
        _logService = logService;
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var info = new UpdateInfo
        {
            CurrentVersion = AppInfo.Version,
            LastCheckedAt = DateTime.Now
        };

        _logService.Log("[Update]", $"Début vérification - Version actuelle : {AppInfo.Version}");

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CarrezanceSupport");

            using var response = await httpClient.GetAsync(GitHubLatestReleaseApi, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                info.ErrorMessage = $"GitHub indisponible ou limite API atteinte ({(int)response.StatusCode}).";
                _logService.Log("[Update]", "Erreur", info.ErrorMessage);
                return info;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var isPrerelease = ReadBool(root, "prerelease");
            var tagName = ReadString(root, "tag_name");
            if (isPrerelease || NormalizeVersion(tagName).Length == 0)
            {
                info.LatestVersion = tagName;
                info.ErrorMessage = "Dernière release ignorée car préversion ou version invalide.";
                _logService.Log("[Update]", "Release ignorée", info.ErrorMessage);
                return info;
            }

            var assets = ReadAssets(root).ToArray();
            var exeAsset = FindPreferredExeAsset(assets, tagName);
            var zipAsset = FindZipAsset(assets, tagName);
            var sha256Asset = FindSha256Asset(assets);

            info.LatestVersion = NormalizeVersion(tagName);
            info.ReleaseName = ReadString(root, "name");
            info.ReleaseUrl = ReadString(root, "html_url");
            info.ReleaseNotes = ReadString(root, "body");
            info.PublishedAt = ReadDate(root, "published_at");
            info.AssetName = exeAsset?.Name ?? zipAsset?.Name ?? string.Empty;
            info.AssetDownloadUrl = exeAsset?.DownloadUrl ?? zipAsset?.DownloadUrl ?? string.Empty;
            info.Sha256AssetName = sha256Asset?.Name ?? string.Empty;
            info.Sha256AssetDownloadUrl = sha256Asset?.DownloadUrl ?? string.Empty;
            info.IsUpdateAvailable = CompareVersions(info.CurrentVersion, info.LatestVersion) > 0;

            var result = info.IsUpdateAvailable ? "Mise à jour disponible" : "À jour";
            _logService.Log("[Update]", $"{result} - Actuelle : {info.CurrentVersion} - Dernière : {info.LatestVersion} - Release : {info.ReleaseUrl}");
            return info;
        }
        catch (OperationCanceledException)
        {
            info.ErrorMessage = "Vérification annulée.";
            _logService.Log("[Update]", "Erreur", info.ErrorMessage);
            return info;
        }
        catch (Exception ex)
        {
            info.ErrorMessage = "Vérification impossible.";
            _logService.Log("[Update]", "Erreur", ex.Message);
            return info;
        }
    }

    public int CompareVersions(string currentVersion, string latestVersion)
    {
        var current = NormalizeVersion(currentVersion);
        var latest = NormalizeVersion(latestVersion);
        if (!Version.TryParse(current, out var currentParsed) ||
            !Version.TryParse(latest, out var latestParsed))
        {
            return 0;
        }

        return latestParsed.CompareTo(currentParsed);
    }

    public string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var normalized = version.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        return normalized.Contains('-', StringComparison.Ordinal) ? string.Empty : normalized;
    }

    public ReleaseAsset? FindPreferredExeAsset(IEnumerable<ReleaseAsset> assets, string latestVersion)
    {
        var normalizedVersion = NormalizeVersion(latestVersion);
        var expectedName = $"CarrezanceSupport-v{normalizedVersion}-win-x64.exe";
        return assets.FirstOrDefault(asset => asset.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase)) ??
               assets.FirstOrDefault(asset => asset.Name.EndsWith("-win-x64.exe", StringComparison.OrdinalIgnoreCase));
    }

    public ReleaseAsset? FindZipAsset(IEnumerable<ReleaseAsset> assets, string latestVersion)
    {
        var normalizedVersion = NormalizeVersion(latestVersion);
        var expectedName = $"CarrezanceSupport-v{normalizedVersion}-win-x64.zip";
        return assets.FirstOrDefault(asset => asset.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase)) ??
               assets.FirstOrDefault(asset => asset.Name.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase));
    }

    public ReleaseAsset? FindSha256Asset(IEnumerable<ReleaseAsset> assets)
    {
        return assets.FirstOrDefault(asset => asset.Name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<ReleaseAsset> ReadAssets(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assetsElement) ||
            assetsElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var asset in assetsElement.EnumerateArray())
        {
            var name = ReadString(asset, "name");
            var downloadUrl = ReadString(asset, "browser_download_url");
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(downloadUrl))
            {
                yield return new ReleaseAsset(name, downloadUrl);
            }
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.True;
    }

    private static DateTimeOffset? ReadDate(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    public sealed record ReleaseAsset(string Name, string DownloadUrl);
}
