```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Cortex-X925, Cortex-A725 2.81GHz, 10 physical cores
.NET SDK 10.0.300
  [Host]   : .NET 10.0.8 (10.0.826.23019), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 10.0.8 (10.0.826.23019), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method    | Categories | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |----------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Dec_Add   | Add        |   2.825 ns | 0.0697 ns | 0.0038 ns |  1.00 |    0.00 |      - |         - |          NA |
| Rat_Add   | Add        |  13.893 ns | 0.2402 ns | 0.0132 ns |  4.92 |    0.01 |      - |         - |          NA |
|           |            |            |           |           |       |         |        |           |             |
| Dec_Chain | Chain5     |  39.270 ns | 0.0640 ns | 0.0035 ns |  1.00 |    0.00 |      - |         - |          NA |
| Rat_Chain | Chain5     | 223.830 ns | 8.9218 ns | 0.4890 ns |  5.70 |    0.01 | 0.0153 |      64 B |          NA |
|           |            |            |           |           |       |         |        |           |             |
| Rat_Div   | Divide     |  12.187 ns | 0.0580 ns | 0.0032 ns |  0.82 |    0.01 |      - |         - |          NA |
| Dec_Div   | Divide     |  14.877 ns | 3.5794 ns | 0.1962 ns |  1.00 |    0.02 |      - |         - |          NA |
| Ucum_Div  | Divide     |  45.630 ns | 0.4930 ns | 0.0270 ns |  3.07 |    0.04 |      - |         - |          NA |
|           |            |            |           |           |       |         |        |           |             |
| Dec_Mul   | Multiply   |   2.198 ns | 0.0202 ns | 0.0011 ns |  1.00 |    0.00 |      - |         - |          NA |
| Rat_Mul   | Multiply   |  10.872 ns | 0.0157 ns | 0.0009 ns |  4.95 |    0.00 |      - |         - |          NA |
| Ucum_Mul  | Multiply   |  44.153 ns | 0.3451 ns | 0.0189 ns | 20.09 |    0.01 |      - |         - |          NA |
