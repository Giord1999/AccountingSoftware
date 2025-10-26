using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;
public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _ctx;
    public ReconciliationService(ApplicationDbContext ctx) { _ctx = ctx; }

    public async Task<ReconciliationResult> ReconcileAsync(Guid companyId, Guid accountId, DateTime from, DateTime to)
    {
        // Basic reconciliation: returns balance and list of posted lines not matched to statement (no statement upload here)
        var lines = await (from je in _ctx.JournalEntries
                           where je.CompanyId == companyId && je.Status == JournalStatus.Posted
                           && je.Date >= from && je.Date <= to
                           from l in je.Lines
                           where l.AccountId == accountId
                           select new { je.Id, je.Date, l.Debit, l.Credit }).ToListAsync();

        var balance = lines.Sum(x => x.Debit - x.Credit);
        // UnmatchedTransactions placeholder: in real use compare to bank statement upload; here we return all
        var unmatched = lines.Select(x => new { x.Id, x.Date, Amount = x.Debit - x.Credit });
        return new ReconciliationResult(accountId, balance, unmatched);
    }
}
