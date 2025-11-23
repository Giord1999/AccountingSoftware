using AccountingApp.Models;

namespace AccountingApp.Services;

public interface ISupplierApiService
{
    Task<Supplier> CreateSupplierAsync(Supplier supplier);
    Task<Supplier?> GetSupplierByIdAsync(Guid supplierId);
    Task<IEnumerable<Supplier>> GetSuppliersByCompanyAsync(Guid companyId, string? search = null);
    Task<Supplier> UpdateSupplierAsync(Guid supplierId, Supplier supplier);
    Task DeleteSupplierAsync(Guid supplierId);
}