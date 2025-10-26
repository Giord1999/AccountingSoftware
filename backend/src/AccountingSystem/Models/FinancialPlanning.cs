using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.FinancialPlanning;

public enum FinancialPlanStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Archived = 4
}

public class FinancialPlan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public Guid? CurrencyId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public FinancialPlanStatus Status { get; set; } = FinancialPlanStatus.Draft;

    [Required]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<FinancialPlanItem> Items { get; set; } = new List<FinancialPlanItem>();
}

public class FinancialPlanItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FinancialPlanId { get; set; }

    [ForeignKey("FinancialPlanId")]
    public FinancialPlan? FinancialPlan { get; set; }

    public Guid? AccountId { get; set; }

    [Required]
    public DateTime Period { get; set; }

    [Required, StringLength(200)]
    public string Category { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class Forecast
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FinancialPlanId { get; set; }

    [ForeignKey("FinancialPlanId")]
    public FinancialPlan? FinancialPlan { get; set; }

    public DateTime Period { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public double Confidence { get; set; }

    public string? GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
