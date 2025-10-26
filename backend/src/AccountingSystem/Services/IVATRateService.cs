using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IVATRateService
{
    Task<VATRate> CreateVATRateAsync(VATRate vatRate, string userId);
    Task<VATRate?> GetVATRateByIdAsync(Guid vatRateId);
    Task<IEnumerable<VATRate>> GetAllVATRatesAsync();
    Task<VATRate> UpdateVATRateAsync(Guid vatRateId, VATRate vatRate, string userId);
    Task DeleteVATRateAsync(Guid vatRateId, string userId);
}