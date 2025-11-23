using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services;

public class SupplierApiService : ISupplierApiService
{
    private readonly HttpClient _httpClient;

    public SupplierApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("supplier", supplier);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Supplier>() 
                   ?? throw new InvalidOperationException("Failed to create supplier");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating supplier: {ex.Message}");
            throw;
        }
    }

    public async Task<Supplier?> GetSupplierByIdAsync(Guid supplierId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"supplier/{supplierId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Supplier>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting supplier: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Supplier>> GetSuppliersByCompanyAsync(Guid companyId, string? search = null)
    {
        try
        {
            var url = $"supplier?companyId={companyId}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Supplier>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting suppliers: {ex.Message}");
            throw;
        }
    }

    public async Task<Supplier> UpdateSupplierAsync(Guid supplierId, Supplier supplier)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"supplier/{supplierId}", supplier);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Supplier>() 
                   ?? throw new InvalidOperationException("Failed to update supplier");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating supplier: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteSupplierAsync(Guid supplierId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"supplier/{supplierId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting supplier: {ex.Message}");
            throw;
        }
    }
}