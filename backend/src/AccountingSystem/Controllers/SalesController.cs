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
    private readonly IAccountingService _accountingService;
    private readonly IVatRateService _vatRateService;
    private readonly IInventoryService _inventoryService;
    private readonly IInvoiceService _invoiceService;
    private readonly IAccountService _accountService; // Aggiunto
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        IAccountingService accountingService,
        IVatRateService vatRateService,
        IInventoryService inventoryService,
        IInvoiceService invoiceService,
        IAccountService accountService, // Aggiunto
        UserManager<ApplicationUser> userManager,
        ILogger<SalesController> logger)
    {
        _accountingService = accountingService;
        _vatRateService = vatRateService;
        _inventoryService = inventoryService;
        _invoiceService = invoiceService;
        _accountService = accountService; // Aggiunto
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Crea una vendita di merci, genera fattura, scrittura contabile e movimenta magazzino
    /// </summary>
    [HttpPost("create-sale")]
    [ProducesResponseType(typeof(Invoice), StatusCodes.Status201Created)]
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

            // Recupera articolo inventario
            var inventoryItem = await _inventoryService.GetInventoryItemByIdAsync(request.InventoryId, request.CompanyId);
            if (inventoryItem == null || inventoryItem.QuantityOnHand < request.Quantity)
            {
                return BadRequest(new { error = "Articolo non disponibile o quantità insufficiente" });
            }

            // Recupera e valida i conti contabili
            Guid clientiAccountId;
            Guid venditeAccountId;
            Guid ivaDebitoAccountId;

            // Se gli AccountId sono forniti nella richiesta, usali
            if (request.ClientiAccountId.HasValue && request.VenditeAccountId.HasValue && request.IvaDebitoAccountId.HasValue)
            {
                // Valida che i conti esistano
                var clientiAccount = await _accountService.GetAccountByIdAsync(request.ClientiAccountId.Value, request.CompanyId);
                var venditeAccount = await _accountService.GetAccountByIdAsync(request.VenditeAccountId.Value, request.CompanyId);
                var ivaDebitoAccount = await _accountService.GetAccountByIdAsync(request.IvaDebitoAccountId.Value, request.CompanyId);

                if (clientiAccount == null)
                    return BadRequest(new { error = "Conto Clienti non trovato" });
                if (venditeAccount == null)
                    return BadRequest(new { error = "Conto Vendite non trovato" });
                if (ivaDebitoAccount == null)
                    return BadRequest(new { error = "Conto IVA a Debito non trovato" });

                clientiAccountId = clientiAccount.Id;
                venditeAccountId = venditeAccount.Id;
                ivaDebitoAccountId = ivaDebitoAccount.Id;
            }
            else
            {
                // Altrimenti, cerca i conti per codice (backward compatibility o configurazione automatica)
                var accounts = await _accountService.GetAccountsByCompanyAsync(request.CompanyId);
                var accountsList = accounts.ToList();

                var clientiAccount = accountsList.FirstOrDefault(a => a.Code == "140000");
                var venditeAccount = accountsList.FirstOrDefault(a => a.Code == "500000");
                var ivaDebitoAccount = accountsList.FirstOrDefault(a => a.Code == "260000");

                if (clientiAccount == null || venditeAccount == null || ivaDebitoAccount == null)
                {
                    return BadRequest(new
                    {
                        error = "Piano dei conti non configurato. Fornire gli AccountId nella richiesta o configurare i conti standard (140000, 500000, 260000)",
                        missingAccounts = new
                        {
                            clienti = clientiAccount == null,
                            vendite = venditeAccount == null,
                            ivaDebito = ivaDebitoAccount == null
                        }
                    });
                }

                clientiAccountId = clientiAccount.Id;
                venditeAccountId = venditeAccount.Id;
                ivaDebitoAccountId = ivaDebitoAccount.Id;
            }

            // Crea fattura
            var invoice = new Invoice
            {
                CompanyId = request.CompanyId,
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Type = InvoiceType.Sales,
                Status = InvoiceStatus.Draft,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                CustomerName = request.CustomerName,
                CustomerVatNumber = request.CustomerVatNumber,
                Currency = "EUR",
                SubTotal = subTotal,
                TotalVat = vatAmount,
                TotalAmount = totalAmount,
                OutstandingAmount = totalAmount,
                CreatedBy = userId,
                Lines = new List<InvoiceLine>
                {
                    new InvoiceLine
                    {
                        InventoryId = request.InventoryId,
                        Description = inventoryItem.ItemName,
                        Quantity = request.Quantity,
                        UnitPrice = request.UnitPrice,
                        VatRateId = request.VatRateId,
                        VatAmount = vatAmount,
                        TotalAmount = totalAmount
                    }
                }
            };

            // Salva fattura
            var savedInvoice = await _invoiceService.CreateInvoiceAsync(invoice, userId);

            // Crea scrittura contabile (journal entry) con conti reali
            var journalEntry = new JournalEntry
            {
                CompanyId = request.CompanyId,
                PeriodId = request.PeriodId,
                Description = $"Vendita merci - Fattura {savedInvoice.InvoiceNumber}",
                Date = DateTime.UtcNow,
                Currency = "EUR",
                Reference = savedInvoice.InvoiceNumber,
                Lines = new List<JournalLine>
                {
                    // Dare: Clienti
                    new JournalLine
                    {
                        AccountId = clientiAccountId,
                        Debit = totalAmount,
                        Credit = 0,
                        Narrative = $"Credito cliente {request.CustomerName}"
                    },
                    // Avere: Vendite
                    new JournalLine
                    {
                        AccountId = venditeAccountId,
                        Debit = 0,
                        Credit = subTotal,
                        Narrative = "Ricavi da vendita merci"
                    },
                    // Avere: IVA a debito
                    new JournalLine
                    {
                        AccountId = ivaDebitoAccountId,
                        Debit = 0,
                        Credit = vatAmount,
                        Narrative = $"IVA {vatRate.Rate}%"
                    }
                }
            };

            var savedJournal = await _accountingService.CreateJournalAsync(journalEntry, userId);
            savedInvoice.JournalEntryId = savedJournal.Id;

            // Aggiorna fattura con riferimento journal
            await _invoiceService.UpdateInvoiceAsync(savedInvoice.Id, savedInvoice, userId);

            // Movimentazione magazzino: diminuisci QuantityOnHand
            inventoryItem.QuantityOnHand -= request.Quantity;
            await _inventoryService.UpdateInventoryItemAsync(inventoryItem.Id, inventoryItem, userId);

            _logger.LogInformation("Vendita creata con fattura {InvoiceId} e journal {JournalId}", savedInvoice.Id, savedJournal.Id);
            return CreatedAtAction(nameof(GetInvoice), new { id = savedInvoice.Id }, savedInvoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione vendita");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la creazione della vendita" });
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