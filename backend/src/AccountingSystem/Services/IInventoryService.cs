using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IInventoryService
{
    // ========== INVENTORY ITEMS ==========
    Task<Inventory> CreateInventoryItemAsync(Inventory item, string userId);
    Task<Inventory?> GetInventoryItemByIdAsync(Guid itemId, Guid? companyId = null);
    Task<IEnumerable<Inventory>> GetInventoryItemsByCompanyAsync(Guid companyId);
    Task<Inventory> UpdateInventoryItemAsync(Guid itemId, Inventory item, string userId);
    Task DeleteInventoryItemAsync(Guid itemId, string userId);

    // ========== INVENTORY MOVEMENTS ==========
    Task<InventoryMovement> CreateMovementAsync(InventoryMovement movement, string userId, bool createJournalEntry = true);
    Task<IEnumerable<InventoryMovement>> GetMovementsByInventoryAsync(Guid inventoryId);
    Task<IEnumerable<InventoryMovement>> GetMovementsByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null);

    // ========== VALUATION & REPORTING ==========
    Task<decimal> GetInventoryValueAsync(Guid companyId);
    Task<IEnumerable<object>> GetStockLevelsAsync(Guid companyId);
    Task<IEnumerable<object>> GetLowStockItemsAsync(Guid companyId);
}