using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface IPurchaseService
{
    /// <summary>
    /// Crea un acquisto completo: fattura + scrittura contabile + movimentazione magazzino
    /// </summary>
    Task<Purchase> CreatePurchaseAsync(
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
        string userId);

    /// <summary>
    /// Recupera un acquisto per ID
    /// </summary>
    Task<Purchase?> GetPurchaseByIdAsync(Guid purchaseId, Guid? companyId = null);

    /// <summary>
    /// Recupera tutti gli acquisti di un'azienda
    /// </summary>
    Task<IEnumerable<Purchase>> GetPurchasesByCompanyAsync(Guid companyId, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Aggiorna lo stato di un acquisto
    /// </summary>
    Task<Purchase> UpdatePurchaseStatusAsync(Guid purchaseId, PurchaseStatus status, string userId);

    /// <summary>
    /// Annulla un acquisto (storna fattura, journal e magazzino)
    /// </summary>
    Task<Purchase> CancelPurchaseAsync(Guid purchaseId, string reason, string userId);

    /// <summary>
    /// Recupera la configurazione dei conti per gli acquisti
    /// </summary>
    Task<PurchaseAccountConfiguration?> GetPurchaseAccountConfigurationAsync(Guid companyId);

    /// <summary>
    /// Crea o aggiorna la configurazione dei conti per gli acquisti
    /// </summary>
    Task<PurchaseAccountConfiguration> UpsertPurchaseAccountConfigurationAsync(
        Guid companyId,
        Guid payablesAccountId,
        Guid expenseAccountId,
        Guid vatReceivableAccountId,
        string userId);
}