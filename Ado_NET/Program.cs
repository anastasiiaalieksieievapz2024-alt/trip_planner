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
// Окремий сервіс зі складними ADO.NET JOIN та агрегованим запитами.
builder.Services.AddSingleton(new SqlComplexQueries(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<KpzDbContext>();
// Застосовуємо всі незастосовані міграції під час запуску програми.
await db.Database.MigrateAsync();

//4 point CRUD demo
// var crud = scope.ServiceProvider.GetRequiredService<SqlCrudRepository>();
// var token = CancellationToken.None;
// var email = $"crud.demo.{Guid.NewGuid():N}@example.test";

// // CREATE та READ користувача
// var userId = await crud.CreateUserAsync(
//     "CRUD Demo User", email, "DEMO_ONLY_HASH", token);
// Console.WriteLine($"CREATE User: id={userId}");

// var user = await crud.GetUserAsync(userId, token);
// Console.WriteLine($"READ User: {user?.FullName}, {user?.Email}");

// // UPDATE користувача
// var userUpdated = await crud.UpdateUserAsync(
//     userId, "CRUD Demo User Updated", email, "DEMO_ONLY_HASH", token);
// Console.WriteLine($"UPDATE User: {userUpdated}");

// // CREATE та READ подорожі
// var tripId = await crud.CreateTripAsync(
//     userId, 1, "CRUD Demo Trip", "Lviv",
//     new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 15),
//     "Тестовий запис", token);
// Console.WriteLine($"CREATE Trip: id={tripId}");

// var trip = await crud.GetTripAsync(tripId, token);
// Console.WriteLine($"READ Trip: {trip?.TripName}, owner={trip?.OwnerName}");

// // UPDATE подорожі використовує RowVersion з прочитаного запису
// if (trip is not null)
// {
//     var newRowVersion = await crud.UpdateTripAsync(
//         tripId, userId, 1, "CRUD Demo Trip Updated", "Lviv",
//         new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 15),
//         "Оновлений тестовий запис", trip.RowVersion, token);
//     Console.WriteLine($"UPDATE Trip: {newRowVersion is not null}");
// }

// // DELETE подорожі, потім користувача
// Console.WriteLine($"DELETE Trip: {await crud.DeleteTripAsync(tripId, token)}");
// Console.WriteLine($"DELETE User: {await crud.DeleteUserAsync(userId, token)}");
// // Потрібен для посилання на Program з конфігурації User Secrets.

//5 point 
// var complexQueries = scope.ServiceProvider.GetRequiredService<SqlComplexQueries>();
// await ComplexQueryDemo.RunAsync(complexQueries, CancellationToken.None);

await SqlTransactionDemo.RunAsync(connectionString, CancellationToken.None);

public partial class Program;
