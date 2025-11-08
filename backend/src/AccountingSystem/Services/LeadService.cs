using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LeadService> _logger;

    public LeadService(ApplicationDbContext context, ILogger<LeadService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Lead> CreateLeadAsync(Lead lead, string userId)
    {
        lead.CreatedBy = userId;
        lead.CreatedAt = DateTime.UtcNow;

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} created for company {CompanyId}", lead.Id, lead.CompanyId);
        return lead;
    }

    public async Task<Lead?> GetLeadByIdAsync(Guid leadId, Guid? companyId = null)
    {
        var query = _context.Leads.AsNoTracking().Where(l => l.Id == leadId);
        if (companyId.HasValue) query = query.Where(l => l.CompanyId == companyId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Lead>> GetLeadsByCompanyAsync(Guid companyId, LeadStatus? status = null)
    {
        var query = _context.Leads.AsNoTracking().Where(l => l.CompanyId == companyId);

        if (status.HasValue) query = query.Where(l => l.Status == status.Value);

        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
    }

    public async Task<Lead> UpdateLeadAsync(Guid leadId, Lead lead, string userId)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId);
        if (existing == null) throw new InvalidOperationException("Lead not found");

        existing.Name = lead.Name;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Score = lead.Score;
        existing.Notes = lead.Notes;
        existing.QualifiedDate = lead.QualifiedDate;
        existing.UpdatedBy = userId;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Leads.Update(existing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} updated", leadId);
        return existing;
    }

    public async Task DeleteLeadAsync(Guid leadId, string userId)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead == null) throw new InvalidOperationException("Lead not found");

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} deleted", leadId);
    }

    public async Task<Lead> QualifyLeadAsync(Guid leadId, string userId)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead == null) throw new InvalidOperationException("Lead not found");

        lead.Status = LeadStatus.Qualified;
        lead.QualifiedDate = DateTime.UtcNow;
        lead.UpdatedBy = userId;
        lead.UpdatedAt = DateTime.UtcNow;

        _context.Leads.Update(lead);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} qualified", leadId);
        return lead;
    }

    public async Task<Lead> ConvertLeadToCustomerAsync(Guid leadId, string userId)
    {
        var lead = await _context.Leads.Include(l => l.Customer).FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead == null) throw new InvalidOperationException("Lead not found");

        if (lead.CustomerId.HasValue) throw new InvalidOperationException("Lead already converted");

        var customer = new Customer
        {
            CompanyId = lead.CompanyId,
            Name = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            CreatedBy = userId
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        lead.CustomerId = customer.Id;
        lead.Status = LeadStatus.ClosedWon;
        lead.UpdatedBy = userId;
        lead.UpdatedAt = DateTime.UtcNow;

        _context.Leads.Update(lead);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} converted to customer {CustomerId}", leadId, customer.Id);
        return lead;
    }

    public async Task UpdateLeadScoreAsync(Guid leadId, decimal score)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead == null) throw new InvalidOperationException("Lead not found");

        lead.Score = score;
        _context.Leads.Update(lead);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} score updated to {Score}", leadId, score);
    }
}