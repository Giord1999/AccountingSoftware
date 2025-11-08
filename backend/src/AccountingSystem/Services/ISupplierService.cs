using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface ISupplierService
{
    Task<Supplier> CreateSupplierAsync(Supplier supplier, string userId);
    Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, Guid? companyId = null);
    Task<IEnumerable<Supplier>> GetSuppliersByCompanyAsync(Guid companyId, string? search = null);
    Task<Supplier> UpdateSupplierAsync(Guid supplierId, Supplier supplier, string userId);
    Task DeleteSupplierAsync(Guid supplierId, string userId);
    Task<IEnumerable<Supplier>> SearchSuppliersAsync(Guid companyId, string query);
    Task MigrateExistingPurchaseDataAsync(Guid companyId, string userId);
    Task<Supplier> DeduplicateSupplierAsync(Guid supplierId, Guid duplicateId, string userId);
}