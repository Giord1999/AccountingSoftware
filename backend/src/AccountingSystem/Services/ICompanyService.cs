using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface ICompanyService
{
    Task<Company> CreateCompanyAsync(Company company, string userId);
    Task<Company?> GetCompanyByIdAsync(Guid companyId);
    Task<IEnumerable<Company>> GetAllCompaniesAsync();
    Task<Company> UpdateCompanyAsync(Guid companyId, Company company, string userId);
    Task DeleteCompanyAsync(Guid companyId, string userId);
}