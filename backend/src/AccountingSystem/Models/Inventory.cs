using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un articolo di magazzino
/// </summary>
public class Inventory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Categoria merceologica (es. "Materie Prime", "Prodotti Finiti", "Semilavorati")
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Unità di misura (es. "PZ", "KG", "LT")
    /// </summary>
    [Required]
    [StringLength(10)]
    public string UnitOfMeasure { get; set; } = "PZ";

    /// <summary>
    /// Quantità attuale in magazzino
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Quantità minima di riordino
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? ReorderLevel { get; set; }

    /// <summary>
    /// Quantità massima in magazzino
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxQuantity { get; set; }

    /// <summary>
    /// Costo unitario medio (FIFO/LIFO/Weighted Average)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Prezzo di vendita suggerito
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalePrice { get; set; }

    /// <summary>
    /// Valuta (default: EUR)
    /// </summary>
    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Codice a barre / EAN
    /// </summary>
    [StringLength(50)]
    public string? Barcode { get; set; }

    /// <summary>
    /// Ubicazione fisica in magazzino
    /// </summary>
    [StringLength(100)]
    public string? Location { get; set; }

    /// <summary>
    /// ID fornitore preferenziale
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// Account contabile collegato per inventario (Asset)
    /// </summary>
    public Guid? InventoryAccountId { get; set; }

    /// <summary>
    /// Account contabile collegato per costo venduto (COGS)
    /// </summary>
    public Guid? CostOfSalesAccountId { get; set; }

    /// <summary>
    /// Centro di analisi predefinito per questo articolo (opzionale)
    /// Permette di associare automaticamente gli articoli a centri di costo/ricavo
    /// </summary>
    public Guid? DefaultAnalysisCenterId { get; set; }

    /// <summary>
    /// Indica se l'articolo è attivo
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = "system";

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Account? InventoryAccount { get; set; }
    public Account? CostOfSalesAccount { get; set; }

    /// <summary>
    /// Centro di analisi predefinito per contabilità analitica
    /// </summary>
    [ForeignKey("DefaultAnalysisCenterId")]
    public AnalysisCenter? DefaultAnalysisCenter { get; set; }
}

/// <summary>
/// Rappresenta un movimento di magazzino (carico/scarico)
/// </summary>
public class InventoryMovement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid InventoryId { get; set; }

    [ForeignKey("InventoryId")]
    public Inventory? Inventory { get; set; }

    [Required]
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    [Required]
    public MovementType Type { get; set; }

    /// <summary>
    /// Quantità movimento (positiva per carico, negativa per scarico)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    [Required]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Costo unitario al momento del movimento
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Valore totale movimento
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalValue { get; set; }

    [StringLength(500)]
    public string? Reference { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Journal Entry associato al movimento (se generato automaticamente)
    /// </summary>
    public Guid? JournalEntryId { get; set; }

    [ForeignKey("JournalEntryId")]
    public JournalEntry? JournalEntry { get; set; }

    /// <summary>
    /// Centro di analisi specifico per questo movimento
    /// Se non specificato, viene utilizzato il DefaultAnalysisCenterId dell'articolo
    /// </summary>
    public Guid? AnalysisCenterId { get; set; }

    /// <summary>
    /// Centro di analisi per contabilità analitica
    /// </summary>
    [ForeignKey("AnalysisCenterId")]
    public AnalysisCenter? AnalysisCenter { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = "system";
}

/// <summary>
/// Tipi di movimento inventario
/// </summary>
public enum MovementType
{
    /// <summary>
    /// Carico iniziale
    /// </summary>
    InitialStock = 0,

    /// <summary>
    /// Acquisto
    /// </summary>
    Purchase = 1,

    /// <summary>
    /// Vendita
    /// </summary>
    Sale = 2,

    /// <summary>
    /// Reso da cliente
    /// </summary>
    SalesReturn = 3,

    /// <summary>
    /// Reso a fornitore
    /// </summary>
    PurchaseReturn = 4,

    /// <summary>
    /// Rettifica inventariale
    /// </summary>
    Adjustment = 5,

    /// <summary>
    /// Trasferimento tra magazzini
    /// </summary>
    Transfer = 6,

    /// <summary>
    /// Scarto/Perdita
    /// </summary>
    Waste = 7
}