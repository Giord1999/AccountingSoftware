using AccountingApp.Models;
using System.Net.Http.Json;

namespace AccountingApp.Services;

public class InvoiceApiService : IInvoiceApiService
{
    private readonly HttpClient _httpClient;

    public InvoiceApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"invoice/{invoiceId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Invoice>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting invoice: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByCompanyAsync(Guid companyId, InvoiceType? type = null, InvoiceStatus? status = null)
    {
        try
        {
            var url = $"invoice/company/{companyId}";
            var query = new List<string>();
            if (type.HasValue) query.Add($"type={type.Value}");
            if (status.HasValue) query.Add($"status={status.Value}");
            if (query.Any()) url += "?" + string.Join("&", query);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Invoice>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting invoices: {ex.Message}");
            throw;
        }
    }
}