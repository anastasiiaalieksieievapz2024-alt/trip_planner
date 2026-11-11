using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>Demonstrates EF Core CRUD for three entities and an atomic operation over a trip graph.</summary>
public static class EfCrudBusinessDemo
{
    public static async Task RunAsync(KpzDbContext db, CancellationToken cancellationToken)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"ef.demo.{suffix}@example.test";
        var userId = 0;
        var standaloneTripId = 0;
        var graphTripId = 0;

        Console.WriteLine();
        Console.WriteLine("=== EF CORE CRUD DEMO ===");

        try
        {
            var user = await CreateUserAsync(db, email, cancellationToken);
            userId = user.UserId;
            Console.WriteLine($"CREATE User: id={user.UserId}");

            var readUser = await ReadUserAsync(db, user.UserId, cancellationToken);
            Console.WriteLine($"READ User: {readUser?.FullName} | {readUser?.Email}");

            var userUpdated = await UpdateUserAsync(db, user.UserId, cancellationToken);
            Console.WriteLine($"UPDATE User: {userUpdated}");

            var trip = await CreateTripAsync(db, user.UserId, "EF CRUD Trip", cancellationToken);
            standaloneTripId = trip.TripId;
            Console.WriteLine($"CREATE Trip: id={trip.TripId}");

            var readTrip = await ReadTripAsync(db, trip.TripId, cancellationToken);
            Console.WriteLine($"READ Trip: {readTrip?.TripName} | {readTrip?.Destination}");

            var tripUpdated = await UpdateTripAsync(db, trip.TripId, cancellationToken);
            Console.WriteLine($"UPDATE Trip: {tripUpdated}");

            var item = await CreateItineraryItemAsync(db, trip.TripId, cancellationToken);
            Console.WriteLine($"CREATE ItineraryItem: id={item.ItemId}");

            var readItem = await ReadItineraryItemAsync(db, item.ItemId, cancellationToken);
            Console.WriteLine($"READ ItineraryItem: {readItem?.Title}");

            var itemUpdated = await UpdateItineraryItemAsync(db, item.ItemId, cancellationToken);
            Console.WriteLine($"UPDATE ItineraryItem: {itemUpdated}");

            var itemDeleted = await DeleteItineraryItemAsync(db, item.ItemId, cancellationToken);
            Console.WriteLine($"DELETE ItineraryItem: {itemDeleted}");

            var standaloneTripDeleted = await DeleteTripAsync(db, standaloneTripId, cancellationToken);
            standaloneTripId = 0;
            Console.WriteLine($"DELETE Trip: {standaloneTripDeleted}");

            graphTripId = await CreateTripGraphInTransactionAsync(db, user.UserId, cancellationToken);
            var graph = await ReadTripGraphAsync(db, graphTripId, cancellationToken);
            var bookingCount = graph?.ItineraryItems.Sum(routeItem => routeItem.Bookings.Count) ?? 0;

            Console.WriteLine();
            Console.WriteLine("=== EF CORE BUSINESS OPERATION: CREATE TRIP GRAPH ===");
            Console.WriteLine(
                $"Committed Trip #{graphTripId}: members={graph?.Members.Count ?? 0}, " +
                $"itinerary items={graph?.ItineraryItems.Count ?? 0}, bookings={bookingCount}, " +
                $"documents={graph?.Documents.Count ?? 0}.");

            var graphTripDeleted = await DeleteTripAsync(db, graphTripId, cancellationToken);
            graphTripId = 0;
            Console.WriteLine($"Demo graph cleanup (cascade delete): {graphTripDeleted}");

            var userDeleted = await DeleteUserAsync(db, user.UserId, cancellationToken);
            userId = 0;
            Console.WriteLine($"DELETE User: {userDeleted}");
        }
        finally
        {
            // Best-effort cleanup if the demo is interrupted after creating its temporary records.
            if (graphTripId != 0)
            {
                await DeleteTripAsync(db, graphTripId, cancellationToken);
            }

            if (standaloneTripId != 0)
            {
                await DeleteTripAsync(db, standaloneTripId, cancellationToken);
            }

            if (userId != 0)
            {
                await DeleteUserAsync(db, userId, cancellationToken);
            }
        }
    }

    private static async Task<User> CreateUserAsync(KpzDbContext db, string email, CancellationToken cancellationToken)
    {
        var user = new User
        {
            FullName = "EF CRUD Demo User",
            Email = email,
            PasswordHash = "DEMO_ONLY_HASH",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static Task<User?> ReadUserAsync(KpzDbContext db, int userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);

    private static async Task<bool> UpdateUserAsync(KpzDbContext db, int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.FullName = "EF CRUD Demo User Updated";
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> DeleteUserAsync(KpzDbContext db, int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<Trip> CreateTripAsync(
        KpzDbContext db,
        int ownerId,
        string tripName,
        CancellationToken cancellationToken)
    {
        var trip = new Trip
        {
            OwnerId = ownerId,
            TripCategoryId = 1,
            TripName = tripName,
            Destination = "Lviv",
            StartDate = new DateOnly(2030, 4, 1),
            EndDate = new DateOnly(2030, 4, 4),
            Description = "Temporary EF Core CRUD demonstration record."
        };

        db.Trips.Add(trip);
        await db.SaveChangesAsync(cancellationToken);
        return trip;
    }

    private static Task<Trip?> ReadTripAsync(KpzDbContext db, int tripId, CancellationToken cancellationToken) =>
        db.Trips.AsNoTracking().FirstOrDefaultAsync(trip => trip.TripId == tripId, cancellationToken);

    private static async Task<bool> UpdateTripAsync(KpzDbContext db, int tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(trip => trip.TripId == tripId, cancellationToken);
        if (trip is null)
        {
            return false;
        }

        trip.TripName = "EF CRUD Trip Updated";
        trip.Description = "Updated through EF Core.";
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> DeleteTripAsync(KpzDbContext db, int tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(trip => trip.TripId == tripId, cancellationToken);
        if (trip is null)
        {
            return false;
        }

        db.Trips.Remove(trip);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<ItineraryItem> CreateItineraryItemAsync(
        KpzDbContext db,
        int tripId,
        CancellationToken cancellationToken)
    {
        var item = new ItineraryItem
        {
            TripId = tripId,
            ItineraryItemTypeId = 4,
            Title = "EF CRUD Demo Activity",
            Description = "Temporary activity used to demonstrate EF Core CRUD.",
            StartDateTime = new DateTime(2030, 4, 1, 10, 0, 0),
            EndDateTime = new DateTime(2030, 4, 1, 12, 0, 0),
            Address = "Lviv center",
            Latitude = 49.839700m,
            Longitude = 24.029700m
        };

        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    private static Task<ItineraryItem?> ReadItineraryItemAsync(
        KpzDbContext db,
        int itemId,
        CancellationToken cancellationToken) =>
        db.ItineraryItems.AsNoTracking().FirstOrDefaultAsync(item => item.ItemId == itemId, cancellationToken);

    private static async Task<bool> UpdateItineraryItemAsync(
        KpzDbContext db,
        int itemId,
        CancellationToken cancellationToken)
    {
        var item = await db.ItineraryItems.FirstOrDefaultAsync(item => item.ItemId == itemId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.Title = "EF CRUD Demo Activity Updated";
        item.Description = "Updated through EF Core.";
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> DeleteItineraryItemAsync(
        KpzDbContext db,
        int itemId,
        CancellationToken cancellationToken)
    {
        var item = await db.ItineraryItems.FirstOrDefaultAsync(item => item.ItemId == itemId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        db.ItineraryItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<int> CreateTripGraphInTransactionAsync(
        KpzDbContext db,
        int ownerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var trip = new Trip
            {
                OwnerId = ownerId,
                TripCategoryId = 4,
                TripName = "EF Business Graph Demo",
                Destination = "Prague",
                StartDate = new DateOnly(2030, 5, 10),
                EndDate = new DateOnly(2030, 5, 14),
                Description = "Created as one related entity graph in an explicit transaction."
            };

            trip.Members.Add(new TripMember
            {
                Trip = trip,
                UserId = ownerId,
                Role = "Owner",
                JoinedAt = DateTime.UtcNow
            });

            var museumVisit = new ItineraryItem
            {
                Trip = trip,
                ItineraryItemTypeId = 4,
                Title = "Museum visit",
                Description = "Business operation sample activity.",
                StartDateTime = new DateTime(2030, 5, 11, 10, 0, 0),
                EndDateTime = new DateTime(2030, 5, 11, 12, 0, 0),
                Address = "Prague city center",
                Latitude = 50.087000m,
                Longitude = 14.421000m
            };
            museumVisit.Bookings.Add(new Booking
            {
                Item = museumVisit,
                BookingNumber = $"EF-GRAPH-{Guid.NewGuid():N}"[..20],
                Provider = "Demo Museum",
                BookingDate = DateTime.UtcNow,
                Price = 25.00m,
                Status = "Confirmed",
                ConfirmationDetails = "Created in the EF Core business transaction."
            });

            var walkingTour = new ItineraryItem
            {
                Trip = trip,
                ItineraryItemTypeId = 4,
                Title = "Walking tour",
                Description = "Second sample activity in the trip graph.",
                StartDateTime = new DateTime(2030, 5, 12, 9, 0, 0),
                EndDateTime = new DateTime(2030, 5, 12, 11, 0, 0),
                Address = "Prague old town",
                Latitude = 50.087000m,
                Longitude = 14.421000m
            };

            trip.ItineraryItems.Add(museumVisit);
            trip.ItineraryItems.Add(walkingTour);
            trip.Documents.Add(new TripDocument
            {
                Trip = trip,
                DocumentName = "Demo travel ticket",
                DocumentType = "Ticket",
                FilePath = "demo/ef-graph/ticket.pdf",
                IsRequired = true,
                ExpirationDate = new DateOnly(2031, 1, 1)
            });

            db.Trips.Add(trip);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return trip.TripId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static Task<Trip?> ReadTripGraphAsync(
        KpzDbContext db,
        int tripId,
        CancellationToken cancellationToken) =>
        db.Trips
            .AsNoTracking()
            .Include(trip => trip.Members)
            .Include(trip => trip.ItineraryItems)
                .ThenInclude(item => item.Bookings)
            .Include(trip => trip.Documents)
            .FirstOrDefaultAsync(trip => trip.TripId == tripId, cancellationToken);
}
