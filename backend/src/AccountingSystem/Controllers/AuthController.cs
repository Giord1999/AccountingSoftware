using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly SignInManager<ApplicationUser> _signInMgr;
    private readonly ITokenService _tokenSvc;

    public AuthController(UserManager<ApplicationUser> userMgr, SignInManager<ApplicationUser> signInMgr, ITokenService tokenSvc)
    {
        _userMgr = userMgr;
        _signInMgr = signInMgr;
        _tokenSvc = tokenSvc;
    }

    public record LoginRequest([Required] string Email, [Required] string Password);
    public record LoginResponse(string Token, string UserId, string UserName, IEnumerable<string> Roles);
    public record LogoutResponse(string Message);

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest input)
    {
        var user = await _userMgr.FindByEmailAsync(input.Email);
        if (user == null) return Unauthorized("Credenziali non valide");

        var result = await _signInMgr.PasswordSignInAsync(user.UserName!, input.Password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded) return Unauthorized("Credenziali non valide");

        var roles = await _userMgr.GetRolesAsync(user);
        var token = await _tokenSvc.CreateTokenAsync(user, roles);

        return Ok(new LoginResponse(token, user.Id, user.UserName ?? "", roles));
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // With JWT stateless, logout is client-side (remove token). Optionally implement server-side blacklisting.
        return Ok(new LogoutResponse("Logout effettuato (rimuovi token sul client)"));
    }
}