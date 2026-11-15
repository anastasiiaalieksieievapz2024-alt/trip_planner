using Kpz.Lab2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ������������ ���� ��������� ����������� �� ��������� DI ��� ����������� ����������.
var builder = Host.CreateApplicationBuilder(args);
// User Secrets ���� ������������� ������������ � appsettings.json ��� ���������� ������� � ���������.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// ����� ���������� �������� � ConnectionStrings:KpzDb.
var connectionString = builder.Configuration.GetConnectionString("KpzDb")
    ?? throw new InvalidOperationException("Connection string KpzDb is missing.");

// �������� �������� EF Core �� SQL Server ��������� � ��������� �����������.
builder.Services.AddDbContext<KpzDbContext>(options =>
    options.UseSqlServer(connectionString).UseDevelopmentDataSeeding());
// �������� ADO.NET CRUD ������ �� EF Core, �������������� ��� ����� ����� ����������.
builder.Services.AddSingleton(new SqlCrudRepository(connectionString));
// ������� ����� � ��������� ADO.NET JOIN �� ����������� ��������.
builder.Services.AddSingleton(new SqlComplexQueries(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<KpzDbContext>();
// ����������� �� ������������ ������� �� ��� ������� ��������.
await db.Database.MigrateAsync();

///////////////4 point CRUD demo///////////////////
// var crud = scope.ServiceProvider.GetRequiredService<SqlCrudRepository>();
// var token = CancellationToken.None;
// var email = $"crud.demo.{Guid.NewGuid():N}@example.test";

// // CREATE �� READ �����������
// var userId = await crud.CreateUserAsync(
//     "CRUD Demo User", email, "DEMO_ONLY_HASH", token);
// Console.WriteLine($"CREATE User: id={userId}");

// var user = await crud.GetUserAsync(userId, token);
// Console.WriteLine($"READ User: {user?.FullName}, {user?.Email}");

// // UPDATE �����������
// var userUpdated = await crud.UpdateUserAsync(
//     userId, "CRUD Demo User Updated", email, "DEMO_ONLY_HASH", token);
// Console.WriteLine($"UPDATE User: {userUpdated}");

// // CREATE �� READ �������
// var tripId = await crud.CreateTripAsync(
//     userId, 1, "CRUD Demo Trip", "Lviv",
//     new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 15),
//     "�������� �����", token);
// Console.WriteLine($"CREATE Trip: id={tripId}");

// var trip = await crud.GetTripAsync(tripId, token);
// Console.WriteLine($"READ Trip: {trip?.TripName}, owner={trip?.OwnerName}");

// // UPDATE ������� ����������� RowVersion � ����������� ������
// if (trip is not null)
// {
//     var newRowVersion = await crud.UpdateTripAsync(
//         tripId, userId, 1, "CRUD Demo Trip Updated", "Lviv",
//         new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 15),
//         "��������� �������� �����", trip.RowVersion, token);
//     Console.WriteLine($"UPDATE Trip: {newRowVersion is not null}");
// }

// // DELETE �������, ���� �����������
// Console.WriteLine($"DELETE Trip: {await crud.DeleteTripAsync(tripId, token)}");
// Console.WriteLine($"DELETE User: {await crud.DeleteUserAsync(userId, token)}");


/////////////////5 point  ////////////////////
// var complexQueries = scope.ServiceProvider.GetRequiredService<SqlComplexQueries>();
// await ComplexQueryDemo.RunAsync(complexQueries, CancellationToken.None);

////////////////6 point////////////////////////
//await SqlTransactionDemo.RunAsync(connectionString, CancellationToken.None);


//////////////7 point///////////////////////////
// EfModelDemo.Print(db.Model);

////////////////////8 point///////////////////
//await EfCrudBusinessDemo.RunAsync(db, CancellationToken.None);

////////////////////9 point///////////////////
//await EfLinqQueriesDemo.RunAsync(db, CancellationToken.None);

////////////////////10 point//////////////////
//await EfConcurrencyDemo.RunAsync(connectionString, CancellationToken.None);

////////////////////13 point//////////////////
//EfQuerySqlDemo.Run(db);

// ������� ��� ��������� �� Program � ����������� User Secrets.
public partial class Program;
