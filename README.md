# SimpletonMapper
A very simple demonstration of different ways to build a object to object mapper in dotnet

Build in several versions

- V0 - Something that works on Convention
- V1 - Mapping via Attributes
- V2 - Mapping via Expression
- V3 - Mapper with Roslyn
- V4 - Mapper with IL
- V5 - Source generator

# Latest benchmark results

``` ini
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i5-4590 CPU 3.30GHz (Haswell), 1 CPU, 4 logical and 4 physical cores
.NET SDK=5.0.401
  [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  DefaultJob : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
```
|     Method |       Mean |      Error |     StdDev |
|----------- |-----------:|-----------:|-----------:|
| Reflection | 886.830 ns |  4.1215 ns |  3.8553 ns |
|     Roslyn | 578.621 ns |  2.8293 ns |  2.6465 ns |
|         Il | 597.520 ns | 11.9792 ns | 11.7651 ns |
|  SourceGen |   9.789 ns |  0.0754 ns |  0.0705 ns |
