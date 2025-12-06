using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class BIReportsViewModel : ObservableObject
{
    private readonly IBIApiService _biService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;

    public BIReportsViewModel(
        IBIApiService biService,
        IAuthService authService,
        IAlertService alertService)
    {
        _biService = biService;
        _authService = authService;
        _alertService = alertService;

        // Default date range: last 12 months
        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddMonths(-12);
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;

    [ObservableProperty]
    private decimal totalRevenue;

    [ObservableProperty]
    private decimal totalExpenses;

    [ObservableProperty]
    private decimal netProfit;

    [ObservableProperty]
    private decimal profitMargin;

    [ObservableProperty]
    private decimal rOI;

    [ObservableProperty]
    private decimal cashFlowRatio;

    [ObservableProperty]
    private int transactionCount;

    public ObservableCollection<ChartData> RevenueChartData { get; } = new();
    public ObservableCollection<TrendData> RevenueTrendData { get; } = new();

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

            var dashboard = await _biService.GenerateDashboardAsync(
                _authService.CompanyId.Value,
                null,
                StartDate,
                EndDate);

            // Update KPIs
            TotalRevenue = dashboard.KPIs.TotalRevenue;
            TotalExpenses = dashboard.KPIs.TotalExpenses;
            NetProfit = dashboard.KPIs.NetProfit;
            ProfitMargin = dashboard.KPIs.ProfitMargin;
            ROI = dashboard.KPIs.ROI;
            CashFlowRatio = dashboard.KPIs.CashFlowRatio;
            TransactionCount = dashboard.KPIs.TransactionCount;

            // Update Charts
            RevenueChartData.Clear();
            foreach (var item in dashboard.RevenueChart)
            {
                RevenueChartData.Add(item);
            }

            RevenueTrendData.Clear();
            foreach (var item in dashboard.RevenueTrend)
            {
                RevenueTrendData.Add(item);
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
    private async Task ApplyDateRangeAsync()
    {
        if (StartDate > EndDate)
        {
            await _alertService.ShowAlertAsync("Errore", "La data di inizio deve essere precedente alla data di fine");
            return;
        }

        await LoadDashboardAsync();
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        await _alertService.ShowToastAsync("Funzionalità di export in sviluppo");
    }
}