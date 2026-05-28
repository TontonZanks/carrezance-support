using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Carrezance.Support.App.Models;

namespace Carrezance.Support.App.Services;

public sealed class SystemInfoService
{
    private readonly LogService _logService;
    private readonly AdminService _adminService;

    public SystemInfoService(LogService logService, AdminService adminService)
    {
        _logService = logService;
        _adminService = adminService;
    }

    public SystemReport CreateReport(ObservableCollection<DiagnosticResult>? networkTests = null)
    {
        return CreateQuickReport(networkTests);
    }

    public SystemReport CreateQuickReport(ObservableCollection<DiagnosticResult>? networkTests = null, bool logSystemBlock = false)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var identity = GetMachineIdentityInfo();
            var report = new SystemReport
            {
                GeneratedAt = DateTime.Now,
                ComputerName = SafeRead(() => Environment.MachineName),
                UserName = SafeRead(() => Environment.UserName),
                WindowsVersion = GetWindowsDisplayName(),
                Architecture = SafeRead(() => RuntimeInformation.OSArchitecture.ToString()),
                DomainOrWorkgroup = identity.DisplayName,
                ExecutionMode = _adminService.ExecutionMode,
                Network = GetNetworkInfo(),
                DiskC = GetDiskInfo("C"),
                LastBootTime = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64),
                Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                DiagnosticType = "Diagnostic rapide",
                AntivirusStatus = "Non analysé",
                BitLockerStatus = "Non analysé",
                OneDriveStatus = "Non analysé",
                ActiveDirectoryStatus = identity.Detail,
                AzureAdJoinStatus = "Non analysé",
                ImportantSoftware = CreateNotAnalyzedSoftwareResults(),
                NetworkTests = networkTests ?? new ObservableCollection<DiagnosticResult>()
            };

            if (TryGetMemory(out var total, out var available))
            {
                report.TotalMemoryBytes = total;
                report.UsedMemoryBytes = Math.Max(0, total - available);
            }

            stopwatch.Stop();
            if (logSystemBlock)
            {
                _logService.Log("[Diagnostic]", $"Bloc système terminé en {stopwatch.ElapsedMilliseconds} ms");
            }
            return report;
        }
        catch (Exception ex)
        {
            _logService.Log("[Diagnostic]", "Erreur lecture système", ex.Message);
            return new SystemReport
            {
                GeneratedAt = DateTime.Now,
                ComputerName = SafeRead(() => Environment.MachineName),
                UserName = SafeRead(() => Environment.UserName),
                WindowsVersion = GetWindowsDisplayName(),
                Architecture = "Non disponible",
                DomainOrWorkgroup = GetMachineIdentityInfo().DisplayName,
                ExecutionMode = _adminService.ExecutionMode,
                Network = new NetworkInfo(),
                DiskC = new DiskInfo(),
                DiagnosticType = "Diagnostic rapide",
                AntivirusStatus = "Non analysé",
                BitLockerStatus = "Non analysé",
                OneDriveStatus = "Non analysé",
                ActiveDirectoryStatus = GetMachineIdentityInfo().Detail,
                AzureAdJoinStatus = "Non analysé",
                ImportantSoftware = CreateNotAnalyzedSoftwareResults(),
                NetworkTests = networkTests ?? new ObservableCollection<DiagnosticResult>()
            };
        }
    }

    public async Task<SystemReport> CreateQuickReportAsync(
        ObservableCollection<DiagnosticResult>? networkTests = null,
        IProgress<string>? progress = null,
        bool logSystemBlock = false)
    {
        progress?.Report("Lecture système");
        return await Task.Run(() => CreateQuickReport(networkTests, logSystemBlock));
    }

    public async Task<SystemReport> CreateFullReportAsync(
        ObservableCollection<DiagnosticResult>? networkTests = null,
        IProgress<string>? progress = null)
    {
        var report = await CreateQuickReportAsync(networkTests, progress, logSystemBlock: true);

        progress?.Report("Analyse sécurité");
        var securityWatch = Stopwatch.StartNew();
        report.AntivirusStatus = await Task.Run(GetAntivirusStatus);
        report.BitLockerStatus = await Task.Run(GetBitLockerStatus);
        securityWatch.Stop();
        _logService.Log("[Diagnostic]", $"Bloc sécurité terminé en {securityWatch.ElapsedMilliseconds} ms");

        progress?.Report("Analyse identité");
        var identityWatch = Stopwatch.StartNew();
        report.OneDriveStatus = GetOneDriveStatus();
        var identity = await Task.Run(GetMachineIdentityInfo);
        report.DomainOrWorkgroup = identity.DisplayName;
        report.ActiveDirectoryStatus = identity.Detail;
        report.AzureAdJoinStatus = await Task.Run(GetAzureAdJoinStatus);
        identityWatch.Stop();
        _logService.Log("[Diagnostic]", $"Bloc identité terminé en {identityWatch.ElapsedMilliseconds} ms");

        progress?.Report("Analyse logiciels");
        var softwareWatch = Stopwatch.StartNew();
        report.ImportantSoftware = await GetImportantSoftwareWithTimeoutAsync();
        softwareWatch.Stop();
        _logService.Log("[Diagnostic]", $"Bloc logiciels terminé en {softwareWatch.ElapsedMilliseconds} ms");

        progress?.Report("Génération synthèse");
        report.GeneratedAt = DateTime.Now;
        report.DiagnosticType = "Diagnostic complet";
        return report;
    }

    public DiskInfo GetDiskInfo(string driveLetter)
    {
        try
        {
            var drive = new DriveInfo(driveLetter);
            return new DiskInfo
            {
                DriveName = drive.Name,
                TotalBytes = drive.TotalSize,
                FreeBytes = drive.AvailableFreeSpace
            };
        }
        catch (Exception ex)
        {
            _logService.Log("Lecture disque", "Erreur", ex.Message);
            return new DiskInfo { DriveName = $"{driveLetter}:" };
        }
    }

    public NetworkInfo GetNetworkInfo()
    {
        try
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => new { Adapter = n, Properties = n.GetIPProperties() })
                .FirstOrDefault(n => n.Properties.UnicastAddresses.Any(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork));

            if (adapter is null)
            {
                return new NetworkInfo();
            }

            var ip = adapter.Properties.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString();
            var gateway = adapter.Properties.GatewayAddresses.FirstOrDefault()?.Address.ToString();
            var dns = adapter.Properties.DnsAddresses.Select(a => a.ToString()).Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            var mac = string.Join(":", adapter.Adapter.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));

            return new NetworkInfo
            {
                AdapterName = string.IsNullOrWhiteSpace(adapter.Adapter.Name) ? "Non disponible" : adapter.Adapter.Name,
                LocalIpAddress = string.IsNullOrWhiteSpace(ip) ? "Non disponible" : ip,
                Gateway = string.IsNullOrWhiteSpace(gateway) ? "Non disponible" : gateway,
                MacAddress = string.IsNullOrWhiteSpace(mac) ? "Non disponible" : mac,
                DnsServers = new ObservableCollection<string>(dns)
            };
        }
        catch (Exception ex)
        {
            _logService.Log("Lecture réseau", "Erreur", ex.Message);
            return new NetworkInfo();
        }
    }

    public SystemReport CreateSafeFallbackReport()
    {
        return new SystemReport
        {
            GeneratedAt = DateTime.Now,
            ComputerName = SafeRead(() => Environment.MachineName),
            UserName = SafeRead(() => Environment.UserName),
            WindowsVersion = "Version Windows non disponible",
            Architecture = "Non disponible",
            DomainOrWorkgroup = GetMachineIdentityInfo().DisplayName,
            ExecutionMode = _adminService.ExecutionMode,
            Network = new NetworkInfo(),
            DiskC = new DiskInfo(),
            DiagnosticType = "Diagnostic rapide",
            AntivirusStatus = "Non analysé",
            BitLockerStatus = "Non analysé",
            OneDriveStatus = "Non analysé",
            ActiveDirectoryStatus = GetMachineIdentityInfo().Detail,
            AzureAdJoinStatus = "Non analysé",
            ImportantSoftware = CreateNotAnalyzedSoftwareResults()
        };
    }

    public string GetWindowsDisplayName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key is not null)
            {
                var productName = ReadRegistryString(key, "ProductName");
                var displayVersion = ReadRegistryString(key, "DisplayVersion");
                var currentBuild = ReadRegistryString(key, "CurrentBuild") ??
                                   ReadRegistryString(key, "CurrentBuildNumber");
                var ubr = ReadRegistryString(key, "UBR");
                var editionId = ReadRegistryString(key, "EditionID");
                var installationType = ReadRegistryString(key, "InstallationType");

                if (int.TryParse(currentBuild, out var buildNumber))
                {
                    var fullBuild = string.IsNullOrWhiteSpace(ubr)
                        ? buildNumber.ToString()
                        : $"{buildNumber}.{ubr}";

                    if (IsServerInstallation(installationType, productName))
                    {
                        return FormatWindowsServerName(productName, editionId, fullBuild);
                    }

                    var clientName = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
                    var editionName = FormatClientEdition(editionId, productName);
                    var versionPart = string.IsNullOrWhiteSpace(displayVersion) ? string.Empty : $" {displayVersion}";

                    return $"{clientName}{editionName}{versionPart} - build {fullBuild}";
                }
            }
        }
        catch (Exception ex)
        {
            _logService.Log("Lecture version Windows registre", "Erreur", ex.Message);
        }

        return GetWindowsCaptionFromWmi();
    }

    private string GetAntivirusStatus()
    {
        try
        {
            var output = RunProcessForOutput(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | Select-Object -ExpandProperty displayName\"",
                2000,
                "Antivirus / SecurityCenter2");

            var products = output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return products.Length == 0 ? "Non disponible" : string.Join(", ", products);
        }
        catch (Exception ex)
        {
            _logService.Log("Détection antivirus", "Erreur", ex.Message);
            return "Non disponible";
        }
    }

    private string GetBitLockerStatus()
    {
        try
        {
            var output = RunProcessForOutput("manage-bde.exe", "-status C:", 2000, "BitLocker");
            if (ContainsIgnoreCase(output, "Protection On") ||
                ContainsIgnoreCase(output, "Protection activée") ||
                ContainsIgnoreCase(output, "Protection activ"))
            {
                return "Activé";
            }

            if (ContainsIgnoreCase(output, "Protection Off") ||
                ContainsIgnoreCase(output, "Protection désactivée") ||
                ContainsIgnoreCase(output, "Protection d"))
            {
                return "Désactivé";
            }
        }
        catch (Exception ex)
        {
            _logService.Log("Détection BitLocker", "Erreur", ex.Message);
        }

        return "Non disponible";
    }

    private static string GetOneDriveStatus()
    {
        try
        {
            var paths = new[]
            {
                Environment.GetEnvironmentVariable("OneDrive"),
                Environment.GetEnvironmentVariable("OneDriveCommercial"),
                Environment.GetEnvironmentVariable("OneDriveConsumer"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive")
            };

            return paths.Any(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                ? "OneDrive détecté"
                : "OneDrive non détecté";
        }
        catch
        {
            return "Non disponible";
        }
    }

    private MachineIdentityInfo GetMachineIdentityInfo()
    {
        try
        {
            var output = RunProcessForOutput(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"$cs = Get-CimInstance Win32_ComputerSystem; $part = [bool]$cs.PartOfDomain; $domain = [string]$cs.Domain; $workgroup = [string]$cs.Workgroup; if ($part) { 'DOMAIN|' + $domain } else { 'WORKGROUP|' + $(if ([string]::IsNullOrWhiteSpace($workgroup)) { $domain } else { $workgroup }) }\"",
                2000,
                "Domaine AD / Workgroup");

            return ParseMachineIdentity(output);
        }
        catch (Exception ex)
        {
            _logService.Log("Détection domaine/workgroup", "Erreur", ex.Message);
            return MachineIdentityInfo.Unavailable;
        }
    }

    private static MachineIdentityInfo ParseMachineIdentity(string output)
    {
        var line = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(line))
        {
            return MachineIdentityInfo.Unavailable;
        }

        var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            return MachineIdentityInfo.Unavailable;
        }

        if (parts[0].Equals("DOMAIN", StringComparison.OrdinalIgnoreCase))
        {
            return new MachineIdentityInfo($"Domaine AD : {parts[1]}", $"Domaine AD : oui - {parts[1]}");
        }

        return new MachineIdentityInfo($"Workgroup : {parts[1]}", $"Domaine AD : non - Workgroup : {parts[1]}");
    }

    private string GetAzureAdJoinStatus()
    {
        try
        {
            var output = RunProcessForOutput("dsregcmd.exe", "/status", 3000, "Azure AD Join");
            var line = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(value => value.StartsWith("AzureAdJoined", StringComparison.OrdinalIgnoreCase));

            if (line is null)
            {
                return "Non disponible";
            }

            return ContainsIgnoreCase(line, "YES") ? "Azure AD Join : oui" : "Azure AD Join : non";
        }
        catch (Exception ex)
        {
            _logService.Log("Détection Azure AD Join", "Erreur", ex.Message);
            return "Non disponible";
        }
    }

    private ObservableCollection<SoftwareDetectionResult> GetImportantSoftware()
    {
        var installedSoftware = GetInstalledSoftwareEntries();
        var checks = new (string Name, Func<SoftwareDetectionResult> Detect)[]
        {
            ("Microsoft Office / Microsoft 365", () => DetectOffice(installedSoftware)),
            ("Outlook classique", () => DetectOutlookClassic(installedSoftware)),
            ("Nouveau Outlook", () => DetectNewOutlook()),
            ("Courrier / Calendrier Windows", () => DetectWindowsMailCalendar()),
            ("Microsoft Teams", () => DetectTeams(installedSoftware)),
            ("AnyDesk", () => DetectByRegistryName("AnyDesk", installedSoftware, "AnyDesk")),
            ("TeamViewer", () => DetectByRegistryName("TeamViewer", installedSoftware, "TeamViewer")),
            ("AutoCAD", () => DetectAutoCad(installedSoftware)),
            ("Sage", () => DetectSage(installedSoftware)),
            ("Ciel", () => DetectCiel(installedSoftware)),
            ("Google Chrome", () => DetectChrome(installedSoftware)),
            ("Microsoft Edge", () => DetectEdge(installedSoftware)),
            ("Mozilla Firefox", () => DetectFirefox(installedSoftware)),
            ("Opera", () => DetectOpera(installedSoftware))
        };

        var results = new ObservableCollection<SoftwareDetectionResult>();
        foreach (var check in checks)
        {
            SoftwareDetectionResult result;
            try
            {
                result = check.Detect();
            }
            catch (Exception ex)
            {
                result = SoftwareUnavailable(check.Name);
                _logService.Log("[Détection logiciel]", $"{check.Name} : Non disponible", ex.Message);
            }

            results.Add(result);
            _logService.Log("[Détection logiciel]", $"{result.Name} : {result.DisplayStatus} - Source : {ValueOrNone(result.DetectionSource)} - Chemin/ID : {ValueOrNone(result.DetectionPath)} - Confiance : {result.Confidence}");
        }

        return results;
    }

    private static SoftwareDetectionResult DetectOffice(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Microsoft 365", "Microsoft Office", "Office 16", "Office");
        if (registry is not null)
        {
            return SoftwareDetected("Microsoft Office / Microsoft 365", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var clickToRun = FirstExistingRegistryKey(
            @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration",
            @"SOFTWARE\WOW6432Node\Microsoft\Office\ClickToRun\Configuration");
        if (!string.IsNullOrWhiteSpace(clickToRun))
        {
            return SoftwareDetected("Microsoft Office / Microsoft 365", "ClickToRun", clickToRun, "High");
        }

        var folder = FirstExistingDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Office"));

        return !string.IsNullOrWhiteSpace(folder)
            ? SoftwareDetected("Microsoft Office / Microsoft 365", "Dossier Program Files", folder, "Medium")
            : SoftwareNotDetected("Microsoft Office / Microsoft 365");
    }

    private static SoftwareDetectionResult DetectOutlookClassic(List<InstalledSoftwareEntry> installedSoftware)
    {
        var appPath = FirstExistingRegistryKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE");
        if (!string.IsNullOrWhiteSpace(appPath))
        {
            return SoftwareDetected("Outlook classique", "App Paths OUTLOOK.EXE", appPath, "High");
        }

        var exe = FirstExistingFile(GetCommonOutlookPaths().ToArray());
        if (!string.IsNullOrWhiteSpace(exe))
        {
            return SoftwareDetected("Outlook classique", "Exécutable Microsoft Office", exe, "High");
        }

        var registry = FindInstalledSoftware(installedSoftware, "Microsoft Outlook", "Outlook");
        if (registry is not null)
        {
            return SoftwareDetected("Outlook classique", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var clickToRun = FirstOfficeClickToRunValueContaining("Outlook");
        if (!string.IsNullOrWhiteSpace(clickToRun))
        {
            return SoftwareDetected("Outlook classique", "ClickToRun", clickToRun, "High");
        }

        return SoftwareNotDetected("Outlook classique");
    }

    private SoftwareDetectionResult DetectNewOutlook()
    {
        var appx = DetectAppxPackage("Nouveau Outlook", "Microsoft.OutlookForWindows");
        if (appx.Status == "Detected")
        {
            return appx;
        }

        var packageFolder = FirstMatchingDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages"),
            "Microsoft.OutlookForWindows*");
        if (!string.IsNullOrWhiteSpace(packageFolder))
        {
            return SoftwareDetected("Nouveau Outlook", "Microsoft Store / dossier Appx", packageFolder, "High");
        }

        return SoftwareNotDetected("Nouveau Outlook");
    }

    private SoftwareDetectionResult DetectWindowsMailCalendar()
    {
        var appx = DetectAppxPackage("Courrier / Calendrier Windows", "Microsoft.WindowsCommunicationsApps");
        return appx.Status == "Detected" ? appx : SoftwareNotDetected("Courrier / Calendrier Windows");
    }

    private static IEnumerable<string> GetCommonOutlookPaths()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        yield return Path.Combine(programFiles, @"Microsoft Office\root\Office16\OUTLOOK.EXE");
        yield return Path.Combine(programFilesX86, @"Microsoft Office\root\Office16\OUTLOOK.EXE");
        yield return Path.Combine(programFiles, @"Microsoft Office\Office16\OUTLOOK.EXE");
        yield return Path.Combine(programFilesX86, @"Microsoft Office\Office16\OUTLOOK.EXE");
        yield return Path.Combine(programFiles, @"Microsoft Office 15\root\office15\OUTLOOK.EXE");
        yield return Path.Combine(programFilesX86, @"Microsoft Office 15\root\office15\OUTLOOK.EXE");
    }

    private static SoftwareDetectionResult DetectTeams(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Microsoft Teams", "Teams Machine-Wide Installer");
        if (registry is not null)
        {
            return SoftwareDetected("Microsoft Teams", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var exe = FirstExistingFile(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Teams\current\Teams.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\ms-teams.exe"));

        return !string.IsNullOrWhiteSpace(exe)
            ? SoftwareDetected("Microsoft Teams", "Exécutable utilisateur", exe, "High")
            : SoftwareNotDetected("Microsoft Teams");
    }

    private static SoftwareDetectionResult DetectAutoCad(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Autodesk AutoCAD", "AutoCAD LT", "AutoCAD");
        if (registry is not null)
        {
            return SoftwareDetected("AutoCAD", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var exe = FirstMatchingFile(
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Autodesk")
            },
            "acad.exe");
        if (!string.IsNullOrWhiteSpace(exe))
        {
            return SoftwareDetected("AutoCAD", "Exécutable Autodesk", exe, "High");
        }

        var folder = FirstExistingDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Autodesk"));

        return !string.IsNullOrWhiteSpace(folder)
            ? SoftwareDetected("AutoCAD", "Dossier Autodesk", folder, "Medium")
            : SoftwareNotDetected("AutoCAD");
    }

    private SoftwareDetectionResult DetectSage(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Sage 50", "Sage 100", "Sage Comptabilité", "Sage Gestion Commerciale", "Sage Paie", "Sage Batigest", "Sage");
        if (registry is not null)
        {
            return SoftwareDetected("Sage", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var exe = FirstMatchingFile(
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Sage"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Sage")
            },
            "sage*.exe");
        if (!string.IsNullOrWhiteSpace(exe))
        {
            return SoftwareDetected("Sage", "Exécutable Sage", exe, "High");
        }

        if (DirectoryExistsSafe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Sage")))
        {
            _logService.Log("[Détection logiciel]", "Sage ignoré : dossier générique ProgramData sans exécutable valide");
        }

        return SoftwareNotDetected("Sage");
    }

    private SoftwareDetectionResult DetectCiel(List<InstalledSoftwareEntry> installedSoftware)
    {
        foreach (var partialMatch in installedSoftware.Where(entry =>
                     ContainsIgnoreCase(entry.DisplayName, "Ciel") && !IsCielDisplayName(entry.DisplayName)))
        {
            _logService.Log("[Détection logiciel]", $"Ciel ignoré : correspondance partielle non valide dans '{partialMatch.DisplayName}'");
        }

        var registry = installedSoftware.FirstOrDefault(entry => IsCielDisplayName(entry.DisplayName));
        if (registry is not null)
        {
            return SoftwareDetected("Ciel", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var exe = FirstKnownExecutable(
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ciel"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ciel"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Sage\Ciel")
            },
            "Ciel.exe",
            "Wciel.exe",
            "CielCompta.exe",
            "CielGestion.exe",
            "CielPaye.exe");
        if (!string.IsNullOrWhiteSpace(exe))
        {
            return SoftwareDetected("Ciel", "Exécutable Ciel", exe, "High");
        }

        if (DirectoryExistsSafe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ciel")) ||
            DirectoryExistsSafe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ciel")) ||
            DirectoryExistsSafe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Sage\Ciel")))
        {
            _logService.Log("[Détection logiciel]", "Ciel ignoré : dossier générique sans exécutable valide");
        }

        return SoftwareNotDetected("Ciel");
    }

    private static bool DirectoryExistsSafe(string path)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    private static SoftwareDetectionResult DetectChrome(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Google Chrome");
        if (registry is not null)
        {
            return SoftwareDetected("Google Chrome", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var appPath = FirstExistingRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
        return !string.IsNullOrWhiteSpace(appPath)
            ? SoftwareDetected("Google Chrome", "App Paths", appPath, "High")
            : SoftwareNotDetected("Google Chrome");
    }

    private static SoftwareDetectionResult DetectEdge(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Microsoft Edge");
        if (registry is not null)
        {
            return SoftwareDetected("Microsoft Edge", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var appPath = FirstExistingRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe");
        return !string.IsNullOrWhiteSpace(appPath)
            ? SoftwareDetected("Microsoft Edge", "App Paths", appPath, "High")
            : SoftwareNotDetected("Microsoft Edge");
    }

    private static SoftwareDetectionResult DetectFirefox(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Mozilla Firefox");
        if (registry is not null)
        {
            return SoftwareDetected("Mozilla Firefox", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var appPath = FirstExistingRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
        if (!string.IsNullOrWhiteSpace(appPath))
        {
            return SoftwareDetected("Mozilla Firefox", "App Paths", appPath, "High");
        }

        var exe = FirstExistingFile(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Mozilla Firefox\firefox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Mozilla Firefox\firefox.exe"));

        return !string.IsNullOrWhiteSpace(exe)
            ? SoftwareDetected("Mozilla Firefox", "Exécutable", exe, "High")
            : SoftwareNotDetected("Mozilla Firefox");
    }

    private static SoftwareDetectionResult DetectOpera(List<InstalledSoftwareEntry> installedSoftware)
    {
        var registry = FindInstalledSoftware(installedSoftware, "Opera");
        if (registry is not null)
        {
            return SoftwareDetected("Opera", "Registre applications installées", RegistryDetectionPath(registry), "High");
        }

        var appPath = FirstExistingRegistryKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\launcher.exe",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\opera.exe");
        if (!string.IsNullOrWhiteSpace(appPath))
        {
            return SoftwareDetected("Opera", "App Paths", appPath, "High");
        }

        var exe = FirstExistingFile(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Opera\launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Opera GX\launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Opera\launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Opera\launcher.exe"));

        return !string.IsNullOrWhiteSpace(exe)
            ? SoftwareDetected("Opera", "Exécutable", exe, "High")
            : SoftwareNotDetected("Opera");
    }

    private async Task<ObservableCollection<SoftwareDetectionResult>> GetImportantSoftwareWithTimeoutAsync()
    {
        var task = Task.Run(GetImportantSoftware);
        var timeout = Task.Delay(2000);
        var completed = await Task.WhenAny(task, timeout);
        if (completed == task)
        {
            return await task;
        }

        _logService.Log("[Diagnostic]", "Logiciels importants timeout après 2000 ms");
        return CreateUnavailableSoftwareResults();
    }

    private static ObservableCollection<SoftwareDetectionResult> CreateNotAnalyzedSoftwareResults()
    {
        return CreateSoftwareResults("NotAnalyzed");
    }

    private static ObservableCollection<SoftwareDetectionResult> CreateUnavailableSoftwareResults()
    {
        return CreateSoftwareResults("Unavailable");
    }

    private static ObservableCollection<SoftwareDetectionResult> CreateSoftwareResults(string status)
    {
        string[] softwareNames =
        [
            "Microsoft Office / Microsoft 365",
            "Outlook classique",
            "Nouveau Outlook",
            "Courrier / Calendrier Windows",
            "Microsoft Teams",
            "AnyDesk",
            "TeamViewer",
            "AutoCAD",
            "Sage",
            "Ciel",
            "Google Chrome",
            "Microsoft Edge",
            "Mozilla Firefox",
            "Opera"
        ];

        return new ObservableCollection<SoftwareDetectionResult>(
            softwareNames.Select(name => new SoftwareDetectionResult
            {
                Name = name,
                Status = status,
                DetectionSource = status == "NotDetected" ? "aucune" : string.Empty,
                Confidence = "High"
            }));
    }

    private static List<InstalledSoftwareEntry> GetInstalledSoftwareEntries()
    {
        var entries = new List<InstalledSoftwareEntry>();
        var roots = new[] { Registry.LocalMachine, Registry.CurrentUser };
        var paths = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var root in roots)
        {
            foreach (var path in paths)
            {
                try
                {
                    using var key = root.OpenSubKey(path);
                    if (key is null)
                    {
                        continue;
                    }

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        var displayName = subKey?.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            entries.Add(new InstalledSoftwareEntry(displayName, $"{root.Name}\\{path}\\{subKeyName}"));
                        }
                    }
                }
                catch
                {
                    // Registry access can vary by policy; software detection is best effort.
                }
            }
        }

        return entries;
    }

    private SoftwareDetectionResult DetectAppxPackage(string softwareName, params string[] packageNames)
    {
        try
        {
            var filter = string.Join(",", packageNames.Select(name => $"'{name}'"));
            var command = $"Get-AppxPackage | Where-Object {{ $names = @({filter}); $names -contains $_.Name }} | Select-Object -First 1 -ExpandProperty Name";
            var output = RunProcessForOutput("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"", 1000, $"{softwareName} Appx");
            var packageName = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

            return string.IsNullOrWhiteSpace(packageName)
                ? SoftwareNotDetected(softwareName)
                : SoftwareDetected(softwareName, "Microsoft Store / Appx", packageName, "High");
        }
        catch (Exception ex)
        {
            _logService.Log("[Détection logiciel]", $"{softwareName} Appx indisponible", ex.Message);
            return SoftwareNotDetected(softwareName);
        }
    }

    private static SoftwareDetectionResult DetectByRegistryName(string softwareName, List<InstalledSoftwareEntry> installedSoftware, params string[] expectedNames)
    {
        var registry = FindInstalledSoftware(installedSoftware, expectedNames);
        return registry is null
            ? SoftwareNotDetected(softwareName)
            : SoftwareDetected(softwareName, "Registre applications installées", RegistryDetectionPath(registry), "High");
    }

    private static InstalledSoftwareEntry? FindInstalledSoftware(List<InstalledSoftwareEntry> installedSoftware, params string[] expectedNames)
    {
        return installedSoftware.FirstOrDefault(entry =>
            expectedNames.Any(expected => ContainsIgnoreCase(entry.DisplayName, expected)));
    }

    // Validation Ciel:
    // true: "Ciel", "Ciel Compta", "Sage Ciel", "Ciel Gestion Commerciale"
    // false: "Logiciel", "NVIDIA Logiciel système PhysX", "Pack logiciel", "Gestionnaire logiciel"
    private static bool IsCielDisplayName(string? displayName)
    {
        return !string.IsNullOrWhiteSpace(displayName) &&
               Regex.IsMatch(displayName, @"(^|[^A-Za-zÀ-ÿ0-9])Ciel([^A-Za-zÀ-ÿ0-9]|$)", RegexOptions.IgnoreCase);
    }

    private static string RegistryDetectionPath(InstalledSoftwareEntry entry)
    {
        return $"{entry.DisplayName} | {entry.RegistryPath}";
    }

    private static SoftwareDetectionResult SoftwareDetected(string name, string source, string path, string confidence)
    {
        return new SoftwareDetectionResult
        {
            Name = name,
            Status = "Detected",
            DetectionSource = source,
            DetectionPath = path,
            Confidence = confidence
        };
    }

    private static SoftwareDetectionResult SoftwareNotDetected(string name)
    {
        return new SoftwareDetectionResult
        {
            Name = name,
            Status = "NotDetected",
            DetectionSource = "aucune",
            Confidence = "High"
        };
    }

    private static SoftwareDetectionResult SoftwareUnavailable(string name)
    {
        return new SoftwareDetectionResult
        {
            Name = name,
            Status = "Unavailable",
            Confidence = "Low"
        };
    }

    private static string ValueOrNone(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "aucune" : value;
    }

    private static string? FirstExistingRegistryKey(params string[] paths)
    {
        foreach (var path in paths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key is not null)
                {
                    return $@"HKLM\{path}";
                }
            }
            catch
            {
                // Software detection is best effort.
            }
        }

        return null;
    }

    private static string? FirstOfficeClickToRunValueContaining(string expected)
    {
        foreach (var path in new[]
                 {
                     @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration",
                     @"SOFTWARE\WOW6432Node\Microsoft\Office\ClickToRun\Configuration"
                 })
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key is null)
                {
                    continue;
                }

                foreach (var valueName in key.GetValueNames())
                {
                    var value = key.GetValue(valueName)?.ToString();
                    if (ContainsIgnoreCase(valueName, expected) || ContainsIgnoreCase(value, expected))
                    {
                        return $@"HKLM\{path}\{valueName}";
                    }
                }
            }
            catch
            {
                // Click-to-Run detection is optional.
            }
        }

        return null;
    }

    private static string? FirstExistingDirectory(params string[] paths)
    {
        return paths.FirstOrDefault(DirectoryExistsSafe);
    }

    private static string? FirstExistingFile(params string[] paths)
    {
        return paths.FirstOrDefault(FileExistsSafe);
    }

    private static string? FirstMatchingDirectory(string rootPath, string pattern)
    {
        try
        {
            return DirectoryExistsSafe(rootPath)
                ? Directory.EnumerateDirectories(rootPath, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FirstMatchingFile(IEnumerable<string> rootPaths, string pattern)
    {
        foreach (var rootPath in rootPaths)
        {
            try
            {
                if (!DirectoryExistsSafe(rootPath))
                {
                    continue;
                }

                var direct = Directory.EnumerateFiles(rootPath, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(direct))
                {
                    return direct;
                }

                foreach (var child in Directory.EnumerateDirectories(rootPath, "*", SearchOption.TopDirectoryOnly))
                {
                    var childMatch = Directory.EnumerateFiles(child, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(childMatch))
                    {
                        return childMatch;
                    }
                }
            }
            catch
            {
                // Keep disk checks narrowly scoped and best effort.
            }
        }

        return null;
    }

    private static string? FirstKnownExecutable(IEnumerable<string> rootPaths, params string[] executableNames)
    {
        foreach (var executableName in executableNames)
        {
            var match = FirstMatchingFile(rootPaths, executableName);
            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }
        }

        return null;
    }

    private static bool FileExistsSafe(string path)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    private string GetWindowsCaptionFromWmi()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"(Get-CimInstance Win32_OperatingSystem).Caption\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process is null)
            {
                return "Version Windows non disponible";
            }

            if (!process.WaitForExit(3000))
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // If the fallback process cannot be killed, continue with the safe fallback value.
                }

                _logService.Log("Lecture version Windows WMI", "Erreur", "Délai dépassé");
                return "Version Windows non disponible";
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logService.Log("Lecture version Windows WMI", "Erreur", error.Trim());
            }
        }
        catch (Exception ex)
        {
            _logService.Log("Lecture version Windows WMI", "Erreur", ex.Message);
        }

        return "Version Windows non disponible";
    }

    private string RunProcessForOutput(string fileName, string arguments, int timeoutMilliseconds, string actionName)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process is null)
        {
            return string.Empty;
        }

        if (!process.WaitForExit(timeoutMilliseconds))
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                // Best-effort diagnostic commands must not block the application.
            }

            _logService.Log("[Diagnostic]", $"{actionName} timeout après {timeoutMilliseconds} ms");
            return string.Empty;
        }

        return process.StandardOutput.ReadToEnd();
    }

    private static string? ReadRegistryString(RegistryKey key, string valueName)
    {
        return key.GetValue(valueName)?.ToString();
    }

    private static bool IsServerInstallation(string? installationType, string? productName)
    {
        return ContainsIgnoreCase(installationType, "Server") ||
               ContainsIgnoreCase(productName, "Server");
    }

    private static string FormatWindowsServerName(string? productName, string? editionId, string fullBuild)
    {
        var name = string.IsNullOrWhiteSpace(productName) ? "Windows Server" : productName.Trim();
        var edition = FormatServerEdition(editionId);

        if (!string.IsNullOrWhiteSpace(edition) &&
            !ContainsIgnoreCase(name, edition))
        {
            name = $"{name} {edition}";
        }

        return $"{name} - build {fullBuild}";
    }

    private static string FormatClientEdition(string? editionId, string? productName)
    {
        var source = string.IsNullOrWhiteSpace(editionId) ? productName : editionId;
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var edition = source.Trim();
        var normalized = edition.Replace("Windows 10", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Windows 11", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var displayName = normalized.ToUpperInvariant() switch
        {
            "PROFESSIONAL" or "PRO" => "Professionnel",
            "CORE" or "HOME" => "Famille",
            "ENTERPRISE" => "Entreprise",
            "EDUCATION" => "Education",
            "PROFESSIONALEDUCATION" => "Professionnel Education",
            "PROFESSIONALWORKSTATION" => "Professionnel Workstation",
            "PROFESSIONALN" => "Professionnel N",
            "COREN" => "Famille N",
            _ => normalized
        };

        return string.IsNullOrWhiteSpace(displayName) ? string.Empty : $" {displayName}";
    }

    private static string FormatServerEdition(string? editionId)
    {
        if (string.IsNullOrWhiteSpace(editionId))
        {
            return string.Empty;
        }

        return editionId.Trim().ToUpperInvariant() switch
        {
            "SERVERSTANDARD" => "Standard",
            "SERVERDATACENTER" => "Datacenter",
            "SERVERAZURESTACKHCICOR" => "Azure Stack HCI",
            "SERVERSTANDARDCORE" => "Standard Core",
            "SERVERDATACENTERCORE" => "Datacenter Core",
            _ => editionId.Trim()
        };
    }

    private static bool ContainsIgnoreCase(string? value, string expected)
    {
        return value?.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private sealed record MachineIdentityInfo(string DisplayName, string Detail)
    {
        public static MachineIdentityInfo Unavailable { get; } = new("Non disponible", "Non disponible");
    }

    private sealed record InstalledSoftwareEntry(string DisplayName, string RegistryPath);

    private static string SafeRead(Func<string> read)
    {
        try
        {
            var value = read();
            return string.IsNullOrWhiteSpace(value) ? "Non disponible" : value;
        }
        catch
        {
            return "Non disponible";
        }
    }

    private static bool TryGetMemory(out long totalBytes, out long availableBytes)
    {
        try
        {
            var status = new MemoryStatusEx();
            if (GlobalMemoryStatusEx(status))
            {
                totalBytes = (long)status.TotalPhys;
                availableBytes = (long)status.AvailPhys;
                return true;
            }
        }
        catch
        {
            // Memory information is optional in the support report.
        }

        totalBytes = 0;
        availableBytes = 0;
        return false;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}
