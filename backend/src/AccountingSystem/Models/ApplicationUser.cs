using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public Guid? CompanyId { get; set; } // default company context for user
}
