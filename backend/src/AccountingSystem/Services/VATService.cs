using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class VatService(ApplicationDbContext ctx, IAuditService audit) : IVatService
{
    private readonly ApplicationDbContext _ctx = ctx;
    private readonly IAuditService _audit = audit;

    public async Task<decimal> CalculateVatAsync(Guid vatRateId, decimal netAmount)
    {
        var vr = await _ctx.VatRates.FindAsync(vatRateId) ?? throw new InvalidOperationException("VAT rate not found");
        var vat = Math.Round(netAmount * vr.Rate / 100m, 2);
        return vat;
    }

    public async Task<JournalEntry> ApplyVatToJournalAsync(JournalEntry entry, Guid companyId, string userId)
    {
        // Example approach: assume each revenue/expense line can have an associated VATRate indicated in Narrative as "VAT:{id}"
        // For production implement structured metadata instead.
        if (entry.Lines == null)
        {
            return entry;
        }
        var newLines = new List<JournalLine>();
        var vatLines = entry.Lines.Where(line => !string.IsNullOrWhiteSpace(line.Narrative) && line.Narrative.Contains("VAT:"));
        foreach (var line in vatLines)
        {
            // Fix: Ensure line.Narrative is not null before splitting
            var narrative = line.Narrative ?? string.Empty;
            var splits = narrative.Split("VAT:");
            if (splits.Length > 1)
            {
                var afterVat = splits[1].Split(' ');
                if (afterVat.Length > 0)
                {
                    var part = afterVat[0];
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
        }

        foreach (var line in newLines)
        {
            entry.Lines.Add(line);
        }
        await _audit.LogAsync(userId, "ApplyVAT", $"Applied VAT lines to Journal {entry.Id}, added {newLines.Count} lines");
        return entry;
    }

    public decimal CalculateVAT(decimal amount, decimal vatRate)
    {
        throw new NotImplementedException();
    }

    public decimal CalculateAmountWithoutVAT(decimal amountWithVAT, decimal vatRate)
    {
        throw new NotImplementedException();
    }

    public decimal CalculateAmountWithVAT(decimal amountWithoutVAT, decimal vatRate)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<VatRate> GetAvailableVATRates()
    {
        throw new NotImplementedException();
    }

    public VatRate GetVATRateByRate(decimal rate)
    {
        throw new NotImplementedException();
    }

    Task IVatService.ApplyVatToJournalAsync(JournalEntry journalEntry, Guid companyId, string userId)
    {
        return ApplyVatToJournalAsync(journalEntry, companyId, userId);
    }
}