using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models;

public class Company
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = "EUR";
}
