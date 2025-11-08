using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IAccountingService _accountingService;
    private readonly IVatService _vatService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext context,
        IAuditService auditService,
        IAccountingService accountingService,
        IVatService vatService,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _auditService = auditService;
        _accountingService = accountingService;
        _vatService = vatService;
        _logger = logger;
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice, string userId)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        // Validazione
        if (invoice.CompanyId == Guid.Empty)
            throw new InvalidOperationException("CompanyId è obbligatorio");

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            throw new InvalidOperationException("Numero fattura è obbligatorio");

        if (string.IsNullOrWhiteSpace(invoice.CustomerName))
            throw new InvalidOperationException("Nome cliente è obbligatorio");

        // Verifica univocità numero fattura
        var exists = await ValidateInvoiceNumberAsync(invoice.CompanyId, invoice.InvoiceNumber);
        if (!exists)
            throw new InvalidOperationException($"Numero fattura '{invoice.InvoiceNumber}' già esistente");

        // Calcola totali
        await CalculateInvoiceTotalsAsync(invoice);

        // Imposta stato iniziale
        invoice.Status = InvoiceStatus.Draft;
        invoice.CreatedAt = DateTime.UtcNow;
        invoice.CreatedBy = userId;
        invoice.OutstandingAmount = invoice.TotalAmount;
        invoice.PaidAmount = 0;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "CREATE_INVOICE",
            $"Creata fattura {invoice.InvoiceNumber} per {invoice.CustomerName} - Importo: {invoice.TotalAmount:C} {invoice.Currency}");

        _logger.LogInformation("Fattura {InvoiceNumber} creata con successo per company {CompanyId}",
            invoice.InvoiceNumber, invoice.CompanyId);

        return invoice;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid? companyId = null)
    {
        var query = _context.Invoices
            .Include(i => i.Lines)
                .ThenInclude(l => l.VatRate)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Account)
            .Include(i => i.JournalEntry)
            .Include(i => i.Period)
            .AsNoTracking()
            .Where(i => i.Id == invoiceId);

        if (companyId.HasValue)
            query = query.Where(i => i.CompanyId == companyId.Value);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByCompanyAsync(
        Guid companyId,
        InvoiceType? type = null,
        InvoiceStatus? status = null)
    {
        var query = _context.Invoices
            .Include(i => i.Lines)
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId);

        if (type.HasValue)
            query = query.Where(i => i.Type == type.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        return await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invoice> UpdateInvoiceAsync(Guid invoiceId, Invoice invoice, string userId)
    {
        var existingInvoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (existingInvoice == null)
            throw new InvalidOperationException($"Fattura {invoiceId} non trovata");

        // Verifica che la fattura sia ancora modificabile
        if (existingInvoice.Status == InvoiceStatus.Posted)
            throw new InvalidOperationException("Impossibile modificare una fattura già registrata contabilmente");

        if (existingInvoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Impossibile modificare una fattura già pagata");

        if (existingInvoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Impossibile modificare una fattura annullata");

        // Verifica univocità numero se modificato
        if (existingInvoice.InvoiceNumber != invoice.InvoiceNumber)
        {
            var exists = await ValidateInvoiceNumberAsync(
                existingInvoice.CompanyId,
                invoice.InvoiceNumber,
                invoiceId);

            if (!exists)
                throw new InvalidOperationException($"Numero fattura '{invoice.InvoiceNumber}' già esistente");
        }

        // Aggiorna campi
        existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
        existingInvoice.Type = invoice.Type;
        existingInvoice.IssueDate = invoice.IssueDate;
        existingInvoice.DueDate = invoice.DueDate;
        existingInvoice.CustomerName = invoice.CustomerName;
        existingInvoice.CustomerVatNumber = invoice.CustomerVatNumber;
        existingInvoice.CustomerAddress = invoice.CustomerAddress;
        existingInvoice.CustomerCity = invoice.CustomerCity;
        existingInvoice.CustomerPostalCode = invoice.CustomerPostalCode;
        existingInvoice.CustomerCountry = invoice.CustomerCountry;
        existingInvoice.Currency = invoice.Currency;
        existingInvoice.Notes = invoice.Notes;
        existingInvoice.PaymentTerms = invoice.PaymentTerms;
        existingInvoice.UpdatedAt = DateTime.UtcNow;
        existingInvoice.UpdatedBy = userId;

        // Rimuovi righe vecchie e aggiungi nuove
        _context.InvoiceLines.RemoveRange(existingInvoice.Lines);
        existingInvoice.Lines = invoice.Lines;

        // Ricalcola totali
        await CalculateInvoiceTotalsAsync(existingInvoice);

        existingInvoice.OutstandingAmount = existingInvoice.TotalAmount - existingInvoice.PaidAmount;

        _context.Invoices.Update(existingInvoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "UPDATE_INVOICE",
            $"Aggiornata fattura {existingInvoice.InvoiceNumber}");

        _logger.LogInformation("Fattura {InvoiceId} aggiornata con successo", invoiceId);

        return existingInvoice;
    }

    public async Task DeleteInvoiceAsync(Guid invoiceId, string userId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            throw new InvalidOperationException($"Fattura {invoiceId} non trovata");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Possono essere eliminate solo fatture in stato Draft");

        _context.InvoiceLines.RemoveRange(invoice.Lines);
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "DELETE_INVOICE",
            $"Eliminata fattura {invoice.InvoiceNumber}");

        _logger.LogInformation("Fattura {InvoiceId} eliminata", invoiceId);
    }

    public async Task<Invoice> IssueInvoiceAsync(Guid invoiceId, string userId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            throw new InvalidOperationException($"Fattura {invoiceId} non trovata");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Solo le fatture in stato Draft possono essere emesse");

        if (!invoice.Lines.Any())
            throw new InvalidOperationException("La fattura deve contenere almeno una riga");

        invoice.Status = InvoiceStatus.Issued;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = userId;

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "ISSUE_INVOICE",
            $"Emessa fattura {invoice.InvoiceNumber} - Importo: {invoice.TotalAmount:C}");

        _logger.LogInformation("Fattura {InvoiceNumber} emessa", invoice.InvoiceNumber);

        return invoice;
    }

    public async Task<Invoice> PostInvoiceAsync(Guid invoiceId, string userId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
                .ThenInclude(l => l.Account)
            .Include(i => i.Lines)
                .ThenInclude(l => l.VatRate)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            throw new InvalidOperationException($"Fattura {invoiceId} non trovata");

        if (invoice.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("Solo le fatture emesse possono essere registrate contabilmente");

        if (invoice.JournalEntryId.HasValue)
            throw new InvalidOperationException("Fattura già registrata contabilmente");

        // Crea registrazione contabile automatica
        var journalEntry = await CreateJournalEntryFromInvoiceAsync(invoice, userId);

        invoice.JournalEntryId = journalEntry.Id;
        invoice.Status = InvoiceStatus.Posted;
        invoice.PostedAt = DateTime.UtcNow;
        invoice.PostedBy = userId;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = userId;

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "POST_INVOICE",
            $"Registrata contabilmente fattura {invoice.InvoiceNumber} - Journal Entry: {journalEntry.Id}");

        _logger.LogInformation("Fattura {InvoiceNumber} registrata con Journal Entry {JournalId}",
            invoice.InvoiceNumber, journalEntry.Id);

        return invoice;
    }

    public async Task<Invoice> CancelInvoiceAsync(Guid invoiceId, string reason, string userId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            throw new InvalidOperationException($"Fattura {invoiceId} non trovata");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Impossibile annullare una fattura già pagata");

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Fattura già annullata");

        var previousStatus = invoice.Status;
        invoice.Status = InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = userId;
        invoice.Notes = $"{invoice.Notes}\n[ANNULLATA: {reason}]";

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "CANCEL_INVOICE",
            $"Annullata fattura {invoice.InvoiceNumber} - Stato precedente: {previousStatus} - Motivo: {reason}");

        _logger.LogWarning("Fattura {InvoiceNumber} annullata: {Reason}",
            invoice.InvoiceNumber, reason);

        return invoice;
    }

    public async Task<Invoice> RecordPaymentAsync(Guid invoiceId, decimal amount, DateTime paymentDate, string userId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("L'importo del pagamento deve essere maggiore di zero");

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            throw new InvalidOperationException($"Fattura {invoiceId} non trovata");

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Impossibile registrare pagamenti su fatture annullate");

        if (invoice.Status == InvoiceStatus.Draft || invoice.Status == InvoiceStatus.Issued)
            throw new InvalidOperationException("La fattura deve essere registrata contabilmente prima di poter registrare pagamenti");

        if (invoice.PaidAmount + amount > invoice.TotalAmount)
            throw new InvalidOperationException($"L'importo totale dei pagamenti ({invoice.PaidAmount + amount:C}) supera il totale fattura ({invoice.TotalAmount:C})");

        invoice.PaidAmount += amount;
        invoice.OutstandingAmount = invoice.TotalAmount - invoice.PaidAmount;

        if (invoice.OutstandingAmount == 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaymentDate = paymentDate;
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = userId;

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "RECORD_PAYMENT",
            $"Registrato pagamento di {amount:C} {invoice.Currency} per fattura {invoice.InvoiceNumber} - Residuo: {invoice.OutstandingAmount:C}");

        _logger.LogInformation("Registrato pagamento {Amount} per fattura {InvoiceNumber}",
            amount, invoice.InvoiceNumber);

        return invoice;
    }

    public async Task<object> GetInvoicesSummaryAsync(Guid companyId, Guid? periodId = null)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId);

        if (periodId.HasValue)
            query = query.Where(i => i.PeriodId == periodId.Value);

        var invoices = await query.ToListAsync();

        var salesInvoices = invoices.Where(i => i.Type == InvoiceType.Sales).ToList();
        var purchaseInvoices = invoices.Where(i => i.Type == InvoiceType.Purchase).ToList();

        return new
        {
            TotalInvoices = invoices.Count,
            Sales = new
            {
                Count = salesInvoices.Count,
                TotalAmount = salesInvoices.Sum(i => i.TotalAmount),
                PaidAmount = salesInvoices.Sum(i => i.PaidAmount),
                OutstandingAmount = salesInvoices.Sum(i => i.OutstandingAmount),
                ByStatus = salesInvoices.GroupBy(i => i.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), Total = g.Sum(i => i.TotalAmount) })
            },
            Purchases = new
            {
                Count = purchaseInvoices.Count,
                TotalAmount = purchaseInvoices.Sum(i => i.TotalAmount),
                PaidAmount = purchaseInvoices.Sum(i => i.PaidAmount),
                OutstandingAmount = purchaseInvoices.Sum(i => i.OutstandingAmount),
                ByStatus = purchaseInvoices.GroupBy(i => i.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), Total = g.Sum(i => i.TotalAmount) })
            }
        };
    }

    public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(Guid companyId)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId &&
                       i.DueDate.HasValue &&
                       i.DueDate.Value < today &&
                       i.Status != InvoiceStatus.Paid &&
                       i.Status != InvoiceStatus.Cancelled &&
                       i.OutstandingAmount > 0)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<object> GetAgedReceivablesAsync(Guid companyId, DateTime asOfDate)
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId &&
                       i.Type == InvoiceType.Sales &&
                       i.Status != InvoiceStatus.Paid &&
                       i.Status != InvoiceStatus.Cancelled &&
                       i.OutstandingAmount > 0)
            .ToListAsync();

        var aged = invoices.Select(i => new
        {
            i.InvoiceNumber,
            i.CustomerName,
            i.IssueDate,
            i.DueDate,
            i.TotalAmount,
            i.OutstandingAmount,
            DaysOverdue = i.DueDate.HasValue ? (asOfDate - i.DueDate.Value).Days : 0,
            AgingBucket = GetAgingBucket(i.DueDate, asOfDate)
        }).ToList();

        return new
        {
            AsOfDate = asOfDate,
            TotalOutstanding = aged.Sum(a => a.OutstandingAmount),
            Current = aged.Where(a => a.AgingBucket == "Current").Sum(a => a.OutstandingAmount),
            Days_1_30 = aged.Where(a => a.AgingBucket == "1-30").Sum(a => a.OutstandingAmount),
            Days_31_60 = aged.Where(a => a.AgingBucket == "31-60").Sum(a => a.OutstandingAmount),
            Days_61_90 = aged.Where(a => a.AgingBucket == "61-90").Sum(a => a.OutstandingAmount),
            Days_Over90 = aged.Where(a => a.AgingBucket == "90+").Sum(a => a.OutstandingAmount),
            Details = aged
        };
    }

    public async Task<object> GetAgedPayablesAsync(Guid companyId, DateTime asOfDate)
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId &&
                       i.Type == InvoiceType.Purchase &&
                       i.Status != InvoiceStatus.Paid &&
                       i.Status != InvoiceStatus.Cancelled &&
                       i.OutstandingAmount > 0)
            .ToListAsync();

        var aged = invoices.Select(i => new
        {
            i.InvoiceNumber,
            Supplier = i.CustomerName,
            i.IssueDate,
            i.DueDate,
            i.TotalAmount,
            i.OutstandingAmount,
            DaysOverdue = i.DueDate.HasValue ? (asOfDate - i.DueDate.Value).Days : 0,
            AgingBucket = GetAgingBucket(i.DueDate, asOfDate)
        }).ToList();

        return new
        {
            AsOfDate = asOfDate,
            TotalOutstanding = aged.Sum(a => a.OutstandingAmount),
            Current = aged.Where(a => a.AgingBucket == "Current").Sum(a => a.OutstandingAmount),
            Days_1_30 = aged.Where(a => a.AgingBucket == "1-30").Sum(a => a.OutstandingAmount),
            Days_31_60 = aged.Where(a => a.AgingBucket == "31-60").Sum(a => a.OutstandingAmount),
            Days_61_90 = aged.Where(a => a.AgingBucket == "61-90").Sum(a => a.OutstandingAmount),
            Days_Over90 = aged.Where(a => a.AgingBucket == "90+").Sum(a => a.OutstandingAmount),
            Details = aged
        };
    }

    public async Task<bool> ValidateInvoiceNumberAsync(Guid companyId, string invoiceNumber, Guid? excludeInvoiceId = null)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.InvoiceNumber == invoiceNumber);

        if (excludeInvoiceId.HasValue)
            query = query.Where(i => i.Id != excludeInvoiceId.Value);

        return !await query.AnyAsync();
    }

    public async Task<decimal> CalculateInvoiceTotalsAsync(Invoice invoice)
    {
        decimal subTotal = 0;
        decimal totalVat = 0;

        foreach (var line in invoice.Lines)
        {
            // Calcola importo riga
            line.LineAmount = line.Quantity * line.UnitPrice;

            // Calcola IVA
            if (line.VatRateId.HasValue)
            {
                var vatRate = await _context.VatRates.FindAsync(line.VatRateId.Value);
                if (vatRate != null)
                {
                    line.VatPercentage = vatRate.Rate;
                }
            }

            line.VatAmount = _vatService.CalculateVAT(line.LineAmount, line.VatPercentage);
            line.TotalAmount = line.LineAmount + line.VatAmount;

            subTotal += line.LineAmount;
            totalVat += line.VatAmount;
        }

        invoice.SubTotal = subTotal;
        invoice.TotalVat = totalVat;
        invoice.TotalAmount = subTotal + totalVat;

        return invoice.TotalAmount;
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task<JournalEntry> CreateJournalEntryFromInvoiceAsync(Invoice invoice, string userId)
    {
        var journalEntry = new JournalEntry
        {
            CompanyId = invoice.CompanyId,
            PeriodId = invoice.PeriodId ?? throw new InvalidOperationException("Period ID obbligatorio"),
            Date = invoice.IssueDate,
            Description = $"Fattura {invoice.InvoiceNumber} - {invoice.CustomerName}",
            Reference = invoice.InvoiceNumber,
            Status = JournalStatus.Posted,
            Lines = new List<JournalLine>()
        };

        if (invoice.Type == InvoiceType.Sales)
        {
            // FATTURA VENDITA
            // Crediti vs Clienti (Debito) - Asset aumenta
            var receivableAccount = await GetAccountByCodeAsync(invoice.CompanyId, "1100"); // Crediti commerciali
            journalEntry.Lines.Add(new JournalLine
            {
                AccountId = receivableAccount.Id,
                Debit = invoice.TotalAmount,
                Credit = 0,
                Narrative = $"Cliente: {invoice.CustomerName}"
            });

            // Ricavi per righe (Credito) - Revenue aumenta
            foreach (var line in invoice.Lines)
            {
                var revenueAccount = line.Account ?? await GetAccountByCodeAsync(invoice.CompanyId, "4000");
                journalEntry.Lines.Add(new JournalLine
                {
                    AccountId = revenueAccount.Id,
                    Debit = 0,
                    Credit = line.LineAmount,
                    Narrative = line.Description
                });
            }

            // IVA a debito (Credito) - Liability aumenta
            if (invoice.TotalVat > 0)
            {
                var vatPayableAccount = await GetAccountByCodeAsync(invoice.CompanyId, "2400"); // IVA a debito
                journalEntry.Lines.Add(new JournalLine
                {
                    AccountId = vatPayableAccount.Id,
                    Debit = 0,
                    Credit = invoice.TotalVat,
                    Narrative = "IVA a debito"
                });
            }
        }
        else // Purchase
        {
            // FATTURA ACQUISTO
            // Costi per righe (Debito) - Expense aumenta
            foreach (var line in invoice.Lines)
            {
                var expenseAccount = line.Account ?? await GetAccountByCodeAsync(invoice.CompanyId, "5000");
                journalEntry.Lines.Add(new JournalLine
                {
                    AccountId = expenseAccount.Id,
                    Debit = line.LineAmount,
                    Credit = 0,
                    Narrative = line.Description
                });
            }

            // IVA a credito (Debito) - Asset aumenta
            if (invoice.TotalVat > 0)
            {
                var vatReceivableAccount = await GetAccountByCodeAsync(invoice.CompanyId, "1500"); // IVA a credito
                journalEntry.Lines.Add(new JournalLine
                {
                    AccountId = vatReceivableAccount.Id,
                    Debit = invoice.TotalVat,
                    Credit = 0,
                    Narrative = "IVA a credito"
                });
            }

            // Debiti vs Fornitori (Credito) - Liability aumenta
            var payableAccount = await GetAccountByCodeAsync(invoice.CompanyId, "2100"); // Debiti commerciali
            journalEntry.Lines.Add(new JournalLine
            {
                AccountId = payableAccount.Id,
                Debit = 0,
                Credit = invoice.TotalAmount,
                Narrative = $"Fornitore: {invoice.CustomerName}"
            });
        }

        // Crea journal entry
        return await _accountingService.CreateJournalAsync(journalEntry, userId);
    }

    private async Task<Account> GetAccountByCodeAsync(Guid companyId, string code)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == code);

        if (account == null)
            throw new InvalidOperationException($"Account con codice '{code}' non trovato per company {companyId}");

        return account;
    }

    private static string GetAgingBucket(DateTime? dueDate, DateTime asOfDate)
    {
        if (!dueDate.HasValue || dueDate.Value >= asOfDate)
            return "Current";

        var daysOverdue = (asOfDate - dueDate.Value).Days;

        return daysOverdue switch
        {
            <= 30 => "1-30",
            <= 60 => "31-60",
            <= 90 => "61-90",
            _ => "90+"
        };
    }
}