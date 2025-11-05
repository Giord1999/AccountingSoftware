using AccountingSystem.Models;


namespace AccountingSystem.Services;

public interface IBIService
{
    /// <summary>
    /// Genera dashboard completa con ML forecasts
    /// </summary>
    Task<BIDashboardResult> GenerateDashboardAsync(
        Guid companyId,
        Guid? periodId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Previsioni revenue con ML.NET
    /// </summary>
    Task<List<ForecastData>> GenerateMLForecastsAsync(
        Guid companyId,
        int monthsAhead = 6,
        CancellationToken ct = default);

    /// <summary>
    /// Analisi trend avanzata
    /// </summary>
    Task<List<TrendData>> GetRevenueTrendAsync(
        Guid companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Breakdown categorie
    /// </summary>
    Task<List<CategoryBreakdownData>> GetCategoryBreakdownAsync(
        Guid companyId,
        AccountCategory category,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Salva snapshot BI
    /// </summary>
    Task<BISnapshot> SaveSnapshotAsync(
        Guid companyId,
        BIDashboardResult dashboard,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Recupera snapshot storico
    /// </summary>
    Task<BISnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default);
}