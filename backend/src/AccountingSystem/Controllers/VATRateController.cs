using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

// Rinominato da VatRateController a VatRateController per rispettare le regole PascalCase
[ApiController]
[Route("api/VatRates")]
[Authorize]
public class VatRateController : ControllerBase
{
    private readonly IVatRateService _VatRateService;
    private readonly ILogger<VatRateController> _logger;

    public VatRateController(
        IVatRateService VatRateService,
        ILogger<VatRateController> logger)
    {
        _VatRateService = VatRateService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nuova aliquota IVA (solo Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VatRate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateVatRate([FromBody] CreateVatRateRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreateVatRate");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var VatRate = new VatRate
            {
                Name = request.Name,
                Rate = request.Rate
            };

            _logger.LogInformation("Admin {UserId} sta creando VAT rate {Name} ({Rate}%)",
                userId, VatRate.Name, VatRate.Rate);

            var result = await _VatRateService.CreateVatRateAsync(VatRate, userId);

            _logger.LogInformation("VAT rate {VatRateId} creato con successo", result.Id);
            return CreatedAtAction(nameof(GetVatRateById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore di validazione durante creazione VAT rate");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante creazione VAT rate");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante la creazione dell'aliquota IVA" });
        }
    }

    /// <summary>
    /// Ottiene tutte le aliquote IVA
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VatRate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllVatRates()
    {
        try
        {
            var VatRates = await _VatRateService.GetAllVatRatesAsync();
            return Ok(VatRates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero VAT rates");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero delle aliquote IVA" });
        }
    }

    /// <summary>
    /// Ottiene un'aliquota IVA per ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VatRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVatRateById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "VAT rate ID non valido" });
        }

        try
        {
            var VatRate = await _VatRateService.GetVatRateByIdAsync(id);

            if (VatRate == null)
            {
                _logger.LogWarning("VAT rate {VatRateId} non trovato", id);
                return NotFound(new { error = $"VAT rate {id} non trovato" });
            }

            return Ok(VatRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero VAT rate {VatRateId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero dell'aliquota IVA" });
        }
    }

    /// <summary>
    /// Aggiorna un'aliquota IVA (solo Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VatRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateVatRate(Guid id, [FromBody] UpdateVatRateRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "VAT rate ID non valido" });
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per UpdateVatRate");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var VatRate = new VatRate
            {
                Name = request.Name,
                Rate = request.Rate
            };

            _logger.LogInformation("Admin {UserId} sta aggiornando VAT rate {VatRateId}", userId, id);

            var result = await _VatRateService.UpdateVatRateAsync(id, VatRate, userId);

            _logger.LogInformation("VAT rate {VatRateId} aggiornato con successo", id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante aggiornamento VAT rate {VatRateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante aggiornamento VAT rate {VatRateId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante l'aggiornamento dell'aliquota IVA" });
        }
    }

    /// <summary>
    /// Elimina un'aliquota IVA (solo Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteVatRate(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "VAT rate ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            _logger.LogInformation("Admin {UserId} sta eliminando VAT rate {VatRateId}", userId, id);

            await _VatRateService.DeleteVatRateAsync(id, userId);

            _logger.LogInformation("VAT rate {VatRateId} eliminato con successo", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante eliminazione VAT rate {VatRateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante eliminazione VAT rate {VatRateId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante l'eliminazione dell'aliquota IVA" });
        }
    }

    public record CreateVatRateRequest(
        [Required][StringLength(100)] string Name,
        [Required][Range(0, 100)] decimal Rate
    );

    // Rinomina il record da UpdateVatRateRequest a UpdateVatRateRequest per rispettare le regole PascalCase
    public record UpdateVatRateRequest(
        [Required][StringLength(100)] string Name,
        [Required][Range(0, 100)] decimal Rate
    );
}