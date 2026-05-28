using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Carrezance.Support.App.Models;

namespace Carrezance.Support.App.Services;

public sealed class LogService
{
    private readonly string _logPath = Path.Combine(AppContext.BaseDirectory, "CarrezanceSupport.log");
    private readonly object _fileLock = new();

    public ObservableCollection<LogEntry> Entries { get; } = new();
    public string LogPath => _logPath;
    public string LogFolder => Path.GetDirectoryName(_logPath) ?? AppContext.BaseDirectory;

    public void Log(string action, string result, string? error = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            UserName = Environment.UserName,
            Action = action,
            Result = result,
            Error = error
        };

        AddEntryOnUiThread(entry);

        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logPath, entry.DisplayText + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never block support actions.
        }
    }

    private void AddEntryOnUiThread(LogEntry entry)
    {
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                Entries.Insert(0, entry);
                return;
            }

            dispatcher.BeginInvoke(() =>
            {
                try
                {
                    Entries.Insert(0, entry);
                }
                catch
                {
                    // UI log display must never block diagnostic work.
                }
            });
        }
        catch
        {
            // Logging must never crash the application.
        }
    }
}
