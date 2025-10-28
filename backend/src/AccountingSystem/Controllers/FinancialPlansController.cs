using AccountingSystem.Models.FinancialPlanning;
using AccountingSystem.Services.FinancialPlanning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinancialPlansController(IFinancialPlanService service) : ControllerBase
    {
        private readonly IFinancialPlanService _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid companyId, CancellationToken ct = default)
        {
            // Risolvi l'ambiguità castando esplicitamente il tipo di ct
            var list = await _service.ListAsync(companyId, ct: ct);
            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, [FromQuery] Guid companyId, CancellationToken ct = default)
        {
            // Risolvi l'ambiguità specificando il nome del parametro 'ct'
            var plan = await _service.GetAsync(companyId, id, ct: ct);
            return plan is null ? NotFound() : Ok(plan);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Contabile")]
        public async Task<IActionResult> Create([FromQuery] Guid companyId, [FromBody] CreateFinancialPlanInput input, CancellationToken ct = default)
        {
            var userId = User?.Identity?.Name ?? "system";
            var created = await _service.CreateAsync(companyId: companyId, input: input, userId: userId, ct: ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id, companyId }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Contabile")]
        public async Task<IActionResult> Update(Guid id, [FromQuery] Guid companyId, [FromBody] UpdateFinancialPlanInput input, CancellationToken ct = default)
        {
            if (id != input.Id) return BadRequest("Id mismatch");
            var userId = User?.Identity?.Name ?? "system";
            var updated = await _service.UpdateAsync(companyId, input, userId, ct: ct);
            return Ok(updated);
        }

        [HttpPost("{id:guid}/submit")]
        [Authorize(Roles = "Admin,Contabile")]
        public async Task<IActionResult> Submit(Guid id, [FromQuery] Guid companyId, CancellationToken ct = default)
        {
            var userId = User?.Identity?.Name ?? "system";
            await _service.SubmitAsync(companyId, id, userId, ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(Guid id, [FromQuery] Guid companyId, CancellationToken ct = default)
        {
            var approver = User?.Identity?.Name ?? "system";
            await _service.ApproveAsync(companyId, id, approver, ct);
            return NoContent();
        }

        // 🔮 Forecast
        [HttpPost("{id:guid}/generate-forecasts")]
        [Authorize(Roles = "Admin,Contabile")]
        public async Task<IActionResult> GenerateForecasts(
            Guid id,
            [FromQuery] Guid companyId,
            [FromQuery] int monthsAhead = 6,
            [FromQuery] double growthFactor = 1.0,
            [FromQuery] int historicalMonths = 12,
            CancellationToken ct = default)
        {
            var userId = User?.Identity?.Name ?? "system";
            var forecasts = await _service.GenerateForecastsAsync(
                companyId: companyId,
                financialPlanId: id,
                monthsAhead: monthsAhead,
                growthFactor: growthFactor,
                historicalMonths: historicalMonths,
                generatedBy: userId,
                ct: ct
            );
            return Ok(forecasts);
        }

        [HttpGet("{id:guid}/forecasts")]
        public async Task<IActionResult> GetForecasts(Guid id, [FromQuery] Guid companyId, CancellationToken ct = default)
        {
            var forecasts = await _service.GetForecastsAsync(companyId, id, ct);
            return Ok(forecasts);
        }
    }
}