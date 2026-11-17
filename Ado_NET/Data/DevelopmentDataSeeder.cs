using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>
/// Реєструє та створює повторювані демонстраційні дані. Довідники задаються через HasData;
/// цей сідер після міграцій додає користувачів, поїздки та пов’язані тестові записи.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string MarkerEmail = "demo.user01@example.test";

    /// <summary>Реєструє обидва способи заповнення, оскільки EF Tools і застосунок можуть викликати різні шляхи.</summary>
    public static DbContextOptionsBuilder UseDevelopmentDataSeeding(this DbContextOptionsBuilder options)
    {
        return options
            .UseSeeding((context, _) => Seed(context))
            .UseAsyncSeeding((context, _, cancellationToken) => SeedAsync(context, cancellationToken));
    }

    private static void Seed(DbContext context)
    {
        if (context.Set<User>().Any(user => user.Email == MarkerEmail))
        {
            return;
        }

        AddSampleRecords(context);
        context.SaveChanges();
    }

    private static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<User>().AnyAsync(user => user.Email == MarkerEmail, cancellationToken))
        {
            return;
        }

        AddSampleRecords(context);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddSampleRecords(DbContext context)
    {
        var users = Enumerable.Range(1, 10)
            .Select(id => new User
            {
                FullName = $"Демо-мандрівник {id:D2}",
                Email = $"demo.user{id:D2}@example.test",
                // Це тестове значення; лабораторна не використовує його для автентифікації.
                PasswordHash = $"DEMO_ONLY_HASH_{id:D2}",
                CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc).AddDays(id)
            })
            .ToList();

        var trips = Enumerable.Range(0, 10)
            .Select(index =>
            {
                var id = index + 1;
                var start = new DateOnly(2026, 10, 1).AddDays((id - 1) * 14);
                return new Trip
                {
                    Owner = users[index],
                    TripCategoryId = ((id - 1) % 5) + 1,
                    TripName = $"Демо-поїздка {id:D2}",
                    Destination = Destinations[(id - 1) % Destinations.Length],
                    StartDate = start,
                    EndDate = start.AddDays(4 + (id % 4)),
                Description = $"Демонстраційний опис поїздки №{id}; тестові дані для перевірки роботи системи."
                };
            })
            .ToList();

        // Для кожної поїздки додаємо трьох учасників із ролями власника, редактора та читача.
        var members = trips.SelectMany((trip, tripIndex) => Enumerable.Range(0, 3).Select(offset =>
        {
            var userIndex = (tripIndex + offset) % users.Count;
            return new TripMember
            {
                Trip = trip,
                User = users[userIndex],
                Role = offset switch { 0 => "Owner", 1 => "Editor", _ => "Viewer" },
                JoinedAt = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(tripIndex + 1 + offset)
            };
        })).ToList();

        // Створюємо по три демонстраційні пункти маршруту для кожної поїздки.
        var items = trips.SelectMany((trip, tripIndex) => Enumerable.Range(0, 3).Select(offset =>
        {
            var start = trip.StartDate.ToDateTime(new TimeOnly(9, 0)).AddDays(offset);
            return new ItineraryItem
            {
                Trip = trip,
                ItineraryItemTypeId = ((tripIndex + offset) % 5) + 1,
                Title = $"Демо-зупинка {tripIndex + 1:D2}-{offset + 1}",
                Description = "Демонстраційний пункт маршруту для перевірки зв’язків і запитів.",
                StartDateTime = start,
                EndDateTime = start.AddHours(2),
                Address = $"Central square {tripIndex + 1}",
                Latitude = 48.000000m + (tripIndex + 1) / 100m,
                Longitude = 30.000000m + offset / 100m
            };
        })).ToList();

        // Бронювання прив’язуємо до перших двадцяти тестових пунктів маршруту.
        var bookings = Enumerable.Range(1, 20)
            .Select(id => new Booking
            {
                Item = items[id - 1],
                BookingNumber = $"DEMO-{id:D5}",
                Provider = Providers[(id - 1) % Providers.Length],
                BookingDate = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(id),
                Price = 45.50m + (id * 17.25m),
                Status = BookingStatuses[(id - 1) % BookingStatuses.Length],
                ConfirmationDetails = $"Demo confirmation {id:D2}"
            })
            .ToList();

        var documents = trips.Select((trip, index) => new TripDocument
        {
            Trip = trip,
            DocumentName = $"Trip {index + 1:D2} travel document",
            DocumentType = DocumentTypes[index % DocumentTypes.Length],
            FilePath = $"demo/trips/{index + 1:D2}/document.pdf",
            IsRequired = (index + 1) % 2 == 0,
            ExpirationDate = (index + 1) % 3 == 0 ? null : new DateOnly(2028, 12, 31)
        }).ToList();

        context.AddRange(users);
        context.AddRange(trips);
        context.AddRange(members);
        context.AddRange(items);
        context.AddRange(bookings);
        context.AddRange(documents);
    }

    private static readonly string[] Destinations = ["Kyiv", "Lviv", "Odesa", "Prague", "Rome"];
    private static readonly string[] Providers = ["SkyWays", "StayEasy", "GoTransfer", "CityTours"];
    private static readonly string[] BookingStatuses = ["Pending", "Confirmed", "Cancelled"];
    private static readonly string[] DocumentTypes = ["Ticket", "Insurance", "Passport copy", "Visa"];
}
