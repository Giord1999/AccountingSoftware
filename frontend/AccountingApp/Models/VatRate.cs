namespace AccountingApp.Models;

public class VatRate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }

    public string DisplayName => $"{Name} ({Rate}%)";
}