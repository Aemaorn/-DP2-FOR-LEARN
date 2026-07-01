namespace Benchmark.SqlProfiler;

/// <summary>
/// Database type for benchmark comparison.
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// Vanilla PostgreSQL (port 5432).
    /// </summary>
    Vanilla,

    /// <summary>
    /// Hardened PostgreSQL with pgaudit/set_user extensions (port 54320).
    /// </summary>
    Hardened,
}

/// <summary>
/// Connection string definitions for benchmark databases.
/// </summary>
public static class DatabaseConfig
{
    private const string Database = "GHB.DP2";
    private const string Username = "postgres";
    private const string Password = "postgres";

    /// <summary>
    /// Gets the connection string for the specified database type.
    /// </summary>
    public static string GetConnectionString(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.Vanilla => $"Host=localhost;Port=5432;Database={Database};Username={Username};Password={Password}",
        DatabaseType.Hardened => $"Host=localhost;Port=54320;Database={Database};Username={Username};Password={Password}",
        _ => throw new ArgumentOutOfRangeException(nameof(databaseType)),
    };
}
