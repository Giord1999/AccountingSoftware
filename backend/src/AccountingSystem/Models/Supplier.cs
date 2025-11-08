using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un fornitore nel sistema CRM
/// </summary>
public class Supplier
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
    public string? Sector { get; set; }

    public SupplierRating Rating { get; set; } = SupplierRating.Medium;

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
    public ICollection<Purchase>? Purchases { get; set; }
    public ICollection<Invoice>? Invoices { get; set; }
    public ICollection<Activity>? Activities { get; set; }
}

/// <summary>
/// Rating del fornitore
/// </summary>
public enum SupplierRating
{
    Low = 1,
    Medium = 2,
    High = 3,
    Preferred = 4
}