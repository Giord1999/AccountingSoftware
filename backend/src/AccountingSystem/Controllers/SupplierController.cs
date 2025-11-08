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
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SupplierController> _logger;

    public SupplierController(
        ISupplierService supplierService,
        UserManager<ApplicationUser> userManager,
        ILogger<SupplierController> logger)
    {
        _supplierService = supplierService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Supplier), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        supplier.CompanyId = user?.CompanyId ?? Guid.Empty;

        var created = await _supplierService.CreateSupplierAsync(supplier, userId);
        return CreatedAtAction(nameof(GetSupplier), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Supplier), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplier(Guid id)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        var supplier = await _supplierService.GetSupplierByIdAsync(id, user?.CompanyId);

        if (supplier == null) return NotFound();
        return Ok(supplier);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Supplier>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? search = null)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (!user?.CompanyId.HasValue ?? true) return BadRequest("User not associated with company");

        var suppliers = await _supplierService.GetSuppliersByCompanyAsync(user.CompanyId.Value, search);
        return Ok(suppliers);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Supplier), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] Supplier supplier)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var updated = await _supplierService.UpdateSupplierAsync(id, supplier, userId);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _supplierService.DeleteSupplierAsync(id, userId);
        return NoContent();
    }

    [HttpPost("migrate-purchases")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MigratePurchaseData()
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (!user?.CompanyId.HasValue ?? true) return BadRequest("User not associated with company");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _supplierService.MigrateExistingPurchaseDataAsync(user.CompanyId.Value, userId);
        return Ok("Migration completed");
    }
}