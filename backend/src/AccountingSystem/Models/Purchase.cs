using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un acquisto completo (fattura + scrittura contabile + movimentazione magazzino)
/// </summary>
public class Purchase
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
    /// Fornitore
    /// </summary>
    [Required]
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SupplierVatNumber { get; set; }

    /// <summary>
    /// Importo totale dell'acquisto (inclusa IVA)
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
    /// Data dell'acquisto
    /// </summary>
    [Required]
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Stato dell'acquisto
    /// </summary>
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

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
/// Stato dell'acquisto
/// </summary>
public enum PurchaseStatus
{
    Draft = 0,          // Bozza
    Confirmed = 1,      // Confermata
    Invoiced = 2,       // Fatturata
    Posted = 3,         // Registrata in contabilità
    Cancelled = 4       // Annullata
}

/// <summary>
/// Configurazione dei conti contabili per gli acquisti
/// </summary>
public class PurchaseAccountConfiguration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Conto Debiti vs Fornitori (es. 210000)
    /// </summary>
    [Required]
    public Guid PayablesAccountId { get; set; }

    [ForeignKey("PayablesAccountId")]
    public Account? PayablesAccount { get; set; }

    /// <summary>
    /// Conto Acquisti (es. 600000)
    /// </summary>
    [Required]
    public Guid ExpenseAccountId { get; set; }

    [ForeignKey("ExpenseAccountId")]
    public Account? ExpenseAccount { get; set; }

    /// <summary>
    /// Conto IVA a Credito (es. 150000)
    /// </summary>
    [Required]
    public Guid VatReceivableAccountId { get; set; }

    [ForeignKey("VatReceivableAccountId")]
    public Account? VatReceivableAccount { get; set; }

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