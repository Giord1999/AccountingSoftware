using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services.Api;

public class PurchaseApiService : IPurchaseApiService
{
    private readonly HttpClient _httpClient;

    public PurchaseApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Purchase> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("purchase/create-purchase", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Purchase>() 
                   ?? throw new InvalidOperationException("Failed to create purchase");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating purchase: {ex.Message}");
            throw;
        }
    }

    public async Task<Purchase?> GetPurchaseByIdAsync(Guid purchaseId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"purchase/{purchaseId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Purchase>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting purchase: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Purchase>> GetPurchasesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var url = $"purchase/company/{companyId}";
            if (from.HasValue || to.HasValue)
            {
                var query = new List<string>();
                if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
                if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
                url += "?" + string.Join("&", query);
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Purchase>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting purchases: {ex.Message}");
            throw;
        }
    }

    public async Task<Purchase> UpdatePurchaseStatusAsync(Guid purchaseId, PurchaseStatus status)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"purchase/{purchaseId}/status", 
                new { Status = status });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Purchase>() 
                   ?? throw new InvalidOperationException("Failed to update purchase status");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating purchase status: {ex.Message}");
            throw;
        }
    }

    public async Task<Purchase> CancelPurchaseAsync(Guid purchaseId, string reason)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"purchase/{purchaseId}/cancel", 
                new { Reason = reason });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Purchase>() 
                   ?? throw new InvalidOperationException("Failed to cancel purchase");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelling purchase: {ex.Message}");
            throw;
        }
    }
}