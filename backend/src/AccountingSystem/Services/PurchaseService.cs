using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class PurchaseService : IPurchaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IAccountingService _accountingService;
    private readonly IInventoryService _inventoryService;
    private readonly IVatRateService _vatRateService;
    private readonly IAccountService _accountService;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        ApplicationDbContext context,
        IInvoiceService invoiceService,
        IAccountingService accountingService,
        IInventoryService inventoryService,
        IVatRateService vatRateService,
        IAccountService accountService,
        ILogger<PurchaseService> logger)
    {
        _context = context;
        _invoiceService = invoiceService;
        _accountingService = accountingService;
        _inventoryService = inventoryService;
        _vatRateService = vatRateService;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<Purchase> CreatePurchaseAsync(
        Guid companyId,
        Guid periodId,
        Guid inventoryId,
        Guid vatRateId,
        decimal quantity,
        decimal unitPrice,
        string supplierName,
        string? supplierVatNumber,
        Guid? fornitoriAccountId,
        Guid? acquistiAccountId,
        Guid? ivaCreditoAccountId,
        string userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Recupera tasso IVA
            var vatRate = await _vatRateService.GetVatRateByIdAsync(vatRateId);
            if (vatRate == null)
                throw new InvalidOperationException("Tasso IVA non trovato");

            // 2. Calcoli
            decimal subTotal = quantity * unitPrice;
            decimal vatAmount = subTotal * (vatRate.Rate / 100);
            decimal totalAmount = subTotal + vatAmount;

            // 3. Recupera articolo inventario
            var inventoryItem = await _inventoryService.GetInventoryItemByIdAsync(inventoryId, companyId);
            if (inventoryItem == null)
                throw new InvalidOperationException("Articolo di magazzino non trovato");

            // 4. Determina gli AccountId
            var (payablesId, expenseId, vatReceivableId) = await ResolveAccountIdsAsync(
                companyId, fornitoriAccountId, acquistiAccountId, ivaCreditoAccountId);

            // 5. Crea fattura
            var invoice = new Invoice
            {
                CompanyId = companyId,
                InvoiceNumber = await GenerateInvoiceNumberAsync(companyId),
                Type = InvoiceType.Purchase,
                Status = InvoiceStatus.Draft,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                CustomerName = supplierName,
                CustomerVatNumber = supplierVatNumber,
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
                        InventoryId = inventoryId,
                        Description = inventoryItem.ItemName,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        VatRateId = vatRateId,
                        VatAmount = vatAmount,
                        TotalAmount = totalAmount
                    }
                }
            };

            var savedInvoice = await _invoiceService.CreateInvoiceAsync(invoice, userId);

            // 6. Crea scrittura contabile
            var journalEntry = new JournalEntry
            {
                CompanyId = companyId,
                PeriodId = periodId,
                Description = $"Acquisto merci - Fattura {savedInvoice.InvoiceNumber}",
                Date = DateTime.UtcNow,
                Currency = "EUR",
                Reference = savedInvoice.InvoiceNumber,
                Lines = new List<JournalLine>
                {
                    new JournalLine
                    {
                        AccountId = expenseId,
                        Debit = subTotal,
                        Credit = 0,
                        Narrative = "Costi da acquisto merci"
                    },
                    new JournalLine
                    {
                        AccountId = vatReceivableId,
                        Debit = vatAmount,
                        Credit = 0,
                        Narrative = $"IVA {vatRate.Rate}%"
                    },
                    new JournalLine
                    {
                        AccountId = payablesId,
                        Debit = 0,
                        Credit = totalAmount,
                        Narrative = $"Debito fornitore {supplierName}"
                    }
                }
            };

            var savedJournal = await _accountingService.CreateJournalAsync(journalEntry, userId);

            // 7. Aggiorna fattura con riferimento journal
            savedInvoice.JournalEntryId = savedJournal.Id;
            await _invoiceService.UpdateInvoiceAsync(savedInvoice.Id, savedInvoice, userId);

            // 8. Movimentazione magazzino: aumenta QuantityOnHand
            inventoryItem.QuantityOnHand += quantity;
            await _inventoryService.UpdateInventoryItemAsync(inventoryItem.Id, inventoryItem, userId);

            // 9. Crea record Purchase
            var purchase = new Purchase
            {
                CompanyId = companyId,
                PeriodId = periodId,
                InvoiceId = savedInvoice.Id,
                JournalEntryId = savedJournal.Id,
                SupplierName = supplierName,
                SupplierVatNumber = supplierVatNumber,
                TotalAmount = totalAmount,
                SubTotal = subTotal,
                TotalVat = vatAmount,
                Currency = "EUR",
                PurchaseDate = DateTime.UtcNow,
                Status = PurchaseStatus.Confirmed,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Acquisto {PurchaseId} creato con successo. Fattura: {InvoiceId}, Journal: {JournalId}",
                purchase.Id, savedInvoice.Id, savedJournal.Id);

            return purchase;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Purchase?> GetPurchaseByIdAsync(Guid purchaseId, Guid? companyId = null)
    {
        var query = _context.Purchases
            .Include(p => p.Invoice)
            .Include(p => p.JournalEntry)
            .Include(p => p.Period)
            .AsNoTracking()
            .Where(p => p.Id == purchaseId);

        if (companyId.HasValue)
            query = query.Where(p => p.CompanyId == companyId.Value);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Purchase>> GetPurchasesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Purchases
            .Include(p => p.Invoice)
            .Include(p => p.JournalEntry)
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId);

        if (from.HasValue)
            query = query.Where(p => p.PurchaseDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PurchaseDate <= to.Value);

        return await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();
    }

    public async Task<Purchase> UpdatePurchaseStatusAsync(Guid purchaseId, PurchaseStatus status, string userId)
    {
        var purchase = await _context.Purchases.FindAsync(purchaseId);
        if (purchase == null)
            throw new InvalidOperationException("Acquisto non trovato");

        purchase.Status = status;
        purchase.UpdatedBy = userId;
        purchase.UpdatedAt = DateTime.UtcNow;

        _context.Purchases.Update(purchase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stato acquisto {PurchaseId} aggiornato a {Status} da {UserId}", purchaseId, status, userId);

        return purchase;
    }

    public async Task<Purchase> CancelPurchaseAsync(Guid purchaseId, string reason, string userId)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);

        if (purchase == null)
            throw new InvalidOperationException("Acquisto non trovato");

        if (purchase.Status == PurchaseStatus.Cancelled)
            throw new InvalidOperationException("L'acquisto è già stato annullato");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Annulla fattura
            if (purchase.InvoiceId != Guid.Empty)
                await _invoiceService.CancelInvoiceAsync(purchase.InvoiceId, reason, userId);

            // Aggiorna stato
            purchase.Status = PurchaseStatus.Cancelled;
            purchase.Notes = $"Annullata: {reason}";
            purchase.UpdatedBy = userId;
            purchase.UpdatedAt = DateTime.UtcNow;

            _context.Purchases.Update(purchase);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogWarning("Acquisto {PurchaseId} annullato da {UserId}. Motivo: {Reason}", purchaseId, userId, reason);

            return purchase;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PurchaseAccountConfiguration?> GetPurchaseAccountConfigurationAsync(Guid companyId)
    {
        return await _context.PurchaseAccountConfigurations
            .Include(c => c.PayablesAccount)
            .Include(c => c.ExpenseAccount)
            .Include(c => c.VatReceivableAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsDefault);
    }

    public async Task<PurchaseAccountConfiguration> UpsertPurchaseAccountConfigurationAsync(
        Guid companyId,
        Guid payablesAccountId,
        Guid expenseAccountId,
        Guid vatReceivableAccountId,
        string userId)
    {
        var existing = await _context.PurchaseAccountConfigurations
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsDefault);

        if (existing != null)
        {
            existing.PayablesAccountId = payablesAccountId;
            existing.ExpenseAccountId = expenseAccountId;
            existing.VatReceivableAccountId = vatReceivableAccountId;
            existing.UpdatedBy = userId;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.PurchaseAccountConfigurations.Update(existing);
        }
        else
        {
            existing = new PurchaseAccountConfiguration
            {
                CompanyId = companyId,
                PayablesAccountId = payablesAccountId,
                ExpenseAccountId = expenseAccountId,
                VatReceivableAccountId = vatReceivableAccountId,
                IsDefault = true,
                CreatedBy = userId
            };

            _context.PurchaseAccountConfigurations.Add(existing);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Configurazione conti acquisti aggiornata per company {CompanyId}", companyId);

        return existing;
    }

    // ========== METODI PRIVATI ==========

    private async Task<(Guid payables, Guid expense, Guid vatReceivable)> ResolveAccountIdsAsync(
        Guid companyId,
        Guid? fornitoriAccountId,
        Guid? acquistiAccountId,
        Guid? ivaCreditoAccountId)
    {
        // Se tutti gli ID sono forniti, validali e usali
        if (fornitoriAccountId.HasValue && acquistiAccountId.HasValue && ivaCreditoAccountId.HasValue)
        {
            var fornitori = await _accountService.GetAccountByIdAsync(fornitoriAccountId.Value, companyId);
            var acquisti = await _accountService.GetAccountByIdAsync(acquistiAccountId.Value, companyId);
            var iva = await _accountService.GetAccountByIdAsync(ivaCreditoAccountId.Value, companyId);

            if (fornitori == null || acquisti == null || iva == null)
                throw new InvalidOperationException("Uno o più conti forniti non sono validi");

            return (fornitori.Id, acquisti.Id, iva.Id);
        }

        // Altrimenti cerca la configurazione predefinita
        var config = await GetPurchaseAccountConfigurationAsync(companyId);
        if (config != null)
        {
            return (config.PayablesAccountId, config.ExpenseAccountId, config.VatReceivableAccountId);
        }

        // Fallback: cerca per codice
        var accounts = await _accountService.GetAccountsByCompanyAsync(companyId);
        var accountsList = accounts.ToList();

        var fornitoriAccount = accountsList.FirstOrDefault(a => a.Code == "210000");
        var acquistiAccount = accountsList.FirstOrDefault(a => a.Code == "600000");
        var ivaCreditoAccount = accountsList.FirstOrDefault(a => a.Code == "150000");

        if (fornitoriAccount == null || acquistiAccount == null || ivaCreditoAccount == null)
        {
            throw new InvalidOperationException(
                "Piano dei conti non configurato. Configurare i conti standard o fornire gli AccountId nella richiesta.");
        }

        return (fornitoriAccount.Id, acquistiAccount.Id, ivaCreditoAccount.Id);
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        var date = DateTime.UtcNow;
        var prefix = $"PUR-{date:yyyyMMdd}";

        var lastInvoice = await _context.Invoices
            .Where(i => i.CompanyId == companyId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        if (lastInvoice != null && lastInvoice.InvoiceNumber.Length > prefix.Length + 1)
        {
            var lastNumberPart = lastInvoice.InvoiceNumber.Substring(prefix.Length + 1);
            if (int.TryParse(lastNumberPart, out var lastNumber))
            {
                return $"{prefix}-{(lastNumber + 1):D4}";
            }
        }

        return $"{prefix}-0001";
    }
}