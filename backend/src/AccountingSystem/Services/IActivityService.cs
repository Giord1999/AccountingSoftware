using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IActivityService
{
    Task<Activity> CreateActivityAsync(Activity activity, string userId);
    Task<Activity?> GetActivityByIdAsync(Guid activityId, Guid? companyId = null);
    Task<IEnumerable<Activity>> GetActivitiesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null);
    Task<Activity> UpdateActivityAsync(Guid activityId, Activity activity, string userId);
    Task DeleteActivityAsync(Guid activityId, string userId);
    Task<Activity> CompleteActivityAsync(Guid activityId, string userId);
    Task<IEnumerable<Activity>> GetActivitiesByCustomerAsync(Guid customerId);
    Task<IEnumerable<Activity>> GetActivitiesByLeadAsync(Guid leadId);
}