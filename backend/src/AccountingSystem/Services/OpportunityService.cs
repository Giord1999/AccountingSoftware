using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class OpportunityService(ApplicationDbContext context, ILogger<OpportunityService> logger) : IOpportunityService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<OpportunityService> _logger = logger;

    public async Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity, string userId)
    {
        opportunity.CreatedBy = userId;
        opportunity.CreatedAt = DateTime.UtcNow;

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Opportunity {OpportunityId} created for company {CompanyId}", opportunity.Id, opportunity.CompanyId);
        return opportunity;
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(Guid opportunityId, Guid? companyId = null)
    {
        var query = _context.Opportunities.AsNoTracking().Where(o => o.Id == opportunityId);
        if (companyId.HasValue) query = query.Where(o => o.CompanyId == companyId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Opportunity>> GetOpportunitiesByCompanyAsync(Guid companyId, OpportunityStage? stage = null)
    {
        var query = _context.Opportunities.AsNoTracking().Where(o => o.CompanyId == companyId);

        if (stage.HasValue) query = query.Where(o => o.Stage == stage.Value);

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Opportunity> UpdateOpportunityAsync(Guid opportunityId, Opportunity opportunity, string userId)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId);
        if (existing == null) throw new InvalidOperationException("Opportunity not found");

        existing.Title = opportunity.Title;
        existing.Amount = opportunity.Amount;
        existing.Currency = opportunity.Currency;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.CloseDate = opportunity.CloseDate;
        existing.Description = opportunity.Description;
        existing.UpdatedBy = userId;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Opportunities.Update(existing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Opportunity {OpportunityId} updated", opportunityId);
        return existing;
    }

    public async Task DeleteOpportunityAsync(Guid opportunityId, string userId)
    {
        var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId);
        if (opportunity == null) throw new InvalidOperationException("Opportunity not found");

        _context.Opportunities.Remove(opportunity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Opportunity {OpportunityId} deleted", opportunityId);
    }

    public async Task<Opportunity> UpdateStageAsync(Guid opportunityId, OpportunityStage stage, string userId)
    {
        var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId);
        if (opportunity == null) throw new InvalidOperationException("Opportunity not found");

        opportunity.Stage = stage;
        opportunity.UpdatedBy = userId;
        opportunity.UpdatedAt = DateTime.UtcNow;

        _context.Opportunities.Update(opportunity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Opportunity {OpportunityId} stage updated to {Stage}", opportunityId, stage);
        return opportunity;
    }

    public async Task CloseOpportunityAsync(Guid opportunityId, bool won, string userId)
    {
        var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId);
        if (opportunity == null) throw new InvalidOperationException("Opportunity not found");

        opportunity.Stage = won ? OpportunityStage.ClosedWon : OpportunityStage.ClosedLost;
        opportunity.CloseDate = DateTime.UtcNow;
        opportunity.UpdatedBy = userId;
        opportunity.UpdatedAt = DateTime.UtcNow;

        _context.Opportunities.Update(opportunity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Opportunity {OpportunityId} closed as {Status}", opportunityId, won ? "Won" : "Lost");
    }
}