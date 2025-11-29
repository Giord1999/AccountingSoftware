using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Core;
using AccountingApp.Services.Api;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IBIApiService _biService;
    private readonly IAlertService _alertService;

    public DashboardViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IBIApiService biService,
        IAlertService alertService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _biService = biService;
        _alertService = alertService;
    }

    [ObservableProperty]
    private string companyName = "Accounting System";

    [ObservableProperty]
    private decimal totalRevenue;

    [ObservableProperty]
    private decimal totalExpenses;

    [ObservableProperty]
    private decimal netProfit;

    [ObservableProperty]
    private decimal profitMargin;

    [ObservableProperty]
    private bool isLoading;

    public ObservableCollection<ChartData> RevenueChartData { get; } = new();

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var dashboard = await _biService.GenerateDashboardAsync(_authService.CompanyId.Value);

            TotalRevenue = dashboard.KPIs.TotalRevenue;
            TotalExpenses = dashboard.KPIs.TotalExpenses;
            NetProfit = dashboard.KPIs.NetProfit;
            ProfitMargin = dashboard.KPIs.ProfitMargin;

            RevenueChartData.Clear();
            foreach (var item in dashboard.RevenueChart)
            {
                RevenueChartData.Add(item);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento dashboard: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToAccountsAsync()
    {
        await _navigationService.NavigateToAsync("accounts");
    }

    [RelayCommand]
    private async Task NavigateToSalesAsync()
    {
        await _navigationService.NavigateToAsync("sales");
    }

    [RelayCommand]
    private async Task NavigateToPurchasesAsync()
    {
        await _navigationService.NavigateToAsync("purchases");
    }

    [RelayCommand]
    private async Task NavigateToInventoryAsync()
    {
        await _navigationService.NavigateToAsync("inventory");
    }

    [RelayCommand]
    private async Task NavigateToCustomersAsync()
    {
        await _navigationService.NavigateToAsync("customers");
    }

    [RelayCommand]
    private async Task NavigateToSuppliersAsync()
    {
        await _navigationService.NavigateToAsync("suppliers");
    }

    [RelayCommand]
    private async Task NavigateToLeadsAsync()
    {
        await _navigationService.NavigateToAsync("leads");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _alertService.ShowConfirmAsync("Logout", "Sei sicuro di voler uscire?");
        if (confirm)
        {
            await _authService.LogoutAsync();
            await _navigationService.NavigateToLoginAsync();
        }
    }
}