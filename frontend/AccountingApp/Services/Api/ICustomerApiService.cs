using AccountingApp.Models;

namespace AccountingApp.Services;

public interface ICustomerApiService
{
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer?> GetCustomerByIdAsync(Guid customerId);
    Task<IEnumerable<Customer>> GetCustomersByCompanyAsync(Guid companyId, string? search = null);
    Task<Customer> UpdateCustomerAsync(Guid customerId, Customer customer);
    Task DeleteCustomerAsync(Guid customerId);
}