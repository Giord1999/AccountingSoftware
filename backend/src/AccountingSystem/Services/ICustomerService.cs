using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface ICustomerService
{
    Task<Customer> CreateCustomerAsync(Customer customer, string userId);
    Task<Customer?> GetCustomerByIdAsync(Guid customerId, Guid? companyId = null);
    Task<IEnumerable<Customer>> GetCustomersByCompanyAsync(Guid companyId, string? search = null);
    Task<Customer> UpdateCustomerAsync(Guid customerId, Customer customer, string userId);
    Task DeleteCustomerAsync(Guid customerId, string userId);
    Task<IEnumerable<Customer>> SearchCustomersAsync(Guid companyId, string query);
    Task MigrateExistingSalesDataAsync(Guid companyId, string userId);
    Task<Customer> DeduplicateCustomerAsync(Guid customerId, Guid duplicateId, string userId);
}