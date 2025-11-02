using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IAnalysisCenterService
{
    Task<AnalysisCenter> CreateAnalysisCenterAsync(AnalysisCenter center, string userId);
    Task<AnalysisCenter?> GetAnalysisCenterByIdAsync(Guid id, Guid? companyId = null);
    Task<IEnumerable<AnalysisCenter>> GetAnalysisCentersByCompanyAsync(Guid companyId, AnalysisCenterType? type = null);
    Task<AnalysisCenter> UpdateAnalysisCenterAsync(Guid id, AnalysisCenter center, string userId);
    Task DeleteAnalysisCenterAsync(Guid id, string userId);
    Task<bool> ValidateCodeAsync(Guid companyId, string code, Guid? excludeId = null);
}