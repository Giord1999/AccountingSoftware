using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

public enum AccountCategory { Asset, Liability, Equity, Revenue, Expense }

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Code { get; set; } = string.Empty; // e.g., 1000
    [Required]
    public string Name { get; set; } = string.Empty;
    public AccountCategory Category { get; set; }
    public Guid CompanyId { get; set; }

    // currency support
    public string Currency { get; set; } = "EUR";

    // optional parent
    public Guid? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }

    public bool IsPostedRestricted { get; set; } = false; // system/config

    // Audit properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}