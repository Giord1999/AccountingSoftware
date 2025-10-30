using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services
{
    public class AccountService(ApplicationDbContext context, ILogger<AccountService> logger) : IAccountService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AccountService> _logger = logger;

        public async Task<Account> CreateAccountAsync(Account account, string userId)
        {
            ArgumentNullException.ThrowIfNull(account);

            if (string.IsNullOrWhiteSpace(account.Code))
                throw new ArgumentException("Account code is required.", nameof(account));

            if (string.IsNullOrWhiteSpace(account.Name))
                throw new ArgumentException("Account name is required.", nameof(account));

            // Controlla duplicato
            var exists = await _context.Accounts
                .AnyAsync(a => a.Code == account.Code && a.CompanyId == account.CompanyId);

            if (exists)
                throw new InvalidOperationException($"An account with code '{account.Code}' already exists for this company.");

            // Controlla Parent
            if (account.ParentAccountId.HasValue)
            {
                bool parentExists = await _context.Accounts
                    .AnyAsync(a => a.Id == account.ParentAccountId.Value && a.CompanyId == account.CompanyId);

                if (!parentExists)
                    throw new InvalidOperationException("Parent account not found.");
            }

            account.Id = Guid.NewGuid();
            account.CreatedAt = DateTime.UtcNow;
            account.CreatedBy = userId;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Account {Code} created by {UserId} for company {CompanyId}",
                account.Code, userId, account.CompanyId);

            return account;
        }

        public async Task<Account?> GetAccountByIdAsync(Guid id, Guid? companyId = null)
        {
            var query = _context.Accounts
                .Include(a => a.ParentAccount)
                .AsNoTracking()
                .Where(a => a.Id == id);

            if (companyId.HasValue)
                query = query.Where(a => a.CompanyId == companyId.Value);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Account>> GetAccountsByCompanyAsync(Guid companyId)
        {
            return await _context.Accounts
                .Include(a => a.ParentAccount)
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId)
                .OrderBy(a => a.Code)
                .ToListAsync();
        }

        public async Task<Account> UpdateAccountAsync(Guid id, Account account, string userId)
        {
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingAccount == null)
                throw new InvalidOperationException("Account not found.");

            // Duplicato?
            var duplicate = await _context.Accounts
                .AnyAsync(a => a.Code == account.Code &&
                               a.CompanyId == existingAccount.CompanyId &&
                               a.Id != id);

            if (duplicate)
                throw new InvalidOperationException($"An account with code '{account.Code}' already exists for this company.");

            // Parent valido?
            if (account.ParentAccountId.HasValue)
            {
                if (account.ParentAccountId.Value == id)
                    throw new InvalidOperationException("An account cannot be its own parent.");

                bool parentExists = await _context.Accounts
                    .AnyAsync(a => a.Id == account.ParentAccountId.Value &&
                                   a.CompanyId == existingAccount.CompanyId);

                if (!parentExists)
                    throw new InvalidOperationException("Parent account not found.");
            }

            existingAccount.Code = account.Code;
            existingAccount.Name = account.Name;
            existingAccount.Category = account.Category;
            existingAccount.Currency = account.Currency;
            existingAccount.ParentAccountId = account.ParentAccountId;
            existingAccount.IsPostedRestricted = account.IsPostedRestricted;
            existingAccount.UpdatedAt = DateTime.UtcNow;
            existingAccount.UpdatedBy = userId;

            _context.Accounts.Update(existingAccount);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Account {Id} updated by {UserId}", id, userId);

            return existingAccount;
        }

        public async Task DeleteAccountAsync(Guid id, string userId)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
            if (account == null)
                throw new InvalidOperationException("Account not found.");

            bool hasChildren = await _context.Accounts.AnyAsync(a => a.ParentAccountId == id);
            if (hasChildren)
                throw new InvalidOperationException("Cannot delete an account with child accounts.");

            bool usedInJournals = await _context.JournalLines.AnyAsync(jl => jl.AccountId == id);
            if (usedInJournals)
                throw new InvalidOperationException("Cannot delete an account used in journal entries.");

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Account {Id} deleted by {UserId}", id, userId);
        }
    }
}