using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services;
using AccountingApp.Models;
using System.Collections.ObjectModel;

namespace AccountingApp.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomerApiService _customerService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public CustomersViewModel(
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
    private bool isRefreshing;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<Customer> Customers { get; } = new();

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        if (!_authService.CompanyId.HasValue)
        {
            await _alertService.ShowAlertAsync("Errore", "Nessuna azienda selezionata");
            return;
        }

        try
        {
            IsLoading = true;

            var customers = await _customerService.GetCustomersByCompanyAsync(
                _authService.CompanyId.Value,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento clienti: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshCustomersAsync()
    {
        IsRefreshing = true;
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task SearchCustomersAsync()
    {
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task AddCustomerAsync()
    {
        await _navigationService.NavigateToAsync("customerdetail?mode=create");
    }

    [RelayCommand]
    private async Task EditCustomerAsync(Customer customer)
    {
        if (customer == null) return;
        await _navigationService.NavigateToAsync($"customerdetail?id={customer.Id}&mode=edit");
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer customer)
    {
        if (customer == null) return;

        var confirm = await _alertService.ShowConfirmAsync(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il cliente '{customer.Name}'?");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            await _customerService.DeleteCustomerAsync(customer.Id);
            Customers.Remove(customer);

            await _alertService.ShowToastAsync("Cliente eliminato con successo");
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore eliminazione cliente: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}