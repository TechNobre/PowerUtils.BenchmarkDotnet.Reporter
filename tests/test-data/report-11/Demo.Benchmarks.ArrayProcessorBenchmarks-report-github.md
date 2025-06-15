```

BenchmarkDotNet v0.15.1, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
AMD Ryzen 5 3600X 3.80GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100-preview.4.25258.110
  [Host]     : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2


```
| Method        | Mean     | Error    | StdDev   | Gen0   | Allocated |
|-------------- |---------:|---------:|---------:|-------:|----------:|
| GenerateArray | 17.95 μs | 0.107 μs | 0.095 μs | 2.6245 |  21.56 KB |
