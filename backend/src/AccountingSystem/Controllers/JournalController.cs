using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalController : ControllerBase
{
    private readonly IAccountingService _acct;
    public JournalController(IAccountingService acct) { _acct = acct; }

    [HttpPost]
    [Authorize(Roles = "Admin,Contabile")]
    public async Task<IActionResult> Create(JournalEntry entry)
    {
        var userId = User?.Identity?.Name ?? "system";
        try
        {
            var r = await _acct.CreateJournalAsync(entry, userId);
            return Ok(r);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/post")]
    [Authorize(Roles = "Admin,Contabile")]
    public async Task<IActionResult> Post(Guid id)
    {
        var userId = User?.Identity?.Name ?? "system";
        try
        {
            var r = await _acct.PostJournalAsync(id, userId);
            return Ok(r);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
