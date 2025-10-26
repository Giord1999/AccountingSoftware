using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/reconcile")]
[Authorize(Roles = "Admin,Contabile,Auditor")]
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _rec;
    private readonly ILogger<ReconciliationController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReconciliationController(
        IReconciliationService rec,
        ILogger<ReconciliationController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _rec = rec;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Esegue la riconciliazione di un conto contabile per un periodo specificato
    /// </summary>
    /// <param name="accountId">ID del conto da riconciliare</param>
    /// <param name="companyId">ID dell'azienda</param>
    /// <param name="from">Data inizio periodo</param>
    /// <param name="to">Data fine periodo</param>
    /// <returns>Risultato della riconciliazione</returns>
    /// <response code="200">Riconciliazione completata con successo</response>
    /// <response code="400">Parametri non validi</response>
    /// <response code="401">Non autorizzato</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reconcile(
        Guid accountId,
        [FromQuery] Guid companyId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        // Validazione parametri
        if (accountId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di riconciliazione con AccountId vuoto");
            return BadRequest(new { error = "AccountId non valido" });
        }

        if (companyId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di riconciliazione con CompanyId vuoto");
            return BadRequest(new { error = "CompanyId non valido" });
        }

        if (from == default)
        {
            _logger.LogWarning("Data 'from' non specificata");
            return BadRequest(new { error = "La data 'from' è obbligatoria" });
        }

        if (to == default)
        {
            _logger.LogWarning("Data 'to' non specificata");
            return BadRequest(new { error = "La data 'to' è obbligatoria" });
        }

        if (from > to)
        {
            _logger.LogWarning("Data 'from' {From} successiva a 'to' {To}", from, to);
            return BadRequest(new { error = "La data 'from' deve essere precedente o uguale a 'to'" });
        }

        // Validazione periodo massimo (es. 5 anni)
        if ((to - from).TotalDays > 1825) // ~5 anni
        {
            _logger.LogWarning("Periodo richiesto troppo ampio: {Days} giorni", (to - from).TotalDays);
            return BadRequest(new { error = "Il periodo massimo consentito è di 5 anni" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy: verifica che l'utente abbia accesso alla company
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");
            var isAuditor = User.IsInRole("Auditor");

            // Admin e Auditor possono riconciliare per tutte le company, Contabile solo la propria
            if (!isAdmin && !isAuditor && user.CompanyId.HasValue && user.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di riconciliare account per Company {RequestedCompanyId}",
                    userId, user.CompanyId, companyId);

                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            _logger.LogInformation(
                "Riconciliazione account {AccountId} per company {CompanyId} dal {From} al {To}",
                accountId, companyId, from, to);

            var result = await _rec.ReconcileAsync(companyId, accountId, from, to);

            _logger.LogInformation("Riconciliazione completata per account {AccountId}", accountId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante riconciliazione account {AccountId}", accountId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante riconciliazione account {AccountId}", accountId);

            var errorResponse = new
            {
                error = "Errore interno del server durante la riconciliazione",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }
}