using AccountingApp.Models;

namespace AccountingApp.Services;

public interface IInvoiceApiService
{
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId);
    Task<IEnumerable<Invoice>> GetInvoicesByCompanyAsync(Guid companyId, InvoiceType? type = null, InvoiceStatus? status = null);
}