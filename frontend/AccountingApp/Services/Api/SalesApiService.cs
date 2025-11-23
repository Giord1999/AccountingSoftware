using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services;

public class SalesApiService : ISalesApiService
{
    private readonly HttpClient _httpClient;

    public SalesApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Sale> CreateSaleAsync(CreateSaleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("sales/create-sale", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Sale>() 
                   ?? throw new InvalidOperationException("Failed to create sale");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating sale: {ex.Message}");
            throw;
        }
    }

    public async Task<Sale?> GetSaleByIdAsync(Guid saleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"sales/sale/{saleId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Sale>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting sale: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Sale>> GetSalesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var url = $"sales/company/{companyId}";
            if (from.HasValue || to.HasValue)
            {
                var query = new List<string>();
                if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
                if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
                url += "?" + string.Join("&", query);
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Sale>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting sales: {ex.Message}");
            throw;
        }
    }

    public async Task<Sale> UpdateSaleStatusAsync(Guid saleId, SaleStatus status)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"sales/{saleId}/status", 
                new { Status = status });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Sale>() 
                   ?? throw new InvalidOperationException("Failed to update sale status");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating sale status: {ex.Message}");
            throw;
        }
    }

    public async Task<Sale> CancelSaleAsync(Guid saleId, string reason)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"sales/{saleId}/cancel", 
                new { Reason = reason });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Sale>() 
                   ?? throw new InvalidOperationException("Failed to cancel sale");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelling sale: {ex.Message}");
            throw;
        }
    }
}