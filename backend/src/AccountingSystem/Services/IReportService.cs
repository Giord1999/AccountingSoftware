using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IReportService
{
    Task<IEnumerable<BalanceSheetLine>> GetBalanceSheetAsync(Guid companyId, Guid periodId);
    Task<IEnumerable<PLLine>> GetProfitAndLossAsync(Guid companyId, Guid periodId);
    Task<TrialBalanceSummary> GetTrialBalanceSummaryAsync(Guid companyId, Guid periodId);
    Task<DashboardKpi> GetDashboardKpiAsync(Guid companyId, Guid periodId);
}

public record BalanceSheetLine(string AccountCode, string AccountName, AccountCategory Category, decimal Balance);
public record PLLine(string AccountCode, string AccountName, decimal Amount);
public record TrialBalanceSummary(decimal TotalDebit, decimal TotalCredit, decimal Difference);
public record DashboardKpi(decimal CurrentAssets, decimal CurrentLiabilities, decimal NetProfit, decimal Revenue, decimal Expenses);
