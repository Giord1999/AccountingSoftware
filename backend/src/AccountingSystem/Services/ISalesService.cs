using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface ISalesService
{
    /// <summary>
    /// Crea una vendita completa: fattura + scrittura contabile + movimentazione magazzino
    /// </summary>
    Task<Sale> CreateSaleAsync(
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
        string userId);

    /// <summary>
    /// Recupera una vendita per ID
    /// </summary>
    Task<Sale?> GetSaleByIdAsync(Guid saleId, Guid? companyId = null);

    /// <summary>
    /// Recupera tutte le vendite di un'azienda
    /// </summary>
    Task<IEnumerable<Sale>> GetSalesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Aggiorna lo stato di una vendita
    /// </summary>
    Task<Sale> UpdateSaleStatusAsync(Guid saleId, SaleStatus status, string userId);

    /// <summary>
    /// Annulla una vendita (storna fattura, journal e magazzino)
    /// </summary>
    Task<Sale> CancelSaleAsync(Guid saleId, string reason, string userId);

    /// <summary>
    /// Recupera la configurazione dei conti per le vendite
    /// </summary>
    Task<SalesAccountConfiguration?> GetSalesAccountConfigurationAsync(Guid companyId);

    /// <summary>
    /// Crea o aggiorna la configurazione dei conti per le vendite
    /// </summary>
    Task<SalesAccountConfiguration> UpsertSalesAccountConfigurationAsync(
        Guid companyId,
        Guid receivablesAccountId,
        Guid revenueAccountId,
        Guid vatPayableAccountId,
        string userId);
}