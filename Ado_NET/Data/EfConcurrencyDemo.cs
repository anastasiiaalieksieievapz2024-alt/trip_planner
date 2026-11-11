using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>Demonstrates optimistic concurrency using SQL Server rowversion.</summary>
public static class EfConcurrencyDemo
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<KpzDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        Console.WriteLine();
        Console.WriteLine("=== EF CORE OPTIMISTIC CONCURRENCY DEMO ===");

        int demoTripId;

        // Create a dedicated temporary row so the demonstration does not change sample data.
        await using (var setupDb = new KpzDbContext(options))
        {
            var template = await setupDb.Trips
                .AsNoTracking()
                .OrderBy(trip => trip.TripId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No trip exists to use as a concurrency demo template.");

            var demoTrip = new Trip
            {
                OwnerId = template.OwnerId,
                TripCategoryId = template.TripCategoryId,
                TripName = $"Concurrency Demo {Guid.NewGuid():N}",
                Destination = template.Destination,
                StartDate = template.StartDate,
                EndDate = template.EndDate,
                Description = "Temporary row for the optimistic concurrency demonstration."
            };

            setupDb.Trips.Add(demoTrip);
            await setupDb.SaveChangesAsync(cancellationToken);
            demoTripId = demoTrip.TripId;
        }

        try
        {
            await using var firstDb = new KpzDbContext(options);
            await using var secondDb = new KpzDbContext(options);

            // Both contexts read the same rowversion before either one saves.
            var firstCopy = await firstDb.Trips.SingleAsync(
                trip => trip.TripId == demoTripId, cancellationToken);
            var secondCopy = await secondDb.Trips.SingleAsync(
                trip => trip.TripId == demoTripId, cancellationToken);

            Console.WriteLine($"Both contexts loaded TripId={demoTripId} with the same rowversion.");

            firstCopy.Description = "Updated by the first DbContext.";
            await firstDb.SaveChangesAsync(cancellationToken);
            Console.WriteLine("First DbContext saved successfully; SQL Server generated a new rowversion.");

            secondCopy.Description = "Conflicting update from the second DbContext.";

            try
            {
                await secondDb.SaveChangesAsync(cancellationToken);
                Console.WriteLine("Unexpected: the second update succeeded without a concurrency conflict.");
            }
            catch (DbUpdateConcurrencyException exception)
            {
                Console.WriteLine("Concurrency conflict detected: another context changed this trip first.");

                var entry = exception.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues is null)
                {
                    Console.WriteLine("The trip was deleted by another operation; the attempted update was discarded.");
                }
                else
                {
                    var currentTrip = (Trip)databaseValues.ToObject();
                    Console.WriteLine($"Current description in database: {currentTrip.Description}");
                    Console.WriteLine("The stale second update was not applied. Reload the latest values before retrying.");
                }
            }
        }
        finally
        {
            // Remove only this run's temporary row, including when the demo is cancelled or fails.
            await using var cleanupDb = new KpzDbContext(options);
            var demoTrip = await cleanupDb.Trips.FindAsync([demoTripId], cancellationToken);
            if (demoTrip is not null)
            {
                cleanupDb.Trips.Remove(demoTrip);
                await cleanupDb.SaveChangesAsync(cancellationToken);
            }

            Console.WriteLine("Temporary concurrency demo trip was removed.");
        }
    }
}
