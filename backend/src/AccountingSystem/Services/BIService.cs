using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;

namespace AccountingSystem.Services;

public class BIService(ApplicationDbContext context, ILogger<BIService> logger) : IBIService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<BIService> _logger = logger;
    private readonly MLContext _mlContext = new MLContext(seed: 42);

    public async Task<BIDashboardResult> GenerateDashboardAsync(
        Guid companyId,
        Guid? periodId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating BI Dashboard for company {CompanyId}", companyId);

        // Determina date range
        DateTime fromDate, toDate;
        if (periodId.HasValue)
        {
            var period = await _context.AccountingPeriods.FindAsync(new object[] { periodId.Value }, ct);
            if (period == null) throw new InvalidOperationException("Period not found");
            fromDate = period.Start;
            toDate = period.End;
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            fromDate = startDate.Value;
            toDate = endDate.Value;
        }
        else
        {
            // Default: ultimi 12 mesi
            toDate = DateTime.UtcNow;
            fromDate = toDate.AddMonths(-12);
        }

        var result = new BIDashboardResult();

        // 1. Calcola KPIs
        result.KPIs = await CalculateKPIsAsync(companyId, fromDate, toDate, ct);

        // 2. Revenue Chart (mensile)
        result.RevenueChart = await GetMonthlyRevenueChartAsync(companyId, fromDate, toDate, ct);

        // 3. Expense Chart (mensile)
        result.ExpenseChart = await GetMonthlyExpenseChartAsync(companyId, fromDate, toDate, ct);

        // 4. Profit Margin Chart
        result.ProfitMarginChart = await GetProfitMarginChartAsync(companyId, fromDate, toDate, ct);

        // 5. Revenue by Category
        result.RevenueByCategory = await GetCategoryBreakdownAsync(companyId, AccountCategory.Revenue, fromDate, toDate, ct);

        // 6. Expense by Category
        result.ExpenseByCategory = await GetCategoryBreakdownAsync(companyId, AccountCategory.Expense, fromDate, toDate, ct);

        // 7. Revenue Trend
        result.RevenueTrend = await GetRevenueTrendAsync(companyId, fromDate, toDate, ct);

        // 8. Expense Trend
        result.ExpenseTrend = await GetExpenseTrendAsync(companyId, fromDate, toDate, ct);

        // 9. ML Forecasts
        result.RevenueForecasts = await GenerateMLForecastsAsync(companyId, 6, ct);

        // 10. Cash Flow Analysis
        result.CashFlow = await GetCashFlowAnalysisAsync(companyId, fromDate, toDate, ct);

        _logger.LogInformation("BI Dashboard generated successfully with {ForecastCount} ML forecasts", result.RevenueForecasts.Count);

        return result;
    }

    public async Task<List<ForecastData>> GenerateMLForecastsAsync(
        Guid companyId,
        int monthsAhead = 6,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating ML forecasts for {Months} months ahead", monthsAhead);

        // Recupera dati storici (ultimi 24 mesi)
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-24);

        var historicalData = await GetHistoricalRevenueDataAsync(companyId, startDate, endDate, ct);

        if (historicalData.Count < 3)
        {
            _logger.LogWarning("Insufficient historical data for ML forecasting");
            return new List<ForecastData>();
        }

        // Prepara training data
        var trainingData = PrepareTrainingData(historicalData);

        // Train model
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(RevenuePredictionData.Month),
                nameof(RevenuePredictionData.PreviousMonthRevenue),
                nameof(RevenuePredictionData.AverageRevenueLastThreeMonths),
                nameof(RevenuePredictionData.YearOverYearGrowth),
                nameof(RevenuePredictionData.SeasonalityIndex))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(RevenuePredictionData.Revenue),
                featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);

        // Prediction engine
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<RevenuePredictionData, RevenuePrediction>(model);

        // Genera forecast
        var forecasts = new List<ForecastData>();
        var lastData = trainingData[trainingData.Count - 1];

        for (int i = 1; i <= monthsAhead; i++)
        {
            var futureMonth = endDate.AddMonths(i);
            var monthNumber = ((int)lastData.Month + i) % 12;
            if (monthNumber == 0) monthNumber = 12;

            var input = new RevenuePredictionData
            {
                Month = monthNumber,
                PreviousMonthRevenue = lastData.Revenue,
                AverageRevenueLastThreeMonths = trainingData.TakeLast(3).Average(x => x.Revenue),
                YearOverYearGrowth = CalculateGrowthRate(trainingData),
                SeasonalityIndex = CalculateSeasonalityIndex(monthNumber, historicalData)
            };

            var prediction = predictionEngine.Predict(input);

            // Confidence interval (95%)
            var stdDev = CalculateStandardDeviation(trainingData.Select(x => x.Revenue).ToList());
            var margin = stdDev * 1.96f;

            forecasts.Add(new ForecastData
            {
                Period = futureMonth,
                PredictedValue = (decimal)prediction.PredictedRevenue,
                ConfidenceLevel = 0.95m,
                UpperBound = (decimal)(prediction.PredictedRevenue + margin),
                LowerBound = (decimal)Math.Max(0, prediction.PredictedRevenue - margin)
            });

            // Update per prossima iterazione
            lastData = input;
            lastData.Revenue = prediction.PredictedRevenue;
        }

        _logger.LogInformation("Generated {Count} ML-based forecasts", forecasts.Count);

        return forecasts;
    }

    public async Task<List<TrendData>> GetRevenueTrendAsync(
        Guid companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        var query = from je in _context.JournalEntries
                    where je.CompanyId == companyId
                       && je.Status == JournalStatus.Posted
                       && je.Date >= startDate
                       && je.Date <= endDate
                    from l in je.Lines
                    join a in _context.Accounts on l.AccountId equals a.Id
                    where a.Category == AccountCategory.Revenue
                    select new { je.Date, Amount = l.Credit - l.Debit };

        var data = await query.ToListAsync(ct);

        var grouped = data
            .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .Select(g => new
            {
                Period = g.Key,
                Value = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Period)
            .ToList();

        var trend = grouped.Select((item, index) =>
        {
            var ma = grouped
                .Skip(Math.Max(0, index - 2))
                .Take(3)
                .Average(x => x.Value);

            var yoy = index >= 12
                ? ((item.Value - grouped[index - 12].Value) / grouped[index - 12].Value) * 100
                : (decimal?)null;

            return new TrendData
            {
                Period = item.Period,
                Value = item.Value,
                MovingAverage = ma,
                YearOverYear = yoy
            };
        }).ToList();

        return trend;
    }

    public async Task<List<CategoryBreakdownData>> GetCategoryBreakdownAsync(
        Guid companyId,
        AccountCategory category,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
        var end = endDate ?? DateTime.UtcNow;

        var query = from je in _context.JournalEntries
                    where je.CompanyId == companyId
                       && je.Status == JournalStatus.Posted
                       && je.Date >= start
                       && je.Date <= end
                    from l in je.Lines
                    join a in _context.Accounts on l.AccountId equals a.Id
                    where a.Category == category
                    select new { a.Name, Amount = l.Credit - l.Debit };

        var data = await query.ToListAsync(ct);

        var total = data.Sum(x => Math.Abs(x.Amount));

        var breakdown = data
            .GroupBy(x => x.Name)
            .Select(g => new CategoryBreakdownData
            {
                Category = g.Key,
                Amount = Math.Abs(g.Sum(x => x.Amount)),
                Count = g.Count(),
                Percentage = total > 0 ? (Math.Abs(g.Sum(x => x.Amount)) / total) * 100 : 0
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return breakdown;
    }

    public async Task<BISnapshot> SaveSnapshotAsync(
        Guid companyId,
        BIDashboardResult dashboard,
        string userId,
        CancellationToken ct = default)
    {
        var snapshot = new BISnapshot
        {
            CompanyId = companyId,
            GeneratedBy = userId,
            SnapshotDate = DateTime.UtcNow,
            DashboardData = JsonSerializer.Serialize(dashboard),
            Metadata = JsonSerializer.Serialize(new
            {
                KPICount = 1,
                ChartCount = dashboard.RevenueChart.Count + dashboard.ExpenseChart.Count,
                ForecastCount = dashboard.RevenueForecasts.Count
            })
        };

        _context.Set<BISnapshot>().Add(snapshot);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("BI Snapshot {SnapshotId} saved for company {CompanyId}", snapshot.Id, companyId);

        return snapshot;
    }

    public async Task<BISnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
    {
        return await _context.Set<BISnapshot>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task<BIKPIs> CalculateKPIsAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var revenues = await GetTotalByCategory(companyId, AccountCategory.Revenue, fromDate, toDate, ct);
        var expenses = await GetTotalByCategory(companyId, AccountCategory.Expense, fromDate, toDate, ct);
        var assets = await GetTotalByCategory(companyId, AccountCategory.Asset, fromDate, toDate, ct);

        var netProfit = revenues - expenses;
        var profitMargin = revenues > 0 ? (netProfit / revenues) * 100 : 0;
        var roi = assets > 0 ? (netProfit / assets) * 100 : 0;

        var monthCount = (int)((toDate - fromDate).TotalDays / 30);
        var avgMonthlyRevenue = monthCount > 0 ? revenues / monthCount : 0;
        var avgMonthlyExpenses = monthCount > 0 ? expenses / monthCount : 0;

        var transactionCount = await _context.JournalEntries
            .Where(j => j.CompanyId == companyId && j.Status == JournalStatus.Posted && j.Date >= fromDate && j.Date <= toDate)
            .CountAsync(ct);

        // Calculate growth vs previous period
        var previousPeriodStart = fromDate.AddDays(-(toDate - fromDate).TotalDays);
        var previousRevenue = await GetTotalByCategory(companyId, AccountCategory.Revenue, previousPeriodStart, fromDate, ct);
        var revenueGrowth = previousRevenue > 0 ? ((revenues - previousRevenue) / previousRevenue) * 100 : 0;

        return new BIKPIs
        {
            TotalRevenue = revenues,
            TotalExpenses = expenses,
            NetProfit = netProfit,
            ProfitMargin = profitMargin,
            ROI = roi,
            CashFlowRatio = expenses > 0 ? revenues / expenses : 0,
            AverageMonthlyRevenue = avgMonthlyRevenue,
            AverageMonthlyExpenses = avgMonthlyExpenses,
            TransactionCount = transactionCount,
            RevenueGrowthPercentage = revenueGrowth
        };
    }

    private async Task<decimal> GetTotalByCategory(Guid companyId, AccountCategory category, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var total = await (from je in _context.JournalEntries
                           where je.CompanyId == companyId && je.Status == JournalStatus.Posted && je.Date >= fromDate && je.Date <= toDate
                           from l in je.Lines
                           join a in _context.Accounts on l.AccountId equals a.Id
                           where a.Category == category
                           select (l.Credit - l.Debit)).SumAsync(ct);

        return Math.Abs(total);
    }

    private async Task<List<ChartData>> GetMonthlyRevenueChartAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var data = await (from je in _context.JournalEntries
                          where je.CompanyId == companyId && je.Status == JournalStatus.Posted && je.Date >= fromDate && je.Date <= toDate
                          from l in je.Lines
                          join a in _context.Accounts on l.AccountId equals a.Id
                          where a.Category == AccountCategory.Revenue
                          select new { je.Date, Amount = l.Credit - l.Debit }).ToListAsync(ct);

        return data
            .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .Select(g => new ChartData
            {
                Label = g.Key.ToString("MMM yyyy"),
                Value = g.Sum(x => x.Amount),
                Date = g.Key,
                Color = "#28a745"
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    private async Task<List<ChartData>> GetMonthlyExpenseChartAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var data = await (from je in _context.JournalEntries
                          where je.CompanyId == companyId && je.Status == JournalStatus.Posted && je.Date >= fromDate && je.Date <= toDate
                          from l in je.Lines
                          join a in _context.Accounts on l.AccountId equals a.Id
                          where a.Category == AccountCategory.Expense
                          select new { je.Date, Amount = l.Debit - l.Credit }).ToListAsync(ct);

        return data
            .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .Select(g => new ChartData
            {
                Label = g.Key.ToString("MMM yyyy"),
                Value = Math.Abs(g.Sum(x => x.Amount)),
                Date = g.Key,
                Color = "#dc3545"
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    private async Task<List<ChartData>> GetProfitMarginChartAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var revenues = await GetMonthlyRevenueChartAsync(companyId, fromDate, toDate, ct);
        var expenses = await GetMonthlyExpenseChartAsync(companyId, fromDate, toDate, ct);

        return revenues.Select((r, index) =>
        {
            var expense = expenses.FirstOrDefault(e => e.Date == r.Date)?.Value ?? 0;
            var margin = r.Value > 0 ? ((r.Value - expense) / r.Value) * 100 : 0;

            return new ChartData
            {
                Label = r.Label,
                Value = margin,
                Date = r.Date,
                Color = margin >= 0 ? "#ffc107" : "#dc3545"
            };
        }).ToList();
    }

    private async Task<List<TrendData>> GetExpenseTrendAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var query = from je in _context.JournalEntries
                    where je.CompanyId == companyId && je.Status == JournalStatus.Posted && je.Date >= fromDate && je.Date <= toDate
                    from l in je.Lines
                    join a in _context.Accounts on l.AccountId equals a.Id
                    where a.Category == AccountCategory.Expense
                    select new { je.Date, Amount = l.Debit - l.Credit };

        var data = await query.ToListAsync(ct);

        var grouped = data
            .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .Select(g => new { Period = g.Key, Value = Math.Abs(g.Sum(x => x.Amount)) })
            .OrderBy(x => x.Period)
            .ToList();

        return grouped.Select((item, index) => new TrendData
        {
            Period = item.Period,
            Value = item.Value,
            MovingAverage = grouped.Skip(Math.Max(0, index - 2)).Take(3).Average(x => x.Value)
        }).ToList();
    }

    private async Task<CashFlowAnalysis> GetCashFlowAnalysisAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var revenues = await GetTotalByCategory(companyId, AccountCategory.Revenue, fromDate, toDate, ct);
        var expenses = await GetTotalByCategory(companyId, AccountCategory.Expense, fromDate, toDate, ct);

        return new CashFlowAnalysis
        {
            OperatingCashFlow = revenues - expenses,
            InvestingCashFlow = 0, // Placeholder - implementa logica specifica
            FinancingCashFlow = 0, // Placeholder
            NetCashFlow = revenues - expenses,
            MonthlyFlows = await GetMonthlyRevenueChartAsync(companyId, fromDate, toDate, ct)
        };
    }

    private async Task<List<(DateTime Month, decimal Revenue)>> GetHistoricalRevenueDataAsync(
        Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var data = await (from je in _context.JournalEntries
                          where je.CompanyId == companyId && je.Status == JournalStatus.Posted && je.Date >= startDate && je.Date <= endDate
                          from l in je.Lines
                          join a in _context.Accounts on l.AccountId equals a.Id
                          where a.Category == AccountCategory.Revenue
                          select new { je.Date, Amount = l.Credit - l.Debit }).ToListAsync(ct);

        return data
            .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .Select(g => (g.Key, g.Sum(x => x.Amount)))
            .OrderBy(x => x.Key)
            .ToList();
    }

    private static List<RevenuePredictionData> PrepareTrainingData(List<(DateTime Month, decimal Revenue)> historicalData)
    {
        var training = new List<RevenuePredictionData>();

        for (int i = 0; i < historicalData.Count; i++)
        {
            var current = historicalData[i];

            training.Add(new RevenuePredictionData
            {
                Month = current.Month.Month,
                Revenue = (float)current.Revenue,
                PreviousMonthRevenue = i > 0 ? (float)historicalData[i - 1].Revenue : (float)current.Revenue,
                AverageRevenueLastThreeMonths = i >= 2
                    ? (float)historicalData.Skip(i - 2).Take(3).Average(x => x.Revenue)
                    : (float)current.Revenue,
                YearOverYearGrowth = i >= 12
                    ? (float)(((current.Revenue - historicalData[i - 12].Revenue) / historicalData[i - 12].Revenue) * 100)
                    : 0f,
                SeasonalityIndex = CalculateSeasonalityIndex(current.Month.Month, historicalData)
            });
        }

        return training;
    }

    private static float CalculateSeasonalityIndex(int month, List<(DateTime Month, decimal Revenue)> historicalData)
    {
        var monthlyAverages = historicalData
            .Where(x => x.Month.Month == month)
            .Select(x => (float)x.Revenue)
            .ToList();

        if (monthlyAverages.Count == 0) return 1.0f;

        var overallAverage = (float)historicalData.Average(x => x.Revenue);
        var monthAverage = monthlyAverages.Average();

        return overallAverage > 0 ? monthAverage / overallAverage : 1.0f;
    }

    private static float CalculateGrowthRate(List<RevenuePredictionData> data)
    {
        if (data.Count < 2) return 0f;

        int startIndex = Math.Max(0, data.Count - 6);
        float first = data[startIndex].Revenue;
        float last = data[data.Count - 1].Revenue;
        return (last - first) / first;
    }

    private static float CalculateStandardDeviation(List<float> values)
    {
        if (values.Count < 2) return 0f;

        var avg = values.Average();
        var sumOfSquares = values.Sum(x => Math.Pow(x - avg, 2));
        return (float)Math.Sqrt(sumOfSquares / values.Count);
    }
}