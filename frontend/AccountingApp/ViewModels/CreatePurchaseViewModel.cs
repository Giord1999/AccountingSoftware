using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class CreatePurchaseViewModel : ObservableObject
{
    private readonly IPurchaseApiService _purchaseService;
    private readonly IInventoryApiService _inventoryService;
    private readonly ISupplierApiService _supplierService;
    private readonly IAccountingPeriodApiService _periodService;
    private readonly IVatRateApiService _vatRateService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public CreatePurchaseViewModel(
        IPurchaseApiService purchaseService,
        IInventoryApiService inventoryService,
        ISupplierApiService supplierService,
        IAccountingPeriodApiService periodService,
        IVatRateApiService vatRateService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _purchaseService = purchaseService;
        _inventoryService = inventoryService;
        _supplierService = supplierService;
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
    private Supplier? selectedSupplier;

    [ObservableProperty]
    private AccountingPeriod? selectedPeriod;

    [ObservableProperty]
    private VatRate? selectedVatRate;

    [ObservableProperty]
    private decimal quantity = 1;

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private string supplierName = string.Empty;

    [ObservableProperty]
    private string? supplierVatNumber;

    [ObservableProperty]
    private decimal subtotal;

    [ObservableProperty]
    private decimal vatAmount;

    [ObservableProperty]
    private decimal totalAmount;

    public ObservableCollection<Inventory> InventoryItems { get; } = [];
    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<AccountingPeriod> Periods { get; } = [];
    public ObservableCollection<VatRate> VatRates { get; } = [];

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
            var suppliersTask = _supplierService.GetSuppliersByCompanyAsync(_authService.CompanyId.Value);
            var periodsTask = _periodService.GetPeriodsByCompanyAsync(_authService.CompanyId.Value);
            var vatRatesTask = _vatRateService.GetAllVatRatesAsync();

            await Task.WhenAll(inventoryTask, suppliersTask, periodsTask, vatRatesTask);

            InventoryItems.Clear();
            foreach (var item in await inventoryTask)
            {
                InventoryItems.Add(item);
            }

            Suppliers.Clear();
            foreach (var supplier in await suppliersTask)
            {
                Suppliers.Add(supplier);
            }

            Periods.Clear();
            var periods = (await periodsTask).Where(p => !p.IsClosed).OrderByDescending(p => p.Start);
            foreach (var period in periods)
            {
                Periods.Add(period);
            }

            // Seleziona automaticamente il periodo corrente (quello che include la data odierna)
            SelectedPeriod = Periods.FirstOrDefault(p =>
                DateTime.UtcNow >= p.Start && DateTime.UtcNow <= p.End)
                ?? Periods.FirstOrDefault();

            VatRates.Clear();
            foreach (var vatRate in await vatRatesTask)
            {
                VatRates.Add(vatRate);
            }

            // Seleziona l'aliquota IVA standard al 22% se disponibile
            SelectedVatRate = VatRates.FirstOrDefault(v => v.Rate == 22m)
                ?? VatRates.FirstOrDefault();
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
        var vatRate = SelectedVatRate?.Rate ?? 22m;
        VatAmount = Subtotal * (vatRate / 100m);
        TotalAmount = Subtotal + VatAmount;
    }

    [RelayCommand]
    private async Task CreatePurchaseAsync()
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

        if (string.IsNullOrWhiteSpace(SupplierName))
        {
            await _alertService.ShowAlertAsync("Errore", "Inserisci il nome del fornitore");
            return;
        }

        if (SelectedPeriod == null)
        {
            await _alertService.ShowAlertAsync("Errore", "Seleziona un periodo contabile");
            return;
        }

        if (SelectedVatRate == null)
        {
            await _alertService.ShowAlertAsync("Errore", "Seleziona un'aliquota IVA");
            return;
        }

        try
        {
            IsLoading = true;

            var request = new CreatePurchaseRequest(
                CompanyId: _authService.CompanyId.Value,
                PeriodId: SelectedPeriod.Id,
                InventoryId: SelectedInventoryItem.Id,
                VatRateId: SelectedVatRate.Id,
                Quantity: Quantity,
                UnitPrice: UnitPrice,
                SupplierName: SupplierName,
                SupplierVatNumber: SupplierVatNumber
            );

            await _purchaseService.CreatePurchaseAsync(request);
            await _alertService.ShowToastAsync("Acquisto creato con successo");
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore creazione acquisto: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _navigationService.NavigateBackAsync();
    }

    partial void OnSelectedInventoryItemChanged(Inventory? value)
    {
        if (value != null)
        {
            UnitPrice = value.UnitCost;
            CalculateTotals();
        }
    }

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value != null)
        {
            SupplierName = value.Name;
            SupplierVatNumber = value.VatNumber;
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