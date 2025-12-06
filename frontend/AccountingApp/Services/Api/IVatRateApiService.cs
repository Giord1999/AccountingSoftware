using AccountingApp.Models;

namespace AccountingApp.Services.Api;

public interface IVatRateApiService
{
    Task<IEnumerable<VatRate>> GetAllVatRatesAsync();
    Task<VatRate?> GetVatRateByIdAsync(Guid vatRateId);
}