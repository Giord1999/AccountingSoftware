using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services;

public class BIApiService : IBIApiService
{
    private readonly HttpClient _httpClient;

    public BIApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BIDashboardResult> GenerateDashboardAsync(Guid companyId, Guid? periodId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var url = $"bi/dashboard?companyId={companyId}";
            if (periodId.HasValue) url += $"&periodId={periodId.Value}";
            if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
            if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BIDashboardResult>() 
                   ?? throw new InvalidOperationException("Failed to generate dashboard");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating dashboard: {ex.Message}");
            throw;
        }
    }
}