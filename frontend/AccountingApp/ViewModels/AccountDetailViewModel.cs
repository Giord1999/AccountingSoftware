using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class AccountDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAccountService _accountService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public AccountDetailViewModel(
        IAccountService accountService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _accountService = accountService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private Guid? accountId;

    [ObservableProperty]
    private string code = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private AccountCategory category;

    [ObservableProperty]
    private string currency = "EUR";

    [ObservableProperty]
    private bool isPostedRestricted;

    [ObservableProperty]
    private Guid? parentAccountId;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idValue) && Guid.TryParse(idValue?.ToString(), out var id))
        {
            AccountId = id;
            IsEditMode = true;
        }

        if (query.TryGetValue("mode", out var modeValue) && modeValue?.ToString() == "create")
        {
            IsEditMode = false;
            AccountId = null;
        }
    }

    public async Task InitializeAsync()
    {
        if (IsEditMode && AccountId.HasValue)
        {
            await LoadAccountAsync();
        }
    }

    [RelayCommand]
    private async Task LoadAccountAsync()
    {
        if (!AccountId.HasValue || !_authService.CompanyId.HasValue) return;

        try
        {
            IsLoading = true;

            var account = await _accountService.GetAccountByIdAsync(AccountId.Value);

            if (account != null)
            {
                Code = account.Code;
                Name = account.Name;
                Category = account.Category;
                Currency = account.Currency;
                IsPostedRestricted = account.IsPostedRestricted;
                ParentAccountId = account.ParentAccountId;
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento conto: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            await _alertService.ShowAlertAsync("Errore", "Codice e Nome sono obbligatori");
            return;
        }

        try
        {
            IsLoading = true;

            var account = new Account
            {
                Id = AccountId ?? Guid.NewGuid(),
                CompanyId = _authService.CompanyId.Value,
                Code = Code,
                Name = Name,
                Category = Category,
                Currency = Currency,
                IsPostedRestricted = IsPostedRestricted,
                ParentAccountId = ParentAccountId,
                CreatedBy = _authService.UserId ?? "system"
            };

            if (IsEditMode && AccountId.HasValue)
            {
                await _accountService.UpdateAccountAsync(AccountId.Value, account);
                await _alertService.ShowToastAsync("Conto aggiornato con successo");
            }
            else
            {
                await _accountService.CreateAccountAsync(account);
                await _alertService.ShowToastAsync("Conto creato con successo");
            }

            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore salvataggio conto: {ex.Message}");
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