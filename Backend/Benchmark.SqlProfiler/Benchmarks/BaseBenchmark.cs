using BenchmarkDotNet.Attributes;
using Npgsql;

namespace Benchmark.SqlProfiler.Benchmarks;

/// <summary>
/// Base class for SQL benchmarks with common setup/teardown logic.
/// </summary>
public abstract class BaseBenchmark
{
    protected NpgsqlConnection VanillaConnection = null!;
    protected NpgsqlConnection HardenedConnection = null!;

    protected Guid UserId;
    protected Guid[] UserIds = [];
    protected Guid[] CreatedBys = [];

    /// <summary>
    /// The database type to benchmark against.
    /// </summary>
    [ParamsSource(nameof(DatabaseTypes))]
    public DatabaseType Database { get; set; }

    /// <summary>
    /// Available database types for benchmarking.
    /// </summary>
    public IEnumerable<DatabaseType> DatabaseTypes => [DatabaseType.Vanilla, DatabaseType.Hardened];

    /// <summary>
    /// Gets the connection for the current database type.
    /// </summary>
    protected NpgsqlConnection GetConnection() => Database switch
    {
        DatabaseType.Vanilla => VanillaConnection,
        DatabaseType.Hardened => HardenedConnection,
        _ => throw new ArgumentOutOfRangeException(),
    };

    /// <summary>
    /// Gets the default parameters dictionary for queries.
    /// </summary>
    protected Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["@userId"] = UserId,
        ["@userIds"] = UserIds,
        ["@createdBys"] = CreatedBys,
    };

    /// <summary>
    /// Global setup - runs once before all benchmarks.
    /// Creates connections and fetches test data.
    /// </summary>
    [GlobalSetup]
    public virtual async Task GlobalSetup()
    {
        // Create connections
        VanillaConnection = new NpgsqlConnection(DatabaseConfig.GetConnectionString(DatabaseType.Vanilla));
        HardenedConnection = new NpgsqlConnection(DatabaseConfig.GetConnectionString(DatabaseType.Hardened));

        await VanillaConnection.OpenAsync();
        await HardenedConnection.OpenAsync();

        // Fetch test data from vanilla database
        await LoadTestDataAsync();
    }

    /// <summary>
    /// Loads test user data from the database.
    /// </summary>
    protected virtual async Task LoadTestDataAsync()
    {
        var userIds = await TestUserProvider.GetActiveUserIdsAsync(VanillaConnection, 5);
        var createdBys = await TestUserProvider.GetProcurementCreatedBysAsync(VanillaConnection, 5);

        if (userIds.Count == 0)
        {
            throw new InvalidOperationException("No active users found in the database. Please ensure the database has test data.");
        }

        UserId = userIds[0];
        UserIds = userIds.ToArray();
        CreatedBys = createdBys.Count > 0 ? createdBys.ToArray() : userIds.ToArray();

        Console.WriteLine($"Loaded test data: UserId={UserId}, UserIds.Count={UserIds.Length}, CreatedBys.Count={CreatedBys.Length}");
    }

    /// <summary>
    /// Global cleanup - runs once after all benchmarks.
    /// Disposes connections.
    /// </summary>
    [GlobalCleanup]
    public virtual async Task GlobalCleanup()
    {
        await VanillaConnection.DisposeAsync();
        await HardenedConnection.DisposeAsync();
    }
}
