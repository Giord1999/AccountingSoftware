using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models;

public class VatRate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
