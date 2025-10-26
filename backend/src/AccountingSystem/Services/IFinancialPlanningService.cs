using AccountingSystem.Models.FinancialPlanning;
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