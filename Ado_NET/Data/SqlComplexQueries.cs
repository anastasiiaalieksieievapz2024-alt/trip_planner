using System.Data;
using Microsoft.Data.SqlClient;

namespace Kpz.Lab2.Data;

/// <summary>Рядок результату запиту про подорожі та їхні маршрутні пункти.</summary>
public sealed record TripItineraryRow(
    int TripId,
    string TripName,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string OwnerName,
    string CategoryName,
    int? ItemId,
    string? ItemTitle,
    string? ItemType,
    DateTime? ItemStartDateTime);

/// <summary>Підсумок кількості подорожей і бронювань за категорією та напрямком.</summary>
public sealed record TripBookingSummaryRow(
    string CategoryName,
    string Destination,
    int TripCount,
    int BookingCount,
    decimal TotalBookingAmount);

/// <summary>
/// Складні параметризовані SQL-запити для демонстрації JOIN та GROUP BY через ADO.NET.
/// </summary>
public sealed class SqlComplexQueries(string connectionString)
{
    private readonly string _connectionString = connectionString;

    /// <summary>
    /// Повертає подорожі з власниками, категоріями та пунктами маршруту.
    /// Фільтрує за періодом початку подорожі й необов'язково за категорією;
    /// результат сортує за датою, назвою подорожі та часом пункту маршруту.
    /// </summary>
    public async Task<IReadOnlyList<TripItineraryRow>> GetTripsWithItineraryAsync(
        DateOnly fromDate,
        DateOnly toDateExclusive,
        int? tripCategoryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT t.TripId, t.TripName, t.Destination, t.StartDate, t.EndDate,
                   u.FullName AS OwnerName, c.Name AS CategoryName,
                   i.ItemId, i.Title AS ItemTitle, itemType.Name AS ItemType,
                   i.StartDateTime AS ItemStartDateTime
            FROM dbo.Trips AS t
            INNER JOIN dbo.Users AS u ON u.UserId = t.OwnerId
            INNER JOIN dbo.TripCategories AS c ON c.TripCategoryId = t.TripCategoryId
            LEFT JOIN dbo.ItineraryItems AS i ON i.TripId = t.TripId
            LEFT JOIN dbo.ItineraryItemTypes AS itemType
                ON itemType.ItineraryItemTypeId = i.ItineraryItemTypeId
            WHERE t.StartDate >= @FromDate
              AND t.StartDate < @ToDateExclusive
              AND (@TripCategoryId IS NULL OR t.TripCategoryId = @TripCategoryId)
            ORDER BY t.StartDate, t.TripName, i.StartDateTime;
            """;

        var rows = new List<TripItineraryRow>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@ToDateExclusive", SqlDbType.Date).Value = toDateExclusive.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@TripCategoryId", SqlDbType.Int).Value = (object?)tripCategoryId ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TripItineraryRow(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                DateOnly.FromDateTime(reader.GetDateTime(3)),
                DateOnly.FromDateTime(reader.GetDateTime(4)),
                reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetInt32(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetDateTime(10)));
        }

        return rows;
    }

    /// <summary>
    /// Groups trips by category and destination and calculates trip count,
    /// booking count, and total booked amount for the requested period.
    /// </summary>
    public async Task<IReadOnlyList<TripBookingSummaryRow>> GetTripBookingSummaryAsync(
        DateOnly fromDate,
        DateOnly toDateExclusive,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.Name AS CategoryName,
                   t.Destination,
                   COUNT(DISTINCT t.TripId) AS TripCount,
                   COUNT(DISTINCT b.BookingId) AS BookingCount,
                   COALESCE(SUM(b.Price), 0) AS TotalBookingAmount
            FROM dbo.Trips AS t
            INNER JOIN dbo.TripCategories AS c ON c.TripCategoryId = t.TripCategoryId
            LEFT JOIN dbo.ItineraryItems AS i ON i.TripId = t.TripId
            LEFT JOIN dbo.Bookings AS b ON b.ItemId = i.ItemId
            WHERE t.StartDate >= @FromDate
              AND t.StartDate < @ToDateExclusive
            GROUP BY c.Name, t.Destination
            ORDER BY TotalBookingAmount DESC, c.Name, t.Destination;
            """;

        var rows = new List<TripBookingSummaryRow>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@ToDateExclusive", SqlDbType.Date).Value = toDateExclusive.ToDateTime(TimeOnly.MinValue);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TripBookingSummaryRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetDecimal(4)));
        }

        return rows;
    }
}
