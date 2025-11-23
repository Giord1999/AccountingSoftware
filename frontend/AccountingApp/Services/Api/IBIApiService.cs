using AccountingApp.Models;

namespace AccountingApp.Services;

public interface IBIApiService
{
    Task<BIDashboardResult> GenerateDashboardAsync(Guid companyId, Guid? periodId = null, DateTime? startDate = null, DateTime? endDate = null);
}