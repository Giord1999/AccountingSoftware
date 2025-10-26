using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/periods")]
[Authorize]
public class AccountingPeriodController : ControllerBase
{
    private readonly IAccountingPeriodService _periodService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountingPeriodController> _logger;

    public AccountingPeriodController(
        IAccountingPeriodService periodService,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountingPeriodController> logger)
    {
        _periodService = periodService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuovo periodo contabile
    /// </summary>
    /// <param name="request">Dati del periodo da creare</param>
    /// <returns>Periodo creato</returns>
    /// <response code="201">Periodo creato con successo</response>
    /// <response code="400">Dati non validi</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(AccountingPeriod), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePeriod([FromBody] CreatePeriodRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreatePeriod");
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            _logger.LogWarning("Tentativo di creare periodo con payload null");
            return BadRequest(new { error = "Request non può essere null" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            // Validazione multi-tenancy
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != request.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di creare periodo per Company {RequestedCompanyId}",
                    userId, user.CompanyId, request.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            var period = new AccountingPeriod
            {
                CompanyId = request.CompanyId,
                Start = request.Start,
                End = request.End,
                IsClosed = false
            };

            _logger.LogInformation("Utente {UserId} sta creando periodo {Start} - {End} per company {CompanyId}",
                userId, period.Start, period.End, period.CompanyId);

            var result = await _periodService.CreatePeriodAsync(period, userId);

            _logger.LogInformation("Periodo {PeriodId} creato con successo", result.Id);
            return CreatedAtAction(nameof(GetPeriodById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante creazione periodo");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante creazione periodo");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la creazione del periodo" });
        }
    }

    /// <summary>
    /// Ottiene i periodi contabili di un'azienda
    /// </summary>
    /// <param name="companyId">ID dell'azienda</param>
    /// <returns>Lista periodi</returns>
    /// <response code="200">Lista periodi</response>
    /// <response code="400">CompanyId non valido</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccountingPeriod>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPeriodsByCompany([FromQuery] Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new { error = "Utente non trovato" });
            }

            // Validazione multi-tenancy
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di accedere ai periodi di Company {RequestedCompanyId}",
                    userId, user.CompanyId, companyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            var periods = await _periodService.GetPeriodsByCompanyAsync(companyId);
            return Ok(periods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero periodi per company {CompanyId}", companyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero dei periodi" });
        }
    }

    /// <summary>
    /// Ottiene un periodo per ID
    /// </summary>
    /// <param name="id">ID del periodo</param>
    /// <returns>Dati del periodo</returns>
    /// <response code="200">Periodo trovato</response>
    /// <response code="404">Periodo non trovato</response>
    /// <response code="403">Accesso negato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountingPeriod), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPeriodById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Period ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");
            var companyFilter = isAdmin ? (Guid?)null : user.CompanyId;

            var period = await _periodService.GetPeriodByIdAsync(id, companyFilter);

            if (period == null)
            {
                _logger.LogWarning("Periodo {PeriodId} non trovato o accesso negato per utente {UserId}", id, userId);
                return NotFound(new { error = $"Periodo {id} non trovato" });
            }

            // Doppio controllo multi-tenancy
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != period.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di accedere al periodo {PeriodId} (Company: {PeriodCompanyId})",
                    userId, user.CompanyId, id, period.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questo periodo" });
            }

            return Ok(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero periodo {PeriodId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero del periodo" });
        }
    }

    /// <summary>
    /// Chiude un periodo contabile
    /// </summary>
    /// <param name="id">ID del periodo da chiudere</param>
    /// <returns>Periodo aggiornato</returns>
    /// <response code="200">Periodo chiuso con successo</response>
    /// <response code="400">Operazione non valida</response>
    /// <response code="403">Accesso negato</response>
    /// <response code="404">Periodo non trovato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("{id}/close")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(AccountingPeriod), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClosePeriod(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Period ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");
            var companyFilter = isAdmin ? (Guid?)null : user.CompanyId;

            var periodToClose = await _periodService.GetPeriodByIdAsync(id, companyFilter);
            if (periodToClose == null)
            {
                _logger.LogWarning("Periodo {PeriodId} non trovato o accesso negato per utente {UserId}", id, userId);
                return NotFound(new { error = $"Periodo {id} non trovato" });
            }

            // Doppio controllo multi-tenancy
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != periodToClose.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di chiudere periodo {PeriodId} (Company: {PeriodCompanyId})",
                    userId, user.CompanyId, id, periodToClose.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questo periodo" });
            }

            _logger.LogInformation("Utente {UserId} sta chiudendo periodo {PeriodId}", userId, id);

            var result = await _periodService.ClosePeriodAsync(id, userId);

            _logger.LogInformation("Periodo {PeriodId} chiuso con successo", id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante chiusura periodo {PeriodId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante chiusura periodo {PeriodId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la chiusura del periodo" });
        }
    }

    /// <summary>
    /// Riapre un periodo contabile chiuso
    /// </summary>
    /// <param name="id">ID del periodo da riaprire</param>
    /// <returns>Periodo aggiornato</returns>
    /// <response code="200">Periodo riaperto con successo</response>
    /// <response code="400">Operazione non valida</response>
    /// <response code="403">Accesso negato</response>
    /// <response code="404">Periodo non trovato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("{id}/reopen")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(AccountingPeriod), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReopenPeriod(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Period ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");
            var companyFilter = isAdmin ? (Guid?)null : user.CompanyId;

            var periodToReopen = await _periodService.GetPeriodByIdAsync(id, companyFilter);
            if (periodToReopen == null)
            {
                _logger.LogWarning("Periodo {PeriodId} non trovato o accesso negato per utente {UserId}", id, userId);
                return NotFound(new { error = $"Periodo {id} non trovato" });
            }

            // Doppio controllo multi-tenancy
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != periodToReopen.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di riaprire periodo {PeriodId} (Company: {PeriodCompanyId})",
                    userId, user.CompanyId, id, periodToReopen.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questo periodo" });
            }

            _logger.LogInformation("Utente {UserId} sta riaprendo periodo {PeriodId}", userId, id);

            var result = await _periodService.ReopenPeriodAsync(id, userId);

            _logger.LogInformation("Periodo {PeriodId} riaperto con successo", id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante riapertura periodo {PeriodId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante riapertura periodo {PeriodId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la riapertura del periodo" });
        }
    }

    /// <summary>
    /// Elimina un periodo contabile
    /// </summary>
    /// <param name="id">ID del periodo da eliminare</param>
    /// <returns>Conferma eliminazione</returns>
    /// <response code="204">Periodo eliminato con successo</response>
    /// <response code="400">Operazione non valida</response>
    /// <response code="404">Periodo non trovato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePeriod(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Period ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            _logger.LogInformation("Utente {UserId} sta eliminando periodo {PeriodId}", userId, id);

            await _periodService.DeletePeriodAsync(id, userId);

            _logger.LogInformation("Periodo {PeriodId} eliminato con successo", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante eliminazione periodo {PeriodId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante eliminazione periodo {PeriodId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante l'eliminazione del periodo" });
        }
    }

    public record CreatePeriodRequest(
        [Required] Guid CompanyId,
        [Required] DateTime Start,
        [Required] DateTime End
    );
}