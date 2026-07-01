using Npgsql;

namespace Benchmark.SqlProfiler;

/// <summary>
/// Provides real test user data from the database.
/// </summary>
public static class TestUserProvider
{
    /// <summary>
    /// Fetches active user IDs from the database.
    /// </summary>
    public static async Task<List<Guid>> GetActiveUserIdsAsync(NpgsqlConnection connection, int limit = 10)
    {
        const string sql = """
            SELECT "Id"
            FROM "SystemUtility"."SuUser"
            WHERE "IsActive" = true
            LIMIT @limit
            """;

        var userIds = new List<Guid>();
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            userIds.Add(reader.GetGuid(0));
        }

        return userIds;
    }

    /// <summary>
    /// Fetches created by user IDs from Procurement table.
    /// </summary>
    public static async Task<List<Guid>> GetProcurementCreatedBysAsync(NpgsqlConnection connection, int limit = 10)
    {
        const string sql = """
            SELECT DISTINCT "CreatedBy"
            FROM "Procurement"."Procurement"
            WHERE "CreatedBy" IS NOT NULL
            LIMIT @limit
            """;

        var createdBys = new List<Guid>();
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            createdBys.Add(reader.GetGuid(0));
        }

        return createdBys;
    }

    /// <summary>
    /// Fetches a single active user ID for testing.
    /// </summary>
    public static async Task<Guid?> GetSingleActiveUserIdAsync(NpgsqlConnection connection)
    {
        const string sql = """
            SELECT "Id"
            FROM "SystemUtility"."SuUser"
            WHERE "IsActive" = true
            LIMIT 1
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync();

        return result is Guid guid ? guid : null;
    }
}
