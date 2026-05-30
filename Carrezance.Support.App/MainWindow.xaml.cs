using System.Windows;
using Carrezance.Support.App.Services;
using Carrezance.Support.App.ViewModels;

namespace Carrezance.Support.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var logService = new LogService();
        var repairActionHistoryService = new RepairActionHistoryService(logService);
        var adminService = new AdminService(logService);
        var processService = new ProcessService(logService);
        var systemInfoService = new SystemInfoService(logService, adminService);
        var networkService = new NetworkService(logService, processService, repairActionHistoryService);
        var printerService = new PrinterService(logService, processService, adminService, repairActionHistoryService);
        var officeService = new OfficeService(logService, processService, repairActionHistoryService);
        var cleaningService = new CleaningService(logService, repairActionHistoryService);
        var reportService = new ReportService(logService, repairActionHistoryService);
        var updateService = new UpdateService(logService);
        var assistanceViewModel = new AssistanceViewModel(systemInfoService, reportService, logService, processService, updateService);

        DataContext = new MainViewModel(
            logService,
            new DashboardViewModel(systemInfoService, networkService, printerService, officeService, cleaningService, reportService, repairActionHistoryService, adminService),
            new DiagnosticViewModel(systemInfoService, reportService, processService),
            new NetworkViewModel(networkService, systemInfoService, repairActionHistoryService),
            new PrinterViewModel(printerService, processService, repairActionHistoryService, adminService),
            new OfficeViewModel(officeService, repairActionHistoryService),
            new CleaningViewModel(cleaningService, systemInfoService, repairActionHistoryService),
            assistanceViewModel,
            new TechnicianViewModel(processService),
            adminService);

        Loaded += async (_, _) => await assistanceViewModel.CheckForUpdatesSilentlyAsync();
    }
}
