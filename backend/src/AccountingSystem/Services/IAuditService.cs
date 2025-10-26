namespace AccountingSystem.Services;


public interface IAuditService
{
    Task LogAsync(string userId, string action, string details);

    /// <summary>
    /// Recupera log audit con filtri e paginazione
    /// </summary>
    Task<object> GetLogsAsync(
        string? userId,
        string? action,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
}