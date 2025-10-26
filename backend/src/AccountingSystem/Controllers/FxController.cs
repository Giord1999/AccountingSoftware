using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers;
[ApiController]
[Route("api/fx")]
public class FxController : ControllerBase
{
    private readonly IFXService _fx;
    public FxController(IFXService fx) { _fx = fx; }

    [HttpGet("convert")]
    public async Task<IActionResult> Convert(string from, string to, decimal amount) => Ok(await _fx.ConvertAsync(from, to, amount));

    [HttpPost("revaluate")]
    [Authorize(Roles = "Admin,Contabile")]
    public async Task<IActionResult> Revaluate([FromBody] dynamic payload)
    {
        try
        {
            var companyId = Guid.Parse(payload.companyId.ToString());
            var asOf = DateTime.Parse(payload.asOf.ToString());
            var userId = User?.Identity?.Name ?? "system";
            await _fx.RevaluateAccountsAsync(companyId, asOf, userId);
            return Ok(new { message = "Revaluation executed" });
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}
