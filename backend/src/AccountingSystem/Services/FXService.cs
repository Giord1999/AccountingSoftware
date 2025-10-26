using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;
public class FXService : IFXService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;
    public FXService(ApplicationDbContext ctx, IAuditService audit)
    {
        _ctx = ctx;
        _audit = audit;
    }

    public async Task<decimal> ConvertAsync(string fromCurrency, string toCurrency, decimal amount, DateTime? date = null)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)) return amount;
        var d = date ?? DateTime.UtcNow;
        var rate = await _ctx.ExchangeRates
            .Where(r => r.CurrencyFrom == fromCurrency && r.CurrencyTo == toCurrency && r.Date <= d)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync();

        if (rate == null) throw new InvalidOperationException("Exchange rate not found");
        return Math.Round(amount * rate.Rate, 2);
    }

    public async Task RevaluateAccountsAsync(Guid companyId, DateTime asOfDate, string userId)
    {
        // Simplified example: for all accounts in foreign currency, compute balance -> convert to base and create revaluation journals
        var company = await _ctx.Companies.FindAsync(companyId);
        if (company == null) throw new InvalidOperationException("Company not found");

        // Calculate balances per account (posted entries)
        var postedLines = await (from je in _ctx.JournalEntries
                                 where je.CompanyId == companyId && je.Status == JournalStatus.Posted
                                 && je.Date <= asOfDate
                                 from l in je.Lines
                                 select new { l.AccountId, l.Debit, l.Credit, je.Currency }).ToListAsync();

        var grouped = postedLines.GroupBy(x => x.AccountId)
            .Select(g => new { AccountId = g.Key, Balance = g.Sum(x => x.Debit - x.Credit), Currency = g.Select(x => x.Currency).FirstOrDefault() })
            .Where(x => !string.Equals(x.Currency, company.BaseCurrency, StringComparison.OrdinalIgnoreCase));

        foreach (var item in grouped)
        {
            var acc = await _ctx.Accounts.FindAsync(item.AccountId);
            if (acc == null) continue;
            var converted = await ConvertAsync(item.Currency, company.BaseCurrency, item.Balance, asOfDate);
            // Create revaluation journal: simple approach pushes difference to P&L account 9999 (FX Reval)
            var glDiff = converted - item.Balance; // careful: currencies different; this is illustrative
            var revalJe = new JournalEntry
            {
                CompanyId = companyId,
                PeriodId = (await _ctx.AccountingPeriods.OrderByDescending(p => p.End).FirstOrDefaultAsync(p => p.CompanyId == companyId))?.Id ?? Guid.NewGuid(),
                Date = asOfDate,
                Description = $"FX Revaluation for {acc.Code}",
                Currency = company.BaseCurrency,
                ExchangeRate = 1m,
                Lines = new List<JournalLine>()
            };

            var revalAccount = await _ctx.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "9999");
            if (revalAccount == null)
            {
                revalAccount = new Account { CompanyId = companyId, Code = "9999", Name = "FX Revaluation P&L", Category = AccountCategory.Revenue };
                _ctx.Accounts.Add(revalAccount);
                await _ctx.SaveChangesAsync();
            }

            if (glDiff > 0)
            {
                revalJe.Lines.Add(new JournalLine { AccountId = acc.Id, Credit = (decimal)Math.Abs(glDiff) }); // reduce asset
                revalJe.Lines.Add(new JournalLine { AccountId = revalAccount.Id, Debit = (decimal)Math.Abs(glDiff) });
            }
            else if (glDiff < 0)
            {
                revalJe.Lines.Add(new JournalLine { AccountId = acc.Id, Debit = (decimal)Math.Abs(glDiff) });
                revalJe.Lines.Add(new JournalLine { AccountId = revalAccount.Id, Credit = (decimal)Math.Abs(glDiff) });
            }

            // Validate and post as draft then posted automatically
            if (revalJe.Lines.Sum(l => l.Debit) == revalJe.Lines.Sum(l => l.Credit) && revalJe.Lines.Any())
            {
                _ctx.JournalEntries.Add(revalJe);
                await _ctx.SaveChangesAsync();
                revalJe.Status = JournalStatus.Posted;
                _ctx.JournalEntries.Update(revalJe);
                await _ctx.SaveChangesAsync();
                await _audit.LogAsync(userId, "FXRevaluation", $"Created revaluation JE {revalJe.Id} for account {acc.Code}");
            }
        }
    }
}
