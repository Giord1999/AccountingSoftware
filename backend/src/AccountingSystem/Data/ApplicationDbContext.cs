using AccountingSystem.Models;
using AccountingSystem.Models.FinancialPlanning;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<VatRate> VatRates => Set<VatRate>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Reconciliation> Reconciliations { get; set; }
    public DbSet<ReconciliationItem> ReconciliationItems { get; set; }
    public DbSet<FinancialPlan> FinancialPlans => Set<FinancialPlan>();
    public DbSet<FinancialPlanItem> FinancialPlanItems => Set<FinancialPlanItem>();
    public DbSet<Forecast> Forecasts => Set<Forecast>();

    // Inventory Management
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    // Sales Management
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SalesAccountConfiguration> SalesAccountConfigurations => Set<SalesAccountConfiguration>();

    // Purchase Management
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseAccountConfiguration> PurchaseAccountConfigurations => Set<PurchaseAccountConfiguration>();

    // Analysis Centers
    public DbSet<AnalysisCenter> AnalysisCenters { get; set; }

    public DbSet<BISnapshot> BISnapshots => Set<BISnapshot>();

    // CRM
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

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

        // JournalLine: relazione con AnalysisCenter (Restrict per integrità referenziale)
        builder.Entity<JournalLine>()
            .HasOne(l => l.AnalysisCenter)
            .WithMany()
            .HasForeignKey(l => l.AnalysisCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // JournalLine: indice per query analitiche per centro
        builder.Entity<JournalLine>()
            .HasIndex(l => l.AnalysisCenterId)
            .HasFilter("[AnalysisCenterId] IS NOT NULL");

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

        // FinancialPlan: indici per query su Company e Status
        builder.Entity<FinancialPlan>()
            .HasIndex(fp => new { fp.CompanyId, fp.Status });

        // FinancialPlanItem: relazione con FinancialPlan
        builder.Entity<FinancialPlanItem>()
            .HasOne(fpi => fpi.FinancialPlan)
            .WithMany(fp => fp.Items)
            .HasForeignKey(fpi => fpi.FinancialPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Forecast: relazione con FinancialPlan
        builder.Entity<Forecast>()
            .HasOne(f => f.FinancialPlan)
            .WithMany()
            .HasForeignKey(f => f.FinancialPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // ========== INVENTORY CONFIGURATION ==========

        // Inventory: indice unico per Company + ItemCode
        builder.Entity<Inventory>()
            .HasIndex(i => new { i.CompanyId, i.ItemCode })
            .IsUnique();

        // Inventory: indici per performance
        builder.Entity<Inventory>()
            .HasIndex(i => new { i.CompanyId, i.IsActive, i.Category });

        builder.Entity<Inventory>()
            .HasIndex(i => i.Barcode)
            .HasFilter("[Barcode] IS NOT NULL");

        // Inventory: relazioni con Account (Restrict per integrità referenziale)
        builder.Entity<Inventory>()
            .HasOne(i => i.InventoryAccount)
            .WithMany()
            .HasForeignKey(i => i.InventoryAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Inventory>()
            .HasOne(i => i.CostOfSalesAccount)
            .WithMany()
            .HasForeignKey(i => i.CostOfSalesAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inventory: relazione con AnalysisCenter predefinito (Restrict per integrità referenziale)
        builder.Entity<Inventory>()
            .HasOne(i => i.DefaultAnalysisCenter)
            .WithMany()
            .HasForeignKey(i => i.DefaultAnalysisCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inventory: indice per query analitiche per articolo con centro predefinito
        builder.Entity<Inventory>()
            .HasIndex(i => i.DefaultAnalysisCenterId)
            .HasFilter("[DefaultAnalysisCenterId] IS NOT NULL");

        // Inventory: indice composito per reporting per company + centro
        builder.Entity<Inventory>()
            .HasIndex(i => new { i.CompanyId, i.DefaultAnalysisCenterId })
            .HasFilter("[DefaultAnalysisCenterId] IS NOT NULL");

        // InventoryMovement: relazione con Inventory (Restrict per evitare eliminazioni accidentali)
        builder.Entity<InventoryMovement>()
            .HasOne(m => m.Inventory)
            .WithMany()
            .HasForeignKey(m => m.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // InventoryMovement: relazione con JournalEntry (opzionale)
        builder.Entity<InventoryMovement>()
            .HasOne(m => m.JournalEntry)
            .WithMany()
            .HasForeignKey(m => m.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        // InventoryMovement: relazione con AnalysisCenter (Restrict per integrità referenziale)
        builder.Entity<InventoryMovement>()
            .HasOne(m => m.AnalysisCenter)
            .WithMany()
            .HasForeignKey(m => m.AnalysisCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // InventoryMovement: indice per query analitiche per movimento
        builder.Entity<InventoryMovement>()
            .HasIndex(m => m.AnalysisCenterId)
            .HasFilter("[AnalysisCenterId] IS NOT NULL");

        // InventoryMovement: indice composito per reporting per articolo + centro + data
        builder.Entity<InventoryMovement>()
            .HasIndex(m => new { m.InventoryId, m.AnalysisCenterId, m.MovementDate })
            .IsDescending(false, false, true)
            .HasFilter("[AnalysisCenterId] IS NOT NULL");

        // InventoryMovement: indici per query su Company, Inventory e Date
        builder.Entity<InventoryMovement>()
            .HasIndex(m => new { m.CompanyId, m.MovementDate, m.Type });

        builder.Entity<InventoryMovement>()
            .HasIndex(m => new { m.InventoryId, m.MovementDate })
            .IsDescending(false, true); // Order by date DESC

        builder.Entity<InventoryMovement>()
            .HasIndex(m => new { m.CreatedBy, m.CreatedAt });

        builder.Entity<InventoryMovement>()
            .HasIndex(m => m.JournalEntryId)
            .HasFilter("[JournalEntryId] IS NOT NULL");

        // ========== INVOICE CONFIGURATION ==========

        // Invoice: indice unico per Company + InvoiceNumber
        builder.Entity<Invoice>()
            .HasIndex(i => new { i.CompanyId, i.InvoiceNumber })
            .IsUnique();

        // Invoice: indici per performance su query comuni
        builder.Entity<Invoice>()
            .HasIndex(i => new { i.CompanyId, i.Status, i.IssueDate });

        builder.Entity<Invoice>()
            .HasIndex(i => new { i.Type, i.Status, i.IssueDate });

        builder.Entity<Invoice>()
            .HasIndex(i => new { i.CompanyId, i.DueDate })
            .HasFilter("[DueDate] IS NOT NULL");

        // Invoice: relazione con JournalEntry (opzionale, generata al posting)
        builder.Entity<Invoice>()
            .HasOne(i => i.JournalEntry)
            .WithMany()
            .HasForeignKey(i => i.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Invoice: relazione con AccountingPeriod
        builder.Entity<Invoice>()
            .HasOne(i => i.Period)
            .WithMany()
            .HasForeignKey(i => i.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // InvoiceLine: relazione con Invoice (Cascade per eliminare lines con invoice)
        builder.Entity<InvoiceLine>()
            .HasOne(il => il.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(il => il.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // InvoiceLine: relazione con Inventory (opzionale)
        builder.Entity<InvoiceLine>()
            .HasOne(il => il.Inventory)
            .WithMany()
            .HasForeignKey(il => il.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // InvoiceLine: relazione con Account (opzionale)
        builder.Entity<InvoiceLine>()
            .HasOne(il => il.Account)
            .WithMany()
            .HasForeignKey(il => il.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // InvoiceLine: relazione con VatRate (opzionale)
        builder.Entity<InvoiceLine>()
            .HasOne(il => il.VatRate)
            .WithMany()
            .HasForeignKey(il => il.VatRateId)
            .OnDelete(DeleteBehavior.Restrict);

        // InvoiceLine: relazione con AnalysisCenter (Restrict per integrità referenziale)
        builder.Entity<InvoiceLine>()
            .HasOne(il => il.AnalysisCenter)
            .WithMany()
            .HasForeignKey(il => il.AnalysisCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // InvoiceLine: indice per query analitiche per centro
        builder.Entity<InvoiceLine>()
            .HasIndex(il => il.AnalysisCenterId)
            .HasFilter("[AnalysisCenterId] IS NOT NULL");

        // InvoiceLine: indice composito per reporting per fattura + centro
        builder.Entity<InvoiceLine>()
            .HasIndex(il => new { il.InvoiceId, il.AnalysisCenterId })
            .HasFilter("[AnalysisCenterId] IS NOT NULL");

        // InvoiceLine: indici per performance
        builder.Entity<InvoiceLine>()
            .HasIndex(il => new { il.InvoiceId, il.LineNumber });

        builder.Entity<InvoiceLine>()
            .HasIndex(il => il.InventoryId)
            .HasFilter("[InventoryId] IS NOT NULL");

        builder.Entity<InvoiceLine>()
            .HasIndex(il => il.AccountId)
            .HasFilter("[AccountId] IS NOT NULL");

        // ========== SALES CONFIGURATION ==========

        // Sale: relazione con Invoice
        builder.Entity<Sale>()
            .HasOne(s => s.Invoice)
            .WithMany()
            .HasForeignKey(s => s.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale: relazione con JournalEntry
        builder.Entity<Sale>()
            .HasOne(s => s.JournalEntry)
            .WithMany()
            .HasForeignKey(s => s.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Sale: relazione con AccountingPeriod
        builder.Entity<Sale>()
            .HasOne(s => s.Period)
            .WithMany()
            .HasForeignKey(s => s.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale: relazione con Customer
        builder.Entity<Sale>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale: indici per performance
        builder.Entity<Sale>()
            .HasIndex(s => new { s.CompanyId, s.Status, s.SaleDate });

        builder.Entity<Sale>()
            .HasIndex(s => new { s.CompanyId, s.SaleDate })
            .IsDescending(false, true);

        // SalesAccountConfiguration: indice unico per Company (solo una configurazione default per company)
        builder.Entity<SalesAccountConfiguration>()
            .HasIndex(sac => new { sac.CompanyId, sac.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        // SalesAccountConfiguration: relazioni con Account
        builder.Entity<SalesAccountConfiguration>()
            .HasOne(sac => sac.ReceivablesAccount)
            .WithMany()
            .HasForeignKey(sac => sac.ReceivablesAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalesAccountConfiguration>()
            .HasOne(sac => sac.RevenueAccount)
            .WithMany()
            .HasForeignKey(sac => sac.RevenueAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalesAccountConfiguration>()
            .HasOne(sac => sac.VatPayableAccount)
            .WithMany()
            .HasForeignKey(sac => sac.VatPayableAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== PURCHASE CONFIGURATION ==========

        // Purchase: relazione con Invoice
        builder.Entity<Purchase>()
            .HasOne(p => p.Invoice)
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Purchase: relazione con JournalEntry
        builder.Entity<Purchase>()
            .HasOne(p => p.JournalEntry)
            .WithMany()
            .HasForeignKey(p => p.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Purchase: relazione con AccountingPeriod
        builder.Entity<Purchase>()
            .HasOne(p => p.Period)
            .WithMany()
            .HasForeignKey(p => p.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Purchase: indici per performance
        builder.Entity<Purchase>()
            .HasIndex(p => new { p.CompanyId, p.Status, p.PurchaseDate });

        builder.Entity<Purchase>()
            .HasIndex(p => new { p.CompanyId, p.PurchaseDate })
            .IsDescending(false, true);

        // PurchaseAccountConfiguration: indice unico per Company (solo una configurazione default per company)
        builder.Entity<PurchaseAccountConfiguration>()
            .HasIndex(pac => new { pac.CompanyId, pac.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        // PurchaseAccountConfiguration: relazioni con Account
        builder.Entity<PurchaseAccountConfiguration>()
            .HasOne(pac => pac.PayablesAccount)
            .WithMany()
            .HasForeignKey(pac => pac.PayablesAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PurchaseAccountConfiguration>()
            .HasOne(pac => pac.ExpenseAccount)
            .WithMany()
            .HasForeignKey(pac => pac.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PurchaseAccountConfiguration>()
            .HasOne(pac => pac.VatReceivableAccount)
            .WithMany()
            .HasForeignKey(pac => pac.VatReceivableAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== ANALYSIS CENTER CONFIGURATION ==========

        // AnalysisCenter: indice unico per Company + Code
        builder.Entity<AnalysisCenter>()
            .HasIndex(ac => new { ac.CompanyId, ac.Code })
            .IsUnique();

        // AnalysisCenter: indici per performance
        builder.Entity<AnalysisCenter>()
            .HasIndex(ac => new { ac.CompanyId, ac.IsActive, ac.Type });

        builder.Entity<AnalysisCenter>()
            .HasIndex(ac => new { ac.CompanyId, ac.Type })
            .HasFilter("[IsActive] = 1");

        // AnalysisCenter: relazione con Company
        builder.Entity<AnalysisCenter>()
            .HasOne(ac => ac.Company)
            .WithMany()
            .HasForeignKey(ac => ac.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== BI CONFIGURATION ==========

        // BISnapshot: indice composito per query su Company e SnapshotDate (ordinato per data discendente)
        builder.Entity<BISnapshot>()
            .HasIndex(s => new { s.CompanyId, s.SnapshotDate })
            .IsDescending(false, true);

        // BISnapshot: indice per query su GeneratedBy
        builder.Entity<BISnapshot>()
            .HasIndex(s => s.GeneratedBy);

        // ========== CUSTOMER CONFIGURATION ==========

        // Customer: indice unico per Company + VatNumber (se presente)
        builder.Entity<Customer>()
            .HasIndex(c => new { c.CompanyId, c.VatNumber })
            .IsUnique()
            .HasFilter("[VatNumber] IS NOT NULL");

        // Customer: indici per performance
        builder.Entity<Customer>()
            .HasIndex(c => new { c.CompanyId, c.IsActive });

        builder.Entity<Customer>()
            .HasIndex(c => new { c.CompanyId, c.Name });

        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .HasFilter("[Email] IS NOT NULL");

        // Customer: relazione con Company
        builder.Entity<Customer>()
            .HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== ACTIVITY CONFIGURATION ==========

        // Activity: indici per performance
        builder.Entity<Activity>()
            .HasIndex(a => new { a.CompanyId, a.Status, a.ScheduledDate });

        builder.Entity<Activity>()
            .HasIndex(a => new { a.AssignedTo, a.Status });

        builder.Entity<Activity>()
            .HasIndex(a => a.CustomerId)
            .HasFilter("[CustomerId] IS NOT NULL");

        builder.Entity<Activity>()
            .HasIndex(a => a.LeadId)
            .HasFilter("[LeadId] IS NOT NULL");

        // Activity: relazioni
        builder.Entity<Activity>()
            .HasOne(a => a.Company)
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Activity>()
            .HasOne(a => a.Customer)
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Activity>()
            .HasOne(a => a.Lead)
            .WithMany()
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.SetNull);

        // ========== OPPORTUNITY CONFIGURATION ==========

        // Opportunity: indici per performance
        builder.Entity<Opportunity>()
            .HasIndex(o => new { o.CompanyId, o.Stage });

        builder.Entity<Opportunity>()
            .HasIndex(o => o.CreatedAt);

        // Opportunity: relazioni
        builder.Entity<Opportunity>()
            .HasOne(o => o.Company)
            .WithMany()
            .HasForeignKey(o => o.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Opportunity>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Opportunity>()
            .HasOne(o => o.Lead)
            .WithMany()
            .HasForeignKey(o => o.LeadId)
            .OnDelete(DeleteBehavior.SetNull);

        // Activity: relazione con Opportunity
        builder.Entity<Activity>()
            .HasOne(a => a.Opportunity)
            .WithMany(o => o.Activities)
            .HasForeignKey(a => a.OpportunityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Activity: indice per Opportunity
        builder.Entity<Activity>()
            .HasIndex(a => a.OpportunityId)
            .HasFilter("[OpportunityId] IS NOT NULL");

        // ========== LEAD CONFIGURATION ==========

        // Lead: indici per performance
        builder.Entity<Lead>()
            .HasIndex(l => new { l.CompanyId, l.Status });

        builder.Entity<Lead>()
            .HasIndex(l => l.Email)
            .HasFilter("[Email] IS NOT NULL");

        // Lead: relazione con Company
        builder.Entity<Lead>()
            .HasOne(l => l.Company)
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== SUPPLIER CONFIGURATION ==========

        // Supplier: indice unico per Company + VatNumber (se presente)
        builder.Entity<Supplier>()
            .HasIndex(s => new { s.CompanyId, s.VatNumber })
            .IsUnique()
            .HasFilter("[VatNumber] IS NOT NULL");

        // Supplier: indici per performance
        builder.Entity<Supplier>()
            .HasIndex(s => new { s.CompanyId, s.IsActive });

        builder.Entity<Supplier>()
            .HasIndex(s => new { s.CompanyId, s.Name });

        builder.Entity<Supplier>()
            .HasIndex(s => s.Email)
            .HasFilter("[Email] IS NOT NULL");

        // Supplier: relazione con Company
        builder.Entity<Supplier>()
            .HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}