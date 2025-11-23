using AccountingApp.Models;
using AccountingApp.Services.Core;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace AccountingApp.Services.Api;

public class AccountService : IAccountService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public AccountService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;

        // Imposta l'header di autorizzazione se autenticato
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
        }
    }

    public async Task<IEnumerable<Account>> GetAccountsByCompanyAsync(Guid companyId)
    {
        // Verifica l'accesso alla compagnia
        if (_authService.CompanyId.HasValue && companyId != _authService.CompanyId.Value)
        {
            throw new UnauthorizedAccessException("Accesso negato agli account di questa compagnia");
        }

        try
        {
            var response = await _httpClient.GetAsync($"accounts/company/{companyId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Account>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting accounts: {ex.Message}");
            throw;
        }
    }

    public async Task<Account?> GetAccountByIdAsync(Guid accountId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"accounts/{accountId}");
            if (!response.IsSuccessStatusCode) return null;
            var account = await response.Content.ReadFromJsonAsync<Account>();
            // Verifica l'accesso alla compagnia
            if (account != null && _authService.CompanyId.HasValue && account.CompanyId != _authService.CompanyId.Value)
            {
                return null;
            }
            return account;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting account: {ex.Message}");
            return null;
        }
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        // Imposta la compagnia se non specificata
        if (!_authService.CompanyId.HasValue)
        {
            throw new InvalidOperationException("Compagnia non impostata");
        }
        account.CompanyId = _authService.CompanyId.Value;

        try
        {
            var response = await _httpClient.PostAsJsonAsync("accounts", account);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Account>()
                   ?? throw new InvalidOperationException("Failed to create account");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating account: {ex.Message}");
            throw;
        }
    }

    public async Task<Account> UpdateAccountAsync(Guid accountId, Account account)
    {
        // Ottieni l'account esistente per verificare l'accesso
        var existing = await GetAccountByIdAsync(accountId);
        if (existing == null)
        {
            throw new InvalidOperationException("Account not found");
        }
        // Verifica l'accesso alla compagnia
        if (_authService.CompanyId.HasValue && existing.CompanyId != _authService.CompanyId.Value)
        {
            throw new UnauthorizedAccessException("Accesso negato");
        }
        // Assicura che la compagnia rimanga la stessa
        account.CompanyId = existing.CompanyId;

        try
        {
            var response = await _httpClient.PutAsJsonAsync($"accounts/{accountId}", account);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Account>()
                   ?? throw new InvalidOperationException("Failed to update account");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating account: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAccountAsync(Guid accountId)
    {
        // Ottieni l'account esistente per verificare l'accesso
        var existing = await GetAccountByIdAsync(accountId);
        if (existing == null)
        {
            throw new InvalidOperationException("Account not found");
        }
        // Verifica l'accesso alla compagnia
        if (_authService.CompanyId.HasValue && existing.CompanyId != _authService.CompanyId.Value)
        {
            throw new UnauthorizedAccessException("Accesso negato");
        }

        try
        {
            var response = await _httpClient.DeleteAsync($"accounts/{accountId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting account: {ex.Message}");
            throw;
        }
    }
}