using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class SalesService(
    ApplicationDbContext context,
    IInvoiceService invoiceService,
    IAccountingService accountingService,
    IInventoryService inventoryService,
    IVatRateService vatRateService,
    IAccountService accountService,
    ICustomerService customerService,
    ILogger<SalesService> logger) : ISalesService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IInvoiceService _invoiceService = invoiceService;
    private readonly IAccountingService _accountingService = accountingService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IVatRateService _vatRateService = vatRateService;
    private readonly IAccountService _accountService = accountService;
    private readonly ICustomerService _customerService = customerService;
    private readonly ILogger<SalesService> _logger = logger;

    public async Task<Sale> CreateSaleAsync(
        Guid companyId,
        Guid periodId,
        Guid inventoryId,
        Guid vatRateId,
        decimal quantity,
        decimal unitPrice,
        string customerName,
        string? customerVatNumber,
        Guid? clientiAccountId,
        Guid? venditeAccountId,
        Guid? ivaDebitoAccountId,
        string userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Recupera tasso IVA
            var vatRate = await _vatRateService.GetVatRateByIdAsync(vatRateId);
            if (vatRate is null)
                throw new InvalidOperationException("Tasso IVA non trovato");

            // 2. Calcoli
            decimal subTotal = quantity * unitPrice;
            decimal vatAmount = subTotal * (vatRate.Rate / 100);
            decimal totalAmount = subTotal + vatAmount;

            // 3. Recupera articolo inventario
            var inventoryItem = await _inventoryService.GetInventoryItemByIdAsync(inventoryId, companyId);
            if (inventoryItem is null)
                throw new InvalidOperationException("Articolo di magazzino non trovato");

            if (inventoryItem.QuantityOnHand < quantity)
                throw new InvalidOperationException($"Quantità insufficiente. Disponibile: {inventoryItem.QuantityOnHand}, Richiesta: {quantity}");

            // 4. Determina gli AccountId
            var (receivablesId, revenueId, vatPayableId) = await ResolveAccountIdsAsync(
                companyId, clientiAccountId, venditeAccountId, ivaDebitoAccountId);

            // 5. Gestisci cliente
            var customerId = await ResolveOrCreateCustomerAsync(companyId, customerName, customerVatNumber, userId);

            // 6. Crea fattura
            var invoice = new Invoice
            {
                CompanyId = companyId,
                InvoiceNumber = await GenerateInvoiceNumberAsync(companyId),
                Type = InvoiceType.Sales,
                Status = InvoiceStatus.Draft,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                CustomerName = customerName,
                CustomerVatNumber = customerVatNumber,
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

            // 7. Crea scrittura contabile
            var journalEntry = new JournalEntry
            {
                CompanyId = companyId,
                PeriodId = periodId,
                Description = $"Vendita merci - Fattura {savedInvoice.InvoiceNumber}",
                Date = DateTime.UtcNow,
                Currency = "EUR",
                Reference = savedInvoice.InvoiceNumber,
                Lines = new List<JournalLine>
                {
                    new JournalLine
                    {
                        AccountId = receivablesId,
                        Debit = totalAmount,
                        Credit = 0,
                        Narrative = $"Credito cliente {customerName}"
                    },
                    new JournalLine
                    {
                        AccountId = revenueId,
                        Debit = 0,
                        Credit = subTotal,
                        Narrative = "Ricavi da vendita merci"
                    },
                    new JournalLine
                    {
                        AccountId = vatPayableId,
                        Debit = 0,
                        Credit = vatAmount,
                        Narrative = $"IVA {vatRate.Rate}%"
                    }
                }
            };

            var savedJournal = await _accountingService.CreateJournalAsync(journalEntry, userId);

            // 8. Aggiorna fattura con riferimento journal
            savedInvoice.JournalEntryId = savedJournal.Id;
            await _invoiceService.UpdateInvoiceAsync(savedInvoice.Id, savedInvoice, userId);

            // 9. Movimentazione magazzino
            inventoryItem.QuantityOnHand -= quantity;
            await _inventoryService.UpdateInventoryItemAsync(inventoryItem.Id, inventoryItem, userId);

            // 10. Crea record Sale
            var sale = new Sale
            {
                CompanyId = companyId,
                PeriodId = periodId,
                InvoiceId = savedInvoice.Id,
                JournalEntryId = savedJournal.Id,
                CustomerId = customerId,
                CustomerName = customerName,
                CustomerVatNumber = customerVatNumber,
                TotalAmount = totalAmount,
                SubTotal = subTotal,
                TotalVat = vatAmount,
                Currency = "EUR",
                SaleDate = DateTime.UtcNow,
                Status = SaleStatus.Confirmed,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Vendita {SaleId} creata con successo. Fattura: {InvoiceId}, Journal: {JournalId}",
                sale.Id, savedInvoice.Id, savedJournal.Id);

            return sale;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Sale?> GetSaleByIdAsync(Guid saleId, Guid? companyId = null)
    {
        var query = _context.Sales
            .Include(s => s.Invoice)
            .Include(s => s.JournalEntry)
            .Include(s => s.Period)
            .AsNoTracking()
            .Where(s => s.Id == saleId);

        if (companyId.HasValue)
            query = query.Where(s => s.CompanyId == companyId.Value);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Sale>> GetSalesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Sales
            .Include(s => s.Invoice)
            .Include(s => s.JournalEntry)
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId);

        if (from.HasValue)
            query = query.Where(s => s.SaleDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.SaleDate <= to.Value);

        return await query.OrderByDescending(s => s.SaleDate).ToListAsync();
    }

    public async Task<Sale> UpdateSaleStatusAsync(Guid saleId, SaleStatus status, string userId)
    {
        var sale = await _context.Sales.FindAsync(saleId);
        if (sale is null)
            throw new InvalidOperationException("Vendita non trovata");

        sale.Status = status;
        sale.UpdatedBy = userId;
        sale.UpdatedAt = DateTime.UtcNow;

        _context.Sales.Update(sale);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stato vendita {SaleId} aggiornato a {Status} da {UserId}", saleId, status, userId);

        return sale;
    }

    public async Task<Sale> CancelSaleAsync(Guid saleId, string reason, string userId)
    {
        var sale = await _context.Sales
            .Include(s => s.Invoice)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null)
            throw new InvalidOperationException("Vendita non trovata");

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La vendita è già stata annullata");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Annulla fattura
            if (sale.InvoiceId != Guid.Empty)
                await _invoiceService.CancelInvoiceAsync(sale.InvoiceId, reason, userId);

            // Aggiorna stato
            sale.Status = SaleStatus.Cancelled;
            sale.Notes = $"Annullata: {reason}";
            sale.UpdatedBy = userId;
            sale.UpdatedAt = DateTime.UtcNow;

            _context.Sales.Update(sale);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogWarning("Vendita {SaleId} annullata da {UserId}. Motivo: {Reason}", saleId, userId, reason);

            return sale;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SalesAccountConfiguration?> GetSalesAccountConfigurationAsync(Guid companyId)
    {
        return await _context.SalesAccountConfigurations
            .Include(c => c.ReceivablesAccount)
            .Include(c => c.RevenueAccount)
            .Include(c => c.VatPayableAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsDefault);
    }

    public async Task<SalesAccountConfiguration> UpsertSalesAccountConfigurationAsync(
        Guid companyId,
        Guid receivablesAccountId,
        Guid revenueAccountId,
        Guid vatPayableAccountId,
        string userId)
    {
        var existing = await _context.SalesAccountConfigurations
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsDefault);

        if (existing != null)
        {
            existing.ReceivablesAccountId = receivablesAccountId;
            existing.RevenueAccountId = revenueAccountId;
            existing.VatPayableAccountId = vatPayableAccountId;
            existing.UpdatedBy = userId;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.SalesAccountConfigurations.Update(existing);
        }
        else
        {
            existing = new SalesAccountConfiguration
            {
                CompanyId = companyId,
                ReceivablesAccountId = receivablesAccountId,
                RevenueAccountId = revenueAccountId,
                VatPayableAccountId = vatPayableAccountId,
                IsDefault = true,
                CreatedBy = userId
            };

            _context.SalesAccountConfigurations.Add(existing);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Configurazione conti vendite aggiornata per company {CompanyId}", companyId);

        return existing;
    }

    // ========== METODI PRIVATI ==========

    private async Task<(Guid receivables, Guid revenue, Guid vatPayable)> ResolveAccountIdsAsync(
        Guid companyId,
        Guid? clientiAccountId,
        Guid? venditeAccountId,
        Guid? ivaDebitoAccountId)
    {
        // Se tutti gli ID sono forniti, validali e usali
        if (clientiAccountId.HasValue && venditeAccountId.HasValue && ivaDebitoAccountId.HasValue)
        {
            var clienti = await _accountService.GetAccountByIdAsync(clientiAccountId.Value, companyId);
            var vendite = await _accountService.GetAccountByIdAsync(venditeAccountId.Value, companyId);
            var iva = await _accountService.GetAccountByIdAsync(ivaDebitoAccountId.Value, companyId);

            if (clienti is null || vendite is null || iva is null)
                throw new InvalidOperationException("Uno o più conti forniti non sono validi");

            return (clienti.Id, vendite.Id, iva.Id);
        }

        // Altrimenti cerca la configurazione predefinita
        var config = await GetSalesAccountConfigurationAsync(companyId);
        if (config != null)
        {
            return (config.ReceivablesAccountId, config.RevenueAccountId, config.VatPayableAccountId);
        }

        // Fallback: cerca per codice
        var accounts = await _accountService.GetAccountsByCompanyAsync(companyId);
        var accountsList = accounts.ToList();

        var clientiAccount = accountsList.FirstOrDefault(a => a.Code == "140000");
        var venditeAccount = accountsList.FirstOrDefault(a => a.Code == "500000");
        var ivaDebitoAccount = accountsList.FirstOrDefault(a => a.Code == "260000");

        if (clientiAccount is null || venditeAccount is null || ivaDebitoAccount is null)
        {
            throw new InvalidOperationException(
                "Piano dei conti non configurato. Configurare i conti standard o fornire gli AccountId nella richiesta.");
        }

        return (clientiAccount.Id, venditeAccount.Id, ivaDebitoAccount.Id);
    }

    private async Task<Guid> ResolveOrCreateCustomerAsync(Guid companyId, string customerName, string? customerVatNumber, string userId)
    {
        // Cerca cliente esistente
        var existingCustomer = await _customerService.GetCustomersByCompanyAsync(companyId)
            .ContinueWith(t => t.Result.FirstOrDefault(c => c.Name == customerName && c.VatNumber == customerVatNumber));

        if (existingCustomer != null)
        {
            return existingCustomer.Id;
        }

        // Crea nuovo cliente
        var newCustomer = new Customer
        {
            CompanyId = companyId,
            Name = customerName,
            VatNumber = customerVatNumber,
            IsActive = true
        };

        var createdCustomer = await _customerService.CreateCustomerAsync(newCustomer, userId);
        return createdCustomer.Id;
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        var date = DateTime.UtcNow;
        var prefix = $"INV-{date:yyyyMMdd}";

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