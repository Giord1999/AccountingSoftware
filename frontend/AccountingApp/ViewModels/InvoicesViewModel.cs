using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    private readonly IInvoiceApiService _invoiceService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public InvoicesViewModel(
        IInvoiceApiService invoiceService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _invoiceService = invoiceService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private InvoiceType? typeFilter;

    [ObservableProperty]
    private InvoiceStatus? statusFilter;

    public ObservableCollection<Invoice> Invoices { get; } = new();

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var invoices = await _invoiceService.GetInvoicesByCompanyAsync(
                _authService.CompanyId.Value,
                TypeFilter,
                StatusFilter);

            Invoices.Clear();
            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento fatture: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        IsRefreshing = true;
        await LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task ViewInvoiceDetailsAsync(Invoice invoice)
    {
        if (invoice == null) return;
        await _navigationService.NavigateToAsync($"invoicedetail?id={invoice.Id}");
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        await LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        TypeFilter = null;
        StatusFilter = null;
        await LoadInvoicesAsync();
    }
}