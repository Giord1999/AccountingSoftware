using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Models.FinancialPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Services.FinancialPlanning;

public interface IFinancialPlanService
{
    Task<FinancialPlan> CreateAsync(Guid companyId, CreateFinancialPlanInput input, string userId, CancellationToken ct = default);
    Task<FinancialPlan?> GetAsync(Guid companyId, Guid id, CancellationToken ct = default);
    Task<IEnumerable<FinancialPlan>> ListAsync(Guid companyId, CancellationToken ct = default);
    Task<FinancialPlan> UpdateAsync(Guid companyId, UpdateFinancialPlanInput input, string userId, CancellationToken ct = default);
    Task DeleteAsync(Guid companyId, Guid id, CancellationToken ct = default);
    Task SubmitAsync(Guid companyId, Guid id, string userId, CancellationToken ct = default);
    Task ApproveAsync(Guid companyId, Guid id, string approverId, CancellationToken ct = default);

    // Forecast
    Task<IEnumerable<Forecast>> GenerateForecastsAsync(Guid companyId, Guid financialPlanId, int monthsAhead = 6, double growthFactor = 1.0, int historicalMonths = 12, string? generatedBy = null, CancellationToken ct = default);
    Task<IEnumerable<Forecast>> GetForecastsAsync(Guid companyId, Guid financialPlanId, CancellationToken ct = default);
}

// DTO
public class CreateFinancialPlanInput
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public List<CreateFinancialPlanItemInput> Items { get; set; } = new();
}

