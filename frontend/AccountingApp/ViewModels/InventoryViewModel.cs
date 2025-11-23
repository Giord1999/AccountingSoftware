using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IInventoryApiService _inventoryService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public InventoryViewModel(
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
    private bool isRefreshing;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<Inventory> InventoryItems { get; } = new();
    public ObservableCollection<Inventory> FilteredInventoryItems { get; } = new();

    [RelayCommand]
    private async Task LoadInventoryAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var items = await _inventoryService.GetInventoryItemsByCompanyAsync(_authService.CompanyId.Value);

            InventoryItems.Clear();
            FilteredInventoryItems.Clear();

            foreach (var item in items)
            {
                InventoryItems.Add(item);
                FilteredInventoryItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento inventario: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshInventoryAsync()
    {
        IsRefreshing = true;
        await LoadInventoryAsync();
    }

    [RelayCommand]
    private void FilterInventory()
    {
        FilteredInventoryItems.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? InventoryItems
            : InventoryItems.Where(i => 
                i.ItemCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.ItemName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (i.Category != null && i.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        foreach (var item in filtered)
        {
            FilteredInventoryItems.Add(item);
        }
    }

    [RelayCommand]
    private async Task AddInventoryItemAsync()
    {
        await _navigationService.NavigateToAsync("inventorydetail?mode=create");
    }

    [RelayCommand]
    private async Task EditInventoryItemAsync(Inventory item)
    {
        if (item == null) return;
        await _navigationService.NavigateToAsync($"inventorydetail?id={item.Id}&mode=edit");
    }

    [RelayCommand]
    private async Task DeleteInventoryItemAsync(Inventory item)
    {
        if (item == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'articolo '{item.ItemCode} - {item.ItemName}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            await _inventoryService.DeleteInventoryItemAsync(item.Id);
            InventoryItems.Remove(item);
            FilteredInventoryItems.Remove(item);

            await _alertService.ShowToastAsync("Articolo eliminato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore eliminazione articolo: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterInventory();
    }
}