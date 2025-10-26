using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models;

/// <summary>
/// Rappresenta un report generato e salvato nel sistema
/// </summary>
public class Report
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid PeriodId { get; set; }

    [Required]
    public ReportType Type { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>
    /// Dati del report serializzati in JSON
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Formato di output (JSON, PDF, Excel, etc.)
    /// </summary>
    public string Format { get; set; } = "JSON";

    /// <summary>
    /// Parametri utilizzati per generare il report
    /// </summary>
    public string? Parameters { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Completed;

    /// <summary>
    /// Percorso del file se salvato su disco
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Dimensione del file in bytes
    /// </summary>
    public long? FileSize { get; set; }

    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public AccountingPeriod? Period { get; set; }
}

public enum ReportType
{
    BalanceSheet = 0,
    ProfitAndLoss = 1,
    TrialBalance = 2,
    DashboardKpi = 3,
    CashFlow = 4,
    GeneralLedger = 5,
    AgedReceivables = 6,
    AgedPayables = 7,
    Custom = 99
}

public enum ReportStatus
{
    Pending = 0,
    Generating = 1,
    Completed = 2,
    Failed = 3,
    Archived = 4
}