using AccountingApp.Models;

namespace AccountingApp.Services.Api;

public interface IAccountingService
{
    Task<JournalEntry> CreateJournalAsync(JournalEntry journalEntry);
    Task<JournalEntry> PostJournalAsync(Guid journalId);
    Task<object> GetTrialBalanceAsync(Guid companyId, Guid periodId);
}