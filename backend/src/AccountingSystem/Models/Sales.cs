using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta una vendita completa (fattura + scrittura contabile + movimentazione magazzino)
/// </summary>
public class Sale
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid PeriodId { get; set; }

    /// <summary>
    /// Riferimento alla fattura generata
    /// </summary>
    [Required]
    public Guid InvoiceId { get; set; }

    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    /// <summary>
    /// Riferimento alla scrittura contabile generata
    /// </summary>
    public Guid? JournalEntryId { get; set; }

    [ForeignKey("JournalEntryId")]
    public JournalEntry? JournalEntry { get; set; }

    /// <summary>
    /// Cliente
    /// </summary>
    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? CustomerVatNumber { get; set; }

    /// <summary>
    /// Importo totale della vendita (inclusa IVA)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Imponibile
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    /// <summary>
    /// IVA totale
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalVat { get; set; }

    /// <summary>
    /// Valuta
    /// </summary>
    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Data della vendita
    /// </summary>
    [Required]
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Stato della vendita
    /// </summary>
    public SaleStatus Status { get; set; } = SaleStatus.Draft;

    /// <summary>
    /// Note aggiuntive
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = "system";

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public AccountingPeriod? Period { get; set; }
}

/// <summary>
/// Stato della vendita
/// </summary>
public enum SaleStatus
{
    Draft = 0,          // Bozza
    Confirmed = 1,      // Confermata
    Invoiced = 2,       // Fatturata
    Posted = 3,         // Registrata in contabilità
    Cancelled = 4       // Annullata
}

/// <summary>
/// Configurazione dei conti contabili per le vendite
/// </summary>
public class SalesAccountConfiguration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Conto Crediti vs Clienti (es. 140000)
    /// </summary>
    [Required]
    public Guid ReceivablesAccountId { get; set; }

    [ForeignKey("ReceivablesAccountId")]
    public Account? ReceivablesAccount { get; set; }

    /// <summary>
    /// Conto Ricavi da Vendite (es. 500000)
    /// </summary>
    [Required]
    public Guid RevenueAccountId { get; set; }

    [ForeignKey("RevenueAccountId")]
    public Account? RevenueAccount { get; set; }

    /// <summary>
    /// Conto IVA a Debito (es. 260000)
    /// </summary>
    [Required]
    public Guid VatPayableAccountId { get; set; }

    [ForeignKey("VatPayableAccountId")]
    public Account? VatPayableAccount { get; set; }

    /// <summary>
    /// Se true, usa questa configurazione come default
    /// </summary>
    public bool IsDefault { get; set; } = true;

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}