using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class SaleDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly ISalesApiService _salesService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public SaleDetailViewModel(
        ISalesApiService salesService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _salesService = salesService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private Guid? saleId;

    [ObservableProperty]
    private Sale? sale;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && Guid.TryParse(value?.ToString(), out var id))
        {
            SaleId = id;
        }
    }

    public async Task InitializeAsync()
    {
        if (!await EnsureAuthenticatedAsync()) return;

        if (SaleId.HasValue)
        {
            await LoadSaleAsync();
        }
    }

    [RelayCommand]
    private async Task LoadSaleAsync()
    {
        if (!SaleId.HasValue) return;
        if (!await EnsureAuthenticatedAsync()) return;

        try
        {
            IsLoading = true;
            Sale = await _salesService.GetSaleByIdAsync(SaleId.Value);
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento vendita: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelSaleAsync()
    {
        if (Sale == null) return;
        if (!await EnsureAuthenticatedAsync()) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Annullamento",
            "Sei sicuro di voler annullare questa vendita?");

        if (!confirm) return;

        try
        {
            IsLoading = true;
            Sale = await _salesService.CancelSaleAsync(Sale.Id, "Annullamento da dettaglio vendita");
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