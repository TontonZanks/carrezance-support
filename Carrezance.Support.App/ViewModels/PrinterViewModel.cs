using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class PrinterViewModel : ObservableObject
{
    private readonly PrinterService _printerService;
    private readonly ProcessService _processService;
    private readonly RepairActionHistoryService _historyService;
    private readonly AdminService _adminService;
    private string _actionStatus = "Prêt";
    private string _lastActionBadge = "Non exécuté";

    public PrinterViewModel(PrinterService printerService, ProcessService processService, RepairActionHistoryService historyService, AdminService adminService)
    {
        _printerService = printerService;
        _processService = processService;
        _historyService = historyService;
        _adminService = adminService;
        Printers = new ObservableCollection<PrinterInfo>();
        RepairPrintCommand = new AsyncRelayCommand(RepairPrintAsync);
        ClearQueueCommand = new AsyncRelayCommand(ClearQueueAsync);
        ShowPrintersCommand = new RelayCommand(ShowPrinters);
        OpenPrinterSettingsCommand = new RelayCommand(OpenPrinterSettings);
    }

    public ObservableCollection<PrinterInfo> Printers { get; }
    public ICommand RepairPrintCommand { get; }
    public ICommand ClearQueueCommand { get; }
    public ICommand ShowPrintersCommand { get; }
    public ICommand OpenPrinterSettingsCommand { get; }
    public string ActionStatus { get => _actionStatus; private set => SetProperty(ref _actionStatus, value); }
    public string LastActionBadge { get => _lastActionBadge; private set => SetProperty(ref _lastActionBadge, value); }

    private async Task RepairPrintAsync()
    {
        if (HandleAdminRequired("Réparer l'impression"))
        {
            return;
        }

        if (!MessageHelper.Confirm("Cette action redémarre le service d'impression Windows. Elle peut résoudre les impressions bloquées. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Réparer l'impression", "Imprimantes", "Action annulée par l'utilisateur.", requiresAdmin: true);
            LastActionBadge = "Annulé";
            ActionStatus = "Action annulée.";
            return;
        }

        ActionStatus = "Action en cours...";
        var result = await _printerService.RestartSpoolerAsync();
        LastActionBadge = result.Success ? "Succès" : "Échec";
        ActionStatus = result.Message;
        MessageHelper.ShowResult(result);
    }

    private async Task ClearQueueAsync()
    {
        if (HandleAdminRequired("Vider la file d'attente impression"))
        {
            return;
        }

        if (!MessageHelper.Confirm("Cette action vide la file d'attente d'impression Windows. Elle supprime uniquement les fichiers temporaires du dossier du spouleur d'impression. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Vider la file d'attente impression", "Imprimantes", "Action annulée par l'utilisateur.", requiresAdmin: true);
            LastActionBadge = "Annulé";
            ActionStatus = "Action annulée.";
            return;
        }

        ActionStatus = "Action en cours...";
        var result = await _printerService.ClearPrintQueueAsync();
        LastActionBadge = result.Success ? "Succès" : "Échec";
        ActionStatus = result.Message;
        MessageHelper.ShowResult(result);
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

        ActionStatus = "Cette action nécessite les droits administrateur.";
        LastActionBadge = "Non exécuté";
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
            ActionStatus = "Impossible d'ouvrir la demande d'élévation administrateur.";
            LastActionBadge = "Échec";
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
            ActionStatus = "Droits administrateur requis, relance annulée par l'utilisateur.";
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
            ActionStatus = result.Message;
            MessageHelper.ShowResult(result);
        }

        return true;
    }

    private void ShowPrinters()
    {
        Printers.Clear();
        foreach (var printer in _printerService.GetPrinters())
        {
            Printers.Add(printer);
        }

        if (!string.IsNullOrWhiteSpace(_printerService.LastError))
        {
            MessageHelper.ShowError("La liste des imprimantes n'a pas pu être lue. Vous pouvez ouvrir les paramètres imprimantes Windows.");
        }
        else if (Printers.Count == 0)
        {
            MessageBox.Show("Aucune imprimante installée n'a été trouvée sur ce poste.", "Carrezance Support", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenPrinterSettings()
    {
        var result = _processService.Open("ms-settings:printers", "Ouvrir paramètres imprimantes Windows");
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }
}
