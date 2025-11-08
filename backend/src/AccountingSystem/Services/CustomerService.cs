using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer, string userId)
    {
        customer.CreatedBy = userId;
        customer.CreatedAt = DateTime.UtcNow;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {CustomerId} created for company {CompanyId}", customer.Id, customer.CompanyId);
        return customer;
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid customerId, Guid? companyId = null)
    {
        var query = _context.Customers.AsNoTracking().Where(c => c.Id == customerId);
        if (companyId.HasValue) query = query.Where(c => c.CompanyId == companyId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Customer>> GetCustomersByCompanyAsync(Guid companyId, string? search = null)
    {
        var query = _context.Customers.AsNoTracking().Where(c => c.CompanyId == companyId && c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search) || c.VatNumber.Contains(search));
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Customer> UpdateCustomerAsync(Guid customerId, Customer customer, string userId)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (existing == null) throw new InvalidOperationException("Customer not found");

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.PostalCode = customer.PostalCode;
        existing.Country = customer.Country;
        existing.VatNumber = customer.VatNumber;
        existing.Sector = customer.Sector;
        existing.Rating = customer.Rating;
        existing.Notes = customer.Notes;
        existing.IsActive = customer.IsActive;
        existing.UpdatedBy = userId;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Customers.Update(existing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {CustomerId} updated", customerId);
        return existing;
    }

    public async Task DeleteCustomerAsync(Guid customerId, string userId)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) throw new InvalidOperationException("Customer not found");

        customer.IsActive = false;
        customer.UpdatedBy = userId;
        customer.UpdatedAt = DateTime.UtcNow;

        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {CustomerId} deactivated", customerId);
    }

    public async Task<IEnumerable<Customer>> SearchCustomersAsync(Guid companyId, string query)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive &&
                       (c.Name.Contains(query) || c.Email.Contains(query) || c.VatNumber.Contains(query)))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task MigrateExistingSalesDataAsync(Guid companyId, string userId)
    {
        var sales = await _context.Sales
            .Where(s => s.CompanyId == companyId && !string.IsNullOrWhiteSpace(s.CustomerName))
            .ToListAsync();

        foreach (var sale in sales)
        {
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId &&
                                         c.Name == sale.CustomerName &&
                                         c.VatNumber == sale.CustomerVatNumber);

            if (existingCustomer == null)
            {
                var customer = new Customer
                {
                    CompanyId = companyId,
                    Name = sale.CustomerName,
                    VatNumber = sale.CustomerVatNumber,
                    CreatedBy = userId
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Update sale with CustomerId (assuming Sale model has CustomerId)
                // sale.CustomerId = customer.Id;
                // _context.Sales.Update(sale);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Migrated existing sales data to customers for company {CompanyId}", companyId);
    }

    public async Task<Customer> DeduplicateCustomerAsync(Guid customerId, Guid duplicateId, string userId)
    {
        var primary = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        var duplicate = await _context.Customers.FirstOrDefaultAsync(c => c.Id == duplicateId);

        if (primary == null || duplicate == null) throw new InvalidOperationException("Customer not found");

        // Merge data (simple implementation)
        if (string.IsNullOrWhiteSpace(primary.Email) && !string.IsNullOrWhiteSpace(duplicate.Email))
            primary.Email = duplicate.Email;

        // Update references
        var sales = await _context.Sales.Where(s => s.CustomerId == duplicateId).ToListAsync();
        foreach (var sale in sales)
        {
            sale.CustomerId = customerId;
            _context.Sales.Update(sale);
        }

        // Deactivate duplicate
        duplicate.IsActive = false;
        duplicate.UpdatedBy = userId;
        duplicate.UpdatedAt = DateTime.UtcNow;

        _context.Customers.Update(primary);
        _context.Customers.Update(duplicate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deduplicated customer {DuplicateId} into {CustomerId}", duplicateId, customerId);
        return primary;
    }
}