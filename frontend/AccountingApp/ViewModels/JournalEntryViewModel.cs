using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class JournalEntryViewModel : ObservableObject
{
    private readonly IAccountingService _accountingService;
    private readonly IAccountService _accountService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public JournalEntryViewModel(
        IAccountingService accountingService,
        IAccountService accountService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _accountingService = accountingService;
        _accountService = accountService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private DateTime date = DateTime.Today;

    [ObservableProperty]
    private string reference = string.Empty;

    [ObservableProperty]
    private decimal totalDebit;

    [ObservableProperty]
    private decimal totalCredit;

    [ObservableProperty]
    private bool isBalanced;

    public ObservableCollection<JournalLine> Lines { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        if (!_authService.CompanyId.HasValue) return;

        try
        {
            var accounts = await _accountService.GetAccountsByCompanyAsync(_authService.CompanyId.Value);
            
            Accounts.Clear();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento conti: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        Lines.Add(new JournalLine
        {
            Id = Guid.NewGuid(),
            Narrative = string.Empty
        });

        CalculateTotals();
    }

    [RelayCommand]
    private void RemoveLine(JournalLine line)
    {
        if (line == null) return;
        Lines.Remove(line);
        CalculateTotals();
    }

    [RelayCommand]
    private void CalculateTotals()
    {
        TotalDebit = Lines.Sum(l => l.Debit);
        TotalCredit = Lines.Sum(l => l.Credit);
        IsBalanced = TotalDebit == TotalCredit;
    }

    [RelayCommand]
    private async Task SaveJournalAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            await _alertService.ShowAlertAsync("Errore", "Inserire una descrizione");
            return;
        }

        if (!Lines.Any())
        {
            await _alertService.ShowAlertAsync("Errore", "Aggiungere almeno una riga");
            return;
        }

        if (!IsBalanced)
        {
            await _alertService.ShowAlertAsync("Errore", "La registrazione non è in pareggio");
            return;
        }

        try
        {
            IsLoading = true;

            var journalEntry = new JournalEntry
            {
                CompanyId = _authService.CompanyId.Value,
                PeriodId = Guid.Empty, // TODO: Get from period selector
                Description = Description,
                Date = Date,
                Reference = Reference,
                Currency = "EUR",
                Lines = Lines.ToList()
            };

            await _accountingService.CreateJournalAsync(journalEntry);

            await _alertService.ShowToastAsync("Registrazione contabile creata con successo");
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore creazione registrazione: {ex.Message}");
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