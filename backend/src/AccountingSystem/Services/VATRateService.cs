using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class VATRateService : IVATRateService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;

    public VATRateService(ApplicationDbContext ctx, IAuditService audit)
    {
        _ctx = ctx;
        _audit = audit;
    }

    public async Task<VATRate> CreateVATRateAsync(VATRate vatRate, string userId)
    {
        // Validazione: rate deve essere tra 0 e 100
        if (vatRate.Rate < 0 || vatRate.Rate > 100)
        {
            throw new InvalidOperationException("VAT rate must be between 0 and 100");
        }

        // Validazione: verifica che il nome non sia duplicato
        var existingRate = await _ctx.VATRates
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Name.ToLower() == vatRate.Name.ToLower());

        if (existingRate != null)
        {
            throw new InvalidOperationException($"VAT rate with name '{vatRate.Name}' already exists");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            _ctx.VATRates.Add(vatRate);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "CreateVATRate",
                $"VAT rate {vatRate.Id} ({vatRate.Name} - {vatRate.Rate}%) created");

            await tx.CommitAsync(cts.Token);
            return vatRate;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task<VATRate?> GetVATRateByIdAsync(Guid vatRateId)
    {
        return await _ctx.VATRates
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vatRateId);
    }

    public async Task<IEnumerable<VATRate>> GetAllVATRatesAsync()
    {
        return await _ctx.VATRates
            .AsNoTracking()
            .OrderBy(v => v.Rate)
            .ToListAsync();
    }

    public async Task<VATRate> UpdateVATRateAsync(Guid vatRateId, VATRate vatRate, string userId)
    {
        if (vatRate.Rate < 0 || vatRate.Rate > 100)
        {
            throw new InvalidOperationException("VAT rate must be between 0 and 100");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var existingRate = await _ctx.VATRates.FindAsync(new object[] { vatRateId }, cts.Token);
            if (existingRate == null)
            {
                throw new InvalidOperationException("VAT rate not found");
            }

            // Verifica nome duplicato (escluso rate corrente)
            var duplicateName = await _ctx.VATRates
                .AsNoTracking()
                .AnyAsync(v => v.Id != vatRateId && v.Name.ToLower() == vatRate.Name.ToLower(), cts.Token);

            if (duplicateName)
            {
                throw new InvalidOperationException($"VAT rate with name '{vatRate.Name}' already exists");
            }

            existingRate.Name = vatRate.Name;
            existingRate.Rate = vatRate.Rate;

            _ctx.VATRates.Update(existingRate);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "UpdateVATRate",
                $"VAT rate {vatRateId} updated to {vatRate.Name} - {vatRate.Rate}%");

            await tx.CommitAsync(cts.Token);
            return existingRate;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }

    public async Task DeleteVATRateAsync(Guid vatRateId, string userId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var vatRate = await _ctx.VATRates.FindAsync(new object[] { vatRateId }, cts.Token);
            if (vatRate == null)
            {
                throw new InvalidOperationException("VAT rate not found");
            }

            // Per sicurezza: verifica che non sia in uso
            // (richiederebbe campo VATRateId in JournalLine - al momento non c'è)

            _ctx.VATRates.Remove(vatRate);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "DeleteVATRate",
                $"VAT rate {vatRateId} ({vatRate.Name} - {vatRate.Rate}%) deleted");

            await tx.CommitAsync(cts.Token);
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }
}