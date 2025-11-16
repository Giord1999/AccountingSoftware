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
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Avvio dell'applicazione Accounting System");

    // Config
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? @"Server=(localdb)\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true";

    // DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret non configurato in appsettings.json");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AccountingApp";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AccountingAppClient";

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

    // CORS for Blazor WASM
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://localhost:7001", "http://localhost:5001" };

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
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAccountingService, AccountingService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IVatService, VatService>();
    builder.Services.AddScoped<IBatchService, BatchService>();
    builder.Services.AddScoped<IFXService, FXService>();
    builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
    builder.Services.AddScoped<ICompanyService, CompanyService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<IVatRateService, VatRateService>();
    builder.Services.AddScoped<IAccountingPeriodService, AccountingPeriodService>();
    builder.Services.AddScoped<IInventoryService, InventoryService>();
    builder.Services.AddScoped<ISalesService, SalesService>();
    builder.Services.AddScoped<IPurchaseService, PurchaseService>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
    builder.Services.AddScoped<IAnalysisCenterService, AnalysisCenterService>();
    builder.Services.AddScoped<IBIService, BIService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<ISupplierService, SupplierService>();
    builder.Services.AddScoped<ILeadService, LeadService>();
    builder.Services.AddScoped<IOpportunityService, OpportunityService>();
    builder.Services.AddScoped<IActivityService, ActivityService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Accounting System API", Version = "v1" });
    });

    // Authorization policies
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireContabileOrAdmin", policy => policy.RequireRole("Contabile", "Admin"))
        .AddPolicy("RequireAuditorOrAdmin", policy => policy.RequireRole("Auditor", "Admin"));

    // Rate Limiting
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

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "sql" });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Accounting System API v1"));
    }

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // Endpoint per gestione errori
    app.MapGet("/error", (HttpContext context) =>
    {
        return Results.Problem(
            title: "Si è verificato un errore",
            statusCode: StatusCodes.Status500InternalServerError
        );
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors("AllowBlazorWasm");

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers().RequireRateLimiting("fixed");
    app.MapHealthChecks("/health");

    // Seed roles and admin (solo in development)
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var ctx = services.GetRequiredService<ApplicationDbContext>();
            await ctx.Database.MigrateAsync();

            var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
            await DataSeeder.SeedAsync(ctx, roleMgr, userMgr);

            Log.Information("Database migrato e seeded con successo");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'applicazione è terminata inaspettatamente");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;