using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kpz.Lab2.Data;

// Фабрика потрібна dotnet-ef для створення контексту під час команд міграцій.
public sealed class KpzDbContextFactory : IDesignTimeDbContextFactory<KpzDbContext>
{
    public KpzDbContext CreateDbContext(string[] args)
    {
        // Завантажуємо ті самі налаштування підключення, що й основний застосунок.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<KpzDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("KpzDb")
            ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

        // Створюємо контекст із SQL Server провайдером для EF Core tools.
        var options = new DbContextOptionsBuilder<KpzDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new KpzDbContext(options);
    }
}
