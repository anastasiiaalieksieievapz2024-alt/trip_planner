using System.Globalization;

namespace Kpz.Lab2.Data;

/// <summary>Runs the two ADO.NET complex queries and prints their results for a lab demonstration.</summary>
public static class ComplexQueryDemo
{
    public static async Task RunAsync(SqlComplexQueries queries, CancellationToken cancellationToken)
    {
        var fromDate = new DateOnly(2026, 10, 1);
        var toDateExclusive = new DateOnly(2027, 3, 1);

        Console.WriteLine();
        Console.WriteLine("=== QUERY 1: JOIN Trips, Users, TripCategories, ItineraryItems, ItineraryItemTypes ===");
        Console.WriteLine($"Filter: StartDate >= {fromDate:yyyy-MM-dd}, StartDate < {toDateExclusive:yyyy-MM-dd}; all categories.");

        var itineraryRows = await queries.GetTripsWithItineraryAsync(
            fromDate, toDateExclusive, tripCategoryId: null, cancellationToken);

        Console.WriteLine($"Rows returned: {itineraryRows.Count}");
        foreach (var row in itineraryRows)
        {
            var item = row.ItemId is null
                ? "no itinerary item"
                : $"item={row.ItemTitle}, type={row.ItemType}, starts={row.ItemStartDateTime:yyyy-MM-dd HH:mm}";

            Console.WriteLine(
                $"Trip #{row.TripId}: {row.TripName} | {row.Destination} | " +
                $"owner={row.OwnerName} | category={row.CategoryName} | {item}");
        }

        Console.WriteLine();
        Console.WriteLine("=== QUERY 2: GROUP BY category and destination ===");
        Console.WriteLine($"Filter: StartDate >= {fromDate:yyyy-MM-dd}, StartDate < {toDateExclusive:yyyy-MM-dd}.");
        Console.WriteLine("Aggregates: COUNT(DISTINCT TripId), COUNT(DISTINCT BookingId), SUM(Price).");

        var summaries = await queries.GetTripBookingSummaryAsync(
            fromDate, toDateExclusive, cancellationToken);

        Console.WriteLine($"Groups returned: {summaries.Count}");
        foreach (var row in summaries)
        {
            Console.WriteLine(
                $"{row.CategoryName} | {row.Destination} | trips={row.TripCount} | " +
                $"bookings={row.BookingCount} | total={row.TotalBookingAmount.ToString("0.00", CultureInfo.InvariantCulture)}");
        }
    }
}
