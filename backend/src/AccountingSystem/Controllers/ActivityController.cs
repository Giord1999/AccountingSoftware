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
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        IActivityService activityService,
        UserManager<ApplicationUser> userManager,
        ILogger<ActivityController> logger)
    {
        _activityService = activityService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Activity), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateActivity([FromBody] Activity activity)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        activity.CompanyId = user?.CompanyId ?? Guid.Empty;

        var created = await _activityService.CreateActivityAsync(activity, userId);
        return CreatedAtAction(nameof(GetActivity), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Activity), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivity(Guid id)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        var activity = await _activityService.GetActivityByIdAsync(id, user?.CompanyId);

        if (activity == null) return NotFound();
        return Ok(activity);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Activity>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivities([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (user == null || !user.CompanyId.HasValue) return BadRequest("User not associated with company");

        var activities = await _activityService.GetActivitiesByCompanyAsync(user.CompanyId.Value, from, to);
        return Ok(activities);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Activity), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateActivity(Guid id, [FromBody] Activity activity)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var updated = await _activityService.UpdateActivityAsync(id, activity, userId);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteActivity(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _activityService.DeleteActivityAsync(id, userId);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(Activity), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteActivity(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var activity = await _activityService.CompleteActivityAsync(id, userId);
        return Ok(activity);
    }

    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<Activity>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivitiesByCustomer(Guid customerId)
    {
        var activities = await _activityService.GetActivitiesByCustomerAsync(customerId);
        return Ok(activities);
    }

    [HttpGet("lead/{leadId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<Activity>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivitiesByLead(Guid leadId)
    {
        var activities = await _activityService.GetActivitiesByLeadAsync(leadId);
        return Ok(activities);
    }
}