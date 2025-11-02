using AccountingSystem.Controllers;
using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvoiceController(
        IInvoiceService invoiceService,
        ILogger<InvoiceController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _invoiceService = invoiceService;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Ottiene tutte le fatture per una specifica company
    /// </summary>
    [HttpGet("company/{companyId:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoicesByCompany(
        Guid companyId,
        [FromQuery] InvoiceType? type = null,
        [FromQuery] InvoiceStatus? status = null)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);
            var invoices = await _invoiceService.GetInvoicesByCompanyAsync(companyId, type, status);
            return Ok(invoices);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero fatture per company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene una fattura specifica per ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = User.IsInRole("Admin");

            var companyFilter = isAdmin ? (Guid?)null : user?.CompanyId;

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, companyFilter);

            if (invoice == null)
                return NotFound(new { error = $"Fattura {id} non trovata" });

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Crea una nuova fattura
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await ValidateCompanyAccessAsync(request.CompanyId);
            var userId = GetUserId();

            var invoice = new Invoice
            {
                CompanyId = request.CompanyId,
                InvoiceNumber = request.InvoiceNumber,
                Type = request.Type,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                CustomerName = request.CustomerName,
                CustomerVatNumber = request.CustomerVatNumber,
                CustomerAddress = request.CustomerAddress,
                CustomerCity = request.CustomerCity,
                CustomerPostalCode = request.CustomerPostalCode,
                CustomerCountry = request.CustomerCountry,
                Currency = request.Currency ?? "EUR",
                Notes = request.Notes,
                PaymentTerms = request.PaymentTerms,
                PeriodId = request.PeriodId,
                Lines = request.Lines.Select((l, index) => new InvoiceLine
                {
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    VatRateId = l.VatRateId,
                    AccountId = l.AccountId,
                    Notes = l.Notes,
                    LineNumber = index + 1
                }).ToList()
            };

            var created = await _invoiceService.CreateInvoiceAsync(invoice, userId);

            _logger.LogInformation("Fattura {InvoiceNumber} creata con successo", created.InvoiceNumber);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione fattura");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore creazione fattura");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Aggiorna una fattura esistente (solo se in stato Draft)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceRequest request)
    {
        if (id != request.Id)
            return BadRequest(new { error = "ID mismatch" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();

            var invoice = new Invoice
            {
                Id = id,
                InvoiceNumber = request.InvoiceNumber,
                Type = request.Type,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                CustomerName = request.CustomerName,
                CustomerVatNumber = request.CustomerVatNumber,
                CustomerAddress = request.CustomerAddress,
                CustomerCity = request.CustomerCity,
                CustomerPostalCode = request.CustomerPostalCode,
                CustomerCountry = request.CustomerCountry,
                Currency = request.Currency ?? "EUR",
                Notes = request.Notes,
                PaymentTerms = request.PaymentTerms,
                Lines = request.Lines.Select((l, index) => new InvoiceLine
                {
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    VatRateId = l.VatRateId,
                    AccountId = l.AccountId,
                    Notes = l.Notes,
                    LineNumber = index + 1
                }).ToList()
            };

            var updated = await _invoiceService.UpdateInvoiceAsync(id, invoice, userId);

            _logger.LogInformation("Fattura {InvoiceId} aggiornata", id);

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore aggiornamento fattura {InvoiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore aggiornamento fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Elimina una fattura (solo se in stato Draft)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _invoiceService.DeleteInvoiceAsync(id, userId);

            _logger.LogInformation("Fattura {InvoiceId} eliminata", id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore eliminazione fattura {InvoiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore eliminazione fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Emette una fattura (cambio stato da Draft a Issued)
    /// </summary>
    [HttpPost("{id:guid}/issue")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    public async Task<IActionResult> Issue(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var invoice = await _invoiceService.IssueInvoiceAsync(id, userId);

            _logger.LogInformation("Fattura {InvoiceNumber} emessa", invoice.InvoiceNumber);

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore emissione fattura {InvoiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore emissione fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Registra contabilmente una fattura (crea Journal Entry automatico)
    /// </summary>
    [HttpPost("{id:guid}/post")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var invoice = await _invoiceService.PostInvoiceAsync(id, userId);

            _logger.LogInformation("Fattura {InvoiceNumber} registrata contabilmente - Journal Entry: {JournalId}",
                invoice.InvoiceNumber, invoice.JournalEntryId);

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore posting fattura {InvoiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore posting fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Annulla una fattura
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelInvoiceRequest request)
    {
        try
        {
            var userId = GetUserId();
            var invoice = await _invoiceService.CancelInvoiceAsync(id, request.Reason, userId);

            _logger.LogWarning("Fattura {InvoiceNumber} annullata: {Reason}",
                invoice.InvoiceNumber, request.Reason);

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore annullamento fattura {InvoiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore annullamento fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Registra un pagamento per una fattura
    /// </summary>
    [HttpPost("{id:guid}/payment")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();
            var invoice = await _invoiceService.RecordPaymentAsync(id, request.Amount, request.PaymentDate, userId);

            _logger.LogInformation("Pagamento {Amount} registrato per fattura {InvoiceNumber}",
                request.Amount, invoice.InvoiceNumber);

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore registrazione pagamento fattura {InvoiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore registrazione pagamento fattura {InvoiceId}", id);
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene il riepilogo fatture per company
    /// </summary>
    [HttpGet("company/{companyId:guid}/summary")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid companyId, [FromQuery] Guid? periodId = null)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);
            var summary = await _invoiceService.GetInvoicesSummaryAsync(companyId, periodId);
            return Ok(summary);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero summary fatture");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene fatture scadute
    /// </summary>
    [HttpGet("company/{companyId:guid}/overdue")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue(Guid companyId)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);
            var invoices = await _invoiceService.GetOverdueInvoicesAsync(companyId);
            return Ok(invoices);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero fatture scadute");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Report crediti per età (Aged Receivables)
    /// </summary>
    [HttpGet("company/{companyId:guid}/aged-receivables")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgedReceivables(Guid companyId, [FromQuery] DateTime? asOfDate = null)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);
            var date = asOfDate ?? DateTime.UtcNow;
            var report = await _invoiceService.GetAgedReceivablesAsync(companyId, date);
            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione aged receivables");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Report debiti per età (Aged Payables)
    /// </summary>
    [HttpGet("company/{companyId:guid}/aged-payables")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgedPayables(Guid companyId, [FromQuery] DateTime? asOfDate = null)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);
            var date = asOfDate ?? DateTime.UtcNow;
            var report = await _invoiceService.GetAgedPayablesAsync(companyId, date);
            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione aged payables");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    // ==================== PRIVATE HELPERS ====================

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    }

    private async Task ValidateCompanyAccessAsync(Guid companyId)
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && user?.CompanyId.HasValue == true && user.CompanyId != companyId)
        {
            _logger.LogWarning("Utente {UserId} negato accesso a company {CompanyId}", userId, companyId);
            throw new UnauthorizedAccessException("Non hai accesso a questa azienda");
        }
    }

    // ==================== DTO CLASSES ====================

    public record CreateInvoiceRequest(
        [Required] Guid CompanyId,
        [Required][StringLength(50)] string InvoiceNumber,
        [Required] InvoiceType Type,
        [Required] DateTime IssueDate,
        DateTime? DueDate,
        [Required][StringLength(200)] string CustomerName,
        string? CustomerVatNumber,
        string? CustomerAddress,
        string? CustomerCity,
        string? CustomerPostalCode,
        string? CustomerCountry,
        string? Currency,
        string? Notes,
        string? PaymentTerms,
        Guid? PeriodId,
        [Required] List<CreateInvoiceLineRequest> Lines
    );

    public record CreateInvoiceLineRequest(
        [Required][StringLength(200)] string Description,
        [Range(0.0001, double.MaxValue)] decimal Quantity,
        [Range(0, double.MaxValue)] decimal UnitPrice,
        Guid? VatRateId,
        Guid? AccountId,
        string? Notes
    );

    public record UpdateInvoiceRequest(
        [Required] Guid Id,
        [Required][StringLength(50)] string InvoiceNumber,
        [Required] InvoiceType Type,
        [Required] DateTime IssueDate,
        DateTime? DueDate,
        [Required][StringLength(200)] string CustomerName,
        string? CustomerVatNumber,
        string? CustomerAddress,
        string? CustomerCity,
        string? CustomerPostalCode,
        string? CustomerCountry,
        string? Currency,
        string? Notes,
        string? PaymentTerms,
        [Required] List<CreateInvoiceLineRequest> Lines
    );

    public record CancelInvoiceRequest(
        [Required][StringLength(500)] string Reason
    );

    public record RecordPaymentRequest(
        [Required][Range(0.01, double.MaxValue)] decimal Amount,
        [Required] DateTime PaymentDate
    );
}