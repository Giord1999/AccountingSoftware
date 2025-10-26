using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountingController : ControllerBase
{
    private readonly IAccountingService _accounting;
    private readonly ILogger<AccountingController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountingController(
        IAccountingService accounting,
        ILogger<AccountingController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _accounting = accounting;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Crea un nuovo journal entry
    /// </summary>
    /// <param name="entry">Dati del journal entry da creare</param>
    /// <returns>Journal entry creato</returns>
    /// <response code="201">Journal entry creato con successo</response>
    /// <response code="400">Dati non validi</response>
    /// <response code="401">Non autorizzato</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("journal")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(JournalEntry), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateJournal([FromBody] JournalEntry entry)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreateJournal");
            return BadRequest(ModelState);
        }

        if (entry == null)
        {
            _logger.LogWarning("Tentativo di creare journal entry con payload null");
            return BadRequest(new { error = "Journal entry non può essere null" });
        }

        // Validazione business logic
        if (entry.Lines == null || !entry.Lines.Any())
        {
            _logger.LogWarning("Tentativo di creare journal entry senza righe");
            return BadRequest(new { error = "Il journal entry deve contenere almeno una riga" });
        }

        if (entry.CompanyId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId non valido" });
        }

        if (entry.PeriodId == Guid.Empty)
        {
            return BadRequest(new { error = "PeriodId non valido" });
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

            // Se l'utente non è Admin, verifica che appartenga alla company richiesta
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != entry.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di creare journal per Company {RequestedCompanyId}",
                    userId, user.CompanyId, entry.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            _logger.LogInformation("Utente {UserId} sta creando journal entry per company {CompanyId}", userId, entry.CompanyId);

            var result = await _accounting.CreateJournalAsync(entry, userId);

            _logger.LogInformation("Journal entry {JournalId} creato con successo", result.Id);
            return CreatedAtAction(nameof(CreateJournal), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante creazione journal");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante creazione journal");

            var errorResponse = new
            {
                error = "Errore interno del server durante la creazione del journal",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Posta un journal entry (cambia status in Posted)
    /// </summary>
    /// <param name="journalId">ID del journal entry da postare</param>
    /// <returns>Journal entry aggiornato</returns>
    /// <response code="200">Journal entry postato con successo</response>
    /// <response code="400">Operazione non valida</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="404">Journal entry non trovato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("journal/{journalId}/post")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    [ProducesResponseType(typeof(JournalEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostJournal(Guid journalId)
    {
        if (journalId == Guid.Empty)
        {
            _logger.LogWarning("Tentativo di postare journal con ID vuoto");
            return BadRequest(new { error = "Journal ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy: verifica che l'utente abbia accesso alla company del journal
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");

            // 🔒 SECURITY FIX: Passa companyId al servizio per filtrare a livello database
            // Admin può vedere tutti i journal, utenti normali solo quelli della propria company
            var companyFilter = isAdmin ? (Guid?)null : user.CompanyId;

            var journalToPost = await _accounting.GetJournalByIdAsync(journalId, companyFilter);
            if (journalToPost == null)
            {
                _logger.LogWarning("Journal {JournalId} non trovato o accesso negato per utente {UserId}", journalId, userId);
                return NotFound(new { error = $"Journal entry {journalId} non trovato" });
            }

            // Doppio controllo: verifica esplicita della company (defense in depth)
            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != journalToPost.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di postare journal {JournalId} (Company: {JournalCompanyId})",
                    userId, user.CompanyId, journalId, journalToPost.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            _logger.LogInformation("Utente {UserId} sta postando journal {JournalId}", userId, journalId);

            var result = await _accounting.PostJournalAsync(journalId, userId);

            _logger.LogInformation("Journal {JournalId} postato con successo", journalId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante posting journal {JournalId}", journalId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante posting journal {JournalId}", journalId);

            var errorResponse = new
            {
                error = "Errore interno del server durante il posting del journal",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Ottiene il trial balance per azienda e periodo
    /// </summary>
    /// <param name="companyId">ID dell'azienda</param>
    /// <param name="periodId">ID del periodo contabile</param>
    /// <returns>Trial balance dettagliato</returns>
    /// <response code="200">Trial balance generato con successo</response>
    /// <response code="400">Parametri non validi</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("trial-balance")]
    [Authorize(Policy = "RequireAuditorOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrialBalance([FromQuery] Guid companyId, [FromQuery] Guid periodId)
    {
        if (companyId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId non valido" });
        }

        if (periodId == Guid.Empty)
        {
            return BadRequest(new { error = "PeriodId non valido" });
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

            // Admin e Auditor possono vedere tutte le company, gli altri solo la propria
            if (!isAdmin && !isAuditor && user.CompanyId.HasValue && user.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di accedere al trial balance di Company {RequestedCompanyId}",
                    userId, user.CompanyId, companyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            _logger.LogInformation("Generazione trial balance per company {CompanyId}, period {PeriodId}", companyId, periodId);

            var result = await _accounting.GetTrialBalanceAsync(companyId, periodId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante generazione trial balance");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante generazione trial balance");

            var errorResponse = new
            {
                error = "Errore interno del server durante la generazione del trial balance",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
}