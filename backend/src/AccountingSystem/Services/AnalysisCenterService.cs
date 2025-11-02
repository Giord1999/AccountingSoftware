using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Services;

public class AnalysisCenterService : IAnalysisCenterService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ILogger<AnalysisCenterService> _logger;

    public AnalysisCenterService(
        ApplicationDbContext context,
        IAuditService audit,
        ILogger<AnalysisCenterService> logger)
    {
        _context = context;
        _audit = audit;
        _logger = logger;
    }

    public async Task<AnalysisCenter> CreateAnalysisCenterAsync(AnalysisCenter center, string userId)
    {
        ArgumentNullException.ThrowIfNull(center);

        if (string.IsNullOrWhiteSpace(center.Code))
            throw new InvalidOperationException("Code è obbligatorio");

        if (string.IsNullOrWhiteSpace(center.Name))
            throw new InvalidOperationException("Name è obbligatorio");

        // Validazione unicità code per company
        if (!await ValidateCodeAsync(center.CompanyId, center.Code))
            throw new InvalidOperationException($"Code '{center.Code}' già esistente per questa azienda");

        center.CreatedAt = DateTime.UtcNow;
        center.CreatedBy = userId;

        _context.AnalysisCenters.Add(center);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "CREATE_ANALYSIS_CENTER",
            $"Creato centro di analisi '{center.Code}' - {center.Name} (ID: {center.Id})");

        _logger.LogInformation("AnalysisCenter {Code} creato per company {CompanyId}", center.Code, center.CompanyId);

        return center;
    }

    public async Task<AnalysisCenter?> GetAnalysisCenterByIdAsync(Guid id, Guid? companyId = null)
    {
        var query = _context.AnalysisCenters.AsNoTracking().Where(ac => ac.Id == id);

        if (companyId.HasValue)
            query = query.Where(ac => ac.CompanyId == companyId.Value);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AnalysisCenter>> GetAnalysisCentersByCompanyAsync(Guid companyId, AnalysisCenterType? type = null)
    {
        var query = _context.AnalysisCenters
            .AsNoTracking()
            .Where(ac => ac.CompanyId == companyId && ac.IsActive);

        if (type.HasValue)
            query = query.Where(ac => ac.Type == type.Value);

        return await query.OrderBy(ac => ac.Code).ToListAsync();
    }

    public async Task<AnalysisCenter> UpdateAnalysisCenterAsync(Guid id, AnalysisCenter center, string userId)
    {
        var existing = await _context.AnalysisCenters.FirstOrDefaultAsync(ac => ac.Id == id);

        if (existing == null)
            throw new InvalidOperationException($"AnalysisCenter {id} non trovato");

        // Validazione unicità code se modificato
        if (existing.Code != center.Code && !await ValidateCodeAsync(center.CompanyId, center.Code, id))
            throw new InvalidOperationException($"Code '{center.Code}' già esistente");

        existing.Code = center.Code;
        existing.Name = center.Name;
        existing.Description = center.Description;
        existing.Type = center.Type;
        existing.IsActive = center.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userId;

        _context.AnalysisCenters.Update(existing);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "UPDATE_ANALYSIS_CENTER",
            $"Aggiornato centro di analisi '{existing.Code}' (ID: {id})");

        _logger.LogInformation("AnalysisCenter {Id} aggiornato", id);

        return existing;
    }

    public async Task DeleteAnalysisCenterAsync(Guid id, string userId)
    {
        var center = await _context.AnalysisCenters.FirstOrDefaultAsync(ac => ac.Id == id);

        if (center == null)
            throw new InvalidOperationException($"AnalysisCenter {id} non trovato");

        // Soft delete: disattiva invece di eliminare
        center.IsActive = false;
        center.UpdatedAt = DateTime.UtcNow;
        center.UpdatedBy = userId;

        _context.AnalysisCenters.Update(center);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "DELETE_ANALYSIS_CENTER",
            $"Disattivato centro di analisi '{center.Code}' (ID: {id})");

        _logger.LogInformation("AnalysisCenter {Id} disattivato", id);
    }

    public async Task<bool> ValidateCodeAsync(Guid companyId, string code, Guid? excludeId = null)
    {
        var query = _context.AnalysisCenters
            .AsNoTracking()
            .Where(ac => ac.CompanyId == companyId && ac.Code == code && ac.IsActive);

        if (excludeId.HasValue)
            query = query.Where(ac => ac.Id != excludeId.Value);

        return !await query.AnyAsync();
    }
}