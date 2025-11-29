using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class PurchasesViewModel : ObservableObject
{
    private readonly IPurchaseApiService _purchaseService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public PurchasesViewModel(
        IPurchaseApiService purchaseService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _purchaseService = purchaseService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    public ObservableCollection<Purchase> Purchases { get; } = new();

    [RelayCommand]
    private async Task LoadPurchasesAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var purchases = await _purchaseService.GetPurchasesByCompanyAsync(
                _authService.CompanyId.Value,
                FromDate,
                ToDate);

            Purchases.Clear();
            foreach (var purchase in purchases)
            {
                Purchases.Add(purchase);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento acquisti: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPurchasesAsync()
    {
        IsRefreshing = true;
        await LoadPurchasesAsync();
    }

    [RelayCommand]
    private async Task CreatePurchaseAsync()
    {
        await _navigationService.NavigateToAsync("createpurchase");
    }

    [RelayCommand]
    private async Task ViewPurchaseDetailsAsync(Purchase purchase)
    {
        if (purchase == null) return;
        await _navigationService.NavigateToAsync($"purchasedetail?id={purchase.Id}");
    }

    [RelayCommand]
    private async Task CancelPurchaseAsync(Purchase purchase)
    {
        if (purchase == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Annullamento",
            "Sei sicuro di voler annullare l'acquisto?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            var result = await _purchaseService.CancelPurchaseAsync(
                purchase.Id, 
                "Annullamento da interfaccia utente");
            
            var index = Purchases.IndexOf(purchase);
            if (index >= 0)
            {
                Purchases[index] = result;
            }

            await _alertService.ShowToastAsync("Acquisto annullato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore annullamento acquisto: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyDateFilterAsync()
    {
        await LoadPurchasesAsync();
    }

    [RelayCommand]
    private void ClearDateFilter()
    {
        FromDate = null;
        ToDate = null;
    }
}