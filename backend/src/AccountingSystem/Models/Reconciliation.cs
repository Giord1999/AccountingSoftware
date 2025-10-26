using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta una riconciliazione bancaria/contabile
/// </summary>
public class Reconciliation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.InProgress;

    /// <summary>
    /// Saldo contabile calcolato
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BookBalance { get; set; }

    /// <summary>
    /// Saldo dell'estratto conto bancario
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? StatementBalance { get; set; }

    /// <summary>
    /// Differenza tra saldo contabile e bancario
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Difference { get; set; }

    /// <summary>
    /// Numero di transazioni riconciliate
    /// </summary>
    public int ReconciledCount { get; set; }

    /// <summary>
    /// Numero di transazioni non riconciliate
    /// </summary>
    public int UnreconciledCount { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Riferimento al file dell'estratto conto caricato
    /// </summary>
    public string? StatementFilePath { get; set; }

    /// <summary>
    /// Dati aggiuntivi serializzati in JSON (transazioni non riconciliate, etc.)
    /// </summary>
    public string? AdditionalData { get; set; }

    // Navigation properties
    public Account? Account { get; set; }
    public ICollection<ReconciliationItem> Items { get; set; } = new List<ReconciliationItem>();
}

/// <summary>
/// Rappresenta una singola riga di riconciliazione
/// </summary>
public class ReconciliationItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ReconciliationId { get; set; }

    public Guid? JournalEntryId { get; set; }

    public Guid? JournalLineId { get; set; }

    public DateTime TransactionDate { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }

    /// <summary>
    /// Indica se la transazione è stata riconciliata
    /// </summary>
    public bool IsReconciled { get; set; }

    /// <summary>
    /// Data in cui è stata riconciliata
    /// </summary>
    public DateTime? ReconciledAt { get; set; }

    /// <summary>
    /// Utente che ha riconciliato
    /// </summary>
    public string? ReconciledBy { get; set; }

    /// <summary>
    /// Riferimento esterno (es. numero transazione bancaria)
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Tipo di item (contabile, bancario, aggiustamento)
    /// </summary>
    public ReconciliationItemType ItemType { get; set; } = ReconciliationItemType.Book;

    public string? Notes { get; set; }

    // Navigation properties
    public Reconciliation? Reconciliation { get; set; }
    public JournalEntry? JournalEntry { get; set; }
}

public enum ReconciliationStatus
{
    InProgress = 0,
    Completed = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum ReconciliationItemType
{
    /// <summary>
    /// Transazione contabile
    /// </summary>
    Book = 0,

    /// <summary>
    /// Transazione da estratto conto bancario
    /// </summary>
    Statement = 1,

    /// <summary>
    /// Aggiustamento manuale
    /// </summary>
    Adjustment = 2
}