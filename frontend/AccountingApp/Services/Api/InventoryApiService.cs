using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services.Api;

public class InventoryApiService : IInventoryApiService
{
    private readonly HttpClient _httpClient;

    public InventoryApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Inventory> CreateInventoryItemAsync(Inventory item)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("inventory", item);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Inventory>() 
                   ?? throw new InvalidOperationException("Failed to create inventory item");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating inventory item: {ex.Message}");
            throw;
        }
    }

    public async Task<Inventory?> GetInventoryItemByIdAsync(Guid itemId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"inventory/{itemId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Inventory>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting inventory item: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Inventory>> GetInventoryItemsByCompanyAsync(Guid companyId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"inventory/company/{companyId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Inventory>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting inventory items: {ex.Message}");
            throw;
        }
    }

    public async Task<Inventory> UpdateInventoryItemAsync(Guid itemId, Inventory item)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"inventory/{itemId}", item);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Inventory>() 
                   ?? throw new InvalidOperationException("Failed to update inventory item");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating inventory item: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteInventoryItemAsync(Guid itemId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"inventory/{itemId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting inventory item: {ex.Message}");
            throw;
        }
    }
}