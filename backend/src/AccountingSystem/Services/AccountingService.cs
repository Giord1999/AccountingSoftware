using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class AccountingService(ApplicationDbContext ctx, IAuditService audit, ILogger<AccountingService> logger) : IAccountingService
{
    private readonly ApplicationDbContext _ctx = ctx;
    private readonly IAuditService _audit = audit;
    private readonly ILogger<AccountingService> _logger = logger;

    public async Task<JournalEntry> CreateJournalAsync(JournalEntry entry, string userId)
    {
        // server-side validation: double-entry
        var totalDebit = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
            throw new InvalidOperationException("Debits must equal Credits");

        // period check
        var period = await _ctx.AccountingPeriods.FindAsync(entry.PeriodId);
        if (period != null && period.IsClosed)
            throw new InvalidOperationException("Period is closed");

        // attach accounts existence - Ottimizzato per evitare N+1 queries
        var accountIds = entry.Lines.Select(l => l.AccountId).Distinct().ToList();
        var existingAccounts = await _ctx.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();

        var missingAccountIds = accountIds.Except(existingAccounts).ToList();
        if (missingAccountIds.Count > 0)
        {
            throw new InvalidOperationException($"Account not found: {string.Join(", ", missingAccountIds)}");
        }

        // ✅ VALIDAZIONE CENTRI DI ANALISI (ora funziona con ICollection)
        await ValidateAnalysisCentersAsync(entry.CompanyId, entry.Lines);

        // Transazione per garantire atomicità tra creazione journal e audit log
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            _ctx.JournalEntries.Add(entry);
            await _ctx.SaveChangesAsync(cts.Token);

            var analysisCenterInfo = entry.Lines.Any(l => l.AnalysisCenterId.HasValue)
                ? " (con centri di analisi)"
                : string.Empty;

            await _audit.LogAsync(userId, "CreateJournal",
                $"Journal {entry.Id} created{analysisCenterInfo}");

            await tx.CommitAsync(cts.Token);

            _logger.LogInformation(
                "JournalEntry {JournalId} creato per company {CompanyId} con {LineCount} righe{AnalysisInfo}",
                entry.Id, entry.CompanyId, entry.Lines.Count, analysisCenterInfo);

            return entry;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task<JournalEntry?> GetJournalByIdAsync(Guid journalId, Guid? companyId = null)
    {
        var query = _ctx.JournalEntries
            .AsNoTracking()
            .Include(j => j.Lines)
                .ThenInclude(l => l.Account)
            .Include(j => j.Lines)
                .ThenInclude(l => l.AnalysisCenter) // ✅ Include centro di analisi
            .Where(j => j.Id == journalId);

        // Filtro multi-tenancy: se specificato companyId, filtra per azienda
        if (companyId.HasValue)
            query = query.Where(j => j.CompanyId == companyId.Value);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<JournalEntry> PostJournalAsync(Guid journalId, string userId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var je = await _ctx.JournalEntries
                .Include(j => j.Lines)
                    .ThenInclude(l => l.AnalysisCenter) // ✅ Include per logging completo
                .FirstOrDefaultAsync(j => j.Id == journalId, cts.Token);

            if (je is null)
                throw new InvalidOperationException("Journal not found");

            if (je.Status == JournalStatus.Posted)
                throw new InvalidOperationException("Already posted");

            // validate again
            var totalDebit = je.Lines.Sum(l => l.Debit);
            var totalCredit = je.Lines.Sum(l => l.Credit);
            if (totalDebit != totalCredit)
                throw new InvalidOperationException("Debits must equal Credits");

            // ✅ VALIDAZIONE CENTRI DI ANALISI (ora funziona con ICollection)
            await ValidateAnalysisCentersAsync(je.CompanyId, je.Lines);

            // posting: create postings in GL (here we just mark and record audit)
            je.Status = JournalStatus.Posted;
            _ctx.JournalEntries.Update(je);
            await _ctx.SaveChangesAsync(cts.Token);

            var analysisCenterInfo = je.Lines.Any(l => l.AnalysisCenterId.HasValue)
                ? $" con {je.Lines.Count(l => l.AnalysisCenterId.HasValue)} righe analitiche"
                : string.Empty;

            await _audit.LogAsync(userId, "PostJournal",
                $"Journal {je.Id} posted{analysisCenterInfo}");

            await tx.CommitAsync(cts.Token);

            _logger.LogInformation(
                "JournalEntry {JournalId} posted{AnalysisInfo}",
                je.Id, analysisCenterInfo);

            return je;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task<IEnumerable<object>> GetTrialBalanceAsync(Guid companyId, Guid periodId)
    {
        // aggregate posted journal lines within period
        var period = await _ctx.AccountingPeriods.FindAsync(periodId);
        if (period is null)
            throw new InvalidOperationException("Period not found");

        // Aggregazione lato database con GROUP BY - evita di caricare tutte le righe in memoria
        var result = await (
            from je in _ctx.JournalEntries
            where je.CompanyId == companyId
                && je.Status == JournalStatus.Posted
                && je.Date >= period.Start
                && je.Date <= period.End
            from l in je.Lines
            join a in _ctx.Accounts on l.AccountId equals a.Id
            group new { l.Debit, l.Credit } by new
            {
                l.AccountId,
                a.Code,
                a.Name,
                a.Category
            } into g
            select new
            {
                AccountId = g.Key.AccountId,
                AccountCode = g.Key.Code,
                AccountName = g.Key.Name,
                Category = g.Key.Category,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            }
        ).ToListAsync();

        return result;
    }

    // ✅ NUOVO: Trial Balance con dettaglio centri di analisi
    public async Task<IEnumerable<object>> GetTrialBalanceWithAnalysisCentersAsync(
        Guid companyId,
        Guid periodId,
        bool includeAnalysisCenterBreakdown = false)
    {
        var period = await _ctx.AccountingPeriods.FindAsync(periodId);
        if (period is null)
            throw new InvalidOperationException("Period not found");

        if (!includeAnalysisCenterBreakdown)
        {
            // Versione standard senza breakdown
            return await GetTrialBalanceAsync(companyId, periodId);
        }

        // Aggregazione con dettaglio centri di analisi
        var result = await (
            from je in _ctx.JournalEntries
            where je.CompanyId == companyId
                && je.Status == JournalStatus.Posted
                && je.Date >= period.Start
                && je.Date <= period.End
            from l in je.Lines
            join a in _ctx.Accounts on l.AccountId equals a.Id
            join ac in _ctx.AnalysisCenters on l.AnalysisCenterId equals ac.Id into acGroup
            from ac in acGroup.DefaultIfEmpty()
            group new { l.Debit, l.Credit, ac } by new
            {
                l.AccountId,
                a.Code,
                a.Name,
                a.Category
            } into g
            select new
            {
                AccountId = g.Key.AccountId,
                AccountCode = g.Key.Code,
                AccountName = g.Key.Name,
                Category = g.Key.Category,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit),
                // Breakdown per centro di analisi
                AnalysisCenters = g
                    .Where(x => x.ac != null)
                    .GroupBy(x => new { x.ac!.Id, x.ac.Code, x.ac.Name })
                    .Select(acg => new
                    {
                        CenterId = acg.Key.Id,
                        CenterCode = acg.Key.Code,
                        CenterName = acg.Key.Name,
                        Debit = acg.Sum(x => x.Debit),
                        Credit = acg.Sum(x => x.Credit)
                    })
                    .ToList()
            }
        ).ToListAsync();

        return result;
    }

    // ✅ NUOVO: Report centri di analisi
    public async Task<IEnumerable<AnalysisCenterReportLine>> GetAnalysisCenterReportAsync(
        Guid companyId,
        Guid? periodId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? analysisCenterId = null,
        AnalysisCenterType? type = null)
    {
        // Determina il range di date
        DateTime fromDate, toDate;

        if (periodId.HasValue)
        {
            var period = await _ctx.AccountingPeriods.FindAsync(periodId.Value);
            if (period is null)
                throw new InvalidOperationException("Period not found");

            fromDate = period.Start;
            toDate = period.End;
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            fromDate = startDate.Value;
            toDate = endDate.Value;
        }
        else
        {
            throw new InvalidOperationException("Either periodId or date range must be specified");
        }

        var query = from je in _ctx.JournalEntries
                    where je.CompanyId == companyId
                       && je.Status == JournalStatus.Posted
                       && je.Date >= fromDate
                       && je.Date <= toDate
                    from l in je.Lines
                    where l.AnalysisCenterId.HasValue
                    join ac in _ctx.AnalysisCenters on l.AnalysisCenterId equals ac.Id
                    join a in _ctx.Accounts on l.AccountId equals a.Id
                    select new { l, ac, a };

        if (analysisCenterId.HasValue)
            query = query.Where(x => x.ac.Id == analysisCenterId.Value);

        if (type.HasValue)
            query = query.Where(x => x.ac.Type == type.Value);

        var data = await query.ToListAsync();

        var result = data
            .GroupBy(x => new
            {
                x.ac.Id,
                x.ac.Code,
                x.ac.Name,
                x.ac.Type
            })
            .Select(g => new AnalysisCenterReportLine
            {
                AnalysisCenterId = g.Key.Id,
                CenterCode = g.Key.Code,
                CenterName = g.Key.Name,
                CenterType = g.Key.Type,
                TotalDebit = g.Sum(x => x.l.Debit),
                TotalCredit = g.Sum(x => x.l.Credit),
                Balance = g.Sum(x => x.l.Debit - x.l.Credit),
                TransactionCount = g.Count(),
                // Dettaglio per conto
                AccountBreakdown = g
                    .GroupBy(x => new { x.a.Id, x.a.Code, x.a.Name })
                    .Select(ag => new AccountBreakdownLine
                    {
                        AccountId = ag.Key.Id,
                        AccountCode = ag.Key.Code,
                        AccountName = ag.Key.Name,
                        Debit = ag.Sum(x => x.l.Debit),
                        Credit = ag.Sum(x => x.l.Credit),
                        Balance = ag.Sum(x => x.l.Debit - x.l.Credit)
                    })
                    .OrderBy(a => a.AccountCode)
                    .ToList()
            })
            .OrderBy(r => r.CenterCode)
            .ToList();

        _logger.LogInformation(
            "Generated analysis center report for company {CompanyId} from {FromDate} to {ToDate}: {CenterCount} centers",
            companyId, fromDate, toDate, result.Count);

        return result;
    }

    // ✅ CORREZIONE: Accetta IEnumerable<JournalLine> invece di List<JournalLine>
    private async Task ValidateAnalysisCentersAsync(Guid companyId, IEnumerable<JournalLine> lines)
    {
        var analysisCenterIds = lines
            .Where(l => l.AnalysisCenterId.HasValue)
            .Select(l => l.AnalysisCenterId!.Value)
            .Distinct()
            .ToList();

        if (!analysisCenterIds.Any())
            return; // Nessun centro specificato, validazione ok

        // Verifica che tutti i centri esistano, siano attivi e appartengano alla company
        var existingCenters = await _ctx.AnalysisCenters
            .AsNoTracking()
            .Where(ac => analysisCenterIds.Contains(ac.Id)
                      && ac.CompanyId == companyId
                      && ac.IsActive)
            .Select(ac => ac.Id)
            .ToListAsync();

        var invalidIds = analysisCenterIds.Except(existingCenters).ToList();

        if (invalidIds.Any())
        {
            _logger.LogWarning(
                "Invalid analysis centers detected for company {CompanyId}: {InvalidIds}",
                companyId, string.Join(", ", invalidIds));

            throw new InvalidOperationException(
                $"I seguenti centri di analisi non sono validi o non appartengono all'azienda: {string.Join(", ", invalidIds)}");
        }

        _logger.LogDebug(
            "Validated {Count} analysis centers for company {CompanyId}",
            analysisCenterIds.Count, companyId);
    }
}

// ✅ NUOVI DTO per report centri di analisi
public class AnalysisCenterReportLine
{
    public Guid AnalysisCenterId { get; set; }
    public string CenterCode { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public AnalysisCenterType CenterType { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public List<AccountBreakdownLine> AccountBreakdown { get; set; } = new();
}

public class AccountBreakdownLine
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}