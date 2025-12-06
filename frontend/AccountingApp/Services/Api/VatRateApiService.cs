using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services.Api;

public class VatRateApiService : IVatRateApiService
{
    private readonly HttpClient _httpClient;

    public VatRateApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<VatRate>> GetAllVatRatesAsync()
    {
        var response = await _httpClient.GetAsync("vatrates");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<VatRate>>() ?? [];
    }

    public async Task<VatRate?> GetVatRateByIdAsync(Guid vatRateId)
    {
        var response = await _httpClient.GetAsync($"vatrates/{vatRateId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<VatRate>();
    }
}