using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/vatrates")]
[Authorize]
public class VATRateController : ControllerBase
{
    private readonly IVATRateService _vatRateService;
    private readonly ILogger<VATRateController> _logger;

    public VATRateController(
        IVATRateService vatRateService,
        ILogger<VATRateController> logger)
    {
        _vatRateService = vatRateService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nuova aliquota IVA (solo Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VATRate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateVATRate([FromBody] CreateVATRateRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per CreateVATRate");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var vatRate = new VATRate
            {
                Name = request.Name,
                Rate = request.Rate
            };

            _logger.LogInformation("Admin {UserId} sta creando VAT rate {Name} ({Rate}%)",
                userId, vatRate.Name, vatRate.Rate);

            var result = await _vatRateService.CreateVATRateAsync(vatRate, userId);

            _logger.LogInformation("VAT rate {VATRateId} creato con successo", result.Id);
            return CreatedAtAction(nameof(GetVATRateById), new { id = result.Id }, result);
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
    [ProducesResponseType(typeof(IEnumerable<VATRate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllVATRates()
    {
        try
        {
            var vatRates = await _vatRateService.GetAllVATRatesAsync();
            return Ok(vatRates);
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
    [ProducesResponseType(typeof(VATRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVATRateById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "VAT rate ID non valido" });
        }

        try
        {
            var vatRate = await _vatRateService.GetVATRateByIdAsync(id);

            if (vatRate == null)
            {
                _logger.LogWarning("VAT rate {VATRateId} non trovato", id);
                return NotFound(new { error = $"VAT rate {id} non trovato" });
            }

            return Ok(vatRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante recupero VAT rate {VATRateId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante il recupero dell'aliquota IVA" });
        }
    }

    /// <summary>
    /// Aggiorna un'aliquota IVA (solo Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VATRate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateVATRate(Guid id, [FromBody] UpdateVATRateRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "VAT rate ID non valido" });
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed per UpdateVATRate");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var vatRate = new VATRate
            {
                Name = request.Name,
                Rate = request.Rate
            };

            _logger.LogInformation("Admin {UserId} sta aggiornando VAT rate {VATRateId}", userId, id);

            var result = await _vatRateService.UpdateVATRateAsync(id, vatRate, userId);

            _logger.LogInformation("VAT rate {VATRateId} aggiornato con successo", id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante aggiornamento VAT rate {VATRateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante aggiornamento VAT rate {VATRateId}", id);
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
    public async Task<IActionResult> DeleteVATRate(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { error = "VAT rate ID non valido" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            _logger.LogInformation("Admin {UserId} sta eliminando VAT rate {VATRateId}", userId, id);

            await _vatRateService.DeleteVATRateAsync(id, userId);

            _logger.LogInformation("VAT rate {VATRateId} eliminato con successo", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Errore durante eliminazione VAT rate {VATRateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore critico durante eliminazione VAT rate {VATRateId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Errore interno del server durante l'eliminazione dell'aliquota IVA" });
        }
    }

    public record CreateVATRateRequest(
        [Required][StringLength(100)] string Name,
        [Required][Range(0, 100)] decimal Rate
    );

    public record UpdateVATRateRequest(
        [Required][StringLength(100)] string Name,
        [Required][Range(0, 100)] decimal Rate
    );
}