using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models;

/// <summary>
/// Modello di dati per machine learning - Revenue Prediction
/// </summary>
public class RevenuePredictionData
{
    public float Month { get; set; }
    public float Revenue { get; set; }
    public float PreviousMonthRevenue { get; set; }
    public float AverageRevenueLastThreeMonths { get; set; }
    public float YearOverYearGrowth { get; set; }
    public float SeasonalityIndex { get; set; }
}

/// <summary>
/// Output prediction per revenue forecasting
/// </summary>
public class RevenuePrediction
{
    [ColumnName("Score")]
    public float PredictedRevenue { get; set; }
}

/// <summary>
/// Risultato dashboard BI con KPI e grafici
/// </summary>
public class BIDashboardResult
{
    public BIKPIs KPIs { get; set; } = new();
    public List<ChartData> RevenueChart { get; set; } = [];
    public List<ChartData> ExpenseChart { get; set; } = [];
    public List<ChartData> ProfitMarginChart { get; set; } = [];
    public List<CategoryBreakdownData> RevenueByCategory { get; set; } = [];
    public List<CategoryBreakdownData> ExpenseByCategory { get; set; } = [];
    public List<TrendData> RevenueTrend { get; set; } = [];
    public List<TrendData> ExpenseTrend { get; set; } = [];
    public List<ForecastData> RevenueForecasts { get; set; } = [];
    public CashFlowAnalysis CashFlow { get; set; } = new();
}

public class BIKPIs
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal ROI { get; set; }
    public decimal CashFlowRatio { get; set; }
    public decimal AverageMonthlyRevenue { get; set; }
    public decimal AverageMonthlyExpenses { get; set; }
    public int TransactionCount { get; set; }
    public decimal RevenueGrowthPercentage { get; set; }
}

/// <summary>
/// Chart data per grafici Power BI style
/// </summary>
public class ChartData
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Color { get; set; } = "#007bff";
    public DateTime? Date { get; set; }
}

/// <summary>
/// Breakdown per categoria (Pie/Donut Charts)
/// </summary>
public class CategoryBreakdownData
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Trend data per line charts
/// </summary>
public class TrendData
{
    public DateTime Period { get; set; }
    public decimal Value { get; set; }
    public decimal? MovingAverage { get; set; }
    public decimal? YearOverYear { get; set; }
}

/// <summary>
/// Forecast data da ML model
/// </summary>
public class ForecastData
{
    public DateTime Period { get; set; }
    public decimal PredictedValue { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public decimal UpperBound { get; set; }
    public decimal LowerBound { get; set; }
}

/// <summary>
/// Analisi Cash Flow
/// </summary>
public class CashFlowAnalysis
{
    public decimal OperatingCashFlow { get; set; }
    public decimal InvestingCashFlow { get; set; }
    public decimal FinancingCashFlow { get; set; }
    public decimal NetCashFlow { get; set; }
    public List<ChartData> MonthlyFlows { get; set; } = [];
}

/// <summary>
/// Snapshot BI salvato nel database
/// </summary>
public class BISnapshot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    public Guid? PeriodId { get; set; }

    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;

    [Required]
    public string GeneratedBy { get; set; } = "system";

    /// <summary>
    /// Dati dashboard serializzati in JSON
    /// </summary>
    [Required]
    public string DashboardData { get; set; } = string.Empty;

    /// <summary>
    /// Metadati aggiuntivi
    /// </summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}