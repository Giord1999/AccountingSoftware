using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Categoria del conto contabile
/// </summary>
public enum AccountCategory
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

/// <summary>
/// Status del journal entry
/// </summary>
public enum JournalStatus
{
    Draft,
    Posted,
    Cancelled
}

/// <summary>
/// Status delle operazioni batch
/// </summary>
public enum BatchStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Rappresenta un conto contabile
/// </summary>
public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty; // e.g., 1000

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public AccountCategory Category { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    // currency support
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    // optional parent
    public Guid? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }

    public bool IsPostedRestricted { get; set; } = false;
}

/// <summary>
/// Rappresenta un journal entry (registrazione contabile)
/// </summary>
public class JournalEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid PeriodId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public JournalStatus Status { get; set; } = JournalStatus.Draft;

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public decimal ExchangeRate { get; set; } = 1.0m; // to company base currency

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}

/// <summary>
/// Rappresenta una riga di un journal entry
/// </summary>
public class JournalLine
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JournalEntryId { get; set; }

    [ForeignKey("JournalEntryId")]
    public JournalEntry? JournalEntry { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    public Account? Account { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }

    [MaxLength(1000)]
    public string? Narrative { get; set; }
}

/// <summary>
/// Rappresenta un periodo contabile
/// </summary>
public class AccountingPeriod
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    public bool IsClosed { get; set; } = false;

    [MaxLength(100)]
    public string? Name { get; set; }
}

/// <summary>
/// Rappresenta un'azienda
/// </summary>
public class Company
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? VATNumber { get; set; }

    [MaxLength(3)]
    public string BaseCurrency { get; set; } = "EUR";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rappresenta una valuta
/// </summary>
public class Currency
{
    [Key]
    [MaxLength(3)]
    public string Code { get; set; } = string.Empty; // ISO 4217

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
}

/// <summary>
/// Rappresenta un tasso di cambio
/// </summary>
public class ExchangeRate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(3)]
    public string FromCurrency { get; set; } = string.Empty;

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Rate { get; set; }
}

/// <summary>
/// Rappresenta un'aliquota IVA
/// </summary>
public class VATRate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Rate { get; set; }

    public Guid CompanyId { get; set; }
}

/// <summary>
/// Rappresenta un log di audit
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Details { get; set; }
}

/// <summary>
/// Rappresenta un'operazione batch di posting di journal entries
/// </summary>
public class Batch
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    public int TotalCount { get; set; }

    public int PostedCount { get; set; }

    public int FailedCount { get; set; }

    /// <summary>
    /// Lista degli ID dei journal entries nel batch (JSON array serialized)
    /// </summary>
    [Required]
    public string JournalIds { get; set; } = "[]";

    /// <summary>
    /// Errori occorsi durante il processing (JSON array serialized)
    /// </summary>
    public string? Errors { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Rappresenta un report generato
/// </summary>
public class Report
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // TrialBalance, BalanceSheet, IncomeStatement, etc.

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(450)]
    public string GeneratedBy { get; set; } = string.Empty;

    public string? Parameters { get; set; } // JSON serialized

    public string? Data { get; set; } // JSON serialized report data
}

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

    // Navigation properties
    public Account? Account { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal StatementBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BookBalance { get; set; }

    public bool IsReconciled { get; set; } = false;

    [MaxLength(450)]
    public string? ReconciledBy { get; set; }

    public DateTime? ReconciledAt { get; set; }

    public ICollection<ReconciliationItem> Items { get; set; } = new List<ReconciliationItem>();
}

/// <summary>
/// Rappresenta un elemento di riconciliazione
/// </summary>
public class ReconciliationItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ReconciliationId { get; set; }

    public Reconciliation? Reconciliation { get; set; }

    public Guid? JournalLineId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsMatched { get; set; } = false;
}