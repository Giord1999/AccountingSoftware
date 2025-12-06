namespace AccountingApp.Models;

public class AccountingPeriod
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsClosed { get; set; }

    // Proprietà per visualizzazione nel Picker
    public string DisplayName => $"{Start:MMM yyyy} - {End:MMM yyyy}{(IsClosed ? " (Chiuso)" : "")}";
}