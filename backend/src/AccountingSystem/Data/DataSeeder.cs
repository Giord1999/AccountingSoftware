using AccountingSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext ctx, RoleManager<IdentityRole> roleMgr, UserManager<ApplicationUser> userMgr)
    {
        // roles
        var roles = new[] { "Admin", "Contabile", "Auditor" };
        foreach (var r in roles)
        {
            if (!await roleMgr.RoleExistsAsync(r)) await roleMgr.CreateAsync(new IdentityRole(r));
        }

        // default company
        if (!ctx.Companies.Any())
        {
            var c = new Company { Name = "Demo Company", BaseCurrency = "EUR" };
            ctx.Companies.Add(c);
            await ctx.SaveChangesAsync();

            // seed basic accounts
            var accounts = new[] {
                new Account { CompanyId = c.Id, Code = "1000", Name = "Cash", Category = AccountCategory.Asset },
                new Account { CompanyId = c.Id, Code = "2000", Name = "Accounts Payable", Category = AccountCategory.Liability },
                new Account { CompanyId = c.Id, Code = "3000", Name = "Equity", Category = AccountCategory.Equity },
                new Account { CompanyId = c.Id, Code = "4000", Name = "Revenue", Category = AccountCategory.Revenue },
                new Account { CompanyId = c.Id, Code = "5000", Name = "Expenses", Category = AccountCategory.Expense }
            };
            ctx.Accounts.AddRange(accounts);
            await ctx.SaveChangesAsync();
        }

        // admin user
        var adminEmail = "admin@demo.local";
        if (await userMgr.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var res = await userMgr.CreateAsync(admin, "Admin123!");
            if (res.Succeeded) await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}
