using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly ISupplierApiService _supplierService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public SuppliersViewModel(
        ISupplierApiService supplierService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _supplierService = supplierService;
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

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var suppliers = await _supplierService.GetSuppliersByCompanyAsync(
                _authService.CompanyId.Value,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento fornitori: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshSuppliersAsync()
    {
        IsRefreshing = true;
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task SearchSuppliersAsync()
    {
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task AddSupplierAsync()
    {
        await _navigationService.NavigateToAsync("supplierdetail?mode=create");
    }

    [RelayCommand]
    private async Task EditSupplierAsync(Supplier supplier)
    {
        if (supplier == null) return;
        await _navigationService.NavigateToAsync($"supplierdetail?id={supplier.Id}&mode=edit");
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(Supplier supplier)
    {
        if (supplier == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il fornitore '{supplier.Name}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            await _supplierService.DeleteSupplierAsync(supplier.Id);
            Suppliers.Remove(supplier);

            await _alertService.ShowToastAsync("Fornitore eliminato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore eliminazione fornitore: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}