using AccountingApp.Models;

namespace AccountingApp.Services.Api;

public interface IAccountingPeriodApiService
{
    Task<IEnumerable<AccountingPeriod>> GetPeriodsByCompanyAsync(Guid companyId);
    Task<AccountingPeriod?> GetPeriodByIdAsync(Guid periodId);
}