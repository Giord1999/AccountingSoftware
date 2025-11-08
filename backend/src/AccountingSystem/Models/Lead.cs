using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un lead (prospect) nel sistema CRM
/// </summary>
public class Lead
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    public Guid? CustomerId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Source { get; set; } // Es. "Website", "Referral", "Cold Call"

    public LeadStatus Status { get; set; } = LeadStatus.New;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 0; // Scoring automatico basato su ML

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime? QualifiedDate { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<Opportunity>? Opportunities { get; set; }
    public ICollection<Activity>? Activities { get; set; }
}

/// <summary>
/// Stato del lead nella pipeline
/// </summary>
public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Proposal = 3,
    Negotiation = 4,
    ClosedWon = 5,
    ClosedLost = 6
}