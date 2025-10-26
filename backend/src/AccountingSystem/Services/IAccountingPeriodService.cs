using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IAccountingPeriodService
{
    Task<AccountingPeriod> CreatePeriodAsync(AccountingPeriod period, string userId);
    Task<AccountingPeriod?> GetPeriodByIdAsync(Guid periodId, Guid? companyId = null);
    Task<IEnumerable<AccountingPeriod>> GetPeriodsByCompanyAsync(Guid companyId);
    Task<AccountingPeriod> ClosePeriodAsync(Guid periodId, string userId);
    Task<AccountingPeriod> ReopenPeriodAsync(Guid periodId, string userId);
    Task DeletePeriodAsync(Guid periodId, string userId);
}