using System.Data;
using Microsoft.Data.SqlClient;

namespace Kpz.Lab2.Data;

/// <summary>Користувач, прочитаний безпосередньо через SqlDataReader.</summary>
public sealed record UserRecord(
    int UserId,
    string FullName,
    string Email,
    string PasswordHash,
    DateTime CreatedAt);

/// <summary>Подорож разом із власником і назвою її категорії.</summary>
public sealed record TripRecord(
    int TripId,
    int OwnerId,
    int TripCategoryId,
    string TripName,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Description,
    byte[] RowVersion,
    string OwnerName,
    string CategoryName);

/// <summary>
/// Асинхронний ADO.NET CRUD для пов'язаних таблиць dbo.Users і dbo.Trips.
/// Усі значення передаються як SQL-параметри, а читання виконується через SqlDataReader.
/// </summary>
public sealed class SqlCrudRepository(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public async Task<int> CreateUserAsync(
        string fullName,
        string email,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Users (FullName, Email, PasswordHash)
            OUTPUT INSERTED.UserId
            VALUES (@FullName, @Email, @PasswordHash);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = fullName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email;
        command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 512).Value = passwordHash;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UserId, FullName, Email, PasswordHash, CreatedAt
            FROM dbo.Users
            WHERE UserId = @UserId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserRecord(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetDateTime(4));
    }

    public async Task<bool> UpdateUserAsync(
        int userId,
        string fullName,
        string email,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Users
            SET FullName = @FullName,
                Email = @Email,
                PasswordHash = @PasswordHash
            WHERE UserId = @UserId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = fullName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email;
        command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 512).Value = passwordHash;

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.Users WHERE UserId = @UserId;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<int> CreateTripAsync(
        int ownerId,
        int tripCategoryId,
        string tripName,
        string destination,
        DateOnly startDate,
        DateOnly endDate,
        string? description,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Trips
                (OwnerId, TripCategoryId, TripName, Destination, StartDate, EndDate, Description)
            OUTPUT INSERTED.TripId
            VALUES
                (@OwnerId, @TripCategoryId, @TripName, @Destination, @StartDate, @EndDate, @Description);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@OwnerId", SqlDbType.Int).Value = ownerId;
        command.Parameters.Add("@TripCategoryId", SqlDbType.Int).Value = tripCategoryId;
        command.Parameters.Add("@TripName", SqlDbType.NVarChar, 150).Value = tripName;
        command.Parameters.Add("@Destination", SqlDbType.NVarChar, 200).Value = destination;
        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = (object?)description ?? DBNull.Value;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<TripRecord?> GetTripAsync(int tripId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT t.TripId, t.OwnerId, t.TripCategoryId, t.TripName, t.Destination,
                   t.StartDate, t.EndDate, t.Description, t.RowVersion,
                   u.FullName AS OwnerName, c.Name AS CategoryName
            FROM dbo.Trips AS t
            INNER JOIN dbo.Users AS u ON u.UserId = t.OwnerId
            INNER JOIN dbo.TripCategories AS c ON c.TripCategoryId = t.TripCategoryId
            WHERE t.TripId = @TripId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TripRecord(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            DateOnly.FromDateTime(reader.GetDateTime(5)),
            DateOnly.FromDateTime(reader.GetDateTime(6)),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.GetFieldValue<byte[]>(8),
            reader.GetString(9),
            reader.GetString(10));
    }

    /// <summary>
    /// Updates a trip only if expectedRowVersion still matches. Returns the new rowversion,
    /// or null if the trip was deleted or changed by another writer.
    /// </summary>
    public async Task<byte[]?> UpdateTripAsync(
        int tripId,
        int ownerId,
        int tripCategoryId,
        string tripName,
        string destination,
        DateOnly startDate,
        DateOnly endDate,
        string? description,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Trips
            SET OwnerId = @OwnerId,
                TripCategoryId = @TripCategoryId,
                TripName = @TripName,
                Destination = @Destination,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Description = @Description
            OUTPUT INSERTED.RowVersion
            WHERE TripId = @TripId AND RowVersion = @ExpectedRowVersion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
        command.Parameters.Add("@OwnerId", SqlDbType.Int).Value = ownerId;
        command.Parameters.Add("@TripCategoryId", SqlDbType.Int).Value = tripCategoryId;
        command.Parameters.Add("@TripName", SqlDbType.NVarChar, 150).Value = tripName;
        command.Parameters.Add("@Destination", SqlDbType.NVarChar, 200).Value = destination;
        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = (object?)description ?? DBNull.Value;
        command.Parameters.Add("@ExpectedRowVersion", SqlDbType.Timestamp, 8).Value = expectedRowVersion;

        return await command.ExecuteScalarAsync(cancellationToken) as byte[];
    }

    public async Task<bool> DeleteTripAsync(int tripId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.Trips WHERE TripId = @TripId;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
