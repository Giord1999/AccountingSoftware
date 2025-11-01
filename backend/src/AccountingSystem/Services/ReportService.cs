using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class ReportService(ApplicationDbContext ctx) : IReportService
{
    private readonly ApplicationDbContext _ctx = ctx;

    public async Task<IEnumerable<BalanceSheetLine>> GetBalanceSheetAsync(Guid companyId, Guid periodId)
    {
        var period = await _ctx.AccountingPeriods.FindAsync(periodId) ?? throw new InvalidOperationException("Period not found");
        var lines = await (from je in _ctx.JournalEntries
                           where je.CompanyId == companyId && je.Status == JournalStatus.Posted
                           && je.Date >= period.Start && je.Date <= period.End
                           from l in je.Lines
                           join a in _ctx.Accounts on l.AccountId equals a.Id
                           select new { a.Code, a.Name, a.Category, Amount = l.Debit - l.Credit }).ToListAsync();
        var grouped = lines.GroupBy(x => new { x.Code, x.Name, x.Category })
            .Select(g => new BalanceSheetLine(g.Key.Code, g.Key.Name, g.Key.Category, g.Sum(x => x.Amount))).ToList();
        // For presentation, assets positive, liabilities negative etc. Keep raw sums.
        return grouped;
    }

    public async Task<IEnumerable<PLLine>> GetProfitAndLossAsync(Guid companyId, Guid periodId)
    {
        var period = await _ctx.AccountingPeriods.FindAsync(periodId) ?? throw new InvalidOperationException("Period not found");
        var lines = await (from je in _ctx.JournalEntries
                           where je.CompanyId == companyId && je.Status == JournalStatus.Posted
                           && je.Date >= period.Start && je.Date <= period.End
                           from l in je.Lines
                           join a in _ctx.Accounts on l.AccountId equals a.Id
                           where a.Category == AccountCategory.Revenue || a.Category == AccountCategory.Expense
                           select new { a.Code, a.Name, a.Category, Amount = l.Credit - l.Debit }).ToListAsync();
        var grouped = lines.GroupBy(x => new { x.Code, x.Name })
            .Select(g => new PLLine(g.Key.Code, g.Key.Name, g.Sum(x => x.Amount))).ToList();
        return grouped;
    }

    public async Task<TrialBalanceSummary> GetTrialBalanceSummaryAsync(Guid companyId, Guid periodId)
    {
        var period = await _ctx.AccountingPeriods.FindAsync(periodId) ?? throw new InvalidOperationException("Period not found");
        var postedLines = await (from je in _ctx.JournalEntries
                                 where je.CompanyId == companyId && je.Status == JournalStatus.Posted
                                 && je.Date >= period.Start && je.Date <= period.End
                                 from l in je.Lines
                                 select new { l.Debit, l.Credit }).ToListAsync();
        var totalDebit = postedLines.Sum(x => x.Debit);
        var totalCredit = postedLines.Sum(x => x.Credit);
        return new TrialBalanceSummary(totalDebit, totalCredit, totalDebit - totalCredit);
    }

    public async Task<DashboardKpi> GetDashboardKpiAsync(Guid companyId, Guid periodId)
    {
        var bs = await GetBalanceSheetAsync(companyId, periodId);
        var pl = await GetProfitAndLossAsync(companyId, periodId);
        decimal currentAssets = bs.Where(x => x.Category == AccountCategory.Asset).Sum(x => x.Balance);
        decimal currentLiabilities = bs.Where(x => x.Category == AccountCategory.Liability).Sum(x => x.Balance);
        decimal revenue = pl.Where(x => true).Where(x => x.Amount > 0).Sum(x => x.Amount);
        decimal expenses = pl.Where(x => true).Where(x => x.Amount < 0).Sum(x => Math.Abs(x.Amount));
        decimal netProfit = revenue - expenses;
        return new DashboardKpi(currentAssets, currentLiabilities, netProfit, revenue, expenses);
    }
}