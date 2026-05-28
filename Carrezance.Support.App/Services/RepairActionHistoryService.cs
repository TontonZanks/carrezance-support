using System.Collections.ObjectModel;
using System.Windows;
using Carrezance.Support.App.Models;

namespace Carrezance.Support.App.Services;

public sealed class RepairActionHistoryService
{
    private readonly LogService _logService;

    public RepairActionHistoryService(LogService logService)
    {
        _logService = logService;
    }

    public ObservableCollection<RepairActionHistoryItem> Items { get; } = new();

    public void Add(RepairActionHistoryItem item)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => Items.Insert(0, item));
            return;
        }

        Items.Insert(0, item);
    }

    public void AddCanceled(string actionName, string category, string message, bool requiresAdmin = false, bool executedAsAdmin = false)
    {
        _logService.Log("[Action]", $"{actionName} annulée");
        Add(new RepairActionHistoryItem
        {
            DateTime = DateTime.Now,
            ActionName = actionName,
            Category = category,
            Status = "Annulé",
            Message = message,
            RequiresAdmin = requiresAdmin,
            ExecutedAsAdmin = executedAsAdmin
        });
    }
}
