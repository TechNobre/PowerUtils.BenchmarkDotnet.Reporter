```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3323)
AMD Ryzen 5 3600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100-preview.1.25120.13
  [Host]     : .NET 9.0.2 (9.0.225.6610), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.2 (9.0.225.6610), X64 RyuJIT AVX2


```
| Method       | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| StringConcat | 15.56 ns | 0.339 ns | 0.333 ns |  0.93 |    0.02 |    I | 0.0057 |      48 B |        1.00 |
| StringJoin   | 16.71 ns | 0.333 ns | 0.278 ns |  1.00 |    0.02 |   II | 0.0057 |      48 B |        1.00 |
