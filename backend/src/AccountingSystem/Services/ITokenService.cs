using Microsoft.AspNetCore.Identity;
using AccountingSystem.Models;

namespace AccountingSystem.Services;

public interface ITokenService
{
    Task<string> CreateTokenAsync(ApplicationUser user, IList<string> roles);
}
