using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IOpportunityService
{
    Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity, string userId);
    Task<Opportunity?> GetOpportunityByIdAsync(Guid opportunityId, Guid? companyId = null);
    Task<IEnumerable<Opportunity>> GetOpportunitiesByCompanyAsync(Guid companyId, OpportunityStage? stage = null);
    Task<Opportunity> UpdateOpportunityAsync(Guid opportunityId, Opportunity opportunity, string userId);
    Task DeleteOpportunityAsync(Guid opportunityId, string userId);
    Task<Opportunity> UpdateStageAsync(Guid opportunityId, OpportunityStage stage, string userId);
    Task CloseOpportunityAsync(Guid opportunityId, bool won, string userId);
}