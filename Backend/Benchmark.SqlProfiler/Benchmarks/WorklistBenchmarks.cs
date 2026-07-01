using BenchmarkDotNet.Attributes;

namespace Benchmark.SqlProfiler.Benchmarks;

/// <summary>
/// Benchmarks for worklist queries (Queries 1-3, 5-10).
/// These are relatively fast queries compared to the Procurement query.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class WorklistBenchmarks : BaseBenchmark
{
    /// <summary>
    /// Query 1: User Context Query (~11ms)
    /// </summary>
    [Benchmark(Description = "Q1: User Context (~11ms)")]
    public async Task<int> UserContextQuery()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteQueryAsync(
            connection,
            QueryDefinitions.UserContextQuery,
            parameters);
    }

    /// <summary>
    /// Query 2: Plan Count Query (~59ms)
    /// </summary>
    [Benchmark(Description = "Q2: Plan Count (~59ms)")]
    public async Task<int> PlanCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.PlanCountQuery,
            parameters);
    }

    /// <summary>
    /// Query 3: Plan Announcement Count Query (~3ms)
    /// </summary>
    [Benchmark(Description = "Q3: Plan Announcement Count (~3ms)")]
    public async Task<int> PlanAnnouncementCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.PlanAnnouncementCountQuery,
            parameters);
    }

    /// <summary>
    /// Query 5: Pw119 Count Query (~2ms)
    /// </summary>
    [Benchmark(Description = "Q5: Pw119 Count (~2ms)")]
    public async Task<int> Pw119Count()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.Pw119CountQuery,
            parameters);
    }

    /// <summary>
    /// Query 6: P79Clause2 Count Query (~2ms)
    /// </summary>
    [Benchmark(Description = "Q6: P79Clause2 Count (~2ms)")]
    public async Task<int> P79Clause2Count()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.P79Clause2CountQuery,
            parameters);
    }

    /// <summary>
    /// Query 7: PPettyCash Count Query (~3ms)
    /// </summary>
    [Benchmark(Description = "Q7: PPettyCash Count (~3ms)")]
    public async Task<int> PPettyCashCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.PPettyCashCountQuery,
            parameters);
    }

    /// <summary>
    /// Query 8: PPettyCashReimbursement Count Query (~2ms)
    /// </summary>
    [Benchmark(Description = "Q8: PPettyCashReimbursement Count (~2ms)")]
    public async Task<int> PPettyCashReimbursementCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.PPettyCashReimbursementCountQuery,
            parameters);
    }

    /// <summary>
    /// Query 9: DeliveryAcceptancePeriod Count Query (~6ms)
    /// </summary>
    [Benchmark(Description = "Q9: DeliveryAcceptancePeriod Count (~6ms)")]
    public async Task<int> DeliveryAcceptancePeriodCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.DeliveryAcceptancePeriodCountQuery,
            parameters);
    }

    /// <summary>
    /// Query 10: ExpenseDisbursement Count Query (~3ms)
    /// </summary>
    [Benchmark(Description = "Q10: ExpenseDisbursement Count (~3ms)")]
    public async Task<int> ExpenseDisbursementCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.ExpenseDisbursementCountQuery,
            parameters);
    }
}
