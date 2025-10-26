using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/batch")]
[Authorize(Policy = "RequireContabileOrAdmin")]
public class BatchController : ControllerBase
{
    private readonly IBatchService _batch;
    private readonly IAccountingService _accounting;
    private readonly ILogger<BatchController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public BatchController(
        IBatchService batch,
        IAccountingService accounting,
        ILogger<BatchController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _batch = batch;
        _accounting = accounting;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Posta un batch di journal entries in modo atomico
    /// </summary>
    /// <param name="journalIds">Lista di ID dei journal entries da postare</param>
    /// <returns>Risultato del batch posting con conteggi e eventuali errori</returns>
    /// <response code="200">Batch processato con successo</response>
    /// <response code="400">Richiesta non valida o lista vuota</response>
    /// <response code="401">Non autorizzato</response>
    /// <response code="403">Accesso negato ad una o più company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("post")]
    [ProducesResponseType(typeof(PostBatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] IEnumerable<Guid> journalIds)
    {
        if (journalIds == null || !journalIds.Any())
        {
            _logger.LogWarning("Tentativo di batch post con lista vuota o null");
            return BadRequest(new { error = "La lista di journal IDs non può essere vuota" });
        }

        // Validazione: rimuovi duplicati e GUID vuoti
        var validIds = journalIds.Where(id => id != Guid.Empty).Distinct().ToList();

        if (!validIds.Any())
        {
            _logger.LogWarning("Nessun ID valido nella lista fornita");
            return BadRequest(new { error = "Nessun journal ID valido fornito" });
        }

        if (validIds.Count > 1000)
        {
            _logger.LogWarning("Tentativo di batch post con {Count} elementi (limite: 1000)", validIds.Count);
            return BadRequest(new { error = "Il batch non può contenere più di 1000 journal entries" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            // Validazione multi-tenancy: verifica che l'utente abbia accesso a tutti i journal
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");

            // Se non è Admin, verifica l'accesso a tutti i journal nel batch
            if (!isAdmin && user.CompanyId.HasValue)
            {
                var journalsToValidate = new List<JournalEntry>();
                var invalidJournals = new List<string>();

                foreach (var id in validIds)
                {
                    var journal = await _accounting.GetJournalByIdAsync(id);
                    if (journal != null)
                    {
                        if (journal.CompanyId != user.CompanyId)
                        {
                            invalidJournals.Add($"{id} (Company: {journal.CompanyId})");
                        }
                    }
                }

                if (invalidJournals.Any())
                {
                    _logger.LogWarning(
                        "Utente {UserId} (Company: {UserCompanyId}) ha tentato di postare journal di altre company: {InvalidJournals}",
                        userId, user.CompanyId, string.Join(", ", invalidJournals));

                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "Non hai accesso ad uno o più journal nel batch",
                        unauthorizedJournals = invalidJournals
                    });
                }
            }

            _logger.LogInformation("Utente {UserId} sta postando batch di {Count} journal entries", userId, validIds.Count);

            var result = await _batch.PostBatchAsync(validIds, userId);

            _logger.LogInformation(
                "Batch completato: {Posted} postati, {Failed} falliti",
                result.PostedCount,
                result.FailedCount);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante batch post");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante batch post");

            var errorResponse = new
            {
                error = "Errore interno del server durante il batch posting",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }
}