using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using AccountingApp.Models;

namespace AccountingApp.ViewModels;

public partial class SupplierDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly ISupplierApiService _supplierService;
    private readonly IAuthService _authService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;

    public SupplierDetailViewModel(
        ISupplierApiService supplierService,
        IAuthService authService,
        IAlertService alertService,
        INavigationService navigationService)
    {
        _supplierService = supplierService;
        _authService = authService;
        _alertService = alertService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private Guid? supplierId;

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
    private SupplierRating rating = SupplierRating.Unrated;

    [ObservableProperty]
    private bool isActive = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("id") && Guid.TryParse(query["id"].ToString(), out var id))
        {
            SupplierId = id;
            IsEditMode = true;
        }

        if (query.ContainsKey("mode") && query["mode"].ToString() == "create")
        {
            IsEditMode = false;
            SupplierId = null;
        }
    }

    public async Task InitializeAsync()
    {
        if (IsEditMode && SupplierId.HasValue)
        {
            await LoadSupplierAsync();
        }
    }

    [RelayCommand]
    private async Task LoadSupplierAsync()
    {
        if (!SupplierId.HasValue) return;

        try
        {
            IsLoading = true;

            var supplier = await _supplierService.GetSupplierByIdAsync(SupplierId.Value);

            if (supplier != null)
            {
                Name = supplier.Name;
                Email = supplier.Email;
                Phone = supplier.Phone;
                VatNumber = supplier.VatNumber;
                Address = supplier.Address;
                City = supplier.City;
                PostalCode = supplier.PostalCode;
                Country = supplier.Country;
                Rating = supplier.Rating;
                IsActive = supplier.IsActive;
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore caricamento fornitore: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveSupplierAsync()
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

            var supplier = new Supplier
            {
                Id = SupplierId ?? Guid.NewGuid(),
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

            if (IsEditMode && SupplierId.HasValue)
            {
                await _supplierService.UpdateSupplierAsync(SupplierId.Value, supplier);
                await _alertService.ShowToastAsync("Fornitore aggiornato con successo");
            }
            else
            {
                await _supplierService.CreateSupplierAsync(supplier);
                await _alertService.ShowToastAsync("Fornitore creato con successo");
            }

            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _alertService.ShowAlertAsync("Errore", $"Errore salvataggio fornitore: {ex.Message}");
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