using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private const long TenGigabytes = 10L * 1024 * 1024 * 1024;
    private const long TwentyGigabytes = 20L * 1024 * 1024 * 1024;

    private readonly SystemInfoService _systemInfoService;
    private readonly NetworkService _networkService;
    private readonly PrinterService _printerService;
    private readonly OfficeService _officeService;
    private readonly CleaningService _cleaningService;
    private readonly ReportService _reportService;
    private readonly RepairActionHistoryService _historyService;
    private readonly AdminService _adminService;
    private SystemReport _report;
    private string _internetStatus = "Non testé";
    private bool _isDiagnosticRunning;
    private double _diagnosticProgressValue;
    private string _diagnosticProgressText = "Prêt";
    private string _diagnosticCurrentStep = "Prêt";
    private string _lastDiagnosticError = string.Empty;

    public DashboardViewModel(
        SystemInfoService systemInfoService,
        NetworkService networkService,
        PrinterService printerService,
        OfficeService officeService,
        CleaningService cleaningService,
        ReportService reportService,
        RepairActionHistoryService historyService,
        AdminService adminService)
    {
        _systemInfoService = systemInfoService;
        _networkService = networkService;
        _printerService = printerService;
        _officeService = officeService;
        _cleaningService = cleaningService;
        _reportService = reportService;
        _historyService = historyService;
        _adminService = adminService;
        _report = SafeCreateQuickReport();

        RunQuickDiagnosticCommand = new AsyncRelayCommand(RunQuickDiagnosticAsync, onException: HandleDiagnosticException);
        RepairCommonIssueCommand = new AsyncRelayCommand(RepairCommonIssueAsync, onException: HandleDiagnosticException);
        RepairPrintCommand = new AsyncRelayCommand(RepairPrintAsync, onException: HandleDiagnosticException);
        CloseOutlookCommand = new AsyncRelayCommand(CloseOutlookAsync, onException: HandleDiagnosticException);
        CleanTempCommand = new AsyncRelayCommand(CleanTempAsync, onException: HandleDiagnosticException);
        ExportReportCommand = new RelayCommand(ExportReport);
    }

    public string ComputerName => _report.ComputerName;
    public string UserName => _report.UserName;
    public string WindowsVersion => _report.WindowsVersion;
    public string DiskStatus => $"{GetDiskStatus(_report.DiskC.FreeBytes)} - {FileSizeHelper.Format(_report.DiskC.FreeBytes)} libres";

    public string InternetStatus
    {
        get => _internetStatus;
        private set => SetProperty(ref _internetStatus, value);
    }

    public bool IsDiagnosticRunning
    {
        get => _isDiagnosticRunning;
        private set => SetProperty(ref _isDiagnosticRunning, value);
    }

    public double DiagnosticProgressValue
    {
        get => _diagnosticProgressValue;
        private set => SetProperty(ref _diagnosticProgressValue, value);
    }

    public string DiagnosticProgressText
    {
        get => _diagnosticProgressText;
        private set => SetProperty(ref _diagnosticProgressText, value);
    }

    public string DiagnosticCurrentStep
    {
        get => _diagnosticCurrentStep;
        private set => SetProperty(ref _diagnosticCurrentStep, value);
    }

    public string LastDiagnosticError
    {
        get => _lastDiagnosticError;
        private set => SetProperty(ref _lastDiagnosticError, value);
    }

    public ICommand RunQuickDiagnosticCommand { get; }
    public ICommand RepairCommonIssueCommand { get; }
    public ICommand RepairPrintCommand { get; }
    public ICommand CloseOutlookCommand { get; }
    public ICommand CleanTempCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ObservableCollection<RepairActionHistoryItem> RepairActions => _historyService.Items;

    private async Task RunQuickDiagnosticAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            StartDiagnostic("Initialisation", 10);

            SetProgress(25, "Lecture système");
            _report = await _systemInfoService.CreateQuickReportAsync();
            Refresh();

            SetProgress(45, "Lecture réseau");
            await Task.Yield();

            SetProgress(65, "Test Internet");
            var tests = await _networkService.TestInternetAsync(logResult: false);

            SetProgress(85, "Mise à jour du rapport");
            _report = await _systemInfoService.CreateQuickReportAsync(tests);
            InternetStatus = GetInternetStatus(tests);
            Refresh();

            SetProgress(100, "Diagnostic terminé");
            stopwatch.Stop();
            _reportService.LogAction("[Diagnostic]", $"Diagnostic rapide terminé en {stopwatch.ElapsedMilliseconds} ms");
            LastDiagnosticError = string.Empty;
            DiagnosticProgressText = "Diagnostic rapide terminé";
        }
        catch (Exception ex)
        {
            HandleDiagnosticException(ex);
        }
        finally
        {
            IsDiagnosticRunning = false;
        }
    }

    private async Task RepairCommonIssueAsync()
    {
        if (!MessageHelper.Confirm("Cette action vide le cache DNS de Windows. Elle peut résoudre certains problèmes d'accès aux sites web. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Réparer l'accès aux sites web", "Internet & Réseau", "Action annulée par l'utilisateur.");
            return;
        }

        var result = await _networkService.FlushDnsAsync();
        MessageHelper.ShowResult(result);
    }

    private async Task RepairPrintAsync()
    {
        if (HandleAdminRequired("Réparer l'impression"))
        {
            return;
        }

        if (!MessageHelper.Confirm("Cette action redémarre le service d'impression Windows. Elle peut résoudre les impressions bloquées. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Réparer l'impression", "Imprimantes", "Action annulée par l'utilisateur.", requiresAdmin: true);
            return;
        }

        MessageHelper.ShowResult(await _printerService.RestartSpoolerAsync());
    }

    private bool HandleAdminRequired(string actionName)
    {
        if (_adminService.IsRunningAsAdministrator())
        {
            return false;
        }

        _historyService.Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = actionName,
            Category = "Imprimantes",
            Status = "Non exécuté",
            Message = "Droits administrateur requis.",
            RequiresAdmin = true,
            ExecutedAsAdmin = false
        });
        _adminService.LogAdminRequired(actionName);
        _adminService.LogAdminRestartProposed(actionName);

        bool restartRequested;
        try
        {
            restartRequested = MessageHelper.AskRestartAsAdministrator();
        }
        catch (Exception ex)
        {
            _adminService.LogAdminDialogFailed(actionName, ex);
            _historyService.Add(new RepairActionHistoryItem
            {
                DateTime = DateTime.Now,
                ActionName = actionName,
                Category = "Imprimantes",
                Status = "Échec",
                Message = "Impossible d'ouvrir la demande d'élévation administrateur.",
                RequiresAdmin = true,
                ExecutedAsAdmin = false
            });
            MessageHelper.ShowError("Impossible d'ouvrir la demande d'élévation administrateur.");
            return true;
        }

        if (!restartRequested)
        {
            _historyService.Add(new RepairActionHistoryItem
            {
                DateTime = DateTime.Now,
                ActionName = actionName,
                Category = "Imprimantes",
                Status = "Non exécuté",
                Message = "Droits administrateur requis, relance annulée par l'utilisateur.",
                RequiresAdmin = true,
                ExecutedAsAdmin = false
            });
            _adminService.LogAdminRestartCanceled(actionName);
            MessageHelper.ShowError("Droits administrateur requis, relance annulée par l'utilisateur.");
            return true;
        }

        _historyService.Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = actionName,
            Category = "Imprimantes",
            Status = "Non exécuté",
            Message = "Relance en administrateur demandée.",
            RequiresAdmin = true,
            ExecutedAsAdmin = false
        });

        var result = _adminService.RestartAsAdministrator();
        if (!result.Success)
        {
            _historyService.Add(new RepairActionHistoryItem
            {
                DateTime = DateTime.Now,
                ActionName = actionName,
                Category = "Imprimantes",
                Status = "Non exécuté",
                Message = "Relance administrateur refusée ou annulée.",
                RequiresAdmin = true,
                ExecutedAsAdmin = false
            });
            MessageHelper.ShowResult(result);
        }

        return true;
    }

    private async Task CloseOutlookAsync()
    {
        if (!MessageHelper.Confirm("Cette action ferme Outlook si celui-ci est bloqué. Les messages non enregistrés peuvent être perdus. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Fermer Outlook bloqué", "Outlook & Office", "Action annulée par l'utilisateur.");
            return;
        }

        MessageHelper.ShowResult(await _officeService.CloseOutlookAsync());
    }

    private async Task CleanTempAsync()
    {
        if (!MessageHelper.Confirm("Cette action nettoie uniquement les fichiers temporaires de votre session Windows. Aucun document personnel n'est supprimé. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Nettoyage simple", "Nettoyage", "Action annulée par l'utilisateur.");
            return;
        }

        MessageHelper.ShowResult(await _cleaningService.CleanUserTempAsync());
    }

    private void ExportReport()
    {
        MessageHelper.ShowExportResult(_reportService.Export(_report));
    }

    private SystemReport SafeCreateQuickReport()
    {
        try
        {
            return _systemInfoService.CreateQuickReport();
        }
        catch (Exception ex)
        {
            HandleDiagnosticException(ex);
            return _systemInfoService.CreateSafeFallbackReport();
        }
    }

    private void StartDiagnostic(string step, double progress)
    {
        IsDiagnosticRunning = true;
        LastDiagnosticError = string.Empty;
        InternetStatus = "Diagnostic en cours...";
        SetProgress(progress, step);
    }

    private void SetProgress(double value, string step)
    {
        DiagnosticProgressValue = value;
        DiagnosticCurrentStep = step;
        DiagnosticProgressText = $"{value:0}% - {step}";
    }

    private void HandleDiagnosticException(Exception ex)
    {
        CrashLogService.LogUnhandledException("[Diagnostic]", ex);
        LastDiagnosticError = "Le diagnostic n'a pas pu se terminer. Une erreur a été enregistrée dans les logs.";
        DiagnosticProgressText = LastDiagnosticError;
        DiagnosticCurrentStep = "Erreur";
        InternetStatus = InternetStatus == "Diagnostic en cours..." ? "Non disponible" : InternetStatus;
    }

    private static string GetDiskStatus(long freeBytes)
    {
        return freeBytes switch
        {
            > TwentyGigabytes => "OK",
            >= TenGigabytes => "Attention",
            _ => "Critique"
        };
    }

    private static string GetInternetStatus(IEnumerable<DiagnosticResult> tests)
    {
        var results = tests.ToArray();
        if (results.Length == 0)
        {
            return "Non testé";
        }

        var successfulTests = results.Where(test => test.Status == "OK").ToArray();
        if (successfulTests.Length == 0)
        {
            return "Erreur";
        }

        var ipPingOk = results.Any(test => test.Name.Contains("Ping", StringComparison.OrdinalIgnoreCase) && test.Status == "OK");
        var dnsKo = results.Any(test => test.Name.Contains("DNS", StringComparison.OrdinalIgnoreCase) && test.Status != "OK");
        return ipPingOk && dnsKo ? "Attention" : "OK";
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(ComputerName));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(WindowsVersion));
        OnPropertyChanged(nameof(DiskStatus));
    }
}
