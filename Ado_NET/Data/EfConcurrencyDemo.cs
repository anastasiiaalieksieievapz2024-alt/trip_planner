using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>Демонструє оптимістичний контроль конкурентності за допомогою SQL Server rowversion.</summary>
public static class EfConcurrencyDemo
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<KpzDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        Console.WriteLine();
        Console.WriteLine("=== ОПТИМІСТИЧНИЙ КОНТРОЛЬ КОНКУРЕНТНОСТІ EF CORE ===");

        int demoTripId;

        // Створюємо окремий тимчасовий запис, щоб не змінювати початкові демонстраційні дані.
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

            Console.WriteLine($"Обидва контексти завантажили TripId={demoTripId} з однаковим rowversion.");

            firstCopy.Description = "Updated by the first DbContext.";
            await firstDb.SaveChangesAsync(cancellationToken);
            Console.WriteLine("Перший DbContext зберіг зміни; SQL Server створив нове значення rowversion.");

            secondCopy.Description = "Conflicting update from the second DbContext.";

            try
            {
                await secondDb.SaveChangesAsync(cancellationToken);
                Console.WriteLine("Неочікувано: друге оновлення пройшло без конфлікту конкурентності.");
            }
            catch (DbUpdateConcurrencyException exception)
            {
                Console.WriteLine("Виявлено конфлікт конкурентності: інший контекст раніше змінив цю поїздку.");

                var entry = exception.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues is null)
                {
                    Console.WriteLine("Поїздку видалила інша операція; спробу оновлення скасовано.");
                }
                else
                {
                    var currentTrip = (Trip)databaseValues.ToObject();
                    Console.WriteLine($"Поточний опис у базі даних: {currentTrip.Description}");
                    Console.WriteLine("Застаріле оновлення не застосовано. Перед повтором потрібно завантажити актуальні дані.");
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

            Console.WriteLine("Тимчасову поїздку для демонстрації конкурентності видалено.");
        }
    }
}
