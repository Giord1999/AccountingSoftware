using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(ApplicationDbContext context, ILogger<ActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Activity> CreateActivityAsync(Activity activity, string userId)
    {
        activity.CreatedBy = userId;
        activity.CreatedAt = DateTime.UtcNow;

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activity {ActivityId} created for company {CompanyId}", activity.Id, activity.CompanyId);
        return activity;
    }

    public async Task<Activity?> GetActivityByIdAsync(Guid activityId, Guid? companyId = null)
    {
        var query = _context.Activities.AsNoTracking().Where(a => a.Id == activityId);
        if (companyId.HasValue) query = query.Where(a => a.CompanyId == companyId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Activity>> GetActivitiesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Activities.AsNoTracking().Where(a => a.CompanyId == companyId);

        if (from.HasValue) query = query.Where(a => a.ScheduledDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ScheduledDate <= to.Value);

        return await query.OrderByDescending(a => a.ScheduledDate).ToListAsync();
    }

    public async Task<Activity> UpdateActivityAsync(Guid activityId, Activity activity, string userId)
    {
        var existing = await _context.Activities.FirstOrDefaultAsync(a => a.Id == activityId);
        if (existing == null) throw new InvalidOperationException("Activity not found");

        existing.Type = activity.Type;
        existing.Subject = activity.Subject;
        existing.Description = activity.Description;
        existing.ScheduledDate = activity.ScheduledDate;
        existing.CompletedDate = activity.CompletedDate;
        existing.Status = activity.Status;
        existing.AssignedTo = activity.AssignedTo;
        existing.UpdatedBy = userId;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Activities.Update(existing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activity {ActivityId} updated", activityId);
        return existing;
    }

    public async Task DeleteActivityAsync(Guid activityId, string userId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == activityId);
        if (activity == null) throw new InvalidOperationException("Activity not found");

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activity {ActivityId} deleted", activityId);
    }

    public async Task<Activity> CompleteActivityAsync(Guid activityId, string userId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == activityId);
        if (activity == null) throw new InvalidOperationException("Activity not found");

        activity.Status = ActivityStatus.Completed;
        activity.CompletedDate = DateTime.UtcNow;
        activity.UpdatedBy = userId;
        activity.UpdatedAt = DateTime.UtcNow;

        _context.Activities.Update(activity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activity {ActivityId} completed", activityId);
        return activity;
    }

    public async Task<IEnumerable<Activity>> GetActivitiesByCustomerAsync(Guid customerId)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.ScheduledDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Activity>> GetActivitiesByLeadAsync(Guid leadId)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.ScheduledDate)
            .ToListAsync();
    }
}