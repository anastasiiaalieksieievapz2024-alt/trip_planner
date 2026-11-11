using Kpz.Lab2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Узагальнений хост завантажує конфігурацію та налаштовує DI для консольного застосунку.
var builder = Host.CreateApplicationBuilder(args);
// User Secrets може перевизначити налаштування з appsettings.json без збереження секретів у репозиторії.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Рядок підключення читається з ConnectionStrings:KpzDb.
var connectionString = builder.Configuration.GetConnectionString("KpzDb")
    ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

// Реєструємо контекст EF Core та SQL Server провайдер у контейнері залежностей.
builder.Services.AddDbContext<KpzDbContext>(options =>
    options.UseSqlServer(connectionString).UseDevelopmentDataSeeding());
// Реєструємо ADO.NET CRUD окремо від EF Core, використовуючи той самий рядок підключення.
builder.Services.AddSingleton(new SqlCrudRepository(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<KpzDbContext>();
// Застосовуємо всі незастосовані міграції під час запуску програми.
await db.Database.MigrateAsync();
Console.WriteLine("Database KpzDb is up to date.");

// Потрібен для посилання на Program з конфігурації User Secrets.
public partial class Program;
