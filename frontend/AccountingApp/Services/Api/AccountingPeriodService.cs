using AccountingApp.Models;
using AccountingApp.Services.Core;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace AccountingApp.Services.Api;

public class AccountingPeriodService : IAccountingPeriodService
{
    private readonly HttpClient _httpClient;

    public AccountingPeriodService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;

        if (authService.IsAuthenticated && !string.IsNullOrEmpty(authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);
        }
    }

    public async Task<IEnumerable<AccountingPeriod>> GetPeriodsByCompanyAsync(Guid companyId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"accountingperiods/company/{companyId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<AccountingPeriod>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting periods: {ex.Message}");
            throw;
        }
    }

    public async Task<AccountingPeriod?> GetPeriodByIdAsync(Guid periodId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"accountingperiods/{periodId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AccountingPeriod>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting period: {ex.Message}");
            return null;
        }
    }

    public async Task<AccountingPeriod?> GetActivePeriodForDateAsync(Guid companyId, DateTime date)
    {
        try
        {
            var periods = await GetPeriodsByCompanyAsync(companyId);
            return periods.FirstOrDefault(p => !p.IsClosed && date >= p.Start && date <= p.End);
        }
        catch
        {
            return null;
        }
    }
}