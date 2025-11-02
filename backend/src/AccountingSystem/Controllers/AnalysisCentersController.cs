using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisCentersController : ControllerBase
{
    private readonly IAnalysisCenterService _service;
    private readonly ILogger<AnalysisCentersController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AnalysisCentersController(
        IAnalysisCenterService service,
        ILogger<AnalysisCentersController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet("company/{companyId:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    public async Task<IActionResult> GetByCompany(Guid companyId, [FromQuery] AnalysisCenterType? type = null)
    {
        try
        {
            await ValidateCompanyAccessAsync(companyId);
            var centers = await _service.GetAnalysisCentersByCompanyAsync(companyId, type);
            return Ok(centers);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero centri per company {CompanyId}", companyId);
            return StatusCode(500, "Errore interno");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var user = await _userManager.FindByIdAsync(userId);
            var companyFilter = user?.CompanyId;

            var center = await _service.GetAnalysisCenterByIdAsync(id, companyFilter);

            if (center == null)
                return NotFound($"Centro di analisi {id} non trovato");

            return Ok(center);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore recupero centro {Id}", id);
            return StatusCode(500, "Errore interno");
        }
    }

    [HttpPost]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateAnalysisCenterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await ValidateCompanyAccessAsync(request.CompanyId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var center = new AnalysisCenter
            {
                CompanyId = request.CompanyId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Type = request.Type
            };

            var created = await _service.CreateAnalysisCenterAsync(center, userId);

            _logger.LogInformation("Centro di analisi {Code} creato", created.Code);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore creazione centro");
            return StatusCode(500, "Errore interno");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnalysisCenterRequest request)
    {
        if (id != request.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var center = new AnalysisCenter
            {
                Id = id,
                CompanyId = request.CompanyId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                IsActive = request.IsActive
            };

            var updated = await _service.UpdateAnalysisCenterAsync(id, center, userId);

            _logger.LogInformation("Centro di analisi {Id} aggiornato", id);

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore aggiornamento centro {Id}", id);
            return StatusCode(500, "Errore interno");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _service.DeleteAnalysisCenterAsync(id, userId);

            _logger.LogInformation("Centro di analisi {Id} eliminato", id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore eliminazione centro {Id}", id);
            return StatusCode(500, "Errore interno");
        }
    }

    private async Task ValidateCompanyAccessAsync(Guid companyId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && user?.CompanyId.HasValue == true && user.CompanyId != companyId)
        {
            _logger.LogWarning("Accesso negato a company {CompanyId} per user {UserId}", companyId, userId);
            throw new UnauthorizedAccessException();
        }
    }

    public record CreateAnalysisCenterRequest(
        [Required] Guid CompanyId,
        [Required][StringLength(100)] string Code,
        [Required][StringLength(200)] string Name,
        [StringLength(1000)] string? Description,
        [Required] AnalysisCenterType Type
    );

    public record UpdateAnalysisCenterRequest(
        [Required] Guid Id,
        [Required] Guid CompanyId,
        [Required][StringLength(100)] string Code,
        [Required][StringLength(200)] string Name,
        [StringLength(1000)] string? Description,
        [Required] AnalysisCenterType Type,
        bool IsActive = true
    );
}