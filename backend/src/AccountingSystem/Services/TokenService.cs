using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccountingSystem.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AccountingSystem.Services;

public class TokenService(IConfiguration config) : ITokenService
{
    private readonly IConfiguration _config = config;

    public Task<string> CreateTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var key = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT key missing");
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var minutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 1440;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new("displayName", user.DisplayName ?? "")
        };

        // add roles
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var cred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: cred
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }
}