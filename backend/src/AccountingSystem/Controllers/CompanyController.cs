using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(
        ICompanyService companyService,
        ILogger<CompanyController> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nuova azienda (solo Admin)
    /// </summary>
    /// <param name="request">Dati dell'azienda da creare</param>
    /// <returns>Azienda creata</returns>
    /// <response code="201">Azienda creata con successo</response>
    /// <response code="400">Dati non validi</response>
    /// <response code="401">Non autorizzato</response>
    /// <response code="403">Solo Admin può creare aziende</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Company), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreateCompany");
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            _logger.LogWarning("Tentativo di creare company con payload null");
            return BadRequest(new { error = "Request non può essere null" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var company = new Company
            {
                Name = request.Name,
                BaseCurrency = request.BaseCurrency ?? "EUR"
            };

            _logger.LogInformation("Admin {UserId} sta creando company {CompanyName}", userId, company.Name);

            var result = await _companyService.CreateCompanyAsync(company, userId);

            _logger.LogInformation("Company {CompanyId} ({CompanyName}) creata con successo", result.Id, result.Name);
            return CreatedAtAction(nameof(GetCompanyById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante creazione company");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante creazione company");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la creazione dell'azienda" });
        }
    }

    /// <summary>
    /// Ottiene tutte le aziende (Admin vede tutte, altri solo la propria)
    /// </summary>
    /// <returns>Lista delle aziende</returns>
    /// <response code="200">Lista aziende</response>
    /// <response code="401">Non autorizzato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Company>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllCompanies()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            _logger.LogInformation("Utente {UserId} sta recuperando lista companies", userId);

            var companies = await _companyService.GetAllCompaniesAsync();

            return Ok(companies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero lista companies");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero delle aziende" });
        }
    }

    /// <summary>
    /// Ottiene un'azienda per ID
    /// </summary>
    /// <param name="id">ID dell'azienda</param>
    /// <returns>Dati dell'azienda</returns>
    /// <response code="200">Azienda trovata</response>
    /// <response code="404">Azienda non trovata</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Company), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCompanyById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Company ID non valido" });
        }

        try
        {
            var company = await _companyService.GetCompanyByIdAsync(id);

            if (company == null)
            {
                _logger.LogWarning("Company {CompanyId} non trovata", id);
                return NotFound(new { error = $"Company {id} non trovata" });
            }

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero company {CompanyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero dell'azienda" });
        }
    }

    /// <summary>
    /// Aggiorna un'azienda (solo Admin)
    /// </summary>
    /// <param name="id">ID dell'azienda da aggiornare</param>
    /// <param name="request">Nuovi dati dell'azienda</param>
    /// <returns>Azienda aggiornata</returns>
    /// <response code="200">Azienda aggiornata con successo</response>
    /// <response code="400">Dati non validi</response>
    /// <response code="404">Azienda non trovata</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Company), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Company ID non valido" });
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per UpdateCompany");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var company = new Company
            {
                Name = request.Name,
                BaseCurrency = request.BaseCurrency ?? "EUR"
            };

            _logger.LogInformation("Admin {UserId} sta aggiornando company {CompanyId}", userId, id);

            var result = await _companyService.UpdateCompanyAsync(id, company, userId);

            _logger.LogInformation("Company {CompanyId} aggiornata con successo", id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante aggiornamento company {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante aggiornamento company {CompanyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante l'aggiornamento dell'azienda" });
        }
    }

    /// <summary>
    /// Elimina un'azienda (solo Admin)
    /// </summary>
    /// <param name="id">ID dell'azienda da eliminare</param>
    /// <returns>Conferma eliminazione</returns>
    /// <response code="204">Azienda eliminata con successo</response>
    /// <response code="400">Operazione non valida</response>
    /// <response code="404">Azienda non trovata</response>
    /// <response code="500">Errore interno del server</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "Company ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            _logger.LogInformation("Admin {UserId} sta eliminando company {CompanyId}", userId, id);

            await _companyService.DeleteCompanyAsync(id, userId);

            _logger.LogInformation("Company {CompanyId} eliminata con successo", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante eliminazione company {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante eliminazione company {CompanyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante l'eliminazione dell'azienda" });
        }
    }

    /// <summary>
    /// DTO per creazione company
    /// </summary>
    public record CreateCompanyRequest(
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Il nome deve essere tra 2 e 200 caratteri")]
        string Name,

        [StringLength(3, MinimumLength = 3, ErrorMessage = "La valuta deve essere di 3 caratteri (es. EUR, USD)")]
        string? BaseCurrency
    );

    /// <summary>
    /// DTO per aggiornamento company
    /// </summary>
    public record UpdateCompanyRequest(
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Il nome deve essere tra 2 e 200 caratteri")]
        string Name,

        [StringLength(3, MinimumLength = 3, ErrorMessage = "La valuta deve essere di 3 caratteri (es. EUR, USD)")]
        string? BaseCurrency
    );
}