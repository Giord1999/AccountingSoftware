// MANCA l'implementazione di IBatchService!
public class BatchService : IBatchService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;
    private readonly IAccountingService _accounting;

    public BatchService(ApplicationDbContext ctx, IAuditService audit, IAccountingService accounting)
    {
        _ctx = ctx;
        _audit = audit;
        _accounting = accounting;
    }

    public async Task<PostBatchResult> PostBatchAsync(IEnumerable<Guid> journalIds, string userId)
    {
        var result = new PostBatchResult
        {
            PostedCount = 0,
            FailedCount = 0,
            Errors = new List<string>()
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cts.Token);

        try
        {
            foreach (var id in journalIds)
            {
                try
                {
                    await _accounting.PostJournalAsync(id, userId);
                    result.PostedCount++;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Journal {id}: {ex.Message}");
                }
            }

            await tx.CommitAsync(cts.Token);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(cts.Token);
            throw;
        }
    }
}

public class PostBatchResult
{
    public int PostedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}