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
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public CreatePurchaseViewModel(
        IPurchaseApiService purchaseService,
        IInventoryApiService inventoryService,
        ISupplierApiService supplierService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _purchaseService = purchaseService;
        _inventoryService = inventoryService;
        _supplierService = supplierService;
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

    public ObservableCollection<Inventory> InventoryItems { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();

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

            await Task.WhenAll(inventoryTask, suppliersTask);

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
        VatAmount = Subtotal * 0.22m;
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

        try
        {
            IsLoading = true;

            var request = new CreatePurchaseRequest(
                CompanyId: _authService.CompanyId.Value,
                PeriodId: Guid.NewGuid(), // TODO: Recuperare il periodo corrente
                InventoryId: SelectedInventoryItem.Id,
                VatRateId: Guid.NewGuid(), // TODO: Recuperare l'aliquota IVA
                Quantity: Quantity,
                UnitPrice: UnitPrice,
                SupplierName: SupplierName,
                SupplierVatNumber: SupplierVatNumber
            );

            await _purchaseService.CreatePurchaseAsync(request);
            await _alertService.ShowToastAsync("Acquisto creato con successo");
            await _navigationService.GoBackAsync();
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
        await _navigationService.GoBackAsync();
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

    partial void OnQuantityChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        CalculateTotals();
    }
}