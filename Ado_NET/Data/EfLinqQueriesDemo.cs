using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

public sealed record TripListDto(
    int TripId,
    string TripName,
    string Destination,
    DateOnly StartDate,
    string OwnerName,
    string CategoryName,
    int ItineraryItemCount);

public sealed record CategoryBookingStatsDto(
    string CategoryName,
    int BookingCount,
    decimal TotalBookingAmount);

public sealed record TripPageResult(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<Trip> Items);

/// <summary>Examples of six EF Core LINQ query patterns required by the lab.</summary>
public static class EfLinqQueriesDemo
{
    // 1. Multi-parameter filtering; AsNoTracking is appropriate because returned entities are read-only.
    public static async Task<List<Trip>> FilterTripsAsync(
        KpzDbContext db,
        int? categoryId,
        string? destinationContains,
        DateOnly? startsOnOrAfter,
        DateOnly? startsBefore,
        CancellationToken cancellationToken)
    {
        var query = db.Trips.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(trip => trip.TripCategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(destinationContains))
        {
            query = query.Where(trip => trip.Destination.Contains(destinationContains));
        }

        if (startsOnOrAfter.HasValue)
        {
            query = query.Where(trip => trip.StartDate >= startsOnOrAfter.Value);
        }

        if (startsBefore.HasValue)
        {
            query = query.Where(trip => trip.StartDate < startsBefore.Value);
        }

        return await query
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .ToListAsync(cancellationToken);
    }

    // 2. Sorting by multiple columns with a stable unique-key tie-breaker.
    public static Task<List<Trip>> SortTripsAsync(KpzDbContext db, CancellationToken cancellationToken) =>
        db.Trips
            .AsNoTracking()
            .OrderByDescending(trip => trip.StartDate)
            .ThenBy(trip => trip.TripName)
            .ThenBy(trip => trip.TripId)
            .ToListAsync(cancellationToken);

    // 3. DTO projection selects only the fields needed by a list view.
    public static Task<List<TripListDto>> ProjectTripsToDtoAsync(
        KpzDbContext db,
        CancellationToken cancellationToken) =>
        db.Trips
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .Select(trip => new TripListDto(
                trip.TripId,
                trip.TripName,
                trip.Destination,
                trip.StartDate,
                trip.Owner.FullName,
                trip.Category.Name,
                trip.ItineraryItems.Count))
            .ToListAsync(cancellationToken);

    // 4. Related data is loaded explicitly; AsSplitQuery avoids a cartesian product for collections.
    public static Task<List<Trip>> GetTripsWithRelatedDataAsync(
        KpzDbContext db,
        CancellationToken cancellationToken) =>
        db.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(trip => trip.Owner)
            .Include(trip => trip.Category)
            .Include(trip => trip.ItineraryItems)
                .ThenInclude(item => item.Type)
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .ToListAsync(cancellationToken);

    // 5. Group bookings by trip category and aggregate booking count and total price.
    public static async Task<List<CategoryBookingStatsDto>> GroupBookingsByCategoryAsync(
        KpzDbContext db,
        DateOnly startsOnOrAfter,
        DateOnly startsBefore,
        CancellationToken cancellationToken)
    {
        var rows = await db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Item.Trip.StartDate >= startsOnOrAfter &&
                              booking.Item.Trip.StartDate < startsBefore)
            .GroupBy(booking => booking.Item.Trip.Category.Name)
            .Select(categoryGroup => new
            {
                CategoryName = categoryGroup.Key,
                BookingCount = categoryGroup.Count(),
                TotalBookingAmount = categoryGroup.Sum(booking => booking.Price)
            })
            .OrderByDescending(row => row.TotalBookingAmount)
            .ThenBy(row => row.CategoryName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CategoryBookingStatsDto(
                row.CategoryName,
                row.BookingCount,
                row.TotalBookingAmount))
            .ToList();
    }

    // 6. Pagination uses a stable order, Skip, and Take; TotalCount is returned for page controls.
    public static async Task<TripPageResult> GetTripPageAsync(
        KpzDbContext db,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var query = db.Trips.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TripPageResult(pageNumber, pageSize, totalCount, items);
    }

    public static async Task RunAsync(KpzDbContext db, CancellationToken cancellationToken)
    {
        var fromDate = new DateOnly(2026, 10, 1);
        var toDateExclusive = new DateOnly(2027, 3, 1);

        Console.WriteLine();
        Console.WriteLine("=== EF CORE LINQ DEMO ===");

        var filtered = await FilterTripsAsync(
            db, categoryId: 1, destinationContains: "Kyiv", fromDate, toDateExclusive, cancellationToken);
        Console.WriteLine($"1. Multi-filter (category=1, destination contains Kyiv, date range): {filtered.Count} trip(s)");
        foreach (var trip in filtered)
        {
            Console.WriteLine($"   {trip.TripName} | {trip.Destination} | {trip.StartDate:yyyy-MM-dd}");
        }

        var sorted = await SortTripsAsync(db, cancellationToken);
        Console.WriteLine($"2. Sorted trips: {sorted.Count}; first={sorted.FirstOrDefault()?.TripName ?? "(none)"}");

        var projected = await ProjectTripsToDtoAsync(db, cancellationToken);
        Console.WriteLine($"3. DTO projection: {projected.Count} row(s)");
        foreach (var trip in projected.Take(3))
        {
            Console.WriteLine($"   {trip.TripName} | owner={trip.OwnerName} | category={trip.CategoryName} | items={trip.ItineraryItemCount}");
        }

        var relatedTrips = await GetTripsWithRelatedDataAsync(db, cancellationToken);
        var firstRelatedTrip = relatedTrips.FirstOrDefault();
        Console.WriteLine(
            $"4. Related data: {relatedTrips.Count} trip(s); first has " +
            $"{firstRelatedTrip?.ItineraryItems.Count ?? 0} itinerary item(s) loaded.");

        var grouped = await GroupBookingsByCategoryAsync(db, fromDate, toDateExclusive, cancellationToken);
        Console.WriteLine($"5. GroupBy category with COUNT and SUM: {grouped.Count} group(s)");
        foreach (var row in grouped)
        {
            Console.WriteLine(
                $"   {row.CategoryName} | bookings={row.BookingCount} | " +
                $"total={row.TotalBookingAmount:0.00}");
        }

        var page = await GetTripPageAsync(db, pageNumber: 1, pageSize: 5, cancellationToken);
        Console.WriteLine($"6. Pagination: page {page.PageNumber}, size {page.PageSize}, total {page.TotalCount}, returned {page.Items.Count}");
        foreach (var trip in page.Items)
        {
            Console.WriteLine($"   {trip.TripName} | {trip.Destination} | {trip.StartDate:yyyy-MM-dd}");
        }
    }
}
