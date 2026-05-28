using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Carrezance.Support.App.Helpers;
using Carrezance.Support.App.Models;
using Carrezance.Support.App.Services;

namespace Carrezance.Support.App.ViewModels;

public sealed class NetworkViewModel : ObservableObject
{
    private readonly NetworkService _networkService;
    private readonly SystemInfoService _systemInfoService;
    private readonly RepairActionHistoryService _historyService;
    private string _configuration = "Cliquez sur \"Afficher la configuration réseau\".";
    private string _actionStatus = "Prêt";
    private string _lastActionBadge = "Non exécuté";

    public NetworkViewModel(NetworkService networkService, SystemInfoService systemInfoService, RepairActionHistoryService historyService)
    {
        _networkService = networkService;
        _systemInfoService = systemInfoService;
        _historyService = historyService;
        Results = new ObservableCollection<DiagnosticResult>();
        TestInternetCommand = new AsyncRelayCommand(TestInternetAsync);
        FlushDnsCommand = new AsyncRelayCommand(FlushDnsAsync);
        ShowConfigurationCommand = new RelayCommand(ShowConfiguration);
    }

    public ObservableCollection<DiagnosticResult> Results { get; }
    public string Configuration
    {
        get => _configuration;
        private set => SetProperty(ref _configuration, value);
    }

    public ICommand TestInternetCommand { get; }
    public ICommand FlushDnsCommand { get; }
    public ICommand ShowConfigurationCommand { get; }
    public string ActionStatus { get => _actionStatus; private set => SetProperty(ref _actionStatus, value); }
    public string LastActionBadge { get => _lastActionBadge; private set => SetProperty(ref _lastActionBadge, value); }

    private async Task TestInternetAsync()
    {
        Results.Clear();
        foreach (var result in await _networkService.TestInternetAsync())
        {
            Results.Add(result);
        }

        ActionStatus = GetInternetSummary(Results);
        LastActionBadge = Results.Any(result => result.Status == "OK") ? "Succès" : "Échec";
    }

    private async Task FlushDnsAsync()
    {
        if (!MessageHelper.Confirm("Cette action vide le cache DNS de Windows. Elle peut résoudre certains problèmes d'accès aux sites web. Voulez-vous continuer ?"))
        {
            _historyService.AddCanceled("Réparer l'accès aux sites web", "Internet & Réseau", "Action annulée par l'utilisateur.");
            LastActionBadge = "Annulé";
            ActionStatus = "Action annulée.";
            return;
        }

        ActionStatus = "Action en cours...";
        var result = await _networkService.FlushDnsAsync();
        LastActionBadge = result.Success ? "Succès" : "Échec";
        ActionStatus = result.Message;
        MessageHelper.ShowResult(result);
    }

    private void ShowConfiguration()
    {
        var info = _systemInfoService.GetNetworkInfo();
        var dns = info.DnsServers.Count == 0 ? "Non disponible" : string.Join(", ", info.DnsServers);
        Configuration = $"Carte : {info.AdapterName}{Environment.NewLine}IP locale : {info.LocalIpAddress}{Environment.NewLine}Passerelle : {info.Gateway}{Environment.NewLine}DNS : {dns}{Environment.NewLine}MAC : {info.MacAddress}";
    }

    private static string GetInternetSummary(IEnumerable<DiagnosticResult> results)
    {
        var values = results.ToArray();
        if (values.Length == 0)
        {
            return "Test impossible";
        }

        var pingOk = values.Any(result => result.Name.Contains("Ping", StringComparison.OrdinalIgnoreCase) && result.Status == "OK");
        var dnsOk = values.Any(result => result.Name.Contains("DNS", StringComparison.OrdinalIgnoreCase) && result.Status == "OK");
        if (pingOk && dnsOk)
        {
            return "Connexion OK";
        }

        if (pingOk && !dnsOk)
        {
            return "DNS à vérifier";
        }

        return "Connexion indisponible";
    }
}
