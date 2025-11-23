using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services;

public class AccountService : IAccountService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public AccountService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<IEnumerable<Account>> GetAccountsByCompanyAsync(Guid companyId)
    {
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
            return await response.Content.ReadFromJsonAsync<Account>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting account: {ex.Message}");
            return null;
        }
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
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