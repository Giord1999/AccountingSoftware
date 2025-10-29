using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/controller")]
[Authorize]
public class VatController : ControllerBase
{
    private readonly IVATService _vatService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<VatController> _logger;

    public VatController(
        IVATService vatService,
        UserManager<ApplicationUser> userManager,
        ILogger<VatController> logger)
    {
        _vatService = vatService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Applica il VAT a un journal entry
    /// </summary>
    /// <param name="request">Dati del journal entry e company ID</param>
    /// <returns>Journal entry con righe VAT applicate</returns>
    /// <response code="200">VAT applicato con successo</response>
    /// <response code="400">Dati non validi</response>
    /// <response code="401">Non autorizzato</response>
    /// <response code="403">Accesso negato alla company</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("apply")]
    [Authorize(Roles = "Admin,Contabile")]
    [ProducesResponseType(typeof(JournalEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Apply([FromBody] ApplyVatRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per ApplyVat");
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            _logger.LogWarning("Tentativo di applicare VAT con payload null");
            return BadRequest(new { error = "Request non può essere null" });
        }

        if (request.CompanyId == Guid.Empty)
        {
            return BadRequest(new { error = "CompanyId non valido" });
        }

        if (request.JournalEntry == null)
        {
            return BadRequest(new { error = "JournalEntry non può essere null" });
        }

        try
        {
            // Controllo multi-tenancy
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato", userId);
                return Unauthorized(new { error = "Utente non trovato" });
            }

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && user.CompanyId.HasValue && user.CompanyId != request.CompanyId)
            {
                _logger.LogWarning(
                    "Utente {UserId} (Company: {UserCompanyId}) ha tentato di applicare VAT per Company {RequestedCompanyId}",
                    userId, user.CompanyId, request.CompanyId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Non hai accesso a questa azienda" });
            }

            _logger.LogInformation(
                "Utente {UserId} sta applicando VAT per journal {JournalId} in company {CompanyId}",
                userId, request.JournalEntry.Id, request.CompanyId);

            await _vatService.ApplyVatToJournalAsync(request.JournalEntry, request.CompanyId, userId);

            _logger.LogInformation("VAT applicato con successo per journal {JournalId}", request.JournalEntry.Id);
            return Ok(request.JournalEntry);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante applicazione VAT");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante applicazione VAT");

            var errorResponse = new
            {
                error = "Errore interno del server durante l'applicazione del VAT",
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.Message
                    : null
            };

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Request per l'applicazione del VAT
    /// </summary>
    public record ApplyVatRequest(JournalEntry JournalEntry, Guid CompanyId);
}