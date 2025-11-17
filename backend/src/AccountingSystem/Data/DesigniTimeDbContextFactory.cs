using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AccountingSystem.Data;

/// <summary>
/// Factory per la creazione del DbContext a design-time (migrations, scaffolding, etc.)
/// Permette a Entity Framework Tools di creare il DbContext senza avviare l'intera applicazione
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Carica la configurazione da appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        // Ottieni la connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? @"Server=(localdb)\mssqllocaldb;Database=AccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        // Configura DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });

        // Log (opzionale)
        Console.WriteLine($"[DesignTimeDbContextFactory] Using connection: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}