using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BIController(IBIService biService, ILogger<BIController> logger) : ControllerBase
{
    private readonly IBIService _biService = biService;
    private readonly ILogger<BIController> _logger = logger;

    /// <summary>
    /// 📊 Dashboard BI completa con ML forecasts
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(BIDashboardResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? periodId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "CompanyId obbligatorio" });

        try
        {
            var dashboard = await _biService.GenerateDashboardAsync(companyId, periodId, startDate, endDate, ct);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione dashboard BI");
            return StatusCode(500, new { error = "Errore generazione dashboard", details = ex.Message });
        }
    }

    /// <summary>
    /// 🔮 ML Forecasts (previsioni revenue con machine learning)
    /// </summary>
    [HttpGet("ml-forecasts")]
    [ProducesResponseType(typeof(List<ForecastData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMLForecasts(
        [FromQuery] Guid companyId,
        [FromQuery] int monthsAhead = 6,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "CompanyId obbligatorio" });

        try
        {
            var forecasts = await _biService.GenerateMLForecastsAsync(companyId, monthsAhead, ct);
            return Ok(forecasts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione ML forecasts");
            return StatusCode(500, new { error = "Errore ML forecast", details = ex.Message });
        }
    }

    /// <summary>
    /// 📈 Revenue Trend con moving average e YoY
    /// </summary>
    [HttpGet("revenue-trend")]
    [ProducesResponseType(typeof(List<TrendData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueTrend(
        [FromQuery] Guid companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "CompanyId obbligatorio" });

        try
        {
            var trend = await _biService.GetRevenueTrendAsync(companyId, startDate, endDate, ct);
            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero trend revenue");
            return StatusCode(500, new { error = "Errore trend analysis", details = ex.Message });
        }
    }

    /// <summary>
    /// 🥧 Category Breakdown (pie/donut charts)
    /// </summary>
    [HttpGet("category-breakdown")]
    [ProducesResponseType(typeof(List<CategoryBreakdownData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryBreakdown(
        [FromQuery] Guid companyId,
        [FromQuery] string category = "Revenue",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "CompanyId obbligatorio" });

        if (!Enum.TryParse<AccountCategory>(category, out var accountCategory))
            return BadRequest(new { error = "Categoria non valida" });

        try
        {
            var breakdown = await _biService.GetCategoryBreakdownAsync(companyId, accountCategory, startDate, endDate, ct);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore category breakdown");
            return StatusCode(500, new { error = "Errore breakdown analysis", details = ex.Message });
        }
    }

    /// <summary>
    /// 💾 Salva snapshot dashboard
    /// </summary>
    [HttpPost("snapshot")]
    [Authorize(Roles = "Admin,Contabile")]
    [ProducesResponseType(typeof(BISnapshot), StatusCodes.Status201Created)]
    public async Task<IActionResult> SaveSnapshot(
        [FromQuery] Guid companyId,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "CompanyId obbligatorio" });

        try
        {
            var userId = User?.Identity?.Name ?? "system";
            var dashboard = await _biService.GenerateDashboardAsync(companyId, null, null, null, ct);
            var snapshot = await _biService.SaveSnapshotAsync(companyId, dashboard, userId, ct);

            return CreatedAtAction(nameof(GetSnapshot), new { id = snapshot.Id }, snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore salvataggio snapshot");
            return StatusCode(500, new { error = "Errore snapshot", details = ex.Message });
        }
    }

    /// <summary>
    /// 📷 Recupera snapshot storico
    /// </summary>
    [HttpGet("snapshot/{id:guid}")]
    [ProducesResponseType(typeof(BISnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshot(Guid id, CancellationToken ct = default)
    {
        try
        {
            var snapshot = await _biService.GetSnapshotAsync(id, ct);
            return snapshot != null ? Ok(snapshot) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero snapshot");
            return StatusCode(500, new { error = "Errore snapshot", details = ex.Message });
        }
    }
}