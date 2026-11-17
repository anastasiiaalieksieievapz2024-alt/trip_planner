using System.Globalization;

namespace Kpz.Lab2.Data;

/// <summary>Виконує два складні запити ADO.NET і виводить результати для демонстрації.</summary>
public static class ComplexQueryDemo
{
    public static async Task RunAsync(SqlComplexQueries queries, CancellationToken cancellationToken)
    {
        var fromDate = new DateOnly(2026, 10, 1);
        var toDateExclusive = new DateOnly(2027, 3, 1);

        Console.WriteLine();
        Console.WriteLine("=== ЗАПИТ 1: JOIN поїздок, користувачів, категорій і пунктів маршруту ===");
        Console.WriteLine($"Фільтр: StartDate >= {fromDate:yyyy-MM-dd}, StartDate < {toDateExclusive:yyyy-MM-dd}; усі категорії.");

        var itineraryRows = await queries.GetTripsWithItineraryAsync(
            fromDate, toDateExclusive, tripCategoryId: null, cancellationToken);

        Console.WriteLine($"Повернуто рядків: {itineraryRows.Count}");
        foreach (var row in itineraryRows)
        {
            var item = row.ItemId is null
                ? "пунктів маршруту немає"
                : $"пункт={row.ItemTitle}, тип={row.ItemType}, початок={row.ItemStartDateTime:yyyy-MM-dd HH:mm}";

            Console.WriteLine(
                $"Поїздка №{row.TripId}: {row.TripName} | {row.Destination} | " +
                $"власник={row.OwnerName} | категорія={row.CategoryName} | {item}");
        }

        Console.WriteLine();
        Console.WriteLine("=== ЗАПИТ 2: ГРУПУВАННЯ ЗА КАТЕГОРІЄЮ ТА НАПРЯМКОМ ===");
        Console.WriteLine($"Фільтр: StartDate >= {fromDate:yyyy-MM-dd}, StartDate < {toDateExclusive:yyyy-MM-dd}.");
        Console.WriteLine("Агрегати: COUNT(DISTINCT TripId), COUNT(DISTINCT BookingId), SUM(Price).");

        var summaries = await queries.GetTripBookingSummaryAsync(
            fromDate, toDateExclusive, cancellationToken);

        Console.WriteLine($"Повернуто груп: {summaries.Count}");
        foreach (var row in summaries)
        {
            Console.WriteLine(
                $"{row.CategoryName} | {row.Destination} | trips={row.TripCount} | " +
                $"bookings={row.BookingCount} | total={row.TotalBookingAmount.ToString("0.00", CultureInfo.InvariantCulture)}");
        }
    }
}
