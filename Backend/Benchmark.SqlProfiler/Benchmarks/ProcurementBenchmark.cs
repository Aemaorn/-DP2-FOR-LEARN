using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Benchmark.SqlProfiler.Benchmarks;

/// <summary>
/// Benchmark for the critical Procurement PreProcurement count query (~10 seconds).
/// This is the slowest query and the primary target for performance optimization.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 3)]
public class ProcurementBenchmark : BaseBenchmark
{
    /// <summary>
    /// Query 4: Procurement Count - PreProcurement (~10,021ms) - CRITICAL BOTTLENECK
    /// This query counts Procurements in PreProcurement step with complex EXISTS subqueries.
    /// </summary>
    [Benchmark(Description = "Q4: Procurement PreProcurement Count (~10s)")]
    public async Task<int> ProcurementPreProcurementCount()
    {
        var connection = GetConnection();
        var parameters = GetDefaultParameters();

        return await QueryExecutor.ExecuteCountQueryAsync(
            connection,
            QueryDefinitions.ProcurementPreProcurementCountQuery,
            parameters);
    }
}
