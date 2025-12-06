using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class CreateSaleViewModel : ObservableObject
{
    private readonly ISalesApiService _salesService;
    private readonly IInventoryApiService _inventoryService;
    private readonly ICustomerApiService _customerService;
    private readonly IAccountingPeriodApiService _periodService;
    private readonly IVatRateApiService _vatRateService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public CreateSaleViewModel(
        ISalesApiService salesService,
        IInventoryApiService inventoryService,
        ICustomerApiService customerService,
        IAccountingPeriodApiService periodService,
        IVatRateApiService vatRateService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _salesService = salesService;
        _inventoryService = inventoryService;
        _customerService = customerService;
        _periodService = periodService;
        _vatRateService = vatRateService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private Inventory? selectedInventoryItem;

    [ObservableProperty]
    private Customer? selectedCustomer;

    [ObservableProperty]
    private decimal quantity = 1;

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private string customerName = string.Empty;

    [ObservableProperty]
    private string? customerVatNumber;

    [ObservableProperty]
    private decimal subtotal;

    [ObservableProperty]
    private decimal vatAmount;

    [ObservableProperty]
    private decimal totalAmount;

    public ObservableCollection<Inventory> InventoryItems { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var inventoryTask = _inventoryService.GetInventoryItemsByCompanyAsync(_authService.CompanyId.Value);
            var customersTask = _customerService.GetCustomersByCompanyAsync(_authService.CompanyId.Value);

            await Task.WhenAll(inventoryTask, customersTask);

            InventoryItems.Clear();
            foreach (var item in await inventoryTask)
            {
                InventoryItems.Add(item);
            }

            Customers.Clear();
            foreach (var customer in await customersTask)
            {
                Customers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento dati: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CalculateTotals()
    {
        Subtotal = Quantity * UnitPrice;
        VatAmount = Subtotal * 0.22m; // Default 22% IVA
        TotalAmount = Subtotal + VatAmount;
    }

    [RelayCommand]
    private async Task CreateSaleAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        if (SelectedInventoryItem == null)
        {
            await _alertService.ShowAlertAsync("Errore", "Seleziona un articolo");
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            await _alertService.ShowAlertAsync("Errore", "Inserisci il nome del cliente");
            return;
        }

        try
        {
            IsLoading = true;

            // Qui dovresti implementare la logica per ottenere PeriodId e VatRateId
            // Per ora uso valori di placeholder
            var request = new CreateSaleRequest(
                CompanyId: _authService.CompanyId.Value,
                PeriodId: Guid.NewGuid(), // TODO: Recuperare il periodo corrente
                InventoryId: SelectedInventoryItem.Id,
                VatRateId: Guid.NewGuid(), // TODO: Recuperare l'aliquota IVA
                Quantity: Quantity,
                UnitPrice: UnitPrice,
                CustomerName: CustomerName,
                CustomerVatNumber: CustomerVatNumber
            );

            await _salesService.CreateSaleAsync(request);
            await _alertService.ShowToastAsync("Vendita creata con successo");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore creazione vendita: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _navigationService.GoBackAsync();
    }

    partial void OnSelectedInventoryItemChanged(Inventory? value)
    {
        if (value != null)
        {
            UnitPrice = value.SalePrice ?? value.UnitCost;
            CalculateTotals();
        }
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null)
        {
            CustomerName = value.Name;
            CustomerVatNumber = value.VatNumber;
        }
    }

    partial void OnQuantityChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        CalculateTotals();
    }
}