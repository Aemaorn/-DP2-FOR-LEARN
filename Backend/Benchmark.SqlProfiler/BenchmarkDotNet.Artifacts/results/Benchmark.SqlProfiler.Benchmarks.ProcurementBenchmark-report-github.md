```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M5, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 9.0.12 (9.0.12, 9.0.1225.60609), Arm64 RyuJIT armv8.0-a
  Job-UREURL : .NET 9.0.12 (9.0.12, 9.0.1225.60609), Arm64 RyuJIT armv8.0-a

IterationCount=3  RunStrategy=Monitoring  WarmupCount=1  

```
| Method                                        | Database | Mean     | Error    | StdDev   | Allocated |
|---------------------------------------------- |--------- |---------:|---------:|---------:|----------:|
| **&#39;Q4: Procurement PreProcurement Count (~10s)&#39;** | **Vanilla**  | **64.69 ms** | **37.87 ms** | **2.076 ms** |     **15 KB** |
| **&#39;Q4: Procurement PreProcurement Count (~10s)&#39;** | **Hardened** | **99.81 ms** | **63.42 ms** | **3.476 ms** |     **15 KB** |
