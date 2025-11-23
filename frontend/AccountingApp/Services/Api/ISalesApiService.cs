using AccountingApp.Models;

namespace AccountingApp.Services;

public interface ISalesApiService
{
    Task<Sale> CreateSaleAsync(CreateSaleRequest request);
    Task<Sale?> GetSaleByIdAsync(Guid saleId);
    Task<IEnumerable<Sale>> GetSalesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null);
    Task<Sale> UpdateSaleStatusAsync(Guid saleId, SaleStatus status);
    Task<Sale> CancelSaleAsync(Guid saleId, string reason);
}