using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>Виводить SQL, згенерований EF Core для типових LINQ-запитів.</summary>
public static class EfQuerySqlDemo
{
    public static void Run(KpzDbContext db)
    {
        Console.WriteLine();
        Console.WriteLine("=== SQL, ЗГЕНЕРОВАНИЙ EF CORE З LINQ ===");

        var filteredTrips = db.Trips
            .AsNoTracking()
            .Where(trip => trip.TripCategoryId == 1 &&
                           trip.Destination.Contains("Kyiv") &&
                           trip.StartDate >= new DateOnly(2026, 10, 1) &&
                           trip.StartDate < new DateOnly(2027, 3, 1))
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId);

        PrintQuery(
            "1. Фільтрація та сортування",
            "FROM Trips; WHERE фільтрує категорію, напрямок і дати; ORDER BY сортує за датою та ключем.",
            filteredTrips.ToQueryString());

        var tripList = db.Trips
            .AsNoTracking()
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .Select(trip => new TripListDto(
                trip.TripId,
                trip.TripName,
                trip.Destination,
                trip.StartDate,
                trip.Owner.FullName,
                trip.Category.Name,
                trip.ItineraryItems.Count));

        PrintQuery(
            "2. Проєкція DTO зі значеннями пов’язаних сутностей",
            "FROM Trips; JOIN з Users і TripCategories; підрахунок ItineraryItems; вибір лише полів DTO; сортування за датою та ключем.",
            tripList.ToQueryString());

        var relatedTrips = db.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(trip => trip.Owner)
            .Include(trip => trip.Category)
            .Include(trip => trip.ItineraryItems)
                .ThenInclude(item => item.Type)
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId);

        PrintQuery(
            "3. Граф пов’язаних сутностей із AsSplitQuery",
            "Завантажує Trips разом із Users, TripCategories, ItineraryItems та ItineraryItemTypes. EF надсилає фіксований набір SELECT, а не запит на кожну поїздку.",
            relatedTrips.ToQueryString());

        var bookingGroups = db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Item.Trip.StartDate >= new DateOnly(2026, 10, 1) &&
                              booking.Item.Trip.StartDate < new DateOnly(2027, 3, 1))
            .GroupBy(booking => booking.Item.Trip.Category.Name)
            .Select(group => new
            {
                CategoryName = group.Key,
                BookingCount = group.Count(),
                TotalPrice = group.Sum(booking => booking.Price)
            })
            .OrderByDescending(group => group.TotalPrice)
            .ThenBy(group => group.CategoryName);

        PrintQuery(
            "4. Групування з агрегатними функціями",
            "FROM Bookings із JOIN через ItineraryItems і Trips до TripCategories; фільтр дат; GROUP BY категорії; COUNT, SUM і сортування за сумою.",
            bookingGroups.ToQueryString());

        Console.WriteLine("Перевірка N+1: запити транслюються до виконання; DTO використовує JOIN/підзапит, граф сутностей — фіксовану кількість розділених запитів.");
        Console.WriteLine("У прикладах цикл не надсилає окремий запит до бази для кожної поїздки.");
    }

    private static void PrintQuery(string title, string explanation, string sql)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine($"Пояснення: {explanation}");
        Console.WriteLine(sql);
    }
}
