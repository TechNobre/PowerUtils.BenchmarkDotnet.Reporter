```

BenchmarkDotNet v0.15.1, Linux Ubuntu 24.04.2 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2


```
| Method        | Mean     | Error    | StdDev   | Gen0   | Allocated |
|-------------- |---------:|---------:|---------:|-------:|----------:|
| GenerateArray | 11.27 μs | 0.136 μs | 0.114 μs | 1.3123 |  21.52 KB |
