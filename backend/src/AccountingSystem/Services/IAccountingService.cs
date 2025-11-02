using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IAccountingService
{
    Task<JournalEntry> CreateJournalAsync(JournalEntry entry, string userId);
    Task<JournalEntry?> GetJournalByIdAsync(Guid journalId, Guid? companyId = null);
    Task<JournalEntry> PostJournalAsync(Guid journalId, string userId);
    Task<IEnumerable<object>> GetTrialBalanceAsync(Guid companyId, Guid periodId);

    // ✅ NUOVI METODI per contabilità analitica
    Task<IEnumerable<object>> GetTrialBalanceWithAnalysisCentersAsync(
        Guid companyId,
        Guid periodId,
        bool includeAnalysisCenterBreakdown = false);

    Task<IEnumerable<AnalysisCenterReportLine>> GetAnalysisCenterReportAsync(
        Guid companyId,
        Guid? periodId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? analysisCenterId = null,
        AnalysisCenterType? type = null);
}