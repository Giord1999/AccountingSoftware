using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services.Api;

public class AccountingService : IAccountingService
{
    private readonly HttpClient _httpClient;

    public AccountingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JournalEntry> CreateJournalAsync(JournalEntry journalEntry)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("accounting/journal", journalEntry);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JournalEntry>() 
                   ?? throw new InvalidOperationException("Failed to create journal");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating journal: {ex.Message}");
            throw;
        }
    }

    public async Task<JournalEntry> PostJournalAsync(Guid journalId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"accounting/journal/{journalId}/post", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JournalEntry>() 
                   ?? throw new InvalidOperationException("Failed to post journal");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error posting journal: {ex.Message}");
            throw;
        }
    }

    public async Task<object> GetTrialBalanceAsync(Guid companyId, Guid periodId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"accounting/trial-balance?companyId={companyId}&periodId={periodId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>() 
                   ?? throw new InvalidOperationException("Failed to get trial balance");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting trial balance: {ex.Message}");
            throw;
        }
    }
}