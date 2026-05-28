using System.Collections.ObjectModel;

namespace Carrezance.Support.App.Models;

public sealed class NetworkInfo
{
    public string LocalIpAddress { get; init; } = "Non disponible";
    public string Gateway { get; init; } = "Non disponible";
    public ObservableCollection<string> DnsServers { get; init; } = new();
    public string MacAddress { get; init; } = "Non disponible";
    public string AdapterName { get; init; } = "Non disponible";
}
