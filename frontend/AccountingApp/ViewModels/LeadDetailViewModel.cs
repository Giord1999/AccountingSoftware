using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class LeadDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly ILeadApiService _leadService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public LeadDetailViewModel(
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
    private bool isEditMode;

    [ObservableProperty]
    private Guid? leadId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? phone;

    [ObservableProperty]
    private LeadSource source = LeadSource.Website;

    [ObservableProperty]
    private LeadStatus status = LeadStatus.New;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("id") && Guid.TryParse(query["id"].ToString(), out var id))
        {
            LeadId = id;
            IsEditMode = true;
        }

        if (query.ContainsKey("mode") && query["mode"].ToString() == "create")
        {
            IsEditMode = false;
            LeadId = null;
        }
    }

    public async Task InitializeAsync()
    {
        if (IsEditMode && LeadId.HasValue)
        {
            await LoadLeadAsync();
        }
    }

    [RelayCommand]
    private async Task LoadLeadAsync()
    {
        if (!LeadId.HasValue) return;

        try
        {
            IsLoading = true;

            var lead = await _leadService.GetLeadByIdAsync(LeadId.Value);

            if (lead != null)
            {
                Name = lead.Name;
                Email = lead.Email;
                Phone = lead.Phone;
                Source = lead.Source;
                Status = lead.Status;
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento lead: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveLeadAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            await _alertService.ShowAlertAsync("Errore", "Il nome è obbligatorio");
            return;
        }

        try
        {
            IsLoading = true;

            var lead = new Lead
            {
                Id = LeadId ?? Guid.NewGuid(),
                CompanyId = _authService.CompanyId.Value,
                Name = Name,
                Email = Email,
                Phone = Phone,
                Source = Source,
                Status = Status,
                CreatedAt = DateTime.UtcNow
            };

            if (IsEditMode && LeadId.HasValue)
            {
                await _leadService.UpdateLeadAsync(LeadId.Value, lead);
                await _alertService.ShowToastAsync("Lead aggiornato con successo");
            }
            else
            {
                await _leadService.CreateLeadAsync(lead);
                await _alertService.ShowToastAsync("Lead creato con successo");
            }

            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore salvataggio lead: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task QualifyLeadAsync()
    {
        if (!LeadId.HasValue) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Qualifica Lead",
            $"Qualificare il lead '{Name}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;
            var result = await _leadService.QualifyLeadAsync(LeadId.Value);
            Status = result.Status;
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
    private async Task ConvertLeadAsync()
    {
        if (!LeadId.HasValue) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Converti Lead",
            $"Convertire il lead '{Name}' in cliente?");

        if (!confirm) return;

        try
        {
            IsLoading = true;
            var result = await _leadService.ConvertLeadToCustomerAsync(LeadId.Value);
            Status = result.Status;
            await _alertService.ShowToastAsync("Lead convertito in cliente con successo");
            await _navigationService.NavigateBackAsync();
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
    private async Task CancelAsync()
    {
        await _navigationService.NavigateBackAsync();
    }
}