using AccountingSystem.Models.FinancialPlanning;

namespace AccountingSystem.Services
{
    public interface IFinancialPlanService
    {
        Task<IEnumerable<FinancialPlan>> ListAsync(Guid companyId, CancellationToken ct = default);
        Task<FinancialPlan?> GetAsync(Guid companyId, Guid id, CancellationToken ct = default);
        Task<FinancialPlan> CreateAsync(Guid companyId, CreateFinancialPlanInput input, string userId, CancellationToken ct = default);
        Task<FinancialPlan> UpdateAsync(Guid companyId, UpdateFinancialPlanInput input, string userId, CancellationToken ct = default);
        Task SubmitAsync(Guid companyId, Guid id, string userId, CancellationToken ct = default);
        Task ApproveAsync(Guid companyId, Guid id, string approver, CancellationToken ct = default);
        Task<IEnumerable<Forecast>> GenerateForecastsAsync(Guid companyId, Guid financialPlanId, int monthsAhead, double growthFactor, int historicalMonths, string generatedBy, CancellationToken ct = default);
        Task<IEnumerable<Forecast>> GetForecastsAsync(Guid companyId, Guid id, CancellationToken ct = default);
    }
}