namespace AccountingSystem.Services;
public interface IBatchService
{
    /// <summary>
    /// Posting in batch di più journal entries (atomico per batch).
    /// </summary>
    Task<PostBatchResult> PostBatchAsync(IEnumerable<Guid> journalIds, string userId);
}

public record PostBatchResult(int PostedCount, int FailedCount, IEnumerable<string> Errors);
