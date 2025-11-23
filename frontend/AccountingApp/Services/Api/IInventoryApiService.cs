using AccountingApp.Models;

namespace AccountingApp.Services;

public interface IInventoryApiService
{
    Task<Inventory> CreateInventoryItemAsync(Inventory item);
    Task<Inventory?> GetInventoryItemByIdAsync(Guid itemId);
    Task<IEnumerable<Inventory>> GetInventoryItemsByCompanyAsync(Guid companyId);
    Task<Inventory> UpdateInventoryItemAsync(Guid itemId, Inventory item);
    Task DeleteInventoryItemAsync(Guid itemId);
}