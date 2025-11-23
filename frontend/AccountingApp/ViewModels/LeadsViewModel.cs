using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class LeadsViewModel : ObservableObject
{
    private readonly ILeadApiService _leadService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public LeadsViewModel(
        ILeadApiService leadService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _leadService = leadService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private LeadStatus? statusFilter;

    public ObservableCollection<Lead> Leads { get; } = new();

    [RelayCommand]
    private async Task LoadLeadsAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var leads = await _leadService.GetLeadsByCompanyAsync(
                _authService.CompanyId.Value,
                StatusFilter);

            Leads.Clear();
            foreach (var lead in leads)
            {
                Leads.Add(lead);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento lead: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLeadsAsync()
    {
        IsRefreshing = true;
        await LoadLeadsAsync();
    }

    [RelayCommand]
    private async Task AddLeadAsync()
    {
        await _navigationService.NavigateToAsync("leaddetail?mode=create");
    }

    [RelayCommand]
    private async Task EditLeadAsync(Lead lead)
    {
        if (lead == null) return;
        await _navigationService.NavigateToAsync($"leaddetail?id={lead.Id}&mode=edit");
    }

    [RelayCommand]
    private async Task QualifyLeadAsync(Lead lead)
    {
        if (lead == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Qualifica Lead",
            $"Qualificare il lead '{lead.Name}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            var result = await _leadService.QualifyLeadAsync(lead.Id);
            
            var index = Leads.IndexOf(lead);
            if (index >= 0)
            {
                Leads[index] = result;
            }

            await _alertService.ShowToastAsync("Lead qualificato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore qualifica lead: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConvertLeadAsync(Lead lead)
    {
        if (lead == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Converti Lead",
            $"Convertire il lead '{lead.Name}' in cliente?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            var result = await _leadService.ConvertLeadToCustomerAsync(lead.Id);
            
            var index = Leads.IndexOf(lead);
            if (index >= 0)
            {
                Leads[index] = result;
            }

            await _alertService.ShowToastAsync("Lead convertito in cliente con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore conversione lead: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteLeadAsync(Lead lead)
    {
        if (lead == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il lead '{lead.Name}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            await _leadService.DeleteLeadAsync(lead.Id);
            Leads.Remove(lead);

            await _alertService.ShowToastAsync("Lead eliminato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore eliminazione lead: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyStatusFilterAsync()
    {
        await LoadLeadsAsync();
    }

    [RelayCommand]
    private async Task ClearStatusFilterAsync()
    {
        StatusFilter = null;
        await LoadLeadsAsync();
    }
}