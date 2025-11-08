using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un cliente nel sistema CRM
/// </summary>
public class Customer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; } = "Italia";

    [StringLength(50)]
    public string? VatNumber { get; set; }

    [StringLength(100)]
    public string? Sector { get; set; } // Es. "Tecnologia", "Commercio", ecc.

    public CustomerRating Rating { get; set; } = CustomerRating.Medium;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public ICollection<Sale>? Sales { get; set; }
    public ICollection<Invoice>? Invoices { get; set; }
    public ICollection<Lead>? Leads { get; set; }
    public ICollection<Opportunity>? Opportunities { get; set; }
    public ICollection<Activity>? Activities { get; set; }
}

/// <summary>
/// Rating del cliente
/// </summary>
public enum CustomerRating
{
    Low = 1,
    Medium = 2,
    High = 3,
    VIP = 4
}