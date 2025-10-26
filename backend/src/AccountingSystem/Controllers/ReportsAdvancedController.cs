using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsAdvancedController : ControllerBase
{
    private readonly IReportService _report;
    private readonly IAccountingService _accounting;
    private readonly ILogger<ReportsAdvancedController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsAdvancedController(
        IReportService report,
        IAccountingService accounting,
        ILogger<ReportsAdvancedController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _report = report;
        _accounting = accounting;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Validazione multi-tenancy centralizzata per tutti gli endpoint report
    /// </summary>
    private async Task<IActionResult?> ValidateCompanyAccessAsync(Guid companyId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Utente {UserId} non trovato", userId);
            return Unauthorized(new { error = "Utente non trovato" });
        }

        var isAdmin = User.IsInRole("Admin");
        var isAuditor = User.IsInRole("Auditor");

        // Admin e Auditor possono vedere tutti i report, altri solo la propria company
        if (!isAdmin && !isAuditor && user.CompanyId.HasValue && user.CompanyId != companyId)
        {
            _logger.LogWarning(
                "Utente {UserId} (Company: {UserCompanyId}) ha tentato di accedere ai report di Company {RequestedCompanyId}",
                userId, user.CompanyId, companyId);

            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "Non hai accesso a questa azienda" });
        }

        return null; // Nessun errore, accesso consentito
    }

    /// <summary>
    /// Ottiene il trial balance dettagliato (da IAccountingService)
    /// </summary>
    /// <param name="companyId">ID dell'azienda</param>
    /// <param name="periodId">ID del periodo contabile</param>
    /// <returns>Trial balance dettagliato</returns>
    [HttpGet("trial-balance")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TrialBalance([FromQuery] Guid companyId, [FromQuery] Guid periodId)
    {
        if (companyId == Guid.Empty || periodId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId e PeriodId sono obbligatori" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy
            var validationResult = await ValidateCompanyAccessAsync(companyId, userId);
            if (validationResult != null) return validationResult;

            _logger.LogInformation("Generazione trial balance per company {CompanyId}, period {PeriodId}", companyId, periodId);

            var result = await _accounting.GetTrialBalanceAsync(companyId, periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione trial balance");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione trial balance");

            var errorResponse = new
            {
                error = "Errore interno del server",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Ottiene il riepilogo del trial balance (da IReportService)
    /// </summary>
    [HttpGet("trial-balance-summary")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TrialBalanceSummary([FromQuery] Guid companyId, [FromQuery] Guid periodId)
    {
        if (companyId == Guid.Empty || periodId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId e PeriodId sono obbligatori" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy
            var validationResult = await ValidateCompanyAccessAsync(companyId, userId);
            if (validationResult != null) return validationResult;

            _logger.LogInformation("Generazione trial balance summary per company {CompanyId}, period {PeriodId}", companyId, periodId);

            var result = await _report.GetTrialBalanceSummaryAsync(companyId, periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione trial balance summary");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione trial balance summary");

            var errorResponse = new
            {
                error = "Errore interno del server",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Ottiene il bilancio (Balance Sheet)
    /// </summary>
    [HttpGet("balance-sheet")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BalanceSheet([FromQuery] Guid companyId, [FromQuery] Guid periodId)
    {
        if (companyId == Guid.Empty || periodId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId e PeriodId sono obbligatori" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy
            var validationResult = await ValidateCompanyAccessAsync(companyId, userId);
            if (validationResult != null) return validationResult;

            _logger.LogInformation("Generazione balance sheet per company {CompanyId}, period {PeriodId}", companyId, periodId);

            var result = await _report.GetBalanceSheetAsync(companyId, periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione balance sheet");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione balance sheet");

            var errorResponse = new
            {
                error = "Errore interno del server",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Ottiene il conto economico (Profit & Loss)
    /// </summary>
    [HttpGet("profit-loss")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ProfitLoss([FromQuery] Guid companyId, [FromQuery] Guid periodId)
    {
        if (companyId == Guid.Empty || periodId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId e PeriodId sono obbligatori" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy
            var validationResult = await ValidateCompanyAccessAsync(companyId, userId);
            if (validationResult != null) return validationResult;

            _logger.LogInformation("Generazione profit & loss per company {CompanyId}, period {PeriodId}", companyId, periodId);

            var result = await _report.GetProfitAndLossAsync(companyId, periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione profit & loss");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione profit & loss");

            var errorResponse = new
            {
                error = "Errore interno del server",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Ottiene i KPI per la dashboard
    /// </summary>
    [HttpGet("dashboard-kpi")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DashboardKpi([FromQuery] Guid companyId, [FromQuery] Guid periodId)
    {
        if (companyId == Guid.Empty || periodId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId e PeriodId sono obbligatori" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy (tutti gli utenti autenticati possono vedere i KPI della loro company)
            var validationResult = await ValidateCompanyAccessAsync(companyId, userId);
            if (validationResult != null) return validationResult;

            _logger.LogInformation("Generazione dashboard KPI per company {CompanyId}, period {PeriodId}", companyId, periodId);

            var result = await _report.GetDashboardKpiAsync(companyId, periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore validazione dashboard KPI");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione dashboard KPI");

            var errorResponse = new
            {
                error = "Errore interno del server",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }
}