using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Debug()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("========================================");
    Log.Information("🚀 Avvio dell'applicazione Accounting System");
    Log.Information("========================================");

    // Config
    Log.Information("📋 Step 1: Configurazione connection string...");
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? @"Server=(localdb)\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true";
    Log.Information("✅ Connection string configurato");

    // DbContext
    Log.Information("📋 Step 2: Registrazione DbContext...");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    Log.Information("✅ DbContext registrato");

    // Identity
    Log.Information("📋 Step 3: Configurazione Identity...");
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
    Log.Information("✅ Identity configurato");

    // JWT Authentication
    Log.Information("📋 Step 4: Configurazione JWT Authentication...");
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    if (string.IsNullOrEmpty(jwtSecret))
    {
        Log.Fatal("❌ JWT Secret non configurato in appsettings.json!");
        throw new InvalidOperationException("JWT Secret non configurato in appsettings.json");
    }
    Log.Information("✅ JWT Secret trovato (lunghezza: {Length} caratteri)", jwtSecret.Length);

    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AccountingApp";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AccountingAppClient";
    Log.Information("✅ JWT Issuer: {Issuer}, Audience: {Audience}", jwtIssuer, jwtAudience);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
    Log.Information("✅ JWT Authentication configurato");

    // CORS for Blazor WASM
    Log.Information("📋 Step 5: Configurazione CORS...");
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://localhost:7001", "http://localhost:5001" };
    Log.Information("✅ CORS configurato per {Count} origins", allowedOrigins.Length);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorWasm", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Register application services
    Log.Information("========================================");
    Log.Information("📦 Step 6: Registrazione servizi applicativi...");
    Log.Information("========================================");

    Log.Debug("Registering ITokenService → TokenService");
    builder.Services.AddScoped<ITokenService, TokenService>();

    Log.Debug("Registering IAccountingService → AccountingService");
    builder.Services.AddScoped<IAccountingService, AccountingService>();

    Log.Debug("Registering IAuditService → AuditService");
    builder.Services.AddScoped<IAuditService, AuditService>();

    Log.Debug("Registering IReportService → ReportService");
    builder.Services.AddScoped<IReportService, ReportService>();

    Log.Debug("Registering IVatService → VatService");
    builder.Services.AddScoped<IVatService, VatService>();

    Log.Debug("Registering IBatchService → BatchService");
    builder.Services.AddScoped<IBatchService, BatchService>();

    Log.Debug("Registering IFXService → FXService");
    builder.Services.AddScoped<IFXService, FXService>();

    Log.Debug("Registering IReconciliationService → ReconciliationService");
    builder.Services.AddScoped<IReconciliationService, ReconciliationService>();

    Log.Debug("Registering ICompanyService → CompanyService");
    builder.Services.AddScoped<ICompanyService, CompanyService>();

    Log.Debug("Registering IAccountService → AccountService");
    builder.Services.AddScoped<IAccountService, AccountService>();

    Log.Debug("Registering IVatRateService → VatRateService");
    builder.Services.AddScoped<IVatRateService, VatRateService>();

    Log.Debug("Registering IAccountingPeriodService → AccountingPeriodService");
    builder.Services.AddScoped<IAccountingPeriodService, AccountingPeriodService>();

    Log.Debug("Registering IInventoryService → InventoryService");
    builder.Services.AddScoped<IInventoryService, InventoryService>();

    Log.Debug("Registering ISalesService → SalesService");
    builder.Services.AddScoped<ISalesService, SalesService>();

    Log.Debug("Registering IPurchaseService → PurchaseService");
    builder.Services.AddScoped<IPurchaseService, PurchaseService>();

    Log.Debug("Registering IInvoiceService → InvoiceService");
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();

    Log.Debug("Registering IAnalysisCenterService → AnalysisCenterService");
    builder.Services.AddScoped<IAnalysisCenterService, AnalysisCenterService>();

    Log.Debug("Registering IBIService → BIService");
    builder.Services.AddScoped<IBIService, BIService>();

    Log.Debug("Registering ICustomerService → CustomerService");
    builder.Services.AddScoped<ICustomerService, CustomerService>();

    Log.Debug("Registering ISupplierService → SupplierService");
    builder.Services.AddScoped<ISupplierService, SupplierService>();

    Log.Debug("Registering ILeadService → LeadService");
    builder.Services.AddScoped<ILeadService, LeadService>();

    Log.Debug("Registering IOpportunityService → OpportunityService");
    builder.Services.AddScoped<IOpportunityService, OpportunityService>();

    Log.Debug("Registering IActivityService → ActivityService");
    builder.Services.AddScoped<IActivityService, ActivityService>();

    Log.Information("✅ Tutti i 23 servizi applicativi registrati con successo!");

    Log.Information("📋 Step 7: Registrazione Controllers e Swagger...");
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Accounting System API", Version = "v1" });
    });
    Log.Information("✅ Controllers e Swagger registrati");

    // Authorization policies
    Log.Information("📋 Step 8: Configurazione Authorization policies...");
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireContabileOrAdmin", policy => policy.RequireRole("Contabile", "Admin"))
        .AddPolicy("RequireAuditorOrAdmin", policy => policy.RequireRole("Auditor", "Admin"));
    Log.Information("✅ Authorization policies configurate");

    // Rate Limiting
    Log.Information("📋 Step 9: Configurazione Rate Limiting...");
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 100;
            opt.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
    Log.Information("✅ Rate Limiting configurato");

    // Health Checks
    Log.Information("📋 Step 10: Configurazione Health Checks...");
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "sql" });
    Log.Information("✅ Health Checks configurati");

    Log.Information("========================================");
    Log.Information("🏗️ Step 11: BUILD dell'applicazione...");
    Log.Information("========================================");

    var app = builder.Build();
    Log.Information("✅✅✅ BUILD COMPLETATO CON SUCCESSO! ✅✅✅");

    Log.Information("========================================");
    Log.Information("⚙️ Step 12: Configurazione middleware...");
    Log.Information("========================================");

    if (app.Environment.IsDevelopment())
    {
        Log.Information("🔧 Ambiente: Development");
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Accounting System API v1"));
    }
    else
    {
        Log.Information("🔧 Ambiente: Production");
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();

    // Endpoint per gestione errori
    app.MapGet("/error", (HttpContext context) =>
    {
        return Results.Problem(
            title: "Si è verificato un errore",
            statusCode: StatusCodes.Status500InternalServerError
        );
    });

    Log.Information("Configurazione middleware pipeline...");
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("AllowBlazorWasm");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    Log.Information("Configurazione endpoints...");
    app.MapControllers().RequireRateLimiting("fixed");
    app.MapHealthChecks("/health");
    Log.Information("✅ Middleware e endpoints configurati");

    Log.Information("========================================");
    Log.Information("🎉 APPLICAZIONE PRONTA!");
    Log.Information("🌐 Avvio del server...");
    Log.Information("========================================");

    await app.RunAsync();
}
catch (HostAbortedException ex)
{
    Log.Fatal(ex, "❌ HOST ABORTED EXCEPTION - L'host è stato interrotto durante l'avvio");
    return 1;
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ ERRORE FATALE - L'applicazione è terminata inaspettatamente");
    return 1;
}
finally
{
    Log.Information("🛑 Chiusura logging...");
    await Log.CloseAndFlushAsync();
}

return 0;