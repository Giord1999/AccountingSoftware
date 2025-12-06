using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class InventoryDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IInventoryApiService _inventoryService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public InventoryDetailViewModel(
        IInventoryApiService inventoryService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _inventoryService = inventoryService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private Guid? inventoryId;

    [ObservableProperty]
    private string itemCode = string.Empty;

    [ObservableProperty]
    private string itemName = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private string? category;

    [ObservableProperty]
    private string unitOfMeasure = "PZ";

    [ObservableProperty]
    private decimal quantityOnHand;

    [ObservableProperty]
    private decimal? reorderLevel;

    [ObservableProperty]
    private decimal unitCost;

    [ObservableProperty]
    private decimal? salePrice;

    [ObservableProperty]
    private string currency = "EUR";

    [ObservableProperty]
    private bool isActive = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var id))
        {
            InventoryId = id;
            IsEditMode = true;
        }

        if (query.TryGetValue("mode", out var modeObj) && modeObj?.ToString() == "create")
        {
            IsEditMode = false;
            InventoryId = null;
        }
    }

    public async Task InitializeAsync()
    {
        if (IsEditMode && InventoryId.HasValue)
        {
            await LoadInventoryItemAsync();
        }
    }

    [RelayCommand]
    private async Task LoadInventoryItemAsync()
    {
        if (!InventoryId.HasValue || !_authService.CompanyId.HasValue) return;

        try
        {
            IsLoading = true;

            var item = await _inventoryService.GetInventoryItemByIdAsync(InventoryId.Value);

            if (item != null)
            {
                ItemCode = item.ItemCode;
                ItemName = item.ItemName;
                Description = item.Description;
                Category = item.Category;
                UnitOfMeasure = item.UnitOfMeasure;
                QuantityOnHand = item.QuantityOnHand;
                ReorderLevel = item.ReorderLevel;
                UnitCost = item.UnitCost;
                SalePrice = item.SalePrice;
                Currency = item.Currency;
                IsActive = item.IsActive;
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento articolo: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveInventoryItemAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        if (string.IsNullOrWhiteSpace(ItemCode) || string.IsNullOrWhiteSpace(ItemName))
        {
            await _alertService.ShowAlertAsync("Errore", "Codice e Nome sono obbligatori");
            return;
        }

        try
        {
            IsLoading = true;

            var item = new Inventory
            {
                Id = InventoryId ?? Guid.NewGuid(),
                CompanyId = _authService.CompanyId.Value,
                ItemCode = ItemCode,
                ItemName = ItemName,
                Description = Description,
                Category = Category,
                UnitOfMeasure = UnitOfMeasure,
                QuantityOnHand = QuantityOnHand,
                ReorderLevel = ReorderLevel,
                UnitCost = UnitCost,
                SalePrice = SalePrice,
                Currency = Currency,
                IsActive = IsActive
            };

            if (IsEditMode && InventoryId.HasValue)
            {
                await _inventoryService.UpdateInventoryItemAsync(InventoryId.Value, item);
                await _alertService.ShowToastAsync("Articolo aggiornato con successo");
            }
            else
            {
                await _inventoryService.CreateInventoryItemAsync(item);
                await _alertService.ShowToastAsync("Articolo creato con successo");
            }

            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore salvataggio articolo: {ex.Message}");
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
}