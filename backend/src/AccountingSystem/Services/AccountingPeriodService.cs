using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class AccountingPeriodService(ApplicationDbContext ctx, IAuditService audit) : IAccountingPeriodService
{
    private readonly ApplicationDbContext _ctx = ctx;
    private readonly IAuditService _audit = audit;

    public async Task<AccountingPeriod> CreatePeriodAsync(AccountingPeriod period, string userId)
    {
        // Validazione: verifica che la company esista
        var companyExists = await _ctx.Companies.AnyAsync(c => c.Id == period.CompanyId);
        if (!companyExists)
        {
            throw new InvalidOperationException($"Company {period.CompanyId} not found");
        }

        // Validazione: End > Start
        if (period.End <= period.Start)
        {
            throw new InvalidOperationException("Period end date must be after start date");
        }

        // Validazione: verifica sovrapposizione periodi per questa company
        var overlappingPeriod = await _ctx.AccountingPeriods
            .AsNoTracking()
            .Where(p => p.CompanyId == period.CompanyId)
            .Where(p => (p.Start <= period.End && p.End >= period.Start))
            .FirstOrDefaultAsync();

        if (overlappingPeriod is not null)
        {
            throw new InvalidOperationException($"Period overlaps with existing period {overlappingPeriod.Id} ({overlappingPeriod.Start:yyyy-MM-dd} to {overlappingPeriod.End:yyyy-MM-dd})");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            _ctx.AccountingPeriods.Add(period);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "CreatePeriod",
                $"Period {period.Id} ({period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}) created for company {period.CompanyId}");

            await tx.CommitAsync(cts.Token);
            return period;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task<AccountingPeriod?> GetPeriodByIdAsync(Guid periodId, Guid? companyId = null)
    {
        var query = _ctx.AccountingPeriods
            .AsNoTracking()
            .Where(p => p.Id == periodId);

        if (companyId.HasValue)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AccountingPeriod>> GetPeriodsByCompanyAsync(Guid companyId)
    {
        return await _ctx.AccountingPeriods
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.Start)
            .ToListAsync();
    }

    public async Task<AccountingPeriod> ClosePeriodAsync(Guid periodId, string userId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var period = await _ctx.AccountingPeriods.FindAsync(new object[] { periodId }, cts.Token);
            if (period is null)
            {
                throw new InvalidOperationException("Period not found");
            }

            if (period.IsClosed)
            {
                throw new InvalidOperationException("Period is already closed");
            }

            // Verifica che tutti i journal entries del periodo siano Posted
            var hasDraftJournals = await _ctx.JournalEntries
                .AnyAsync(j => j.PeriodId == periodId && j.Status == JournalStatus.Draft, cts.Token);

            if (hasDraftJournals)
            {
                throw new InvalidOperationException("Cannot close period with draft journal entries. Post all journals first.");
            }

            period.IsClosed = true;
            _ctx.AccountingPeriods.Update(period);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "ClosePeriod",
                $"Period {periodId} ({period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}) closed");

            await tx.CommitAsync(cts.Token);
            return period;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task<AccountingPeriod> ReopenPeriodAsync(Guid periodId, string userId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var period = await _ctx.AccountingPeriods.FindAsync(new object[] { periodId }, cts.Token);
            if (period is null)
            {
                throw new InvalidOperationException("Period not found");
            }

            if (!period.IsClosed)
            {
                throw new InvalidOperationException("Period is not closed");
            }

            // Verifica che non ci siano periodi successivi già chiusi
            var laterClosedPeriods = await _ctx.AccountingPeriods
                .Where(p => p.CompanyId == period.CompanyId && p.Start > period.End && p.IsClosed)
                .AnyAsync(cts.Token);

            if (laterClosedPeriods)
            {
                throw new InvalidOperationException("Cannot reopen period when later periods are already closed");
            }

            period.IsClosed = false;
            _ctx.AccountingPeriods.Update(period);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "ReopenPeriod",
                $"Period {periodId} ({period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}) reopened");

            await tx.CommitAsync(cts.Token);
            return period;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task DeletePeriodAsync(Guid periodId, string userId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var period = await _ctx.AccountingPeriods.FindAsync(new object[] { periodId }, cts.Token);
            if (period is null)
            {
                throw new InvalidOperationException("Period not found");
            }

            // Non permettere eliminazione se il periodo è chiuso
            if (period.IsClosed)
            {
                throw new InvalidOperationException("Cannot delete closed period. Reopen it first.");
            }

            // Verifica che non ci siano journal entries collegati
            var hasJournals = await _ctx.JournalEntries.AnyAsync(j => j.PeriodId == periodId, cts.Token);
            if (hasJournals)
            {
                throw new InvalidOperationException("Cannot delete period with existing journal entries");
            }

            _ctx.AccountingPeriods.Remove(period);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "DeletePeriod",
                $"Period {periodId} ({period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}) deleted");

            await tx.CommitAsync(cts.Token);
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }
}