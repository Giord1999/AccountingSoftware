using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(ApplicationDbContext context, ILogger<SupplierService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier, string userId)
    {
        supplier.CreatedBy = userId;
        supplier.CreatedAt = DateTime.UtcNow;

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier {SupplierId} created for company {CompanyId}", supplier.Id, supplier.CompanyId);
        return supplier;
    }

    public async Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, Guid? companyId = null)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.Id == supplierId);
        if (companyId.HasValue) query = query.Where(s => s.CompanyId == companyId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Supplier>> GetSuppliersByCompanyAsync(Guid companyId, string? search = null)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.CompanyId == companyId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search) || s.Email.Contains(search) || s.VatNumber.Contains(search));
        }

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<Supplier> UpdateSupplierAsync(Guid supplierId, Supplier supplier, string userId)
    {
        var existing = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (existing == null) throw new InvalidOperationException("Supplier not found");

        existing.Name = supplier.Name;
        existing.Email = supplier.Email;
        existing.Phone = supplier.Phone;
        existing.Address = supplier.Address;
        existing.City = supplier.City;
        existing.PostalCode = supplier.PostalCode;
        existing.Country = supplier.Country;
        existing.VatNumber = supplier.VatNumber;
        existing.Sector = supplier.Sector;
        existing.Rating = supplier.Rating;
        existing.Notes = supplier.Notes;
        existing.IsActive = supplier.IsActive;
        existing.UpdatedBy = userId;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Update(existing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier {SupplierId} updated", supplierId);
        return existing;
    }

    public async Task DeleteSupplierAsync(Guid supplierId, string userId)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier == null) throw new InvalidOperationException("Supplier not found");

        supplier.IsActive = false;
        supplier.UpdatedBy = userId;
        supplier.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier {SupplierId} deactivated", supplierId);
    }

    public async Task<IEnumerable<Supplier>> SearchSuppliersAsync(Guid companyId, string query)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive &&
                       (s.Name.Contains(query) || s.Email.Contains(query) || s.VatNumber.Contains(query)))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task MigrateExistingPurchaseDataAsync(Guid companyId, string userId)
    {
        var purchases = await _context.Purchases
            .Where(p => p.CompanyId == companyId && !string.IsNullOrWhiteSpace(p.SupplierName))
            .ToListAsync();

        foreach (var purchase in purchases)
        {
            var existingSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.CompanyId == companyId &&
                                         s.Name == purchase.SupplierName &&
                                         s.VatNumber == purchase.SupplierVatNumber);

            if (existingSupplier == null)
            {
                var supplier = new Supplier
                {
                    CompanyId = companyId,
                    Name = purchase.SupplierName,
                    VatNumber = purchase.SupplierVatNumber,
                    CreatedBy = userId
                };
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                // Update purchase with SupplierId (assuming Purchase model has SupplierId)
                // purchase.SupplierId = supplier.Id;
                // _context.Purchases.Update(purchase);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Migrated existing purchase data to suppliers for company {CompanyId}", companyId);
    }

    public async Task<Supplier> DeduplicateSupplierAsync(Guid supplierId, Guid duplicateId, string userId)
    {
        var primary = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        var duplicate = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == duplicateId);

        if (primary == null || duplicate == null) throw new InvalidOperationException("Supplier not found");

        // Merge data (simple implementation)
        if (string.IsNullOrWhiteSpace(primary.Email) && !string.IsNullOrWhiteSpace(duplicate.Email))
            primary.Email = duplicate.Email;

        // Update references
        var purchases = await _context.Purchases.Where(p => p.SupplierId == duplicateId).ToListAsync();
        foreach (var purchase in purchases)
        {
            purchase.SupplierId = supplierId;
            _context.Purchases.Update(purchase);
        }

        // Deactivate duplicate
        duplicate.IsActive = false;
        duplicate.UpdatedBy = userId;
        duplicate.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Update(primary);
        _context.Suppliers.Update(duplicate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deduplicated supplier {DuplicateId} into {SupplierId}", duplicateId, supplierId);
        return primary;
    }
}