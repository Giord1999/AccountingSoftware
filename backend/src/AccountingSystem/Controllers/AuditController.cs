using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAuditorOrAdmin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService audit, ILogger<AuditController> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Crea un log di audit manuale
    /// </summary>
    /// <param name="request">Dati del log da creare</param>
    /// <returns>Conferma creazione</returns>
    /// <response code="201">Log creato con successo</response>
    /// <response code="400">Dati non validi</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("log")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLog([FromBody] CreateLogRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request non può essere null" });
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return BadRequest(new { error = "Action è obbligatorio" });
        }

        if (string.IsNullOrWhiteSpace(request.Details))
        {
            return BadRequest(new { error = "Details è obbligatorio" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            _logger.LogInformation("Utente {UserId} sta creando log audit manuale: {Action}", userId, request.Action);

            await _audit.LogAsync(userId, request.Action, request.Details);

            return StatusCode(201, new { message = "Log creato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione log audit");

            var errorResponse = new
            {
                error = "Errore interno del server durante la creazione del log",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Ottiene i log di audit con filtri opzionali e paginazione
    /// </summary>
    /// <param name="userId">Filtra per ID utente</param>
    /// <param name="action">Filtra per azione (ricerca parziale)</param>
    /// <param name="from">Data inizio periodo</param>
    /// <param name="to">Data fine periodo</param>
    /// <param name="page">Numero pagina (default: 1)</param>
    /// <param name="pageSize">Dimensione pagina (default: 50, max: 200)</param>
    /// <returns>Lista paginata di log audit</returns>
    /// <response code="200">Log recuperati con successo</response>
    /// <response code="400">Parametri non validi</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Validazione paginazione
        if (page < 1)
        {
            return BadRequest(new { error = "Il numero di pagina deve essere >= 1" });
        }

        if (pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { error = "La dimensione della pagina deve essere tra 1 e 200" });
        }

        // Validazione date
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new { error = "La data 'from' deve essere precedente o uguale a 'to'" });
        }

        try
        {
            _logger.LogInformation(
                "Recupero log audit - Page: {Page}, PageSize: {PageSize}, UserId: {UserId}, Action: {Action}",
                page, pageSize, userId, action);

            var result = await _audit.GetLogsAsync(userId, action, from, to, page, pageSize);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero log audit");

            var errorResponse = new
            {
                error = "Errore interno del server durante il recupero dei log",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// DTO per la creazione di un log audit
    /// </summary>
    public record CreateLogRequest(
        string Action,
        string Details
    );
}