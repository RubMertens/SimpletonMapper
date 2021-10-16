# SimpletonMapper
A very simple demonstration of different ways to build a object to object mapper in dotnet.
Mainly a way to explore different kinds of reflection!

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
|                              Method |        Mean |    Error |   StdDev |
|------------------------------------ |------------:|---------:|---------:|
|                          Reflection | 1,179.53 ns | 6.784 ns | 6.346 ns |
|                              Roslyn |   585.85 ns | 3.702 ns | 3.463 ns |
|                                  Il |   586.23 ns | 2.330 ns | 2.066 ns |
|      SourceGen_From_ToToModelMethod |    11.73 ns | 0.069 ns | 0.065 ns |
| SourceGen_NewTo_FromFromModelMethod |    10.92 ns | 0.043 ns | 0.040 ns |
