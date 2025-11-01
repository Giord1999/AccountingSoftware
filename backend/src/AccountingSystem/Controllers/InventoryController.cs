using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    // ========== INVENTORY ITEMS ==========

    /// <summary>
    /// Ottiene tutti gli articoli di magazzino per una company
    /// </summary>
    [HttpGet("company/{companyId:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(IEnumerable<Inventory>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInventoryByCompany(Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "Company ID non valido" });

        try
        {
            var items = await _inventoryService.GetInventoryItemsByCompanyAsync(companyId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero inventory per company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene un articolo di magazzino per ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Inventory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? companyId = null)
    {
        if (id == Guid.Empty)
            return BadRequest(new { error = "Inventory ID non valido" });

        try
        {
            var item = await _inventoryService.GetInventoryItemByIdAsync(id, companyId);
            if (item == null)
                return NotFound($"Articolo inventario {id} non trovato");

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero inventory item {ItemId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Crea un nuovo articolo di magazzino
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Inventory), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var item = new Inventory
            {
                CompanyId = request.CompanyId,
                ItemCode = request.ItemCode,
                ItemName = request.ItemName,
                Description = request.Description,
                Category = request.Category,
                UnitOfMeasure = request.UnitOfMeasure,
                QuantityOnHand = request.InitialQuantity,
                ReorderLevel = request.ReorderLevel,
                MaxQuantity = request.MaxQuantity,
                UnitCost = request.UnitCost,
                SalePrice = request.SalePrice,
                Currency = request.Currency ?? "EUR",
                Barcode = request.Barcode,
                Location = request.Location,
                SupplierId = request.SupplierId,
                InventoryAccountId = request.InventoryAccountId,
                CostOfSalesAccountId = request.CostOfSalesAccountId
            };

            var created = await _inventoryService.CreateInventoryItemAsync(item, userId);
            _logger.LogInformation("Creato inventory item {ItemCode} dall'utente {UserId}",
                created.ItemCode, userId);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione creazione inventory item");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione inventory item");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Aggiorna un articolo di magazzino
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Inventory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInventoryRequest request)
    {
        if (id != request.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var item = new Inventory
            {
                Id = request.Id,
                CompanyId = request.CompanyId,
                ItemCode = request.ItemCode,
                ItemName = request.ItemName,
                Description = request.Description,
                Category = request.Category,
                UnitOfMeasure = request.UnitOfMeasure,
                ReorderLevel = request.ReorderLevel,
                MaxQuantity = request.MaxQuantity,
                UnitCost = request.UnitCost,
                SalePrice = request.SalePrice,
                Currency = request.Currency ?? "EUR",
                Barcode = request.Barcode,
                Location = request.Location,
                SupplierId = request.SupplierId,
                InventoryAccountId = request.InventoryAccountId,
                CostOfSalesAccountId = request.CostOfSalesAccountId,
                IsActive = request.IsActive
            };

            var updated = await _inventoryService.UpdateInventoryItemAsync(id, item, userId);
            _logger.LogInformation("Aggiornato inventory item {ItemId} dall'utente {UserId}", id, userId);

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante aggiornamento inventory item {ItemId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Elimina un articolo di magazzino
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _inventoryService.DeleteInventoryItemAsync(id, userId);
            _logger.LogInformation("Eliminato inventory item {ItemId} dall'utente {UserId}", id, userId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante eliminazione inventory item {ItemId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    // ========== INVENTORY MOVEMENTS ==========

    /// <summary>
    /// Crea un movimento di magazzino
    /// </summary>
    [HttpPost("movements")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(InventoryMovement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMovement([FromBody] CreateMovementRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var movement = new InventoryMovement
            {
                CompanyId = request.CompanyId,
                InventoryId = request.InventoryId,
                MovementDate = request.MovementDate,
                Type = request.Type,
                Quantity = request.Quantity,
                UnitCost = request.UnitCost,
                Reference = request.Reference,
                Notes = request.Notes
            };

            var created = await _inventoryService.CreateMovementAsync(movement, userId, request.CreateJournalEntry);
            _logger.LogInformation("Creato inventory movement {Type} per item {InventoryId}",
                request.Type, request.InventoryId);

            return CreatedAtAction(nameof(GetMovementsByInventory),
                new { inventoryId = created.InventoryId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione inventory movement");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene movimenti per un articolo specifico
    /// </summary>
    [HttpGet("{inventoryId:guid}/movements")]
    [ProducesResponseType(typeof(IEnumerable<InventoryMovement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovementsByInventory(Guid inventoryId)
    {
        if (inventoryId == Guid.Empty)
            return BadRequest(new { error = "Inventory ID non valido" });

        try
        {
            var movements = await _inventoryService.GetMovementsByInventoryAsync(inventoryId);
            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero movements per inventory {InventoryId}", inventoryId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene tutti i movimenti per una company (con filtri opzionali)
    /// </summary>
    [HttpGet("company/{companyId:guid}/movements")]
    [ProducesResponseType(typeof(IEnumerable<InventoryMovement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovementsByCompany(
        Guid companyId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "Company ID non valido" });

        try
        {
            var movements = await _inventoryService.GetMovementsByCompanyAsync(companyId, from, to);
            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero movements per company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    // ========== VALUATION & REPORTING ==========

    /// <summary>
    /// Ottiene il valore totale dell'inventario per una company
    /// </summary>
    [HttpGet("company/{companyId:guid}/value")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryValue(Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "Company ID non valido" });

        try
        {
            var value = await _inventoryService.GetInventoryValueAsync(companyId);
            return Ok(new { companyId, totalInventoryValue = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante calcolo valore inventario per company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene livelli di stock per tutti gli articoli
    /// </summary>
    [HttpGet("company/{companyId:guid}/stock-levels")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockLevels(Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "Company ID non valido" });

        try
        {
            var stockLevels = await _inventoryService.GetStockLevelsAsync(companyId);
            return Ok(stockLevels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero stock levels per company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene articoli con giacenza sotto il livello di riordino
    /// </summary>
    [HttpGet("company/{companyId:guid}/low-stock")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockItems(Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "Company ID non valido" });

        try
        {
            var lowStockItems = await _inventoryService.GetLowStockItemsAsync(companyId);
            return Ok(lowStockItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero low stock items per company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    // ========== DTOs ==========

    public record CreateInventoryRequest(
        [Required] Guid CompanyId,
        [Required][StringLength(50)] string ItemCode,
        [Required][StringLength(200)] string ItemName,
        [StringLength(1000)] string? Description,
        [StringLength(100)] string? Category,
        [Required][StringLength(10)] string UnitOfMeasure,
        decimal InitialQuantity,
        decimal? ReorderLevel,
        decimal? MaxQuantity,
        [Required] decimal UnitCost,
        decimal? SalePrice,
        string? Currency,
        [StringLength(50)] string? Barcode,
        [StringLength(100)] string? Location,
        Guid? SupplierId,
        Guid? InventoryAccountId,
        Guid? CostOfSalesAccountId
    );

    public record UpdateInventoryRequest(
        [Required] Guid Id,
        [Required] Guid CompanyId,
        [Required][StringLength(50)] string ItemCode,
        [Required][StringLength(200)] string ItemName,
        [StringLength(1000)] string? Description,
        [StringLength(100)] string? Category,
        [Required][StringLength(10)] string UnitOfMeasure,
        decimal? ReorderLevel,
        decimal? MaxQuantity,
        [Required] decimal UnitCost,
        decimal? SalePrice,
        string? Currency,
        [StringLength(50)] string? Barcode,
        [StringLength(100)] string? Location,
        Guid? SupplierId,
        Guid? InventoryAccountId,
        Guid? CostOfSalesAccountId,
        bool IsActive
    );

    public record CreateMovementRequest(
        [Required] Guid CompanyId,
        [Required] Guid InventoryId,
        [Required] DateTime MovementDate,
        [Required] MovementType Type,
        [Required] decimal Quantity,
        [Required] decimal UnitCost,
        [StringLength(500)] string? Reference,
        [StringLength(1000)] string? Notes,
        bool CreateJournalEntry = true
    );
}