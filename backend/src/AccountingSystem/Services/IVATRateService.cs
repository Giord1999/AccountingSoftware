using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IVatRateService
{
    Task<VatRate> CreateVatRateAsync(VatRate vatRate, string userId);
    Task<VatRate?> GetVatRateByIdAsync(Guid vatRateId);
    Task<IEnumerable<VatRate>> GetAllVatRatesAsync();
    Task<VatRate> UpdateVatRateAsync(Guid vatRateId, VatRate vatRate, string userId);
    Task DeleteVatRateAsync(Guid vatRateId, string userId);
}