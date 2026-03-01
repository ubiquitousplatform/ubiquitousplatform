```

BenchmarkDotNet v0.13.12, macOS 26.3 (25D125) [Darwin 25.3.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 8.0.2 (8.0.224.6711), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.2 (8.0.224.6711), Arm64 RyuJIT AdvSIMD


```
| Method         | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------- |-------------:|-----------:|-----------:|---------:|--------:|---------:|---------:|---------:|----------:|------------:|
| ColdStartNoop  | 3,176.219 μs | 52.5592 μs | 49.1639 μs | 1,327.23 |   25.78 | 292.9688 | 292.9688 | 117.1875 |  481080 B |    4,625.77 |
| WarmNoop       |     2.396 μs |  0.0227 μs |  0.0190 μs |     1.00 |    0.00 |   0.0153 |        - |        - |     104 B |        1.00 |
| WarmHostCall1  |     4.278 μs |  0.0840 μs |  0.0786 μs |     1.78 |    0.04 |   0.0992 |        - |        - |     632 B |        6.08 |
| WarmHostCall10 |    23.828 μs |  0.4241 μs |  0.3967 μs |     9.98 |    0.15 |   0.8240 |        - |        - |    5328 B |       51.23 |
