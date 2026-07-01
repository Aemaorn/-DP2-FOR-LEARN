```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M5, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 9.0.12 (9.0.12, 9.0.1225.60609), Arm64 RyuJIT armv8.0-a
  Job-YFEFPZ : .NET 9.0.12 (9.0.12, 9.0.1225.60609), Arm64 RyuJIT armv8.0-a

IterationCount=10  WarmupCount=3  

```
| Method                                      | Database | Mean       | Error    | StdDev   | Gen0   | Allocated |
|-------------------------------------------- |--------- |-----------:|---------:|---------:|-------:|----------:|
| **&#39;Q1: User Context (~11ms)&#39;**                  | **Vanilla**  |         **NA** |       **NA** |       **NA** |     **NA** |        **NA** |
| &#39;Q2: Plan Count (~59ms)&#39;                    | Vanilla  | 1,849.2 μs | 16.10 μs |  8.42 μs |      - |   5.67 KB |
| &#39;Q3: Plan Announcement Count (~3ms)&#39;        | Vanilla  |   324.1 μs |  9.04 μs |  4.73 μs | 0.4883 |   5.34 KB |
| &#39;Q5: Pw119 Count (~2ms)&#39;                    | Vanilla  |   246.2 μs |  4.78 μs |  2.85 μs | 0.4883 |   4.52 KB |
| &#39;Q6: P79Clause2 Count (~2ms)&#39;               | Vanilla  |   235.4 μs | 10.78 μs |  7.13 μs | 0.4883 |   4.57 KB |
| &#39;Q7: PPettyCash Count (~3ms)&#39;               | Vanilla  |   337.5 μs |  3.47 μs |  2.30 μs | 0.4883 |   6.34 KB |
| &#39;Q8: PPettyCashReimbursement Count (~2ms)&#39;  | Vanilla  |   198.7 μs |  3.08 μs |  2.04 μs | 0.4883 |    4.7 KB |
| &#39;Q9: DeliveryAcceptancePeriod Count (~6ms)&#39; | Vanilla  | 1,097.9 μs |  7.97 μs |  5.27 μs |      - |  11.23 KB |
| &#39;Q10: ExpenseDisbursement Count (~3ms)&#39;     | Vanilla  |   280.5 μs |  3.33 μs |  2.20 μs | 0.4883 |   4.73 KB |
| **&#39;Q1: User Context (~11ms)&#39;**                  | **Hardened** |         **NA** |       **NA** |       **NA** |     **NA** |        **NA** |
| &#39;Q2: Plan Count (~59ms)&#39;                    | Hardened | 3,031.4 μs | 40.59 μs | 26.85 μs |      - |   5.67 KB |
| &#39;Q3: Plan Announcement Count (~3ms)&#39;        | Hardened |   509.3 μs | 20.56 μs | 13.60 μs |      - |   5.34 KB |
| &#39;Q5: Pw119 Count (~2ms)&#39;                    | Hardened |   356.6 μs |  3.90 μs |  2.32 μs | 0.4883 |   4.52 KB |
| &#39;Q6: P79Clause2 Count (~2ms)&#39;               | Hardened |   354.0 μs |  7.85 μs |  5.19 μs | 0.4883 |   4.57 KB |
| &#39;Q7: PPettyCash Count (~3ms)&#39;               | Hardened |   533.6 μs | 12.98 μs |  8.58 μs |      - |   6.34 KB |
| &#39;Q8: PPettyCashReimbursement Count (~2ms)&#39;  | Hardened |   316.8 μs |  5.18 μs |  3.43 μs | 0.4883 |    4.7 KB |
| &#39;Q9: DeliveryAcceptancePeriod Count (~6ms)&#39; | Hardened | 1,886.5 μs | 25.39 μs | 15.11 μs |      - |  11.23 KB |
| &#39;Q10: ExpenseDisbursement Count (~3ms)&#39;     | Hardened |   458.3 μs | 14.10 μs |  7.38 μs | 0.4883 |   4.73 KB |

Benchmarks with issues:
  WorklistBenchmarks.'Q1: User Context (~11ms)': Job-YFEFPZ(IterationCount=10, WarmupCount=3) [Database=Vanilla]
  WorklistBenchmarks.'Q1: User Context (~11ms)': Job-YFEFPZ(IterationCount=10, WarmupCount=3) [Database=Hardened]
