using System.Windows;
using System.Threading.Tasks;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App;

public partial class App : Application
{
    private static bool IsShuttingDown { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            CrashLogService.LogUnhandledException("DispatcherUnhandledException", args.Exception);
            MessageBox.Show(
                "Une erreur inattendue a été enregistrée dans les logs. L'application va tenter de continuer.",
                "Carrezance Support",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (IsIgnorableNativeShutdownException(args.ExceptionObject))
            {
                CrashLogService.LogInfo("AppDomain.UnhandledException", "Exception native ignorée pendant la fermeture de l'application.");
                return;
            }

            CrashLogService.LogUnhandledException("AppDomain.UnhandledException", args.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            CrashLogService.LogUnhandledException("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IsShuttingDown = true;
        base.OnExit(e);
    }

    private static bool IsIgnorableNativeShutdownException(object? exceptionObject)
    {
        if (!IsShuttingDown || exceptionObject is not DllNotFoundException exception)
        {
            return false;
        }

        var text = exception.ToString();
        return text.Contains("__std_type_info_destroy_list", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("__scrt_uninitialize_type_info", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("_app_exit_callback", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ModuleUninitializer.SingletonDomainUnload", StringComparison.OrdinalIgnoreCase);
    }
}
