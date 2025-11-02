using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un centro di analisi (costo o ricavo) per contabilità analitica
/// </summary>
public class AnalysisCenter
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required, StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public AnalysisCenterType Type { get; set; } = AnalysisCenterType.Cost;

    public bool IsActive { get; set; } = true;

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
}

public enum AnalysisCenterType
{
    Cost = 0,    // Centro di costo (es. Produzione, Magazzino)
    Revenue = 1  // Centro di ricavo (es. Vendite, Marketing)
}