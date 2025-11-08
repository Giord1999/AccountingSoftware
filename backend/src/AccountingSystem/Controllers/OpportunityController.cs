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
[Authorize(Policy = "RequireContabileOrAdmin")]
public class LeadController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ILeadService leadService,
        UserManager<ApplicationUser> userManager,
        ILogger<LeadController> logger)
    {
        _leadService = leadService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLead([FromBody] Lead lead)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        lead.CompanyId = user?.CompanyId ?? Guid.Empty;

        var created = await _leadService.CreateLeadAsync(lead, userId);
        return CreatedAtAction(nameof(GetLead), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLead(Guid id)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        var lead = await _leadService.GetLeadByIdAsync(id, user?.CompanyId);

        if (lead == null) return NotFound();
        return Ok(lead);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Lead>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeads([FromQuery] LeadStatus? status = null)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (!user?.CompanyId.HasValue ?? true) return BadRequest("User not associated with company");

        var leads = await _leadService.GetLeadsByCompanyAsync(user.CompanyId.Value, status);
        return Ok(leads);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLead(Guid id, [FromBody] Lead lead)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var updated = await _leadService.UpdateLeadAsync(id, lead, userId);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _leadService.DeleteLeadAsync(id, userId);
        return NoContent();
    }

    [HttpPost("{id:guid}/qualify")]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status200OK)]
    public async Task<IActionResult> QualifyLead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var lead = await _leadService.QualifyLeadAsync(id, userId);
        return Ok(lead);
    }

    [HttpPost("{id:guid}/convert")]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConvertLeadToCustomer(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var lead = await _leadService.ConvertLeadToCustomerAsync(id, userId);
        return Ok(lead);
    }
}