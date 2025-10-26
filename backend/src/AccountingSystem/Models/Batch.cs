using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un'operazione batch di posting di journal entries
/// </summary>
public class Batch
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    public int TotalCount { get; set; }

    public int PostedCount { get; set; }

    public int FailedCount { get; set; }

    /// <summary>
    /// Lista degli ID dei journal entries nel batch
    /// </summary>
    public string JournalIds { get; set; } = string.Empty; // JSON array serialized

    /// <summary>
    /// Errori occorsi durante il processing
    /// </summary>
    public string? Errors { get; set; } // JSON array serialized

    public string? Description { get; set; }
}

public enum BatchStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4
}