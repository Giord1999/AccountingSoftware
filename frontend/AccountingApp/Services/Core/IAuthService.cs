using AccountingApp.Models;

namespace AccountingApp.Services.Core;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? Token { get; }
    Guid? CompanyId { get; }
    Task<bool> ValidateTokenAsync();
}