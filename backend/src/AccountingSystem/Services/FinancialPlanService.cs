using AccountingSystem.Data;
using AccountingSystem.Models.FinancialPlanning;
using AccountingSystem.Services.FinancialPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services
{
    public class FinancialPlanService(ApplicationDbContext context, ILogger<FinancialPlanService> logger) : IFinancialPlanService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<FinancialPlanService> _logger = logger;

        public async Task<IEnumerable<FinancialPlan>> ListAsync(Guid companyId, CancellationToken ct = default)
        {
            return await _context.FinancialPlans
                .Where(fp => fp.CompanyId == companyId)
                .Include(fp => fp.Items)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<FinancialPlan?> GetAsync(Guid companyId, Guid id, CancellationToken ct = default)
        {
            return await _context.FinancialPlans
                .Where(fp => fp.CompanyId == companyId && fp.Id == id)
                .Include(fp => fp.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        public async Task<FinancialPlan> CreateAsync(Guid companyId, CreateFinancialPlanInput input, string userId, CancellationToken ct = default)
        {
            var plan = new FinancialPlan
            {
                CompanyId = companyId,
                Name = input.Name,
                Description = input.Description,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                CurrencyId = input.CurrencyId,
                TotalAmount = input.TotalAmount,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FinancialPlans.Add(plan);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("FinancialPlan {Id} created by {UserId} for company {CompanyId}", plan.Id, userId, companyId);

            return plan;
        }

        public async Task<FinancialPlan> UpdateAsync(Guid companyId, UpdateFinancialPlanInput input, string userId, CancellationToken ct = default)
        {
            var existing = await _context.FinancialPlans
                .FirstOrDefaultAsync(fp => fp.CompanyId == companyId && fp.Id == input.Id, ct);

            if (existing == null)
                throw new InvalidOperationException("FinancialPlan not found.");

            existing.Name = input.Name;
            existing.Description = input.Description;
            existing.StartDate = input.StartDate;
            existing.EndDate = input.EndDate;
            existing.CurrencyId = input.CurrencyId;
            existing.TotalAmount = input.TotalAmount;
            existing.ModifiedBy = userId;
            existing.ModifiedAt = DateTime.UtcNow;

            // Handle concurrency with RowVersion if needed, but omitted for simplicity

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("FinancialPlan {Id} updated by {UserId}", input.Id, userId);

            return existing;
        }

        public async Task SubmitAsync(Guid companyId, Guid id, string userId, CancellationToken ct = default)
        {
            var plan = await _context.FinancialPlans
                .FirstOrDefaultAsync(fp => fp.CompanyId == companyId && fp.Id == id, ct);

            if (plan == null)
                throw new InvalidOperationException("FinancialPlan not found.");

            if (plan.Status != FinancialPlanStatus.Draft)
                throw new InvalidOperationException("Only draft plans can be submitted.");

            plan.Status = FinancialPlanStatus.Submitted;
            plan.ModifiedBy = userId;
            plan.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("FinancialPlan {Id} submitted by {UserId}", id, userId);
        }

        public async Task ApproveAsync(Guid companyId, Guid id, string approver, CancellationToken ct = default)
        {
            var plan = await _context.FinancialPlans
                .FirstOrDefaultAsync(fp => fp.CompanyId == companyId && fp.Id == id, ct);

            if (plan == null)
                throw new InvalidOperationException("FinancialPlan not found.");

            if (plan.Status != FinancialPlanStatus.Submitted)
                throw new InvalidOperationException("Only submitted plans can be approved.");

            plan.Status = FinancialPlanStatus.Approved;
            plan.ModifiedBy = approver;
            plan.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("FinancialPlan {Id} approved by {Approver}", id, approver);
        }

        public async Task<IEnumerable<Forecast>> GenerateForecastsAsync(Guid companyId, Guid financialPlanId, int monthsAhead, double growthFactor, int historicalMonths, string generatedBy, CancellationToken ct = default)
        {
            var plan = await _context.FinancialPlans
                .FirstOrDefaultAsync(fp => fp.CompanyId == companyId && fp.Id == financialPlanId, ct);

            if (plan == null)
                throw new InvalidOperationException("FinancialPlan not found.");

            // Get historical data from FinancialPlanItems
            var historicalEnd = DateTime.UtcNow;
            var historicalStart = historicalEnd.AddMonths(-historicalMonths);

            var historicalAmounts = await _context.FinancialPlanItems
                .Where(item => item.FinancialPlanId == financialPlanId && item.Period >= historicalStart && item.Period <= historicalEnd)
                .GroupBy(item => item.Period.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(item => item.Amount) })
                .ToListAsync(ct);

            if (!historicalAmounts.Any())
                return Enumerable.Empty<Forecast>();

            var averageMonthly = historicalAmounts.Average(h => h.Total);

            var forecasts = new List<Forecast>();
            var currentDate = DateTime.UtcNow;

            for (int i = 1; i <= monthsAhead; i++)
            {
                var period = currentDate.AddMonths(i);
                var projectedAmount = averageMonthly * (decimal)Math.Pow(growthFactor, i);
                var confidence = Math.Max(0.1, 1.0 - (i * 0.1)); // Decreasing confidence

                var forecast = new Forecast
                {
                    FinancialPlanId = financialPlanId,
                    Period = period,
                    Amount = Math.Round(projectedAmount, 2),
                    Confidence = confidence,
                    GeneratedBy = generatedBy,
                    GeneratedAt = DateTime.UtcNow
                };

                forecasts.Add(forecast);
            }

            _context.Forecasts.AddRange(forecasts);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Generated {Count} forecasts for FinancialPlan {Id}", forecasts.Count, financialPlanId);

            return forecasts;
        }

        public async Task<IEnumerable<Forecast>> GetForecastsAsync(Guid companyId, Guid id, CancellationToken ct = default)
        {
            return await _context.Forecasts
                .Include(f => f.FinancialPlan)
                .Where(f => f.FinancialPlanId == id && f.FinancialPlan!.CompanyId == companyId)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}