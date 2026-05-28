using System.Collections.ObjectModel;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly Dictionary<string, object> _views;
    private readonly AdminService _adminService;
    private object _currentViewModel;

    public MainViewModel(
        LogService logService,
        DashboardViewModel dashboard,
        DiagnosticViewModel diagnostic,
        NetworkViewModel network,
        PrinterViewModel printer,
        OfficeViewModel office,
        CleaningViewModel cleaning,
        AssistanceViewModel assistance,
        TechnicianViewModel technician,
        AdminService adminService)
    {
        _adminService = adminService;
        _views = new Dictionary<string, object>
        {
            ["Dashboard"] = dashboard,
            ["Diagnostic"] = diagnostic,
            ["Network"] = network,
            ["Printer"] = printer,
            ["Office"] = office,
            ["Cleaning"] = cleaning,
            ["Assistance"] = assistance,
            ["Technician"] = technician
        };

        _currentViewModel = dashboard;
        RecentLogs = logService.Entries;
        RepairActions = dashboard.RepairActions;
        NavigateCommand = new RelayCommand(parameter =>
        {
            if (parameter is string key && _views.TryGetValue(key, out var viewModel))
            {
                CurrentViewModel = viewModel;
            }
        });
    }

    public string AppDisplayName => AppInfo.DisplayName;
    public string ExecutionMode => $"Mode actuel : {_adminService.ExecutionMode}";

    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public ObservableCollection<LogEntry> RecentLogs { get; }
    public ObservableCollection<RepairActionHistoryItem> RepairActions { get; }
    public RelayCommand NavigateCommand { get; }
}
