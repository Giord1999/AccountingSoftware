using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Config
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=(localdb)\\\\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true";

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-secret-key-min-32-characters-long!";
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

// CORS for Blazor WASM
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
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
// Servizi CRUD per entità master
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IVatRateService, VatRateService>();
builder.Services.AddScoped<IAccountingPeriodService, AccountingPeriodService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
// Aggiungi questa riga insieme agli altri servizi
builder.Services.AddScoped<ISalesService, SalesService>();

builder.Services.AddScoped<IAnalysisCenterService, AnalysisCenterService>();
// Aggiungi registrazione per BI Service
builder.Services.AddScoped<IBIService, BIService>();

// CRM Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IActivityService, ActivityService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

// Endpoint per gestione errori
app.MapGet("/error", (HttpContext context) =>
{
    return Results.Problem(
        title: "Si è verificato un errore",
        statusCode: StatusCodes.Status500InternalServerError
    );
});

app.UseRouting();

// CORS deve essere prima di Auth
app.UseCors("AllowBlazorWasm");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

// Seed roles and admin (solo in development)
if (app.Environment.IsDevelopment())
{
    await Task.Run(async () =>
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var ctx = services.GetRequiredService<ApplicationDbContext>();
            await ctx.Database.MigrateAsync();
            var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
            await DataSeeder.SeedAsync(ctx, roleMgr, userMgr);
        }
    });
}

await app.RunAsync();