using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class PurchaseDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPurchaseApiService _purchaseService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public PurchaseDetailViewModel(
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
    private Guid? purchaseId;

    [ObservableProperty]
    private Purchase? purchase;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && Guid.TryParse(value?.ToString(), out var id))
        {
            PurchaseId = id;
        }
    }

    public async Task InitializeAsync()
    {
        if (!await EnsureAuthenticatedAsync()) return;

        if (PurchaseId.HasValue)
        {
            await LoadPurchaseAsync();
        }
    }

    [RelayCommand]
    private async Task LoadPurchaseAsync()
    {
        if (!PurchaseId.HasValue) return;
        if (!await EnsureAuthenticatedAsync()) return;

        try
        {
            IsLoading = true;
            Purchase = await _purchaseService.GetPurchaseByIdAsync(PurchaseId.Value);
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento acquisto: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelPurchaseAsync()
    {
        if (Purchase == null) return;
        if (!await EnsureAuthenticatedAsync()) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Annullamento",
            "Sei sicuro di voler annullare questo acquisto?");

        if (!confirm) return;

        try
        {
            IsLoading = true;
            Purchase = await _purchaseService.CancelPurchaseAsync(Purchase.Id, "Annullamento da dettaglio acquisto");
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
    private async Task GoBackAsync()
    {
        await _navigationService.NavigateBackAsync();
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        if (!_authService.IsAuthenticated || !await _authService.ValidateTokenAsync())
        {
            await _alertService.ShowAlertAsync("Sessione scaduta", "Effettua nuovamente il login.");
            await _navigationService.NavigateToLoginAsync();
            return false;
        }
        return true;
    }
}