using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;
public class VATService : IVATService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;
    public VATService(ApplicationDbContext ctx, IAuditService audit)
    {
        _ctx = ctx;
        _audit = audit;
    }

    public async Task<decimal> CalculateVatAsync(Guid vatRateId, decimal netAmount)
    {
        var vr = await _ctx.VATRates.FindAsync(vatRateId) ?? throw new InvalidOperationException("VAT rate not found");
        var vat = Math.Round(netAmount * vr.Rate / 100m, 2);
        return vat;
    }

    public async Task<JournalEntry> ApplyVatToJournalAsync(JournalEntry entry, Guid companyId, string userId)
    {
        // Example approach: assume each revenue/expense line can have an associated VATRate indicated in Narrative as "VAT:{id}"
        // For production implement structured metadata instead.
        var newLines = new List<JournalLine>();
        foreach (var line in entry.Lines)
        {
            // search VAT id in narrative
            if (!string.IsNullOrWhiteSpace(line.Narrative) && line.Narrative.Contains("VAT:"))
            {
                var part = line.Narrative.Split("VAT:").Last().Split(' ').FirstOrDefault();
                if (Guid.TryParse(part, out var vatId))
                {
                    var vatAmount = await CalculateVatAsync(vatId, line.Debit > 0 ? line.Debit : line.Credit);
                    if (vatAmount > 0)
                    {
                        // create VAT line: VAT is liability (collected) on sales, expense on purchase; we keep simple: credit VAT for revenues, debit VAT for expenses
                        var accVat = await _ctx.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2400"); // example VAT liability account
                        if (accVat == null)
                        {
                            // create VAT account if missing
                            accVat = new Account { CompanyId = companyId, Code = "2400", Name = "VAT Payable", Category = AccountCategory.Liability };
                            _ctx.Accounts.Add(accVat);
                            await _ctx.SaveChangesAsync();
                        }

                        if (line.Credit > 0) // revenue line: create credit VAT (liability)
                        {
                            newLines.Add(new JournalLine { AccountId = accVat.Id, Credit = vatAmount });
                            // reduce net in revenue? keep net as is and VAT as separate
                        }
                        else if (line.Debit > 0) // expense: VAT recoverable (asset)
                        {
                            var accVatAsset = await _ctx.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1500");
                            if (accVatAsset == null)
                            {
                                accVatAsset = new Account { CompanyId = companyId, Code = "1500", Name = "VAT Receivable", Category = AccountCategory.Asset };
                                _ctx.Accounts.Add(accVatAsset);
                                await _ctx.SaveChangesAsync();
                            }
                            newLines.Add(new JournalLine { AccountId = accVatAsset.Id, Debit = vatAmount });
                        }
                    }
                }
            }
        }

        foreach (var nl in newLines) entry.Lines.Add(nl);
        await _audit.LogAsync(userId, "ApplyVAT", $"Applied VAT lines to Journal {entry.Id}, added {newLines.Count} lines");
        return entry;
    }
}
