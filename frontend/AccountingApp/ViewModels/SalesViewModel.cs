using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ISalesApiService _salesService;
    private readonly IInventoryApiService _inventoryService;
    private readonly ICustomerApiService _customerService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public SalesViewModel(
        ISalesApiService salesService,
        IInventoryApiService inventoryService,
        ICustomerApiService customerService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _salesService = salesService;
        _inventoryService = inventoryService;
        _customerService = customerService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    [ObservableProperty]
    private string? _customerSearchQuery;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    public ObservableCollection<Sale> Sales { get; } = [];

    public ObservableCollection<Customer> Customers { get; } = [];

    public ObservableCollection<Inventory> InventoryItems { get; } = [];

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var sales = await _salesService.GetSalesByCompanyAsync(
                _authService.CompanyId.Value,
                FromDate,
                ToDate);

            Sales.Clear();
            foreach (var sale in sales)
            {
                Sales.Add(sale);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento vendite: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        if (!_authService.CompanyId.HasValue) return;

        try
        {
            var customers = await _customerService.GetCustomersByCompanyAsync(
                _authService.CompanyId.Value,
                CustomerSearchQuery);

            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento clienti: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadInventoryAsync()
    {
        if (!_authService.CompanyId.HasValue) return;

        try
        {
            var items = await _inventoryService.GetInventoryItemsByCompanyAsync(_authService.CompanyId.Value);

            InventoryItems.Clear();
            foreach (var item in items)
            {
                InventoryItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento inventario: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SearchCustomersAsync(string? query)
    {
        if (!_authService.CompanyId.HasValue || string.IsNullOrWhiteSpace(query)) return;

        try
        {
            // Usa GetCustomersByCompanyAsync con il parametro search
            var customers = await _customerService.GetCustomersByCompanyAsync(_authService.CompanyId.Value, query);

            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore ricerca clienti: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FilterSalesByCustomerAsync()
    {
        if (SelectedCustomer == null)
        {
            await LoadSalesAsync();
            return;
        }

        if (!_authService.CompanyId.HasValue) return;

        try
        {
            IsLoading = true;

            var allSales = await _salesService.GetSalesByCompanyAsync(
                _authService.CompanyId.Value,
                FromDate,
                ToDate);

            var filteredSales = allSales.Where(s => s.CustomerId == SelectedCustomer.Id);

            Sales.Clear();
            foreach (var sale in filteredSales)
            {
                Sales.Add(sale);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore filtro vendite: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshSalesAsync()
    {
        IsRefreshing = true;
        await LoadSalesAsync();
    }

    [RelayCommand]
    private async Task CreateSaleAsync()
    {
        await _navigationService.NavigateToAsync("createsale");
    }

    [RelayCommand]
    private async Task ViewSaleDetailsAsync(Sale sale)
    {
        if (sale == null) return;
        await _navigationService.NavigateToAsync($"saledetail?id={sale.Id}");
    }

    [RelayCommand]
    private async Task CancelSaleAsync(Sale sale)
    {
        if (sale == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Annullamento",
            $"Sei sicuro di voler annullare la vendita?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            var result = await _salesService.CancelSaleAsync(sale.Id, "Annullamento da interfaccia utente");

            var index = Sales.IndexOf(sale);
            if (index >= 0)
            {
                Sales[index] = result;
            }

            await _alertService.ShowToastAsync("Vendita annullata con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore annullamento vendita: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyDateFilterAsync()
    {
        await LoadSalesAsync();
    }

    [RelayCommand]
    private void ClearDateFilter()
    {
        FromDate = null;
        ToDate = null;
    }

    [RelayCommand]
    private void ClearCustomerFilter()
    {
        SelectedCustomer = null;
        CustomerSearchQuery = null;
    }
}