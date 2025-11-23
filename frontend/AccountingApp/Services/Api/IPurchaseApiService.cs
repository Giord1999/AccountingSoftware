using AccountingApp.Models;

namespace AccountingApp.Services;

public interface IPurchaseApiService
{
    Task<Purchase> CreatePurchaseAsync(CreatePurchaseRequest request);
    Task<Purchase?> GetPurchaseByIdAsync(Guid purchaseId);
    Task<IEnumerable<Purchase>> GetPurchasesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null);
    Task<Purchase> UpdatePurchaseStatusAsync(Guid purchaseId, PurchaseStatus status);
    Task<Purchase> CancelPurchaseAsync(Guid purchaseId, string reason);
}