using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models;

public class Currency
{
    [Key]
    public string Code { get; set; } = "EUR";
    public string Name { get; set; } = string.Empty;
}

public class ExchangeRate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CurrencyFrom { get; set; } = "EUR";
    public string CurrencyTo { get; set; } = "EUR";
    public decimal Rate { get; set; } = 1m;
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
