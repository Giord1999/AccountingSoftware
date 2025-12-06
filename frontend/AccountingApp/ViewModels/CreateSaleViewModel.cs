using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

/// <summary>
/// Raggruppa i servizi API necessari per la creazione di una vendita.
/// </summary>
public class SaleViewModelServices
{
    public required ISalesApiService SalesService { get; init; }
    public required IInventoryApiService InventoryService { get; init; }
    public required ICustomerApiService CustomerService { get; init; }
    public required IAccountingPeriodApiService PeriodService { get; init; }
    public required IVatRateApiService VatRateService { get; init; }
    public required IAuthService AuthService { get; init; }
    public required IAlertService AlertService { get; init; }
    public required INavigationService NavigationService { get; init; }
}

public partial class CreateSaleViewModel : ObservableObject
{
    private readonly SaleViewModelServices _services;

    public CreateSaleViewModel(SaleViewModelServices services)
    {
        _services = services;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private Inventory? selectedInventoryItem;

    [ObservableProperty]
    private Customer? selectedCustomer;

    [ObservableProperty]
    private AccountingPeriod? selectedPeriod;

    [ObservableProperty]
    private VatRate? selectedVatRate;

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

    public ObservableCollection<Inventory> InventoryItems { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<AccountingPeriod> Periods { get; } = [];
    public ObservableCollection<VatRate> VatRates { get; } = [];

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (!_services.AuthService.CompanyId.HasValue)
        {
            await _services.AlertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var companyId = _services.AuthService.CompanyId.Value;
            var inventoryTask = _services.InventoryService.GetInventoryItemsByCompanyAsync(companyId);
            var customersTask = _services.CustomerService.GetCustomersByCompanyAsync(companyId);
            var periodsTask = _services.PeriodService.GetPeriodsByCompanyAsync(companyId);
            var vatRatesTask = _services.VatRateService.GetAllVatRatesAsync();

            await Task.WhenAll(inventoryTask, customersTask, periodsTask, vatRatesTask);

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

            Periods.Clear();
            var periods = (await periodsTask).Where(p => !p.IsClosed).OrderByDescending(p => p.Start);
            foreach (var period in periods)
            {
                Periods.Add(period);
            }

            SelectedPeriod = Periods.FirstOrDefault(p =>
                DateTime.UtcNow >= p.Start && DateTime.UtcNow <= p.End)
                ?? Periods.FirstOrDefault();

            VatRates.Clear();
            foreach (var vatRate in await vatRatesTask)
            {
                VatRates.Add(vatRate);
            }

            SelectedVatRate = VatRates.FirstOrDefault(v => v.Rate == 22m)
                ?? VatRates.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await _services.AlertService.ShowAlertAsync("Errore", $"Errore caricamento dati: {ex.Message}");
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
        var vatRate = SelectedVatRate?.Rate ?? 22m;
        VatAmount = Subtotal * (vatRate / 100m);
        TotalAmount = Subtotal + VatAmount;
    }

    [RelayCommand]
    private async Task CreateSaleAsync()
    {
        if (!_services.AuthService.CompanyId.HasValue)
        {
            await _services.AlertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        if (SelectedInventoryItem == null)
        {
            await _services.AlertService.ShowAlertAsync("Errore", "Seleziona un articolo");
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            await _services.AlertService.ShowAlertAsync("Errore", "Inserisci il nome del cliente");
            return;
        }

        if (SelectedPeriod == null)
        {
            await _services.AlertService.ShowAlertAsync("Errore", "Seleziona un periodo contabile");
            return;
        }

        if (SelectedVatRate == null)
        {
            await _services.AlertService.ShowAlertAsync("Errore", "Seleziona un'aliquota IVA");
            return;
        }

        try
        {
            IsLoading = true;

            var request = new CreateSaleRequest(
                CompanyId: _services.AuthService.CompanyId.Value,
                PeriodId: SelectedPeriod.Id,
                InventoryId: SelectedInventoryItem.Id,
                VatRateId: SelectedVatRate.Id,
                Quantity: Quantity,
                UnitPrice: UnitPrice,
                CustomerName: CustomerName,
                CustomerVatNumber: CustomerVatNumber
            );

            await _services.SalesService.CreateSaleAsync(request);
            await _services.AlertService.ShowToastAsync("Vendita creata con successo");
            await _services.NavigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _services.AlertService.ShowAlertAsync("Errore", $"Errore creazione vendita: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _services.NavigationService.NavigateBackAsync();
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

    partial void OnSelectedVatRateChanged(VatRate? value)
    {
        CalculateTotals();
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