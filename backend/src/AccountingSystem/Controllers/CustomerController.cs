using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireContabileOrAdmin")]
public class CustomerController(
    ICustomerService customerService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly ICustomerService _customerService = customerService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPost]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var user = await _userManager.FindByIdAsync(userId);
        customer.CompanyId = user?.CompanyId ?? Guid.Empty;

        var created = await _customerService.CreateCustomerAsync(customer, userId);
        return CreatedAtAction(nameof(GetCustomer), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        var customer = await _customerService.GetCustomerByIdAsync(id, user?.CompanyId);

        if (customer == null) return NotFound();
        return Ok(customer);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search = null)
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (user == null || user.CompanyId == null) return BadRequest("User not associated with company");

        var customers = await _customerService.GetCustomersByCompanyAsync(user.CompanyId.Value, search);
        return Ok(customers);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] Customer customer)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var updated = await _customerService.UpdateCustomerAsync(id, customer, userId);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _customerService.DeleteCustomerAsync(id, userId);
        return NoContent();
    }

    [HttpPost("migrate-sales")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MigrateSalesData()
    {
        var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system");
        if (user == null || user.CompanyId == null) return BadRequest("User not associated with company");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        await _customerService.MigrateExistingSalesDataAsync(user.CompanyId.Value, userId);
        return Ok("Migration completed");
    }
}