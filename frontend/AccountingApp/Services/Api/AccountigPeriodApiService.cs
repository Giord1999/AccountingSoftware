using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services.Api;

public class AccountingPeriodApiService : IAccountingPeriodApiService
{
    private readonly HttpClient _httpClient;

    public AccountingPeriodApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<AccountingPeriod>> GetPeriodsByCompanyAsync(Guid companyId)
    {
        var response = await _httpClient.GetAsync($"periods/company/{companyId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<AccountingPeriod>>() ?? [];
    }

    public async Task<AccountingPeriod?> GetPeriodByIdAsync(Guid periodId)
    {
        var response = await _httpClient.GetAsync($"periods/{periodId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AccountingPeriod>();
    }
}