public class CreateFinancialPlanItemInput
{
    public Guid? AccountId { get; set; }
    [Required] public DateTime Period { get; set; }
    [Required, StringLength(200)] public string Category { get; set; } = string.Empty;
    [Required] public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class UpdateFinancialPlanInput : CreateFinancialPlanInput
{
    [Required] public Guid Id { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class FinancialPlanService : IFinancialPlanService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FinancialPlanService> _logger;

    public FinancialPlanService(ApplicationDbContext db, ILogger<FinancialPlanService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region CRUD
    public async Task<FinancialPlan> CreateAsync(Guid companyId, CreateFinancialPlanInput input, string userId, CancellationToken ct = default)
    {
        var plan = new FinancialPlan
        {
            CompanyId = companyId,
            Name = input.Name,
            Description = input.Description,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            CreatedBy = userId
        };

        foreach (var item in input.Items)
        {
            plan.Items.Add(new FinancialPlanItem
            {
                AccountId = item.AccountId,
                Period = item.Period,
                Category = item.Category,
                Amount = item.Amount,
                Notes = item.Notes
            });
        }

        plan.TotalAmount = plan.Items.Sum(i => i.Amount);

        _db.FinancialPlans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<FinancialPlan?> GetAsync(Guid companyId, Guid id, CancellationToken ct = default)
    {
        return await _db.FinancialPlans
            .Include(p => p.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, ct);
    }

    public async Task<IEnumerable<FinancialPlan>> ListAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.FinancialPlans
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<FinancialPlan> UpdateAsync(Guid companyId, UpdateFinancialPlanInput input, string userId, CancellationToken ct = default)
    {
        var plan = await _db.FinancialPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == input.Id, ct)
            ?? throw new KeyNotFoundException("Financial plan not found");

        plan.Name = input.Name;
        plan.Description = input.Description;
        plan.StartDate = input.StartDate;
        plan.EndDate = input.EndDate;
        plan.ModifiedBy = userId;
        plan.ModifiedAt = DateTime.UtcNow;

        plan.Items.Clear();
        foreach (var item in input.Items)
        {
            plan.Items.Add(new FinancialPlanItem
            {
                AccountId = item.AccountId,
                Period = item.Period,
                Category = item.Category,
                Amount = item.Amount,
                Notes = item.Notes
            });
        }

        plan.TotalAmount = plan.Items.Sum(i => i.Amount);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task DeleteAsync(Guid companyId, Guid id, CancellationToken ct = default)
    {
        var plan = await _db.FinancialPlans.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, ct);
        if (plan == null) return;
        _db.FinancialPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SubmitAsync(Guid companyId, Guid id, string userId, CancellationToken ct = default)
    {
        var plan = await _db.FinancialPlans.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, ct)
            ?? throw new KeyNotFoundException("Plan not found");
        plan.Status = FinancialPlanStatus.Submitted;
        plan.ModifiedBy = userId;
        plan.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApproveAsync(Guid companyId, Guid id, string approverId, CancellationToken ct = default)
    {
        var plan = await _db.FinancialPlans.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, ct)
            ?? throw new KeyNotFoundException("Plan not found");
        plan.Status = FinancialPlanStatus.Approved;
        plan.ModifiedBy = approverId;
        plan.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
    #endregion

    #region Forecast
    public async Task<IEnumerable<Forecast>> GenerateForecastsAsync(
        Guid companyId,
        Guid financialPlanId,
        int monthsAhead = 6,
        double growthFactor = 1.0,
        int historicalMonths = 12,
        string? generatedBy = null,
        CancellationToken ct = default)
    {
        var plan = await _db.FinancialPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == financialPlanId, ct)
            ?? throw new KeyNotFoundException("Financial plan not found");

        var accountIds = plan.Items.Where(i => i.AccountId.HasValue).Select(i => i.AccountId!.Value).Distinct().ToList();

        if (!accountIds.Any())
        {
            accountIds = await _db.Accounts.Where(a => a.CompanyId == companyId).Select(a => a.Id).ToListAsync(ct);
        }

        var endDate = plan.EndDate.Date;
        var startDate = endDate.AddMonths(-historicalMonths + 1);

        // Join JournalEntries + JournalLines
        var journalData = from je in _db.JournalEntries
                          join jl in _db.JournalLines on je.Id equals jl.JournalEntryId
                          where je.CompanyId == companyId
                                && je.Date >= startDate && je.Date <= endDate
                                && accountIds.Contains(jl.AccountId)
                          select new
                          {
                              je.Date,
                              jl.AccountId,
                              NetAmount = jl.Debit - jl.Credit
                          };

        var rawData = await journalData.ToListAsync(ct);

        var monthlyData = rawData
            .GroupBy(x => new { x.AccountId, x.Date.Year, x.Date.Month })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(x => x.NetAmount)
            })
            .ToList();

        var forecasts = new List<Forecast>();
        var projectionStart = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
        generatedBy ??= "system";

        for (int m = 0; m < monthsAhead; m++)
        {
            var period = projectionStart.AddMonths(m);
            foreach (var acc in accountIds)
            {
                var hist = monthlyData
                    .Where(x => x.AccountId == acc)
                    .OrderByDescending(x => new DateTime(x.Year, x.Month, 1))
                    .Take(historicalMonths)
                    .Select(x => x.Total)
                    .ToList();

                var avg = hist.Any() ? hist.Average() : 0m;
                var std = hist.Count > 1 ? Math.Sqrt(hist.Select(h => Math.Pow((double)(h - avg), 2)).Average()) : 0;

                var forecastAmount = Math.Round(avg * (decimal)growthFactor, 2);
                var confidence = avg == 0 ? 0.5 : Math.Clamp(1 - (std / (double)Math.Abs(avg)), 0.1, 0.99);

                forecasts.Add(new Forecast
                {
                    FinancialPlanId = plan.Id,
                    Period = period,
                    Amount = forecastAmount,
                    Confidence = confidence,
                    GeneratedBy = generatedBy,
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        // Delete old forecasts for this plan
        var old = await _db.Forecasts.Where(f => f.FinancialPlanId == plan.Id).ToListAsync(ct);
        if (old.Any()) _db.Forecasts.RemoveRange(old);

        _db.Forecasts.AddRange(forecasts);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Generated {Count} forecasts for plan {PlanId}", forecasts.Count, plan.Id);

        return forecasts;
    }

    public async Task<IEnumerable<Forecast>> GetForecastsAsync(Guid companyId, Guid financialPlanId, CancellationToken ct = default)
    {
        var exists = await _db.FinancialPlans.AnyAsync(p => p.Id == financialPlanId && p.CompanyId == companyId, ct);
        if (!exists) throw new KeyNotFoundException("Financial plan not found");

        return await _db.Forecasts
            .Where(f => f.FinancialPlanId == financialPlanId)
            .OrderBy(f => f.Period)
            .AsNoTracking()
            .ToListAsync(ct);
    }
    #endregion
}
