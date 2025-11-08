using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface ILeadService
{
    Task<Lead> CreateLeadAsync(Lead lead, string userId);
    Task<Lead?> GetLeadByIdAsync(Guid leadId, Guid? companyId = null);
    Task<IEnumerable<Lead>> GetLeadsByCompanyAsync(Guid companyId, LeadStatus? status = null);
    Task<Lead> UpdateLeadAsync(Guid leadId, Lead lead, string userId);
    Task DeleteLeadAsync(Guid leadId, string userId);
    Task<Lead> QualifyLeadAsync(Guid leadId, string userId);
    Task<Lead> ConvertLeadToCustomerAsync(Guid leadId, string userId);
    Task UpdateLeadScoreAsync(Guid leadId, decimal score);
}