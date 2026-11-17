using Kpz.Lab2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

// Налаштування хоста та контейнера залежностей для консольної демонстрації.
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
var builder = Host.CreateApplicationBuilder(args);
// User Secrets дає змогу зберігати конфіденційні налаштування окремо від appsettings.json.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Отримуємо рядок підключення до бази даних із секції ConnectionStrings:KpzDb.
var connectionString = builder.Configuration.GetConnectionString("KpzDb")
    ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

// Реєструємо контекст EF Core для SQL Server і додавання демонстраційних даних.
builder.Services.AddDbContext<KpzDbContext>(options =>
    options.UseSqlServer(connectionString).UseDevelopmentDataSeeding());
// Реєструємо окремий ADO.NET репозиторій для демонстрації CRUD.
builder.Services.AddSingleton(new SqlCrudRepository(connectionString));
// Реєструємо класи ADO.NET для складних SQL-запитів із JOIN.
builder.Services.AddSingleton(new SqlComplexQueries(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<KpzDbContext>();
// Застосовуємо всі наявні міграції перед демонстраціями.
await db.Database.MigrateAsync();

// Пункт 4 — ADO.NET CRUD: створення, читання, оновлення й видалення користувача та поїздки.
// Реалізація: Data/SqlCrudRepository.cs.
// var crud = scope.ServiceProvider.GetRequiredService<SqlCrudRepository>();
// var token = CancellationToken.None;
// var email = $"crud.demo.{Guid.NewGuid():N}@example.test";

// // СТВОРЕННЯ та ЧИТАННЯ користувача
// var userId = await crud.CreateUserAsync(
//     "CRUD Demo User", email, "DEMO_ONLY_HASH", token);
// Console.WriteLine($"СТВОРЕНО користувача: id={userId}");

// var user = await crud.GetUserAsync(userId, token);
// Console.WriteLine($"ПРОЧИТАНО користувача: {user?.FullName}, {user?.Email}");

// // ОНОВЛЕННЯ користувача
// var userUpdated = await crud.UpdateUserAsync(
//     userId, "CRUD Demo User Updated", email, "DEMO_ONLY_HASH", token);
// Console.WriteLine($"Користувача оновлено: {userUpdated}");

// // СТВОРЕННЯ та ЧИТАННЯ поїздки
// var tripId = await crud.CreateTripAsync(
//     userId, 1, "CRUD Demo Trip", "Lviv",
//     new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 15),
//     "Демонстраційна поїздка для перевірки ADO.NET CRUD.", token);
// Console.WriteLine($"СТВОРЕНО поїздку: id={tripId}");

// var trip = await crud.GetTripAsync(tripId, token);
// Console.WriteLine($"ПРОЧИТАНО поїздку: {trip?.TripName}, власник={trip?.OwnerName}");

// // ОНОВЛЕННЯ поїздки з перевіркою RowVersion для конкурентних змін
// if (trip is not null)
// {
//     var newRowVersion = await crud.UpdateTripAsync(
//         tripId, userId, 1, "CRUD Demo Trip Updated", "Lviv",
//         new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 15),
//         "Оновлений опис демонстраційної поїздки.", trip.RowVersion, token);
//     Console.WriteLine($"Поїздку оновлено: {newRowVersion is not null}");
// }

// // ВИДАЛЕННЯ тестової поїздки та користувача
// Console.WriteLine($"Поїздку видалено: {await crud.DeleteTripAsync(tripId, token)}");
// Console.WriteLine($"Користувача видалено: {await crud.DeleteUserAsync(userId, token)}");


// Пункт 5 — складні SQL-запити: JOIN для поїздок і маршруту та GROUP BY для статистики бронювань.
// Реалізація запитів: Data/SqlComplexQueries.cs; запуск і вивід результатів: Data/ComplexQueryDemo.cs.
// var complexQueries = scope.ServiceProvider.GetRequiredService<SqlComplexQueries>();
// await ComplexQueryDemo.RunAsync(complexQueries, CancellationToken.None);

// Пункт 6 — ADO.NET транзакції: успішний COMMIT і ROLLBACK після порушення обмеження дат.
// Реалізація сценаріїв: Data/SqlTransactionDemo.cs.
// await SqlTransactionDemo.RunAsync(connectionString, CancellationToken.None);


// Пункт 7 — перегляд моделі EF Core: таблиці, первинні ключі, унікальні індекси та зовнішні ключі.
// Реалізація: Data/EfModelDemo.cs; налаштування сутностей: Data/EntityConfigurations.cs.
// EfModelDemo.Print(db.Model);

// Пункт 8 — EF Core CRUD і складна транзакційна операція створення графа поїздки.
// Реалізація обох сценаріїв: Data/EfCrudBusinessDemo.cs.
// await EfCrudBusinessDemo.RunAsync(db, CancellationToken.None);

// Пункт 9 — LINQ-запити EF Core: фільтрація, сортування, проєкція, групування та пагінація.
// Реалізація запитів і демонстраційного виводу: Data/EfLinqQueriesDemo.cs.
// await EfLinqQueriesDemo.RunAsync(db, CancellationToken.None);

// Пункт 10 — concurrency conflict: два DbContext оновлюють один запис із rowversion.
// Реалізація сценарію: Data/EfConcurrencyDemo.cs; налаштування rowversion: Data/EntityConfigurations.cs.
// await EfConcurrencyDemo.RunAsync(connectionString, CancellationToken.None);

// Пункт 13 — SQL, який EF Core генерує з LINQ: фільтрація, проєкція, зв’язки та групування.
// Реалізація: Data/EfQuerySqlDemo.cs; SQL отримується через ToQueryString().
//EfQuerySqlDemo.Run(db);

// Потрібно для посилання на тип Program під час налаштування User Secrets.
public partial class Program;
