```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.26100.3323)
AMD Ryzen 5 3600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100-preview.1.25120.13
  [Host]     : .NET 9.0.2 (9.0.225.6610), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.2 (9.0.225.6610), X64 RyuJIT AVX2


```
| Method       | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0       | Gen1      | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|------:|--------:|-----:|-----------:|----------:|----------:|------------:|
| StringJoin   | 13.69 ms | 0.170 ms | 0.150 ms |  1.00 |    0.01 |    I | 45171.8750 | 7984.3750 | 361.86 MB |        1.00 |
| StringConcat | 26.09 ms | 0.959 ms | 2.826 ms |  1.91 |    0.21 |   II | 45156.2500 | 7968.7500 | 361.86 MB |        1.00 |
