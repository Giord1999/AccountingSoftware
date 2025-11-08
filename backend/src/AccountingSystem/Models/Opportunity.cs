using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un'opportunità di vendita nel sistema CRM
/// </summary>
public class Opportunity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    public Guid? LeadId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public OpportunityStage Stage { get; set; } = OpportunityStage.Prospecting;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Probability { get; set; } = 0; // % probabilità di chiusura

    public DateTime? CloseDate { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public Customer? Customer { get; set; }
    public Lead? Lead { get; set; }
    public ICollection<Activity>? Activities { get; set; }
}

/// <summary>
/// Stage dell'opportunità nella pipeline di vendita
/// </summary>
public enum OpportunityStage
{
    Prospecting = 0,
    Qualification = 1,
    Proposal = 2,
    Negotiation = 3,
    ClosedWon = 4,
    ClosedLost = 5
}