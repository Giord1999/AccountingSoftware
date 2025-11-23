using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class AccountsViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public AccountsViewModel(
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
    private bool isRefreshing;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Account? selectedAccount;

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<Account> FilteredAccounts { get; } = new();

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var accounts = await _accountService.GetAccountsByCompanyAsync(_authService.CompanyId.Value);

            Accounts.Clear();
            FilteredAccounts.Clear();

            foreach (var account in accounts)
            {
                Accounts.Add(account);
                FilteredAccounts.Add(account);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento conti: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAccountsAsync()
    {
        IsRefreshing = true;
        await LoadAccountsAsync();
    }

    [RelayCommand]
    private void FilterAccounts()
    {
        FilteredAccounts.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Accounts
            : Accounts.Where(a => 
                a.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var account in filtered)
        {
            FilteredAccounts.Add(account);
        }
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        await _navigationService.NavigateToAsync("accountdetail?mode=create");
    }

    [RelayCommand]
    private async Task EditAccountAsync(Account account)
    {
        if (account == null) return;
        await _navigationService.NavigateToAsync($"accountdetail?id={account.Id}&mode=edit");
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(Account account)
    {
        if (account == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il conto '{account.Code} - {account.Name}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            await _accountService.DeleteAccountAsync(account.Id);
            Accounts.Remove(account);
            FilteredAccounts.Remove(account);

            await _alertService.ShowToastAsync("Conto eliminato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore eliminazione conto: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterAccounts();
    }
}