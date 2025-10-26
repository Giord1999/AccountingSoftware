using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IAccountingService
{
    Task<JournalEntry> CreateJournalAsync(JournalEntry entry, string userId);
    Task<JournalEntry?> GetJournalByIdAsync(Guid journalId, Guid? companyId = null);
    Task<JournalEntry> PostJournalAsync(Guid journalId, string userId);
    Task<IEnumerable<object>> GetTrialBalanceAsync(Guid companyId, Guid periodId);
}