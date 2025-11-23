using AccountingApp.Models;

namespace AccountingApp.Services.Api;

public interface IAccountService
{
    Task<IEnumerable<Account>> GetAccountsByCompanyAsync(Guid companyId);
    Task<Account?> GetAccountByIdAsync(Guid accountId);
    Task<Account> CreateAccountAsync(Account account);
    Task<Account> UpdateAccountAsync(Guid accountId, Account account);
    Task DeleteAccountAsync(Guid accountId);
}