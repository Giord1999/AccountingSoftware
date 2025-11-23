using AccountingApp.Models;

namespace AccountingApp.Services;

public interface ILeadApiService
{
    Task<Lead> CreateLeadAsync(Lead lead);
    Task<Lead?> GetLeadByIdAsync(Guid leadId);
    Task<IEnumerable<Lead>> GetLeadsByCompanyAsync(Guid companyId, LeadStatus? status = null);
    Task<Lead> UpdateLeadAsync(Guid leadId, Lead lead);
    Task DeleteLeadAsync(Guid leadId);
    Task<Lead> QualifyLeadAsync(Guid leadId);
    Task<Lead> ConvertLeadToCustomerAsync(Guid leadId);
}