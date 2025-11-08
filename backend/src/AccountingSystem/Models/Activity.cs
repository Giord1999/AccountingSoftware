using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un'attività (interazione) nel sistema CRM
/// </summary>
public class Activity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? OpportunityId { get; set; }

    [Required]
    public ActivityType Type { get; set; }

    [Required, StringLength(500)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime ScheduledDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public ActivityStatus Status { get; set; } = ActivityStatus.Scheduled;

    [StringLength(200)]
    public string? AssignedTo { get; set; } // User ID

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public Lead? Lead { get; set; }
    public Opportunity? Opportunity { get; set; }
}

/// <summary>
/// Tipo di attività
/// </summary>
public enum ActivityType
{
    Call = 0,
    Email = 1,
    Meeting = 2,
    Task = 3,
    Note = 4
}

/// <summary>
/// Stato dell'attività
/// </summary>
public enum ActivityStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2
}