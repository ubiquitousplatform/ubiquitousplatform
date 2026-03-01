```

BenchmarkDotNet v0.13.12, macOS 26.3 (25D125) [Darwin 25.3.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.2 (8.0.224.6711), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.2 (8.0.224.6711), Arm64 RyuJIT AdvSIMD


```
| Method         | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------- |-------------:|-----------:|-----------:|---------:|--------:|---------:|---------:|---------:|----------:|------------:|
| ColdStartNoop  | 2,986.773 μs | 27.9996 μs | 26.1909 μs | 1,310.77 |   12.47 | 304.6875 | 304.6875 | 109.3750 |  456915 B |    7,139.30 |
| WarmNoop       |     2.281 μs |  0.0117 μs |  0.0103 μs |     1.00 |    0.00 |   0.0076 |        - |        - |      64 B |        1.00 |
| WarmHostCall1  |     3.302 μs |  0.0205 μs |  0.0172 μs |     1.45 |    0.01 |   0.0191 |        - |        - |     128 B |        2.00 |
| WarmHostCall10 |    13.100 μs |  0.1084 μs |  0.0961 μs |     5.74 |    0.04 |   0.1068 |        - |        - |     704 B |       11.00 |
