using AccountingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IAccountingService _acct;
    public ReportsController(IAccountingService acct) { _acct = acct; }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance(Guid companyId, Guid periodId)
    {
        var r = await _acct.GetTrialBalanceAsync(companyId, periodId);
        return Ok(r);
    }
}
