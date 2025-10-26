
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _ctx;

    public AuditService(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task LogAsync(string userId, string action, string details)
    {
        var a = new AuditLog
        {
            UserId = userId ?? "system",
            Action = action,
            Details = details
        };
        _ctx.AuditLogs.Add(a);
        await _ctx.SaveChangesAsync();
    }

    public async Task<object> GetLogsAsync(
        string? userId,
        string? action,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var query = _ctx.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action));

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = logs
        };
    }
}