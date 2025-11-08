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
public class OpportunityController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OpportunityController> _logger;

    public OpportunityController(
        IOpportunityService opportunityService,
        UserManager<ApplicationUser> userManager,
        ILogger<OpportunityController> logger)
    {
        _opportunityService = opportunityService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOpportunity([FromBody] Opportunity opportunity)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        opportunity.CompanyId = user?.CompanyId ?? Guid.Empty;

        var created = await _opportunityService.CreateOpportunityAsync(opportunity, userId);
        _logger.LogInformation("Opportunity created with ID {OpportunityId} by user {UserId}", created.Id, userId);
        return CreatedAtAction(nameof(GetOpportunity), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpportunity(Guid id)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, user?.CompanyId);

        if (opportunity == null)
        {
            _logger.LogWarning("Opportunity with ID {OpportunityId} not found for user {UserId}", id, user?.Id ?? "unknown");
            return NotFound();
        }
        return Ok(opportunity);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Opportunity>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpportunities([FromQuery] OpportunityStage? stage = null)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (user == null || !user.CompanyId.HasValue)
        {
            _logger.LogWarning("User not associated with company or user not found");
            return BadRequest("User not associated with company");
        }

        var opportunities = await _opportunityService.GetOpportunitiesByCompanyAsync(user.CompanyId.Value, stage);
        _logger.LogInformation("Retrieved {OpportunityCount} opportunities for company {CompanyId}", opportunities.Count(), user.CompanyId.Value);
        return Ok(opportunities);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOpportunity(Guid id, [FromBody] Opportunity opportunity)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var updated = await _opportunityService.UpdateOpportunityAsync(id, opportunity, userId);
        _logger.LogInformation("Opportunity with ID {OpportunityId} updated by user {UserId}", id, userId);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOpportunity(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _opportunityService.DeleteOpportunityAsync(id, userId);
        _logger.LogInformation("Opportunity with ID {OpportunityId} deleted by user {UserId}", id, userId);
        return NoContent();
    }

    [HttpPost("{id:guid}/updatestage")]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStage(Guid id, [FromBody] OpportunityStage stage)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var opportunity = await _opportunityService.UpdateStageAsync(id, stage, userId);
        _logger.LogInformation("Stage updated for opportunity with ID {OpportunityId} by user {UserId}", id, userId);
        return Ok(opportunity);
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CloseOpportunity(Guid id, [FromBody] bool won)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _opportunityService.CloseOpportunityAsync(id, won, userId);
        _logger.LogInformation("Opportunity with ID {OpportunityId} closed (won: {Won}) by user {UserId}", id, won, userId);
        return NoContent();
    }
}