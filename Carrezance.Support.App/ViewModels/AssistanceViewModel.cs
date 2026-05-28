using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class AssistanceViewModel
{
    private readonly SystemInfoService _systemInfoService;
    private readonly ReportService _reportService;
    private readonly LogService _logService;
    private readonly ProcessService _processService;
    private SystemReport _report;

    public AssistanceViewModel(
        SystemInfoService systemInfoService,
        ReportService reportService,
        LogService logService,
        ProcessService processService)
    {
        _systemInfoService = systemInfoService;
        _reportService = reportService;
        _logService = logService;
        _processService = processService;
        _report = _systemInfoService.CreateQuickReport();
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
    }

    public ObservableCollection<KeyValuePair<string, string>> SupportItems { get; }
    public ObservableCollection<KeyValuePair<string, string>> AboutItems { get; }
    public ICommand CopyInfoCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ExportHtmlReportCommand { get; }
    public ICommand OpenLastReportFolderCommand { get; }
    public ICommand CopyLastReportPathCommand { get; }
    public ICommand OpenLastHtmlReportCommand { get; }
    public ICommand CopyLastHtmlReportPathCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }

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
