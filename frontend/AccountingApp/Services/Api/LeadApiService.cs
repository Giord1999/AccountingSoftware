using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services;

public class LeadApiService : ILeadApiService
{
    private readonly HttpClient _httpClient;

    public LeadApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Lead> CreateLeadAsync(Lead lead)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("lead", lead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Lead>() 
                   ?? throw new InvalidOperationException("Failed to create lead");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating lead: {ex.Message}");
            throw;
        }
    }

    public async Task<Lead?> GetLeadByIdAsync(Guid leadId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"lead/{leadId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Lead>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting lead: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Lead>> GetLeadsByCompanyAsync(Guid companyId, LeadStatus? status = null)
    {
        try
        {
            var url = $"lead";
            if (status.HasValue) url += $"?status={status.Value}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Lead>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting leads: {ex.Message}");
            throw;
        }
    }

    public async Task<Lead> UpdateLeadAsync(Guid leadId, Lead lead)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"lead/{leadId}", lead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Lead>() 
                   ?? throw new InvalidOperationException("Failed to update lead");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating lead: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteLeadAsync(Guid leadId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"lead/{leadId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting lead: {ex.Message}");
            throw;
        }
    }

    public async Task<Lead> QualifyLeadAsync(Guid leadId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"lead/{leadId}/qualify", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Lead>() 
                   ?? throw new InvalidOperationException("Failed to qualify lead");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error qualifying lead: {ex.Message}");
            throw;
        }
    }

    public async Task<Lead> ConvertLeadToCustomerAsync(Guid leadId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"lead/{leadId}/convert", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Lead>() 
                   ?? throw new InvalidOperationException("Failed to convert lead");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting lead: {ex.Message}");
            throw;
        }
    }
}