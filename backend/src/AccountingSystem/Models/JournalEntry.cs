using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

public enum JournalStatus { Draft, Posted }

public class JournalEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public JournalStatus Status { get; set; } = JournalStatus.Draft;
    public string Currency { get; set; } = "EUR";
    public decimal ExchangeRate { get; set; } = 1m; // to company base currency

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}

public class JournalLine
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JournalEntryId { get; set; }
    [ForeignKey("JournalEntryId")] public JournalEntry? JournalEntry { get; set; }

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public string? Narrative { get; set; }
}
