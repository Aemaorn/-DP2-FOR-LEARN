using BenchmarkDotNet.Running;
using Benchmark.SqlProfiler.Benchmarks;

// SQL Benchmark for comparing Vanilla PostgreSQL vs Hardened PostgreSQL
// =====================================================================
//
// This benchmark compares SQL query performance between:
// - Vanilla PostgreSQL (port 5432)
// - Hardened PostgreSQL (port 54320, with pgaudit/set_user extensions)
//
// Prerequisites:
// 1. Ensure both PostgreSQL instances are running:
//    docker-compose up -d postgres postgres_hardening_ghb (from tuning/)
//
// 2. Ensure the database has test data (active users in SystemUtility.SuUser)
//
// Usage:
// ------
// # Build in Release mode
// dotnet build -c Release
//
// # Run default (critical Procurement query only)
// dotnet run -c Release
//
// # Run all benchmarks
// dotnet run -c Release -- --filter *
//
// # Run specific benchmark
// dotnet run -c Release -- --filter *Procurement*
// dotnet run -c Release -- --filter *Worklist*
//
// # List available benchmarks
// dotnet run -c Release -- --list flat

// If no arguments provided, run the critical Procurement benchmark by default
if (args.Length == 0)
{
    BenchmarkRunner.Run<ProcurementBenchmark>();
}
else
{
    BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(args);
}

Console.WriteLine();
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("Benchmark completed!");
Console.WriteLine("Results are available in: BenchmarkDotNet.Artifacts/results/");
Console.WriteLine("=".PadRight(60, '='));
