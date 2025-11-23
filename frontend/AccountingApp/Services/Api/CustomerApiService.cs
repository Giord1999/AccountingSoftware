using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services.Api;

public class CustomerApiService : ICustomerApiService
{
    private readonly HttpClient _httpClient;

    public CustomerApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("customer", customer);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Customer>() 
                   ?? throw new InvalidOperationException("Failed to create customer");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating customer: {ex.Message}");
            throw;
        }
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"customer/{customerId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Customer>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting customer: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Customer>> GetCustomersByCompanyAsync(Guid companyId, string? search = null)
    {
        try
        {
            var url = $"customer?companyId={companyId}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Customer>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting customers: {ex.Message}");
            throw;
        }
    }

    public async Task<Customer> UpdateCustomerAsync(Guid customerId, Customer customer)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"customer/{customerId}", customer);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Customer>() 
                   ?? throw new InvalidOperationException("Failed to update customer");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating customer: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteCustomerAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"customer/{customerId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting customer: {ex.Message}");
            throw;
        }
    }
}