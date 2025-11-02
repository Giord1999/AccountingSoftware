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
public class PurchaseController : ControllerBase
{
    private readonly IAccountingService _accountingService;
    private readonly IVatRateService _vatRateService;
    private readonly IInventoryService _inventoryService;
    private readonly IInvoiceService _invoiceService;
    private readonly IPurchaseService _purchaseService;
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PurchaseController> _logger;

    public PurchaseController(
        IAccountingService accountingService,
        IVatRateService vatRateService,
        IInventoryService inventoryService,
        IInvoiceService invoiceService,
        IPurchaseService purchaseService,
        IAccountService accountService,
        UserManager<ApplicationUser> userManager,
        ILogger<PurchaseController> logger)
    {
        _accountingService = accountingService;
        _vatRateService = vatRateService;
        _inventoryService = inventoryService;
        _invoiceService = invoiceService;
        _purchaseService = purchaseService;
        _accountService = accountService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Crea un acquisto di merci, genera fattura, scrittura contabile e movimenta magazzino
    /// </summary>
    [HttpPost("create-purchase")]
    [ProducesResponseType(typeof(Purchase), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreatePurchase");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Recupera tasso IVA (es. 22%)
            var vatRate = await _vatRateService.GetVatRateByIdAsync(request.VatRateId);
            if (vatRate == null)
            {
                return BadRequest(new { error = "Tasso IVA non trovato" });
            }

            // Calcoli
            decimal subTotal = request.Quantity * request.UnitPrice;
            decimal vatAmount = subTotal * (vatRate.Rate / 100);
            decimal totalAmount = subTotal + vatAmount;

            // Log dell'importo totale calcolato per tracciabilità
            _logger.LogInformation(
                "Calcolo acquisto: SubTotal={SubTotal}, VAT={VatAmount}, Total={TotalAmount}",
                subTotal, vatAmount, totalAmount);

            // Recupera articolo inventario
            var inventoryItem = await _inventoryService.GetInventoryItemByIdAsync(request.InventoryId, request.CompanyId);
            if (inventoryItem == null)
            {
                return BadRequest(new { error = "Articolo non disponibile" });
            }

            // Recupera e valida i conti contabili
            Guid fornitoriAccountId;
            Guid acquistiAccountId;
            Guid ivaCreditoAccountId;

            // Se gli AccountId sono forniti nella richiesta, usali
            if (request.FornitoriAccountId.HasValue && request.AcquistiAccountId.HasValue && request.IvaCreditoAccountId.HasValue)
            {
                // Valida che i conti esistano
                var fornitoriAccount = await _accountService.GetAccountByIdAsync(request.FornitoriAccountId.Value, request.CompanyId);
                var acquistiAccount = await _accountService.GetAccountByIdAsync(request.AcquistiAccountId.Value, request.CompanyId);
                var ivaCreditoAccount = await _accountService.GetAccountByIdAsync(request.IvaCreditoAccountId.Value, request.CompanyId);

                if (fornitoriAccount == null)
                    return BadRequest(new { error = "Conto Fornitori non trovato" });
                if (acquistiAccount == null)
                    return BadRequest(new { error = "Conto Acquisti non trovato" });
                if (ivaCreditoAccount == null)
                    return BadRequest(new { error = "Conto IVA a Credito non trovato" });

                fornitoriAccountId = fornitoriAccount.Id;
                acquistiAccountId = acquistiAccount.Id;
                ivaCreditoAccountId = ivaCreditoAccount.Id;
            }
            else
            {
                // Altrimenti, cerca i conti per codice (backward compatibility o configurazione automatica)
                var accounts = await _accountService.GetAccountsByCompanyAsync(request.CompanyId);
                var accountsList = accounts.ToList();

                var fornitoriAccount = accountsList.FirstOrDefault(a => a.Code == "210000");
                var acquistiAccount = accountsList.FirstOrDefault(a => a.Code == "600000");
                var ivaCreditoAccount = accountsList.FirstOrDefault(a => a.Code == "150000");

                if (fornitoriAccount == null || acquistiAccount == null || ivaCreditoAccount == null)
                {
                    return BadRequest(new
                    {
                        error = "Piano dei conti non configurato. Fornire gli AccountId nella richiesta o configurare i conti standard (210000, 600000, 150000)",
                        missingAccounts = new
                        {
                            fornitori = fornitoriAccount == null,
                            acquisti = acquistiAccount == null,
                            ivaCredito = ivaCreditoAccount == null
                        }
                    });
                }

                fornitoriAccountId = fornitoriAccount.Id;
                acquistiAccountId = acquistiAccount.Id;
                ivaCreditoAccountId = ivaCreditoAccount.Id;
            }

            // Crea acquisto integrato usando il service
            var purchase = await _purchaseService.CreatePurchaseAsync(
                request.CompanyId,
                request.PeriodId,
                request.InventoryId,
                request.VatRateId,
                request.Quantity,
                request.UnitPrice,
                request.SupplierName,
                request.SupplierVatNumber,
                fornitoriAccountId,
                acquistiAccountId,
                ivaCreditoAccountId,
                userId);

            _logger.LogInformation(
                "Acquisto {PurchaseId} creato con fattura {InvoiceId} e journal {JournalId}. Totale: {TotalAmount}",
                purchase.Id, purchase.InvoiceId, purchase.JournalEntryId, totalAmount);

            return CreatedAtAction(nameof(GetPurchase), new { id = purchase.Id }, purchase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione acquisto");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la creazione dell'acquisto" });
        }
    }

