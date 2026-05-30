using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class AssistanceViewModel : ObservableObject
{
    private readonly SystemInfoService _systemInfoService;
    private readonly ReportService _reportService;
    private readonly LogService _logService;
    private readonly ProcessService _processService;
    private readonly UpdateService _updateService;
    private SystemReport _report;
    private UpdateInfo _updateInfo = new();
    private bool _isCheckingForUpdates;
    private string _updateStatus = "Vérification non lancée";

    public AssistanceViewModel(
        SystemInfoService systemInfoService,
        ReportService reportService,
        LogService logService,
        ProcessService processService,
        UpdateService updateService)
    {
        _systemInfoService = systemInfoService;
        _reportService = reportService;
        _logService = logService;
        _processService = processService;
        _updateService = updateService;
        _report = _systemInfoService.CreateQuickReport();
        _updateInfo.CurrentVersion = AppInfo.Version;
        SupportItems = new ObservableCollection<KeyValuePair<string, string>>();
        AboutItems = new ObservableCollection<KeyValuePair<string, string>>
        {
            new("Nom", AppInfo.Name),
            new("Version", $"v{AppInfo.Version}"),
            new("Description", AppInfo.Description),
            new("Confidentialité", "Aucune donnée sensible ni mot de passe n'est stocké par l'application."),
            new("Technique", "Application portable"),
            new("Compatibilité", "Windows 10 / Windows 11 x64"),
            new("Rapport", "Rapport généré localement")
        };

        Refresh();
        CopyInfoCommand = new RelayCommand(CopyInfo);
        ExportReportCommand = new RelayCommand(ExportReport);
        ExportHtmlReportCommand = new RelayCommand(ExportHtmlReport);
        OpenLastReportFolderCommand = new RelayCommand(OpenLastReportFolder);
        CopyLastReportPathCommand = new RelayCommand(CopyLastReportPath);
        OpenLastHtmlReportCommand = new RelayCommand(OpenLastHtmlReport);
        CopyLastHtmlReportPathCommand = new RelayCommand(CopyLastHtmlReportPath);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
        DownloadUpdateCommand = new RelayCommand(DownloadUpdate, () => HasUpdateDownload);
        OpenReleaseNotesCommand = new RelayCommand(OpenReleaseNotes, () => HasUpdateReleaseUrl);
    }

    public ObservableCollection<KeyValuePair<string, string>> SupportItems { get; }
    public ObservableCollection<KeyValuePair<string, string>> AboutItems { get; }
    public string CurrentVersion => $"v{AppInfo.Version}";
    public string LatestVersion => string.IsNullOrWhiteSpace(_updateInfo.LatestVersion) ? "Non disponible" : $"v{_updateInfo.LatestVersion}";
    public string UpdateStatus { get => _updateStatus; private set => SetProperty(ref _updateStatus, value); }
    public string LastUpdateCheck => _updateInfo.LastCheckedAt == default ? "Jamais" : _updateInfo.LastCheckedAt.ToString("dd/MM/yyyy HH:mm:ss");
    public string UpdateNotification => _updateInfo.IsUpdateAvailable ? $"Une mise à jour est disponible : v{_updateInfo.LatestVersion}" : string.Empty;
    public bool IsCheckingForUpdates { get => _isCheckingForUpdates; private set => SetProperty(ref _isCheckingForUpdates, value); }
    public bool HasUpdateDownload => _updateInfo.IsUpdateAvailable && !string.IsNullOrWhiteSpace(_updateInfo.AssetDownloadUrl);
    public bool HasUpdateReleaseUrl => _updateInfo.IsUpdateAvailable && !string.IsNullOrWhiteSpace(_updateInfo.ReleaseUrl);
    public Visibility UpdateActionVisibility => _updateInfo.IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
    public ICommand CopyInfoCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ExportHtmlReportCommand { get; }
    public ICommand OpenLastReportFolderCommand { get; }
    public ICommand CopyLastReportPathCommand { get; }
    public ICommand OpenLastHtmlReportCommand { get; }
    public ICommand CopyLastHtmlReportPathCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }
    public ICommand DownloadUpdateCommand { get; }
    public ICommand OpenReleaseNotesCommand { get; }

    public async Task CheckForUpdatesSilentlyAsync()
    {
        await CheckForUpdatesCoreAsync(showErrors: false);
    }

    private async Task CheckForUpdatesAsync()
    {
        await CheckForUpdatesCoreAsync(showErrors: true);
    }

    private void Refresh()
    {
        _report = _systemInfoService.CreateQuickReport();
        SupportItems.Clear();
        SupportItems.Add(new("Nom PC", _report.ComputerName));
        SupportItems.Add(new("Utilisateur", _report.UserName));
        SupportItems.Add(new("IP locale", _report.Network.LocalIpAddress));
        SupportItems.Add(new("Domaine / Workgroup", _report.DomainOrWorkgroup));
        SupportItems.Add(new("Windows", _report.WindowsVersion));
        SupportItems.Add(new("Date / heure du diagnostic", _report.GeneratedAt.ToString("dd/MM/yyyy HH:mm:ss")));
    }

    private void CopyInfo()
    {
        try
        {
            Refresh();
            Clipboard.SetText(_reportService.BuildReportText(_report));
            _reportService.LogAction("Copier informations assistance");
            MessageBox.Show("Les informations ont été copiées.", "Carrezance Support", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _reportService.LogAction("Copier informations assistance", "Erreur", ex.Message);
            MessageHelper.ShowError("Les informations n'ont pas pu être copiées. Vous pouvez exporter le rapport TXT à la place.");
        }
    }

    private void ExportReport()
    {
        Refresh();
        MessageHelper.ShowExportResult(_reportService.Export(_report));
    }

    private void ExportHtmlReport()
    {
        Refresh();
        MessageHelper.ShowExportResult(_reportService.ExportHtml(_report));
    }

    private void OpenLastReportFolder()
    {
        var folder = _reportService.LastReportFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageHelper.ShowError("Aucun rapport n'a encore été généré. Exportez d'abord un rapport support.");
            return;
        }

        OpenFolder(folder, "Ouvrir dossier du dernier rapport");
    }

    private void CopyLastReportPath()
    {
        var path = _reportService.LastReportPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageHelper.ShowError("Aucun rapport n'a encore été généré. Exportez d'abord un rapport support.");
            return;
        }

        try
        {
            Clipboard.SetText(path);
            _reportService.LogAction("Copier chemin dernier rapport");
            MessageBox.Show("Le chemin du dernier rapport a été copié.", "Carrezance Support", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _reportService.LogAction("Copier chemin dernier rapport", "Erreur", ex.Message);
            MessageHelper.ShowError("Le chemin du rapport n'a pas pu être copié.");
        }
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

    private void OpenLogsFolder()
    {
        OpenFolder(_logService.LogFolder, "Ouvrir dossier des logs");
    }

    private async Task CheckForUpdatesCoreAsync(bool showErrors)
    {
        IsCheckingForUpdates = true;
        UpdateStatus = "Vérification en cours";
        RefreshUpdateBindings();

        try
        {
            _updateInfo = await _updateService.CheckForUpdatesAsync();
            if (_updateInfo.IsUpdateAvailable)
            {
                UpdateStatus = "Mise à jour disponible";
            }
            else if (!string.IsNullOrWhiteSpace(_updateInfo.ErrorMessage))
            {
                UpdateStatus = "Vérification impossible";
                if (showErrors)
                {
                    MessageHelper.ShowError("La vérification de mise à jour est impossible pour le moment.");
                }
            }
            else
            {
                UpdateStatus = "À jour";
            }
        }
        catch (Exception ex)
        {
            _logService.Log("[Update]", "Erreur", ex.ToString());
            UpdateStatus = "Vérification impossible";
            if (showErrors)
            {
                MessageHelper.ShowError("La vérification de mise à jour est impossible pour le moment.");
            }
        }
        finally
        {
            IsCheckingForUpdates = false;
            RefreshUpdateBindings();
        }
    }

    private void DownloadUpdate()
    {
        if (string.IsNullOrWhiteSpace(_updateInfo.AssetDownloadUrl))
        {
            MessageHelper.ShowError("Aucun fichier de mise à jour n'est disponible.");
            return;
        }

        var result = _processService.Open(_updateInfo.AssetDownloadUrl, "Télécharger mise à jour");
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }

    private void OpenReleaseNotes()
    {
        if (string.IsNullOrWhiteSpace(_updateInfo.ReleaseUrl))
        {
            MessageHelper.ShowError("Les notes de version ne sont pas disponibles.");
            return;
        }

        var result = _processService.Open(_updateInfo.ReleaseUrl, "Voir notes de version");
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }

    private void RefreshUpdateBindings()
    {
        OnPropertyChanged(nameof(CurrentVersion));
        OnPropertyChanged(nameof(LatestVersion));
        OnPropertyChanged(nameof(LastUpdateCheck));
        OnPropertyChanged(nameof(UpdateNotification));
        OnPropertyChanged(nameof(HasUpdateDownload));
        OnPropertyChanged(nameof(HasUpdateReleaseUrl));
        OnPropertyChanged(nameof(UpdateActionVisibility));

        if (DownloadUpdateCommand is RelayCommand downloadCommand)
        {
            downloadCommand.RaiseCanExecuteChanged();
        }

        if (OpenReleaseNotesCommand is RelayCommand notesCommand)
        {
            notesCommand.RaiseCanExecuteChanged();
        }
    }

    private void OpenFolder(string folder, string actionName)
    {
        if (!Directory.Exists(folder))
        {
            _reportService.LogAction(actionName, "Erreur", "Dossier inaccessible");
            MessageHelper.ShowError("Le dossier demandé est inaccessible.");
            return;
        }

        var result = _processService.Open(folder, actionName);
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }
}
