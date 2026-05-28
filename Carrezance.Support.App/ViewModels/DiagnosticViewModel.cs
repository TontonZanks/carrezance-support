using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class DiagnosticViewModel : ObservableObject
{
    private readonly SystemInfoService _systemInfoService;
    private readonly ReportService _reportService;
    private readonly ProcessService _processService;
    private SystemReport _report = new();
    private bool _isDiagnosticRunning;
    private double _diagnosticProgressValue;
    private string _diagnosticProgressText = "Analyse non lancée";
    private string _diagnosticCurrentStep = "Analyse non lancée";
    private string _lastDiagnosticError = string.Empty;

    public DiagnosticViewModel(SystemInfoService systemInfoService, ReportService reportService, ProcessService processService)
    {
        _systemInfoService = systemInfoService;
        _reportService = reportService;
        _processService = processService;
        Items = new ObservableCollection<KeyValuePair<string, string>>();
        RunDiagnosticCommand = new AsyncRelayCommand(RunQuickDiagnosticAsync, onException: HandleDiagnosticException);
        RunFullDiagnosticCommand = new AsyncRelayCommand(RunFullDiagnosticAsync, onException: HandleDiagnosticException);
        CopyInfoCommand = new RelayCommand(CopyInfo);
        ExportReportCommand = new RelayCommand(ExportReport);
        ExportHtmlReportCommand = new RelayCommand(ExportHtmlReport);
        OpenLastHtmlReportCommand = new RelayCommand(OpenLastHtmlReport);
        CopyLastHtmlReportPathCommand = new RelayCommand(CopyLastHtmlReportPath);

        _report = SafeCreateQuickReport();
        PopulateItems();
    }

    public ObservableCollection<KeyValuePair<string, string>> Items { get; }
    public ICommand RunDiagnosticCommand { get; }
    public ICommand RunFullDiagnosticCommand { get; }
    public ICommand CopyInfoCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ExportHtmlReportCommand { get; }
    public ICommand OpenLastHtmlReportCommand { get; }
    public ICommand CopyLastHtmlReportPathCommand { get; }

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

    private async Task RunQuickDiagnosticAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            StartDiagnostic("Initialisation", 10);
            SetProgress(25, "Lecture système");
            _report = await _systemInfoService.CreateQuickReportAsync();

            SetProgress(45, "Lecture réseau");
            await Task.Yield();

            SetProgress(65, "Test Internet");
            await Task.Yield();

            SetProgress(85, "Mise à jour du rapport");
            PopulateItems();

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

    private async Task RunFullDiagnosticAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            StartDiagnostic("Initialisation", 0);
            var progress = new Progress<string>(step =>
            {
                var value = step switch
                {
                    "Lecture système" => 15,
                    "Analyse réseau" => 30,
                    "Analyse sécurité" => 45,
                    "Analyse identité" => 60,
                    "Analyse logiciels" => 75,
                    "Génération synthèse" => 90,
                    _ => DiagnosticProgressValue
                };
                SetProgress(value, step);
            });

            _report = await _systemInfoService.CreateFullReportAsync(progress: progress);
            SetProgress(90, "Préparation rapport");
            PopulateItems();
            SetProgress(100, "Diagnostic terminé");
            stopwatch.Stop();
            _reportService.LogAction("[Diagnostic]", $"Diagnostic complet terminé en {stopwatch.ElapsedMilliseconds} ms");
            LastDiagnosticError = string.Empty;
            DiagnosticProgressText = "Diagnostic complet terminé";
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

    private void PopulateItems()
    {
        Items.Clear();
        Items.Add(new("Nom du PC", _report.ComputerName));
        Items.Add(new("Utilisateur connecté", _report.UserName));
        Items.Add(new("Version Windows", _report.WindowsVersion));
        Items.Add(new("Architecture système", _report.Architecture));
        Items.Add(new("Domaine ou Workgroup", _report.DomainOrWorkgroup));
        Items.Add(new("Adresse IP locale", _report.Network.LocalIpAddress));
        Items.Add(new("Passerelle", _report.Network.Gateway));
        Items.Add(new("DNS configurés", FormatList(_report.Network.DnsServers)));
        Items.Add(new("Adresse MAC principale", _report.Network.MacAddress));
        Items.Add(new("Disque C: total", FileSizeHelper.Format(_report.DiskC.TotalBytes)));
        Items.Add(new("Disque C: utilisé", FileSizeHelper.Format(_report.DiskC.UsedBytes)));
        Items.Add(new("Disque C: libre", $"{FileSizeHelper.Format(_report.DiskC.FreeBytes)} ({_report.DiskC.FreePercent:0.#}%)"));
        Items.Add(new("RAM totale", FileSizeHelper.Format(_report.TotalMemoryBytes)));
        Items.Add(new("RAM utilisée", FileSizeHelper.Format(_report.UsedMemoryBytes)));
        Items.Add(new("Dernier démarrage Windows", _report.LastBootTime == default ? "Non disponible" : _report.LastBootTime.ToString("dd/MM/yyyy HH:mm:ss")));
        Items.Add(new("Uptime", DateTimeHelper.FormatDuration(_report.Uptime)));
        Items.Add(new("Antivirus", _report.AntivirusStatus));
        Items.Add(new("BitLocker C:", _report.BitLockerStatus));
        Items.Add(new("OneDrive", _report.OneDriveStatus));
        Items.Add(new("Domaine AD", _report.ActiveDirectoryStatus));
        Items.Add(new("Azure AD Join", _report.AzureAdJoinStatus));

        foreach (var software in _report.ImportantSoftware)
        {
            var details = software.Status == "Detected"
                ? $"{software.DisplayStatus} - Source : {software.DetectionSource} - Chemin/ID : {software.DetectionPath}"
                : software.DisplayStatus;
            Items.Add(new($"Logiciel - {software.Name}", details));
        }
    }

    private void CopyInfo()
    {
        try
        {
            Clipboard.SetText(_reportService.BuildReportText(_report));
            _reportService.LogAction("Copier informations support");
            MessageBox.Show("Les informations support ont été copiées.", "Carrezance Support", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _reportService.LogAction("Copier informations support", "Erreur", ex.Message);
            MessageHelper.ShowError("Les informations n'ont pas pu être copiées. Vous pouvez exporter le rapport TXT à la place.");
        }
    }

    private void ExportReport()
    {
        MessageHelper.ShowExportResult(_reportService.Export(_report));
    }

    private void ExportHtmlReport()
    {
        MessageHelper.ShowExportResult(_reportService.ExportHtml(_report));
    }

    private void OpenLastHtmlReport()
    {
        var result = _reportService.OpenLastHtmlReport(_processService);
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }

    private void CopyLastHtmlReportPath()
    {
        MessageHelper.ShowResult(_reportService.CopyLastHtmlReportPath());
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
        CrashLogService.LogUnhandledException("Diagnostic page", ex);
        LastDiagnosticError = "Le diagnostic n'a pas pu se terminer. Une erreur a été enregistrée dans les logs.";
        DiagnosticCurrentStep = "Erreur";
        DiagnosticProgressText = LastDiagnosticError;
    }

    private static string FormatList(IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return items.Length == 0 ? "Non disponible" : string.Join(", ", items);
    }
}
