using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountingApp.Services.Api;
using AccountingApp.Services.Core;
using System.ComponentModel.DataAnnotations;

namespace AccountingApp.ViewModels;

public partial class LoginViewModel : ObservableValidator
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IAlertService _alertService;

    public LoginViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IAlertService alertService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _alertService = alertService;
    }

    [ObservableProperty]
    [Required(ErrorMessage = "Email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Email non valida")]
    private string email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Password è obbligatoria")]
    [MinLength(6, ErrorMessage = "Password deve essere almeno 6 caratteri")]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            ValidateAllProperties();
            if (HasErrors)
            {
                ErrorMessage = string.Join(", ", GetErrors().Select(e => e.ErrorMessage));
                return;
            }

            var result = await _authService.LoginAsync(Email, Password);

            if (result != null)
            {
                await _navigationService.NavigateToDashboardAsync();
                await _alertService.ShowToastAsync($"Benvenuto, {result.DisplayName}!");
            }
            else
            {
                ErrorMessage = "Credenziali non valide";
                await _alertService.ShowAlertAsync("Errore", "Login fallito. Verifica le credenziali.");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Errore durante il login";
            await _alertService.ShowAlertAsync("Errore", $"Errore: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}