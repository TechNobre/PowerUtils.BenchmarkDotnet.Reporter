```

BenchmarkDotNet v0.15.1, Windows 11 (10.0.26200.7922)
AMD Ryzen 5 3600X 3.80GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.200-preview.0.26103.119
  [Host]     : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2


```
| Method         | Mean     | Error     | StdDev    | Gen0      | Gen1     | Allocated |
|--------------- |---------:|----------:|----------:|----------:|---------:|----------:|
| GenerateString | 1.384 ms | 0.0277 ms | 0.0714 ms | 4570.3125 | 171.8750 |   36.5 MB |
