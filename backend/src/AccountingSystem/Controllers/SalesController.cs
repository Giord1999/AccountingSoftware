using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireContabileOrAdmin")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly IInvoiceService _invoiceService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ISalesService salesService,
        IInvoiceService invoiceService,
        UserManager<ApplicationUser> userManager,
        ILogger<SalesController> logger)
    {
        _salesService = salesService;
        _invoiceService = invoiceService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Crea una vendita di merci, genera fattura, scrittura contabile e movimenta magazzino
    /// </summary>
    [HttpPost("create-sale")]
    [ProducesResponseType(typeof(Sale), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreateSale");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var sale = await _salesService.CreateSaleAsync(
                request.CompanyId,
                request.PeriodId,
                request.InventoryId,
                request.VatRateId,
                request.Quantity,
                request.UnitPrice,
                request.CustomerName,
                request.CustomerVatNumber,
                request.ClientiAccountId,
                request.VenditeAccountId,
                request.IvaDebitoAccountId,
                userId);

            _logger.LogInformation("Vendita creata con ID {SaleId}", sale.Id);
            return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione vendita");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la creazione della vendita" });
        }
    }

    /// <summary>
    /// Ottiene una vendita per ID
    /// </summary>
    [HttpGet("sale/{id}")]
    [ProducesResponseType(typeof(Sale), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSale(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Sale ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Ottieni companyId dall'utente
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            if (!user.CompanyId.HasValue)
            {
                _logger.LogWarning("Utente {UserId} non ha una company associata", userId);
                return BadRequest(new { error = "Utente non associato a nessuna azienda" });
            }

            var companyId = user.CompanyId.Value;

            var sale = await _salesService.GetSaleByIdAsync(id, companyId);

            if (sale == null)
            {
                _logger.LogWarning("Vendita {SaleId} non trovata per company {CompanyId}", id, companyId);
                return NotFound(new { error = $"Vendita {id} non trovata" });
            }

            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero vendita {SaleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero della vendita" });
        }
    }

    /// <summary>
    /// Ottiene una fattura per ID
    /// </summary>
    [HttpGet("invoice/{id}")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Invoice ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Ottieni companyId dall'utente
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            if (!user.CompanyId.HasValue)
            {
                _logger.LogWarning("Utente {UserId} non ha una company associata", userId);
                return BadRequest(new { error = "Utente non associato a nessuna azienda" });
            }

            var companyId = user.CompanyId.Value;

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, companyId);

            if (invoice == null)
            {
                _logger.LogWarning("Fattura {InvoiceId} non trovata per company {CompanyId}", id, companyId);
                return NotFound(new { error = $"Fattura {id} non trovata" });
            }

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero fattura {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero della fattura" });
        }
    }

    public record CreateSaleRequest(
        [Required] Guid CompanyId,
        [Required] Guid PeriodId,
        [Required] Guid InventoryId,
        [Required] Guid VatRateId,
        [Required] decimal Quantity,
        [Required] decimal UnitPrice,
        [Required] string CustomerName,
        string? CustomerVatNumber,
        // Account IDs opzionali - se non forniti, cerca per codice
        Guid? ClientiAccountId = null,
        Guid? VenditeAccountId = null,
        Guid? IvaDebitoAccountId = null
    );
}