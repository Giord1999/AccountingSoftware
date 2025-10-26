
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class AccountingService : IAccountingService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;

    public AccountingService(ApplicationDbContext ctx, IAuditService audit)
    {
        _ctx = ctx;
        _audit = audit;
    }

    public async Task<JournalEntry> CreateJournalAsync(JournalEntry entry, string userId)
    {
        // server-side validation: double-entry
        var totalDebit = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit) throw new InvalidOperationException("Debits must equal Credits");

        // period check
        var period = await _ctx.AccountingPeriods.FindAsync(entry.PeriodId);
        if (period != null && period.IsClosed) throw new InvalidOperationException("Period is closed");

        // attach accounts existence - Ottimizzato per evitare N+1 queries
        var accountIds = entry.Lines.Select(l => l.AccountId).Distinct().ToList();
        var existingAccounts = await _ctx.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();

        var missingAccountIds = accountIds.Except(existingAccounts).ToList();
        if (missingAccountIds.Any())
        {
            throw new InvalidOperationException($"Account not found: {string.Join(", ", missingAccountIds)}");
        }

        // Transazione per garantire atomicità tra creazione journal e audit log
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            _ctx.JournalEntries.Add(entry);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "CreateJournal", $"Journal {entry.Id} created");

            await tx.CommitAsync(cts.Token);
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
            var je = await _ctx.JournalEntries.Include(j => j.Lines).FirstOrDefaultAsync(j => j.Id == journalId, cts.Token);
            if (je == null) throw new InvalidOperationException("Journal not found");
            if (je.Status == JournalStatus.Posted) throw new InvalidOperationException("Already posted");

            // validate again
            var totalDebit = je.Lines.Sum(l => l.Debit);
            var totalCredit = je.Lines.Sum(l => l.Credit);
            if (totalDebit != totalCredit) throw new InvalidOperationException("Debits must equal Credits");

            // posting: create postings in GL (here we just mark and record audit)
            je.Status = JournalStatus.Posted;
            _ctx.JournalEntries.Update(je);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "PostJournal", $"Journal {je.Id} posted");

            await tx.CommitAsync(cts.Token);
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
        if (period == null) throw new InvalidOperationException("Period not found");

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
}