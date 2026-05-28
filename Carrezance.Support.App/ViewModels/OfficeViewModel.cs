using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class OfficeViewModel : ObservableObject
{
    private readonly OfficeService _officeService;
    private readonly RepairActionHistoryService _historyService;
    private string _actionStatus = "Prêt";
    private string _lastActionBadge = "Non exécuté";

    public OfficeViewModel(OfficeService officeService, RepairActionHistoryService historyService)
    {
        _officeService = officeService;
        _historyService = historyService;
        CloseOutlookCommand = new AsyncRelayCommand(CloseOutlookAsync);
        StartOutlookSafeModeCommand = new RelayCommand(() => MessageHelper.ShowResult(_officeService.StartOutlookSafeMode()));
        OpenOutlookWebCommand = new RelayCommand(() => ShowOnlyIfFailed(_officeService.OpenOutlookWeb()));
        OpenOfficePortalCommand = new RelayCommand(() => ShowOnlyIfFailed(_officeService.OpenOfficePortal()));
    }

    public ICommand CloseOutlookCommand { get; }
    public ICommand StartOutlookSafeModeCommand { get; }
    public ICommand OpenOutlookWebCommand { get; }
    public ICommand OpenOfficePortalCommand { get; }
    public string ActionStatus { get => _actionStatus; private set => SetProperty(ref _actionStatus, value); }
    public string LastActionBadge { get => _lastActionBadge; private set => SetProperty(ref _lastActionBadge, value); }

    private async Task CloseOutlookAsync()
    {
        if (!MessageHelper.Confirm("Cette action ferme Outlook si celui-ci est bloqué. Les messages non enregistrés peuvent être perdus. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Fermer Outlook bloqué", "Outlook & Office", "Action annulée par l'utilisateur.");
            LastActionBadge = "Annulé";
            ActionStatus = "Action annulée.";
            return;
        }

        ActionStatus = "Action en cours...";
        var result = await _officeService.CloseOutlookAsync();
        LastActionBadge = result.Success ? "Succès" : "Échec";
        ActionStatus = result.Message;
        MessageHelper.ShowResult(result);
    }

    private static void ShowOnlyIfFailed(ActionResult result)
    {
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }
}
