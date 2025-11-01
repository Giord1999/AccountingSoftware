using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingSystem.Services
{
    public interface IBatchService
    {
        /// <summary>
        /// Posting in batch di più journal entries (atomico per batch).
        /// </summary>
        Task<PostBatchResult> PostBatchAsync(IEnumerable<Guid> journalIds, string userId);
    }

    public class PostBatchResult
    {
        public int PostedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}