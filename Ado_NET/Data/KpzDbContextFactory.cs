using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kpz.Lab2.Data;

// Фабрика створює контекст під час роботи інструментів dotnet-ef у design-time режимі.
public sealed class KpzDbContextFactory : IDesignTimeDbContextFactory<KpzDbContext>
{
    public KpzDbContext CreateDbContext(string[] args)
    {
        // Читаємо налаштування з appsettings, User Secrets і змінних середовища.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<KpzDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("KpzDb")
            ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

        // Налаштовуємо контекст EF Core на використання SQL Server.
        var optionsBuilder = new DbContextOptionsBuilder<KpzDbContext>();
        optionsBuilder.UseSqlServer(connectionString).UseDevelopmentDataSeeding();

        return new KpzDbContext(optionsBuilder.Options);
    }
}