    /// <summary>
    /// Ottiene un acquisto per ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Purchase), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPurchase(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Purchase ID non valido" });
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

            var purchase = await _purchaseService.GetPurchaseByIdAsync(id, companyId);

            if (purchase == null)
            {
                _logger.LogWarning("Acquisto {PurchaseId} non trovato per company {CompanyId}", id, companyId);
                return NotFound(new { error = $"Acquisto {id} non trovato" });
            }

            return Ok(purchase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero acquisto {PurchaseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero dell'acquisto" });
        }
    }

    /// <summary>
    /// Ottiene il journal entry associato a un acquisto
    /// </summary>
    [HttpGet("{id:guid}/journal-entry")]
    [ProducesResponseType(typeof(JournalEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPurchaseJournalEntry(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Purchase ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user?.CompanyId == null)
            {
                return BadRequest(new { error = "Utente non associato a nessuna azienda" });
            }

            var purchase = await _purchaseService.GetPurchaseByIdAsync(id, user.CompanyId.Value);

            if (purchase == null)
            {
                return NotFound(new { error = $"Acquisto {id} non trovato" });
            }

            if (!purchase.JournalEntryId.HasValue)
            {
                return NotFound(new { error = "Nessun journal entry associato a questo acquisto" });
            }

            // Utilizza _accountingService per recuperare il journal entry
            var journalEntry = await _accountingService.GetJournalByIdAsync(purchase.JournalEntryId.Value);

            if (journalEntry == null)
            {
                return NotFound(new { error = "Journal entry non trovato" });
            }

            return Ok(journalEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero journal entry per acquisto {PurchaseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene tutti gli acquisti per una company
    /// </summary>
    [HttpGet("company/{companyId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<Purchase>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPurchasesByCompany(
        Guid companyId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (companyId == Guid.Empty)
        {
            return BadRequest(new { error = "Company ID non valido" });
        }

        try
        {
            await ValidateCompanyAccessAsync(companyId);

            var purchases = await _purchaseService.GetPurchasesByCompanyAsync(companyId, from, to);

            _logger.LogInformation(
                "Recuperati {Count} acquisti per company {CompanyId}",
                purchases.Count(), companyId);

            return Ok(purchases);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato a company {CompanyId}", companyId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero acquisti per company {CompanyId}", companyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero degli acquisti" });
        }
    }

    /// <summary>
    /// Ottiene tutti i journal entries per gli acquisti di una company in un periodo
    /// </summary>
    [HttpGet("company/{companyId:guid}/journal-entries")]
    [ProducesResponseType(typeof(IEnumerable<JournalEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPurchaseJournalEntriesByCompany(
        Guid companyId,
        [FromQuery] Guid? periodId = null)
    {
        if (companyId == Guid.Empty)
        {
            return BadRequest(new { error = "Company ID non valido" });
        }

        try
        {
            await ValidateCompanyAccessAsync(companyId);

            // Recupera tutti gli acquisti
            var purchases = await _purchaseService.GetPurchasesByCompanyAsync(companyId);

            // Filtra per periodo se specificato
            if (periodId.HasValue)
            {
                purchases = purchases.Where(p => p.PeriodId == periodId.Value);
            }

            // Recupera i journal entry IDs
            var journalEntryIds = purchases
                .Where(p => p.JournalEntryId.HasValue)
                .Select(p => p.JournalEntryId!.Value)
                .ToList();

            if (!journalEntryIds.Any())
            {
                return Ok(new List<JournalEntry>());
            }

            // Utilizza _accountingService per recuperare tutti i journal entries
            var journalEntries = new List<JournalEntry>();
            foreach (var journalId in journalEntryIds)
            {
                var journal = await _accountingService.GetJournalByIdAsync(journalId);
                if (journal != null)
                {
                    journalEntries.Add(journal);
                }
            }

            _logger.LogInformation(
                "Recuperati {Count} journal entries per acquisti della company {CompanyId}",
                journalEntries.Count, companyId);

            return Ok(journalEntries);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato a company {CompanyId}", companyId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero journal entries acquisti per company {CompanyId}", companyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Aggiorna lo stato di un acquisto
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(Purchase), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePurchaseStatus(
        Guid id,
        [FromBody] UpdatePurchaseStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var purchase = await _purchaseService.UpdatePurchaseStatusAsync(id, request.Status, userId);

            _logger.LogInformation(
                "Stato acquisto {PurchaseId} aggiornato a {Status} da {UserId}",
                id, request.Status, userId);

            return Ok(purchase);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore aggiornamento stato acquisto {PurchaseId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante aggiornamento stato acquisto {PurchaseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Annulla un acquisto
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Purchase), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPurchase(Guid id, [FromBody] CancelPurchaseRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var purchase = await _purchaseService.CancelPurchaseAsync(id, request.Reason, userId);

            _logger.LogWarning(
                "Acquisto {PurchaseId} annullato da {UserId}. Motivo: {Reason}",
                id, userId, request.Reason);

            return Ok(purchase);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore annullamento acquisto {PurchaseId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante annullamento acquisto {PurchaseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene la configurazione dei conti per gli acquisti
    /// </summary>
    [HttpGet("account-configuration/{companyId:guid}")]
    [ProducesResponseType(typeof(PurchaseAccountConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPurchaseAccountConfiguration(Guid companyId)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);

            var config = await _purchaseService.GetPurchaseAccountConfigurationAsync(companyId);

            if (config == null)
            {
                return NotFound(new { error = "Configurazione conti acquisti non trovata" });
            }

            return Ok(config);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero configurazione conti acquisti");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Configura i conti contabili per gli acquisti
    /// </summary>
    [HttpPut("account-configuration/{companyId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PurchaseAccountConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertPurchaseAccountConfiguration(
        Guid companyId,
        [FromBody] UpsertPurchaseAccountConfigurationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await ValidateCompanyAccessAsync(companyId);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Valida che i conti esistano
            var payablesAccount = await _accountService.GetAccountByIdAsync(request.PayablesAccountId, companyId);
            var expenseAccount = await _accountService.GetAccountByIdAsync(request.ExpenseAccountId, companyId);
            var vatReceivableAccount = await _accountService.GetAccountByIdAsync(request.VatReceivableAccountId, companyId);

            if (payablesAccount == null || expenseAccount == null || vatReceivableAccount == null)
            {
                return BadRequest(new { error = "Uno o più conti forniti non sono validi" });
            }

            // Validazione categorie conti
            if (payablesAccount.Category != AccountCategory.Liability)
            {
                return BadRequest(new { error = "Il conto Debiti vs Fornitori deve essere di categoria Liability" });
            }

            if (expenseAccount.Category != AccountCategory.Expense)
            {
                return BadRequest(new { error = "Il conto Acquisti deve essere di categoria Expense" });
            }

            if (vatReceivableAccount.Category != AccountCategory.Asset)
            {
                return BadRequest(new { error = "Il conto IVA a Credito deve essere di categoria Asset" });
            }

            var config = await _purchaseService.UpsertPurchaseAccountConfigurationAsync(
                companyId,
                request.PayablesAccountId,
                request.ExpenseAccountId,
                request.VatReceivableAccountId,
                userId);

            _logger.LogInformation(
                "Configurazione conti acquisti aggiornata per company {CompanyId} da {UserId}",
                companyId, userId);

            return Ok(config);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Accesso negato");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore aggiornamento configurazione conti acquisti");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene una fattura per ID (backward compatibility)
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

    // ==================== PRIVATE HELPERS ====================

    private async Task ValidateCompanyAccessAsync(Guid companyId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && user?.CompanyId.HasValue == true && user.CompanyId != companyId)
        {
            _logger.LogWarning("Utente {UserId} negato accesso a company {CompanyId}", userId, companyId);
            throw new UnauthorizedAccessException("Non hai accesso a questa azienda");
        }
    }

    // ==================== DTO CLASSES ====================

    public record CreatePurchaseRequest(
        [Required] Guid CompanyId,
        [Required] Guid PeriodId,
        [Required] Guid InventoryId,
        [Required] Guid VatRateId,
        [Required] decimal Quantity,
        [Required] decimal UnitPrice,
        [Required] string SupplierName,
        string? SupplierVatNumber,
        // Account IDs opzionali - se non forniti, cerca per codice
        Guid? FornitoriAccountId = null,
        Guid? AcquistiAccountId = null,
        Guid? IvaCreditoAccountId = null
    );

    public record UpdatePurchaseStatusRequest(
        [Required] PurchaseStatus Status
    );

    public record CancelPurchaseRequest(
        [Required][StringLength(500)] string Reason
    );

    public record UpsertPurchaseAccountConfigurationRequest(
        [Required] Guid PayablesAccountId,
        [Required] Guid ExpenseAccountId,
        [Required] Guid VatReceivableAccountId
    );
}