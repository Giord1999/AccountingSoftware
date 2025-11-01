using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace AccountingSystem.Services
{
    public class BatchService(ApplicationDbContext ctx, IAccountingService accounting) : IBatchService
    {
        private readonly ApplicationDbContext _ctx = ctx;
        private readonly IAccountingService _accounting = accounting;

        public async Task<PostBatchResult> PostBatchAsync(IEnumerable<Guid> journalIds, string userId)
        {
            var result = new PostBatchResult
            {
                PostedCount = 0,
                FailedCount = 0,
                Errors = new List<string>()
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var tx = await _ctx.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

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
}