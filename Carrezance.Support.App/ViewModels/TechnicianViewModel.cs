using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class TechnicianViewModel
{
    private readonly ProcessService _processService;

    public TechnicianViewModel(ProcessService processService)
    {
        _processService = processService;
        OpenServicesCommand = new RelayCommand(() => OpenTool("services.msc", "Ouvrir Services Windows"));
        OpenEventViewerCommand = new RelayCommand(() => OpenTool("eventvwr.msc", "Ouvrir Observateur d'événements"));
        OpenNetworkConnectionsCommand = new RelayCommand(() => OpenTool("ncpa.cpl", "Ouvrir Connexions réseau"));
        OpenProgramsCommand = new RelayCommand(() => OpenTool("appwiz.cpl", "Ouvrir Programmes et fonctionnalités"));
        OpenComputerManagementCommand = new RelayCommand(() => OpenTool("compmgmt.msc", "Ouvrir Gestion de l'ordinateur"));
    }

    public ICommand OpenServicesCommand { get; }
    public ICommand OpenEventViewerCommand { get; }
    public ICommand OpenNetworkConnectionsCommand { get; }
    public ICommand OpenProgramsCommand { get; }
    public ICommand OpenComputerManagementCommand { get; }

    private void OpenTool(string target, string actionName)
    {
        var result = _processService.Open(target, actionName);
        if (!result.Success)
        {
            MessageHelper.ShowResult(result);
        }
    }
}
