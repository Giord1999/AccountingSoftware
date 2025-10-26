using AccountingSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<VATRate> VATRates => Set<VATRate>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Reconciliation> Reconciliations { get; set; }
    public DbSet<ReconciliationItem> ReconciliationItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Account: indice unico per Company + Code
        builder.Entity<Account>()
            .HasIndex(a => new { a.CompanyId, a.Code })
            .IsUnique();

        // JournalEntry: indici per performance delle query
        builder.Entity<JournalEntry>()
            .HasIndex(j => new { j.CompanyId, j.Status, j.Date });

        builder.Entity<JournalEntry>()
            .HasIndex(j => new { j.PeriodId, j.Status });

        // JournalLine: relazione con Account (Restrict per integrità referenziale)
        builder.Entity<JournalLine>()
            .HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // AuditLog: indici per query su UserId e Timestamp
        builder.Entity<AuditLog>()
            .HasIndex(a => new { a.UserId, a.Timestamp });

        builder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);

        // Batch: indici per query su Company e Status
        builder.Entity<Batch>()
            .HasIndex(b => new { b.CompanyId, b.Status, b.CreatedAt });

        builder.Entity<Batch>()
            .HasIndex(b => new { b.UserId, b.CreatedAt });

        // Report: indici per query su Company, Period e Type
        builder.Entity<Report>()
            .HasIndex(r => new { r.CompanyId, r.PeriodId, r.Type });

        builder.Entity<Report>()
            .HasIndex(r => new { r.GeneratedAt, r.Status });

        builder.Entity<Report>()
            .HasIndex(r => r.ExpiresAt)
            .HasFilter("[ExpiresAt] IS NOT NULL");

        // Report: relazione con AccountingPeriod
        builder.Entity<Report>()
            .HasOne(r => r.Period)
            .WithMany()
            .HasForeignKey(r => r.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reconciliation: indici per query su Company, Account e Date
        builder.Entity<Reconciliation>()
            .HasIndex(r => new { r.CompanyId, r.AccountId, r.FromDate, r.ToDate });

        builder.Entity<Reconciliation>()
            .HasIndex(r => new { r.Status, r.CreatedAt });

        builder.Entity<Reconciliation>()
            .HasIndex(r => new { r.CreatedBy, r.CreatedAt });

        // Reconciliation: relazione con Account
        builder.Entity<Reconciliation>()
            .HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ReconciliationItem: relazione con Reconciliation (Cascade per eliminare items con reconciliation)
        builder.Entity<ReconciliationItem>()
            .HasOne(ri => ri.Reconciliation)
            .WithMany(r => r.Items)
            .HasForeignKey(ri => ri.ReconciliationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReconciliationItem: relazione con JournalEntry
        builder.Entity<ReconciliationItem>()
            .HasOne(ri => ri.JournalEntry)
            .WithMany()
            .HasForeignKey(ri => ri.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ReconciliationItem: indici per query su Reconciliation e Status
        builder.Entity<ReconciliationItem>()
            .HasIndex(ri => new { ri.ReconciliationId, ri.IsReconciled });

        builder.Entity<ReconciliationItem>()
            .HasIndex(ri => new { ri.ItemType, ri.TransactionDate });

        builder.Entity<ReconciliationItem>()
            .HasIndex(ri => ri.ExternalReference)
            .HasFilter("[ExternalReference] IS NOT NULL");
    }
}