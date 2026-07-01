using Npgsql;

namespace Benchmark.SqlProfiler;

/// <summary>
/// Helper class for executing raw SQL queries with Npgsql.
/// </summary>
public static class QueryExecutor
{
    /// <summary>
    /// Executes a count query and returns the integer result.
    /// </summary>
    public static async Task<int> ExecuteCountQueryAsync(
        NpgsqlConnection connection,
        string sql,
        Dictionary<string, object>? parameters = null)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }
        }

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Executes a query and reads all rows (for queries that return data).
    /// Returns the number of rows read.
    /// </summary>
    public static async Task<int> ExecuteQueryAsync(
        NpgsqlConnection connection,
        string sql,
        Dictionary<string, object>? parameters = null)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }
        }

        var rowCount = 0;
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rowCount++;
        }

        return rowCount;
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE).
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        string sql,
        Dictionary<string, object>? parameters = null)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }
        }

        return await cmd.ExecuteNonQueryAsync();
    }
}
