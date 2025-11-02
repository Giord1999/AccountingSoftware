using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IInvoiceService
{
    // CRUD Operations
    Task<Invoice> CreateInvoiceAsync(Invoice invoice, string userId);
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid? companyId = null);
    Task<IEnumerable<Invoice>> GetInvoicesByCompanyAsync(Guid companyId, InvoiceType? type = null, InvoiceStatus? status = null);
    Task<Invoice> UpdateInvoiceAsync(Guid invoiceId, Invoice invoice, string userId);
    Task DeleteInvoiceAsync(Guid invoiceId, string userId);

    // Business Operations
    Task<Invoice> IssueInvoiceAsync(Guid invoiceId, string userId);
    Task<Invoice> PostInvoiceAsync(Guid invoiceId, string userId);
    Task<Invoice> CancelInvoiceAsync(Guid invoiceId, string reason, string userId);
    Task<Invoice> RecordPaymentAsync(Guid invoiceId, decimal amount, DateTime paymentDate, string userId);

    // Reporting & Analytics
    Task<object> GetInvoicesSummaryAsync(Guid companyId, Guid? periodId = null);
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(Guid companyId);
    Task<object> GetAgedReceivablesAsync(Guid companyId, DateTime asOfDate);
    Task<object> GetAgedPayablesAsync(Guid companyId, DateTime asOfDate);

    // Validation
    Task<bool> ValidateInvoiceNumberAsync(Guid companyId, string invoiceNumber, Guid? excludeInvoiceId = null);
    Task<decimal> CalculateInvoiceTotalsAsync(Invoice invoice);
}