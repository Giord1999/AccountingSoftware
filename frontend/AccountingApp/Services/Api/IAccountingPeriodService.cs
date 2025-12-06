using AccountingApp.Models;

namespace AccountingApp.Services.Api;

public interface IAccountingPeriodService
{
    Task<IEnumerable<AccountingPeriod>> GetPeriodsByCompanyAsync(Guid companyId);
    Task<AccountingPeriod?> GetPeriodByIdAsync(Guid periodId);
    Task<AccountingPeriod?> GetActivePeriodForDateAsync(Guid companyId, DateTime date);
}