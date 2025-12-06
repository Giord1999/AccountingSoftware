using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class CustomerDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICustomerApiService _customerService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public CustomerDetailViewModel(
        ICustomerApiService customerService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _customerService = customerService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private Guid? customerId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? phone;

    [ObservableProperty]
    private string? vatNumber;

    [ObservableProperty]
    private string? address;

    [ObservableProperty]
    private string? city;

    [ObservableProperty]
    private string? postalCode;

    [ObservableProperty]
    private string country = "IT";

    [ObservableProperty]
    private CustomerRating rating = CustomerRating.Unrated;

    [ObservableProperty]
    private bool isActive = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("id") && Guid.TryParse(query["id"].ToString(), out var id))
        {
            CustomerId = id;
            IsEditMode = true;
        }

        if (query.ContainsKey("mode") && query["mode"].ToString() == "create")
        {
            IsEditMode = false;
            CustomerId = null;
        }
    }

    public async Task InitializeAsync()
    {
        if (IsEditMode && CustomerId.HasValue)
        {
            await LoadCustomerAsync();
        }
    }

    [RelayCommand]
    private async Task LoadCustomerAsync()
    {
        if (!CustomerId.HasValue) return;

        try
        {
            IsLoading = true;

            var customer = await _customerService.GetCustomerByIdAsync(CustomerId.Value);

            if (customer != null)
            {
                Name = customer.Name;
                Email = customer.Email;
                Phone = customer.Phone;
                VatNumber = customer.VatNumber;
                Address = customer.Address;
                City = customer.City;
                PostalCode = customer.PostalCode;
                Country = customer.Country;
                Rating = customer.Rating;
                IsActive = customer.IsActive;
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento cliente: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
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

            var customer = new Customer
            {
                Id = CustomerId ?? Guid.NewGuid(),
                CompanyId = _authService.CompanyId.Value,
                Name = Name,
                Email = Email,
                Phone = Phone,
                VatNumber = VatNumber,
                Address = Address,
                City = City,
                PostalCode = PostalCode,
                Country = Country,
                Rating = Rating,
                IsActive = IsActive
            };

            if (IsEditMode && CustomerId.HasValue)
            {
                await _customerService.UpdateCustomerAsync(CustomerId.Value, customer);
                await _alertService.ShowToastAsync("Cliente aggiornato con successo");
            }
            else
            {
                await _customerService.CreateCustomerAsync(customer);
                await _alertService.ShowToastAsync("Cliente creato con successo");
            }

            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore salvataggio cliente: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _navigationService.GoBackAsync();
    }
}