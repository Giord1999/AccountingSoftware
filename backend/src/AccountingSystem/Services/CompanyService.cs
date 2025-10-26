using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAuditService _audit;

    public CompanyService(ApplicationDbContext ctx, IAuditService audit)
    {
        _ctx = ctx;
        _audit = audit;
    }

    public async Task<Company> CreateCompanyAsync(Company company, string userId)
    {
        if (company == null)
            throw new InvalidOperationException("Company non può essere null");

        if (string.IsNullOrWhiteSpace(company.Name))
            throw new InvalidOperationException("Il nome dell'azienda è obbligatorio");

        // Validazione: verifica che il nome sia unico
        var existingCompany = await _ctx.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == company.Name);

        if (existingCompany != null)
            throw new InvalidOperationException($"Un'azienda con il nome '{company.Name}' esiste già");

        // Validazione: valuta di default
        if (string.IsNullOrWhiteSpace(company.BaseCurrency))
            company.BaseCurrency = "EUR";

        _ctx.Companies.Add(company);
        await _ctx.SaveChangesAsync();

        // Log audit
        await _audit.LogAsync(userId, "CREATE_COMPANY", $"Company '{company.Name}' (ID: {company.Id}) creata con successo");

        return company;
    }

    public async Task<Company?> GetCompanyByIdAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        return await _ctx.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);
    }

    public async Task<IEnumerable<Company>> GetAllCompaniesAsync()
    {
        return await _ctx.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company> UpdateCompanyAsync(Guid companyId, Company company, string userId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        if (company == null)
            throw new InvalidOperationException("Company non può essere null");

        if (string.IsNullOrWhiteSpace(company.Name))
            throw new InvalidOperationException("Il nome dell'azienda è obbligatorio");

        var existingCompany = await _ctx.Companies.FirstOrDefaultAsync(c => c.Id == companyId);

        if (existingCompany == null)
            throw new InvalidOperationException($"Company {companyId} non trovata");

        // Validazione: verifica unicità nome se è stato modificato
        if (existingCompany.Name != company.Name)
        {
            var duplicateName = await _ctx.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == company.Name && c.Id != companyId);

            if (duplicateName != null)
                throw new InvalidOperationException($"Un'azienda con il nome '{company.Name}' esiste già");
        }

        // Aggiorna i campi
        existingCompany.Name = company.Name;
        existingCompany.BaseCurrency = !string.IsNullOrWhiteSpace(company.BaseCurrency) 
            ? company.BaseCurrency 
            : existingCompany.BaseCurrency;

        _ctx.Companies.Update(existingCompany);
        await _ctx.SaveChangesAsync();

        // Log audit
        await _audit.LogAsync(userId, "UPDATE_COMPANY", $"Company '{existingCompany.Name}' (ID: {companyId}) aggiornata con successo");

        return existingCompany;
    }

    public async Task DeleteCompanyAsync(Guid companyId, string userId)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID non valido");

        var company = await _ctx.Companies.FirstOrDefaultAsync(c => c.Id == companyId);

        if (company == null)
            throw new InvalidOperationException($"Company {companyId} non trovata");

        // Validazione: verifica che non ci siano dati associati (accounts, periods, etc.)
        var hasAccounts = await _ctx.Accounts.AnyAsync(a => a.CompanyId == companyId);
        if (hasAccounts)
            throw new InvalidOperationException("Impossibile eliminare l'azienda: contiene conti contabili");

        var hasJournals = await _ctx.JournalEntries.AnyAsync(j => j.CompanyId == companyId);
        if (hasJournals)
            throw new InvalidOperationException("Impossibile eliminare l'azienda: contiene registrazioni contabili");

        var hasPeriods = await _ctx.AccountingPeriods.AnyAsync(p => p.CompanyId == companyId);
        if (hasPeriods)
            throw new InvalidOperationException("Impossibile eliminare l'azienda: contiene periodi contabili");

        _ctx.Companies.Remove(company);
        await _ctx.SaveChangesAsync();

        // Log audit
        await _audit.LogAsync(userId, "DELETE_COMPANY", $"Company '{company.Name}' (ID: {companyId}) eliminata con successo");
    }
}