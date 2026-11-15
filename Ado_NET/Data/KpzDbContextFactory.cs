using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kpz.Lab2.Data;

// ������� ������� dotnet-ef ��� ��������� ��������� �� ��� ������ �������.
public sealed class KpzDbContextFactory : IDesignTimeDbContextFactory<KpzDbContext>
{
    public KpzDbContext CreateDbContext(string[] args)
    {
        // ����������� � ��� ������������ ����������, �� � �������� ����������.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<KpzDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("KpzDb")
            ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

        // ��������� �������� �� SQL Server ����������� ��� EF Core tools.
        var optionsBuilder = new DbContextOptionsBuilder<KpzDbContext>();
        optionsBuilder.UseSqlServer(connectionString).UseDevelopmentDataSeeding();

        return new KpzDbContext(optionsBuilder.Options);
    }
}
