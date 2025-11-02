using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta una fattura emessa o ricevuta
/// </summary>
public class Invoice
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public InvoiceType Type { get; set; }

    [Required]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Required]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    public DateTime? PaymentDate { get; set; }

    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CustomerVatNumber { get; set; }

    [StringLength(200)]
    public string? CustomerAddress { get; set; }

    [StringLength(100)]
    public string? CustomerCity { get; set; }

    [StringLength(20)]
    public string? CustomerPostalCode { get; set; }

    [StringLength(100)]
    public string? CustomerCountry { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Totale imponibile (senza IVA)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Totale IVA
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalVat { get; set; }

    /// <summary>
    /// Totale fattura (SubTotal + TotalVat)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Importo residuo da pagare
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OutstandingAmount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Riferimento al journal entry generato automaticamente
    /// </summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>
    /// Periodo contabile di riferimento
    /// </summary>
    public Guid? PeriodId { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = "system";

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public string? PostedBy { get; set; }

    // Navigation properties
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();

    public JournalEntry? JournalEntry { get; set; }

    public AccountingPeriod? Period { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Riga di dettaglio della fattura
/// </summary>
public class InvoiceLine
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InvoiceId { get; set; }

    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    /// <summary>
    /// Riferimento all'articolo di magazzino (se applicabile)
    /// </summary>
    public Guid? InventoryId { get; set; }

    [ForeignKey("InventoryId")]
    public Inventory? Inventory { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Importo riga (Quantity * UnitPrice)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineAmount { get; set; }

    /// <summary>
    /// Aliquota IVA applicata
    /// </summary>
    public Guid? VatRateId { get; set; }

    [ForeignKey("VatRateId")]
    public VatRate? VatRate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Conto contabile di riferimento (Revenue o Expense)
    /// </summary>
    public Guid? AccountId { get; set; }

    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public int LineNumber { get; set; }
}

public enum InvoiceType
{
    /// <summary>
    /// Fattura di vendita (attiva)
    /// </summary>
    Sales = 0,

    /// <summary>
    /// Fattura di acquisto (passiva)
    /// </summary>
    Purchase = 1
}

public enum InvoiceStatus
{
    /// <summary>
    /// Bozza - non ancora finalizzata
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Emessa/Ricevuta ma non ancora registrata contabilmente
    /// </summary>
    Issued = 1,

    /// <summary>
    /// Registrata contabilmente (Posted)
    /// </summary>
    Posted = 2,

    /// <summary>
    /// Pagata totalmente
    /// </summary>
    Paid = 3,

    /// <summary>
    /// Pagamento parziale
    /// </summary>
    PartiallyPaid = 4,

    /// <summary>
    /// Scaduta (overdue)
    /// </summary>
    Overdue = 5,

    /// <summary>
    /// Annullata
    /// </summary>
    Cancelled = 6
}