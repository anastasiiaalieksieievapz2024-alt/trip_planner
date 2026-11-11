using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>
/// Registers and creates repeatable development data. Lookup rows are configured with HasData;
/// this seeder adds the users, trips, and dependent sample records after migrations.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string MarkerEmail = "demo.user01@example.test";

    /// <summary>Registers both callbacks because EF tools and the application may use different paths.</summary>
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
                FullName = $"Demo Traveler {id:D2}",
                Email = $"demo.user{id:D2}@example.test",
                // Демонстраційне значення, не є хешем для справжнього пароля.
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
                    TripName = $"Demo Trip {id:D2}",
                    Destination = Destinations[(id - 1) % Destinations.Length],
                    StartDate = start,
                    EndDate = start.AddDays(4 + (id % 4)),
                    Description = $"Тестова подорож №{id}; дані призначені для демонстрації запитів."
                };
            })
            .ToList();

        // Кожна подорож має власника та ще двох учасників; разом створюється 30 зв'язків.
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

        // Три часові пункти на кожну подорож спрощують фільтрування та групування за датою і типом.
        var items = trips.SelectMany((trip, tripIndex) => Enumerable.Range(0, 3).Select(offset =>
        {
            var start = trip.StartDate.ToDateTime(new TimeOnly(9, 0)).AddDays(offset);
            return new ItineraryItem
            {
                Trip = trip,
                ItineraryItemTypeId = ((tripIndex + offset) % 5) + 1,
                Title = $"Demo stop {tripIndex + 1:D2}-{offset + 1}",
                Description = "Тестовий пункт маршруту для перевірки зв'язків і запитів.",
                StartDateTime = start,
                EndDateTime = start.AddHours(2),
                Address = $"Central square {tripIndex + 1}",
                Latitude = 48.000000m + (tripIndex + 1) / 100m,
                Longitude = 30.000000m + offset / 100m
            };
        })).ToList();

        // Два бронювання на один пункт дають змогу перевіряти групування і сумування вартості.
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
