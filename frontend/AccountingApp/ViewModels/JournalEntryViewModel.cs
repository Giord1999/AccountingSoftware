using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class JournalEntryViewModel : ObservableObject
{
    private readonly IAccountingService _accountingService;
    private readonly IAccountService _accountService;
    private readonly IAccountingPeriodService _periodService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public JournalEntryViewModel(
        IAccountingService accountingService,
        IAccountService accountService,
        IAccountingPeriodService periodService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _accountingService = accountingService;
        _accountService = accountService;
        _periodService = periodService;
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

    [ObservableProperty]
    private AccountingPeriod? selectedPeriod;

    public ObservableCollection<JournalLine> Lines { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<AccountingPeriod> Periods { get; } = new();

    partial void OnDateChanged(DateTime value)
    {
        // Auto-seleziona il periodo quando cambia la data
        AutoSelectPeriodForDate(value);
    }

    private void AutoSelectPeriodForDate(DateTime date)
    {
        var matchingPeriod = Periods
            .Where(p => !p.IsClosed && date >= p.Start && date <= p.End)
            .FirstOrDefault();

        if (matchingPeriod != null)
        {
            SelectedPeriod = matchingPeriod;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await Task.WhenAll(LoadAccountsAsync(), LoadPeriodsAsync());
    }

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
    private async Task LoadPeriodsAsync()
    {
        if (!_authService.CompanyId.HasValue) return;

        try
        {
            var periods = await _periodService.GetPeriodsByCompanyAsync(_authService.CompanyId.Value);
            
            Periods.Clear();
            foreach (var period in periods.Where(p => !p.IsClosed).OrderByDescending(p => p.Start))
            {
                Periods.Add(period);
            }

            // Auto-seleziona il periodo per la data corrente
            AutoSelectPeriodForDate(Date);
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento periodi: {ex.Message}");
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

        if (SelectedPeriod == null)
        {
            await _alertService.ShowAlertAsync("Errore", "Selezionare un periodo contabile");
            return;
        }

        if (SelectedPeriod.IsClosed)
        {
            await _alertService.ShowAlertAsync("Errore", "Il periodo selezionato è chiuso");
            return;
        }

        if (Date < SelectedPeriod.Start || Date > SelectedPeriod.End)
        {
            await _alertService.ShowAlertAsync("Errore", 
                $"La data deve essere compresa nel periodo selezionato ({SelectedPeriod.Start:dd/MM/yyyy} - {SelectedPeriod.End:dd/MM/yyyy})");
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
                PeriodId = SelectedPeriod.Id,
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