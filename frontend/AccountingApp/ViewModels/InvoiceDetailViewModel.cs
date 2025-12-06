using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class InvoiceDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IInvoiceApiService _invoiceService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public InvoiceDetailViewModel(
        IInvoiceApiService invoiceService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _invoiceService = invoiceService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private Guid? invoiceId;

    [ObservableProperty]
    private Invoice? invoice;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && Guid.TryParse(value?.ToString(), out var id))
        {
            InvoiceId = id;
        }
    }

    public async Task InitializeAsync()
    {
        if (InvoiceId.HasValue)
        {
            await LoadInvoiceAsync();
        }
    }

    [RelayCommand]
    private async Task LoadInvoiceAsync()
    {
        if (!InvoiceId.HasValue) return;

        try
        {
            IsLoading = true;

            // Verifica autenticazione usando le proprietà di IAuthService
            if (!_authService.IsAuthenticated || !await _authService.ValidateTokenAsync())
            {
                await _alertService.ShowAlertAsync("Errore", "Sessione scaduta. Effettua nuovamente l'accesso.");
                await _navigationService.NavigateToAsync("LoginPage");
                return;
            }

            Invoice = await _invoiceService.GetInvoiceByIdAsync(InvoiceId.Value);
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento fattura: {ex.Message}");
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
}