using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class VatRateService(ApplicationDbContext ctx, IAuditService audit) : IVatRateService
{
    private readonly ApplicationDbContext _ctx = ctx;
    private readonly IAuditService _audit = audit;

    public async Task<VatRate> CreateVatRateAsync(VatRate vatRate, string userId)
    {
        // Validazione: rate deve essere tra 0 e 100
        if (vatRate.Rate < 0 || vatRate.Rate > 100)
        {
            throw new InvalidOperationException("VAT rate must be between 0 and 100");
        }

        // Validazione: verifica che il nome non sia duplicato
        var existingRate = await _ctx.VatRates
            .AsNoTracking()
            .FirstOrDefaultAsync(v => string.Equals(v.Name, vatRate.Name, StringComparison.OrdinalIgnoreCase));

        if (existingRate != null)
        {
            throw new InvalidOperationException($"VAT rate with name '{vatRate.Name}' already exists");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            _ctx.VatRates.Add(vatRate);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "CreateVatRate",
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

    public async Task<VatRate?> GetVatRateByIdAsync(Guid vatRateId)
    {
        return await _ctx.VatRates
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vatRateId);
    }

    public async Task<IEnumerable<VatRate>> GetAllVatRatesAsync()
    {
        return await _ctx.VatRates
            .AsNoTracking()
            .OrderBy(v => v.Rate)
            .ToListAsync();
    }

    public async Task<VatRate> UpdateVatRateAsync(Guid vatRateId, VatRate vatRate, string userId)
    {
        if (vatRate.Rate < 0 || vatRate.Rate > 100)
        {
            throw new InvalidOperationException("VAT rate must be between 0 and 100");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var existingRate = await _ctx.VatRates.FindAsync(new object[] { vatRateId }, cts.Token);
            if (existingRate == null)
            {
                throw new InvalidOperationException("VAT rate not found");
            }

            // Verifica nome duplicato (escluso rate corrente)
            var duplicateName = await _ctx.VatRates
                .AsNoTracking()
                .AnyAsync(v => v.Id != vatRateId && string.Equals(v.Name, vatRate.Name, StringComparison.OrdinalIgnoreCase), cts.Token);

            if (duplicateName)
            {
                throw new InvalidOperationException($"VAT rate with name '{vatRate.Name}' already exists");
            }

            existingRate.Name = vatRate.Name;
            existingRate.Rate = vatRate.Rate;

            _ctx.VatRates.Update(existingRate);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "UpdateVatRate",
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

    public async Task DeleteVatRateAsync(Guid vatRateId, string userId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            var vatRate = await _ctx.VatRates.FindAsync(new object[] { vatRateId }, cts.Token);
            if (vatRate == null)
            {
                throw new InvalidOperationException("VAT rate not found");
            }

            // Per sicurezza: verifica che non sia in uso
            // (richiederebbe campo VatRateId in JournalLine - al momento non c'è)

            _ctx.VatRates.Remove(vatRate);
            await _ctx.SaveChangesAsync(cts.Token);

            await _audit.LogAsync(userId, "DeleteVatRate",
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