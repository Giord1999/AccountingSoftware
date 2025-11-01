using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        ApplicationDbContext ctx,
        IAuditService audit,
        ILogger<InventoryService> logger)
    {
        _ctx = ctx;
        _audit = audit;
        _logger = logger;
    }

    // ========== INVENTORY ITEMS ==========

    public async Task<Inventory> CreateInventoryItemAsync(Inventory item, string userId)
    {
        if (item == null)
            throw new InvalidOperationException("Inventory item non può essere null");

        if (string.IsNullOrWhiteSpace(item.ItemCode))
            throw new InvalidOperationException("ItemCode è obbligatorio");

        if (string.IsNullOrWhiteSpace(item.ItemName))
            throw new InvalidOperationException("ItemName è obbligatorio");

        // Validazione: verifica unicità ItemCode per company
        var existingItem = await _ctx.Set<Inventory>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.CompanyId == item.CompanyId && i.ItemCode == item.ItemCode);

        if (existingItem != null)
            throw new InvalidOperationException($"Un articolo con codice '{item.ItemCode}' esiste già per questa azienda");

        item.CreatedAt = DateTime.UtcNow;
        item.CreatedBy = userId;

        _ctx.Set<Inventory>().Add(item);
        await _ctx.SaveChangesAsync();

        await _audit.LogAsync(userId, "CREATE_INVENTORY_ITEM",
            $"Creato articolo inventario '{item.ItemCode}' - {item.ItemName} (ID: {item.Id})");

        _logger.LogInformation("Creato inventory item {ItemCode} per company {CompanyId}",
            item.ItemCode, item.CompanyId);

        return item;
    }

    public async Task<Inventory?> GetInventoryItemByIdAsync(Guid itemId, Guid? companyId = null)
    {
        if (itemId == Guid.Empty)
            throw new InvalidOperationException("Inventory item ID non valido");

        var query = _ctx.Set<Inventory>()
            .Include(i => i.InventoryAccount)
            .Include(i => i.CostOfSalesAccount)
            .AsNoTracking();

        if (companyId.HasValue)
            query = query.Where(i => i.CompanyId == companyId.Value);

        return await query.FirstOrDefaultAsync(i => i.Id == itemId);
    }

    public async Task<IEnumerable<Inventory>> GetInventoryItemsByCompanyAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        return await _ctx.Set<Inventory>()
            .Include(i => i.InventoryAccount)
            .Include(i => i.CostOfSalesAccount)
            .Where(i => i.CompanyId == companyId)
            .OrderBy(i => i.ItemCode)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Inventory> UpdateInventoryItemAsync(Guid itemId, Inventory item, string userId)
    {
        if (itemId == Guid.Empty)
            throw new InvalidOperationException("Inventory item ID non valido");

        if (item == null)
            throw new InvalidOperationException("Inventory item non può essere null");

        var existingItem = await _ctx.Set<Inventory>().FirstOrDefaultAsync(i => i.Id == itemId);

        if (existingItem == null)
            throw new InvalidOperationException($"Inventory item {itemId} non trovato");

        // Validazione: verifica unicità ItemCode se modificato
        if (existingItem.ItemCode != item.ItemCode)
        {
            var duplicateCode = await _ctx.Set<Inventory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.CompanyId == item.CompanyId
                    && i.ItemCode == item.ItemCode
                    && i.Id != itemId);

            if (duplicateCode != null)
                throw new InvalidOperationException($"Un articolo con codice '{item.ItemCode}' esiste già");
        }

        // Aggiorna campi
        existingItem.ItemCode = item.ItemCode;
        existingItem.ItemName = item.ItemName;
        existingItem.Description = item.Description;
        existingItem.Category = item.Category;
        existingItem.UnitOfMeasure = item.UnitOfMeasure;
        existingItem.ReorderLevel = item.ReorderLevel;
        existingItem.MaxQuantity = item.MaxQuantity;
        existingItem.UnitCost = item.UnitCost;
        existingItem.SalePrice = item.SalePrice;
        existingItem.Currency = item.Currency;
        existingItem.Barcode = item.Barcode;
        existingItem.Location = item.Location;
        existingItem.SupplierId = item.SupplierId;
        existingItem.InventoryAccountId = item.InventoryAccountId;
        existingItem.CostOfSalesAccountId = item.CostOfSalesAccountId;
        existingItem.IsActive = item.IsActive;
        existingItem.UpdatedAt = DateTime.UtcNow;
        existingItem.UpdatedBy = userId;

        _ctx.Set<Inventory>().Update(existingItem);
        await _ctx.SaveChangesAsync();

        await _audit.LogAsync(userId, "UPDATE_INVENTORY_ITEM",
            $"Aggiornato articolo '{existingItem.ItemCode}' (ID: {itemId})");

        _logger.LogInformation("Aggiornato inventory item {ItemId}", itemId);

        return existingItem;
    }

    public async Task DeleteInventoryItemAsync(Guid itemId, string userId)
    {
        if (itemId == Guid.Empty)
            throw new InvalidOperationException("Inventory item ID non valido");

        var item = await _ctx.Set<Inventory>().FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            throw new InvalidOperationException($"Inventory item {itemId} non trovato");

        // Validazione: verifica che non ci siano movimenti associati
        var hasMovements = await _ctx.Set<InventoryMovement>().AnyAsync(m => m.InventoryId == itemId);
        if (hasMovements)
            throw new InvalidOperationException("Impossibile eliminare l'articolo: esistono movimenti di magazzino associati");

        _ctx.Set<Inventory>().Remove(item);
        await _ctx.SaveChangesAsync();

        await _audit.LogAsync(userId, "DELETE_INVENTORY_ITEM",
            $"Eliminato articolo '{item.ItemCode}' (ID: {itemId})");

        _logger.LogInformation("Eliminato inventory item {ItemId}", itemId);
    }

    // ========== INVENTORY MOVEMENTS ==========

    public async Task<InventoryMovement> CreateMovementAsync(
        InventoryMovement movement,
        string userId,
        bool createJournalEntry = true)
    {
        if (movement == null)
            throw new InvalidOperationException("Inventory movement non può essere null");

        // Validazione: verifica che l'inventory item esista
        var item = await _ctx.Set<Inventory>().FirstOrDefaultAsync(i => i.Id == movement.InventoryId);
        if (item == null)
            throw new InvalidOperationException($"Inventory item {movement.InventoryId} non trovato");

        // Calcola valore totale
        movement.TotalValue = movement.Quantity * movement.UnitCost;
        movement.CreatedAt = DateTime.UtcNow;
        movement.CreatedBy = userId;

        // Aggiorna quantità in magazzino
        item.QuantityOnHand += movement.Quantity;

        // Aggiorna costo medio se è un carico
        if (movement.Quantity > 0 && movement.Type != MovementType.SalesReturn)
        {
            var totalValue = (item.QuantityOnHand * item.UnitCost) + movement.TotalValue;
            item.UnitCost = item.QuantityOnHand > 0 ? totalValue / item.QuantityOnHand : movement.UnitCost;
        }

        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;

        _ctx.Set<InventoryMovement>().Add(movement);
        _ctx.Set<Inventory>().Update(item);

        // Crea journal entry automatico (se richiesto e account configurati)
        if (createJournalEntry && item.InventoryAccountId.HasValue && item.CostOfSalesAccountId.HasValue)
        {
            var journalEntry = await CreateJournalEntryForMovementAsync(movement, item, userId);
            if (journalEntry != null)
            {
                movement.JournalEntryId = journalEntry.Id;
            }
        }

        await _ctx.SaveChangesAsync();

        await _audit.LogAsync(userId, "CREATE_INVENTORY_MOVEMENT",
            $"Movimento {movement.Type} per articolo '{item.ItemCode}': {movement.Quantity} {item.UnitOfMeasure}");

        _logger.LogInformation("Creato inventory movement {Type} per item {ItemId}: qty {Quantity}",
            movement.Type, movement.InventoryId, movement.Quantity);

        return movement;
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsByInventoryAsync(Guid inventoryId)
    {
        if (inventoryId == Guid.Empty)
            throw new InvalidOperationException("Inventory ID non valido");

        return await _ctx.Set<InventoryMovement>()
            .Include(m => m.Inventory)
            .Include(m => m.JournalEntry)
            .Where(m => m.InventoryId == inventoryId)
            .OrderByDescending(m => m.MovementDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsByCompanyAsync(
        Guid companyId,
        DateTime? from = null,
        DateTime? to = null)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        var query = _ctx.Set<InventoryMovement>()
            .Include(m => m.Inventory)
            .Include(m => m.JournalEntry)
            .Where(m => m.CompanyId == companyId);

        if (from.HasValue)
            query = query.Where(m => m.MovementDate >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.MovementDate <= to.Value);

        return await query
            .OrderByDescending(m => m.MovementDate)
            .AsNoTracking()
            .ToListAsync();
    }

    // ========== VALUATION & REPORTING ==========

    public async Task<decimal> GetInventoryValueAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        var items = await _ctx.Set<Inventory>()
            .Where(i => i.CompanyId == companyId && i.IsActive)
            .AsNoTracking()
            .ToListAsync();

        return items.Sum(i => i.QuantityOnHand * i.UnitCost);
    }

    public async Task<IEnumerable<object>> GetStockLevelsAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        var items = await _ctx.Set<Inventory>()
            .Where(i => i.CompanyId == companyId && i.IsActive)
            .OrderBy(i => i.ItemCode)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(i => new
        {
            i.Id,
            i.ItemCode,
            i.ItemName,
            i.Category,
            i.QuantityOnHand,
            i.UnitOfMeasure,
            i.UnitCost,
            TotalValue = i.QuantityOnHand * i.UnitCost,
            i.Location,
            i.ReorderLevel,
            NeedsReorder = i.ReorderLevel.HasValue && i.QuantityOnHand <= i.ReorderLevel.Value
        });
    }

    public async Task<IEnumerable<object>> GetLowStockItemsAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        var items = await _ctx.Set<Inventory>()
            .Where(i => i.CompanyId == companyId
                && i.IsActive
                && i.ReorderLevel.HasValue
                && i.QuantityOnHand <= i.ReorderLevel.Value)
            .OrderBy(i => i.ItemCode)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(i => new
        {
            i.Id,
            i.ItemCode,
            i.ItemName,
            i.QuantityOnHand,
            i.ReorderLevel,
            Shortage = i.ReorderLevel - i.QuantityOnHand,
            i.UnitOfMeasure,
            i.SupplierId
        });
    }

    // ========== PRIVATE HELPERS ==========

    private async Task<JournalEntry?> CreateJournalEntryForMovementAsync(
        InventoryMovement movement,
        Inventory item,
        string userId)
    {
        try
        {
            // Ottieni il periodo contabile attivo per la company
            var activePeriod = await _ctx.AccountingPeriods
                .FirstOrDefaultAsync(p => p.CompanyId == movement.CompanyId
                    && !p.IsClosed
                    && p.Start <= movement.MovementDate
                    && p.End >= movement.MovementDate);

            if (activePeriod == null)
            {
                _logger.LogWarning("Nessun periodo contabile attivo trovato per movement {MovementId}", movement.Id);
                return null;
            }

            var journalEntry = new JournalEntry
            {
                CompanyId = movement.CompanyId,
                PeriodId = activePeriod.Id,
                Date = movement.MovementDate,
                Description = $"Movimento inventario {movement.Type} - {item.ItemName}",
                Currency = item.Currency,
                Status = JournalStatus.Draft,
                Lines = new List<JournalLine>()
            };

            // Logica contabile in base al tipo di movimento
            switch (movement.Type)
            {
                case MovementType.Purchase:
                case MovementType.InitialStock:
                    // DR Inventory (Asset), CR Accounts Payable/Cash
                    journalEntry.Lines.Add(new JournalLine
                    {
                        AccountId = item.InventoryAccountId!.Value,
                        Debit = Math.Abs(movement.TotalValue),
                        Credit = 0,
                        Narrative = $"Carico inventario {item.ItemCode}"
                    });
                    break;

                case MovementType.Sale:
                    // DR Cost of Sales (Expense), CR Inventory (Asset)
                    if (item.CostOfSalesAccountId.HasValue)
                    {
                        journalEntry.Lines.Add(new JournalLine
                        {
                            AccountId = item.CostOfSalesAccountId.Value,
                            Debit = Math.Abs(movement.TotalValue),
                            Credit = 0,
                            Narrative = $"Costo venduto {item.ItemCode}"
                        });
                        journalEntry.Lines.Add(new JournalLine
                        {
                            AccountId = item.InventoryAccountId!.Value,
                            Debit = 0,
                            Credit = Math.Abs(movement.TotalValue),
                            Narrative = $"Scarico inventario {item.ItemCode}"
                        });
                    }
                    break;

                case MovementType.Adjustment:
                    // Rettifica: può essere +/- in base alla quantità
                    if (movement.Quantity > 0)
                    {
                        // Carico: DR Inventory
                        journalEntry.Lines.Add(new JournalLine
                        {
                            AccountId = item.InventoryAccountId!.Value,
                            Debit = Math.Abs(movement.TotalValue),
                            Credit = 0,
                            Narrative = $"Rettifica positiva {item.ItemCode}"
                        });
                    }
                    else
                    {
                        // Scarico: CR Inventory
                        journalEntry.Lines.Add(new JournalLine
                        {
                            AccountId = item.InventoryAccountId!.Value,
                            Debit = 0,
                            Credit = Math.Abs(movement.TotalValue),
                            Narrative = $"Rettifica negativa {item.ItemCode}"
                        });
                    }
                    break;
            }

            // Aggiungi solo se ci sono righe
            if (journalEntry.Lines.Any())
            {
                _ctx.JournalEntries.Add(journalEntry);

                // ✅ FIX WARNING S1172: Utilizzo del parametro userId
                await _audit.LogAsync(userId, "CREATE_JOURNAL_FROM_INVENTORY",
                    $"Creato journal entry automatico per movimento {movement.Type} - articolo '{item.ItemCode}' (Movement ID: {movement.Id})");

                return journalEntry;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione journal entry per movement {MovementId}", movement.Id);
            return null;
        }
    }
}