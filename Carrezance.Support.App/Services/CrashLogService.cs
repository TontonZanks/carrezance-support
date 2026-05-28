using System.IO;

namespace Carrezance.Support.App.Services;

public static class CrashLogService
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "CarrezanceSupport.log");

    public static void LogUnhandledException(string source, Exception exception)
    {
        try
        {
            var message =
                $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] [{Environment.UserName}] [{source}] [Erreur non gérée] {exception}{Environment.NewLine}";
            File.AppendAllText(LogPath, message);
        }
        catch
        {
            // Error logging must never crash the application.
        }
    }

    public static void LogInfo(string source, string messageText)
    {
        try
        {
            var message =
                $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] [{Environment.UserName}] [{source}] [Information] {messageText}{Environment.NewLine}";
            File.AppendAllText(LogPath, message);
        }
        catch
        {
            // Error logging must never crash the application.
        }
    }

    public static void LogUnhandledException(string source, object? exceptionObject)
    {
        var exception = exceptionObject as Exception ?? new Exception(exceptionObject?.ToString() ?? "Erreur inconnue");
        LogUnhandledException(source, exception);
    }
}
