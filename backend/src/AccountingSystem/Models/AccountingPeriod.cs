using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models;

public class AccountingPeriod
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsClosed { get; set; } = false;
}
