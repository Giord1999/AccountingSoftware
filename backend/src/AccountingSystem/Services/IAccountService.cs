using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IAccountService
{
    Task<Account> CreateAccountAsync(Account account, string userId);
    Task<Account?> GetAccountByIdAsync(Guid id, Guid? companyId = null);
    Task<IEnumerable<Account>> GetAccountsByCompanyAsync(Guid companyId);
    Task<Account> UpdateAccountAsync(Guid id, Account account, string userId);
    Task DeleteAccountAsync(Guid id, string userId);
}