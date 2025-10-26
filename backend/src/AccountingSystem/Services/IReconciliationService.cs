using AccountingSystem.Models;

namespace AccountingSystem.Services;
public interface IReconciliationService
{
    Task<ReconciliationResult> ReconcileAsync(Guid companyId, Guid accountId, DateTime from, DateTime to);
}

public record ReconciliationResult(Guid AccountId, decimal Balance, IEnumerable<object> UnmatchedTransactions);
