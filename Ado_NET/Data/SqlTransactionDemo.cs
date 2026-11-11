using System.Data;
using Microsoft.Data.SqlClient;

namespace Kpz.Lab2.Data;

/// <summary>Demonstrates a successful ADO.NET transaction and a business-rule rollback.</summary>
public static class SqlTransactionDemo
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        await RunCommitScenarioAsync(connectionString, cancellationToken);
        await RunRollbackScenarioAsync(connectionString, cancellationToken);
    }

    private static async Task RunCommitScenarioAsync(string connectionString, CancellationToken cancellationToken)
    {
        var email = $"tx.commit.{Guid.NewGuid():N}@example.test";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var userId = await InsertUserAsync(connection, transaction, email, cancellationToken);
            var tripId = await InsertTripAsync(connection, transaction, userId, cancellationToken);
            var itemId = await InsertItineraryItemAsync(
                connection,
                transaction,
                tripId,
                new DateTime(2026, 11, 12, 10, 0, 0),
                new DateTime(2026, 11, 12, 12, 0, 0),
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            Console.WriteLine();
            Console.WriteLine("=== TRANSACTION DEMO: COMMIT ===");
            Console.WriteLine($"Committed User #{userId}, Trip #{tripId}, ItineraryItem #{itemId}.");
            Console.WriteLine($"Demo user email: {email}");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task RunRollbackScenarioAsync(string connectionString, CancellationToken cancellationToken)
    {
        var email = $"tx.rollback.{Guid.NewGuid():N}@example.test";
        var rolledBackUserId = 0;

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                rolledBackUserId = await InsertUserAsync(connection, transaction, email, cancellationToken);
                var tripId = await InsertTripAsync(connection, transaction, rolledBackUserId, cancellationToken);

                // EndDateTime is intentionally earlier than StartDateTime; CK_ItineraryItems_DateRange rejects it.
                await InsertItineraryItemAsync(
                    connection,
                    transaction,
                    tripId,
                    new DateTime(2026, 11, 12, 12, 0, 0),
                    new DateTime(2026, 11, 12, 10, 0, 0),
                    cancellationToken);

                throw new InvalidOperationException("The invalid itinerary item unexpectedly passed its check constraint.");
            }
            catch (SqlException exception) when (exception.Number == 547)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                Console.WriteLine();
                Console.WriteLine("=== TRANSACTION DEMO: ROLLBACK ===");
                Console.WriteLine("The itinerary date check rejected the invalid item (SQL error 547).");
                Console.WriteLine("ROLLBACK was executed for the user and trip inserted earlier in this transaction.");
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        // Verify outside the rolled-back transaction that the earlier user insert is gone.
        await using var verifyConnection = new SqlConnection(connectionString);
        await verifyConnection.OpenAsync(cancellationToken);
        await using var verifyCommand = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Users WHERE UserId = @UserId AND Email = @Email;",
            verifyConnection);
        verifyCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = rolledBackUserId;
        verifyCommand.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email;
        var remainingRows = Convert.ToInt32(await verifyCommand.ExecuteScalarAsync(cancellationToken));
        Console.WriteLine($"Rollback verification: matching Users rows = {remainingRows} (expected 0).");
    }

    private static Task<int> InsertUserAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string email,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Users (FullName, Email, PasswordHash)
            OUTPUT INSERTED.UserId
            VALUES (@FullName, @Email, @PasswordHash);
            """;

        return ExecuteIdentityAsync(connection, transaction, sql, parameters =>
        {
            parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = "Transaction Demo User";
            parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email;
            parameters.Add("@PasswordHash", SqlDbType.NVarChar, 512).Value = "DEMO_ONLY_HASH";
        }, cancellationToken);
    }

    private static Task<int> InsertTripAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int ownerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Trips
                (OwnerId, TripCategoryId, TripName, Destination, StartDate, EndDate, Description)
            OUTPUT INSERTED.TripId
            VALUES
                (@OwnerId, @TripCategoryId, @TripName, @Destination, @StartDate, @EndDate, @Description);
            """;

        return ExecuteIdentityAsync(connection, transaction, sql, parameters =>
        {
            parameters.Add("@OwnerId", SqlDbType.Int).Value = ownerId;
            parameters.Add("@TripCategoryId", SqlDbType.Int).Value = 1;
            parameters.Add("@TripName", SqlDbType.NVarChar, 150).Value = "Transaction Demo Trip";
            parameters.Add("@Destination", SqlDbType.NVarChar, 200).Value = "Lviv";
            parameters.Add("@StartDate", SqlDbType.Date).Value = new DateTime(2026, 11, 12);
            parameters.Add("@EndDate", SqlDbType.Date).Value = new DateTime(2026, 11, 15);
            parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = "Created within a transaction demo.";
        }, cancellationToken);
    }

    private static Task<int> InsertItineraryItemAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int tripId,
        DateTime startDateTime,
        DateTime endDateTime,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ItineraryItems
                (TripId, ItineraryItemTypeId, Title, Description, StartDateTime, EndDateTime, Address, Latitude, Longitude)
            OUTPUT INSERTED.ItemId
            VALUES
                (@TripId, @ItineraryItemTypeId, @Title, @Description, @StartDateTime, @EndDateTime, @Address, @Latitude, @Longitude);
            """;

        return ExecuteIdentityAsync(connection, transaction, sql, parameters =>
        {
            parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
            parameters.Add("@ItineraryItemTypeId", SqlDbType.Int).Value = 1;
            parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = "Transaction Demo Itinerary Item";
            parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = "Inserted inside the same transaction.";
            parameters.Add("@StartDateTime", SqlDbType.DateTime2).Value = startDateTime;
            parameters.Add("@EndDateTime", SqlDbType.DateTime2).Value = endDateTime;
            parameters.Add("@Address", SqlDbType.NVarChar, 400).Value = "Lviv center";
            parameters.Add("@Latitude", SqlDbType.Decimal).Value = 49.839700m;
            parameters.Add("@Longitude", SqlDbType.Decimal).Value = 24.029700m;
            parameters["@Latitude"].Precision = 9;
            parameters["@Latitude"].Scale = 6;
            parameters["@Longitude"].Precision = 9;
            parameters["@Longitude"].Scale = 6;
        }, cancellationToken);
    }

    private static async Task<int> ExecuteIdentityAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        Action<SqlParameterCollection> addParameters,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        addParameters(command.Parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }
}
