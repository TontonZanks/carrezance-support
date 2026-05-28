using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class CleaningViewModel : ObservableObject
{
    private readonly CleaningService _cleaningService;
    private readonly SystemInfoService _systemInfoService;
    private readonly RepairActionHistoryService _historyService;
    private string _diskSummary = "Cliquez sur \"Analyser l'espace disque\".";
    private string _actionStatus = "Prêt";
    private string _lastActionBadge = "Non exécuté";

    public CleaningViewModel(CleaningService cleaningService, SystemInfoService systemInfoService, RepairActionHistoryService historyService)
    {
        _cleaningService = cleaningService;
        _systemInfoService = systemInfoService;
        _historyService = historyService;
        AnalyzeDiskCommand = new RelayCommand(AnalyzeDisk);
        CleanTempCommand = new AsyncRelayCommand(CleanTempAsync);
    }

    public string DiskSummary
    {
        get => _diskSummary;
        private set => SetProperty(ref _diskSummary, value);
    }

    public ICommand AnalyzeDiskCommand { get; }
    public ICommand CleanTempCommand { get; }
    public string ActionStatus { get => _actionStatus; private set => SetProperty(ref _actionStatus, value); }
    public string LastActionBadge { get => _lastActionBadge; private set => SetProperty(ref _lastActionBadge, value); }

    private void AnalyzeDisk()
    {
        var disk = _systemInfoService.GetDiskInfo("C");
        DiskSummary = $"Disque C: {disk.Status}{Environment.NewLine}Total : {FileSizeHelper.Format(disk.TotalBytes)}{Environment.NewLine}Libre : {FileSizeHelper.Format(disk.FreeBytes)} ({disk.FreePercent:0.#}%)";
    }

    private async Task CleanTempAsync()
    {
        if (!MessageHelper.Confirm("Cette action nettoie uniquement les fichiers temporaires de votre session Windows. Aucun document personnel n'est supprimé. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Nettoyage simple", "Nettoyage", "Action annulée par l'utilisateur.");
            LastActionBadge = "Annulé";
            ActionStatus = "Action annulée.";
            return;
        }

        ActionStatus = "Action en cours...";
        var result = await _cleaningService.CleanUserTempAsync();
        LastActionBadge = result.Success ? "Succès" : "Échec";
        ActionStatus = result.Message;
        MessageBox.Show(result.Message, "Carrezance Support", MessageBoxButton.OK, result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }
}
