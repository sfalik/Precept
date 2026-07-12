using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Precept.Language;

namespace Precept.Bench;

// Decimal vs arbitrary-precision arithmetic. Each benchmark returns its result so
// BenchmarkDotNet's consumer defeats dead-code elimination (no manual sink, no overflow).
// Ucum* appears only under Multiply/Divide — UcumExactFactor has no add.
[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ArithmeticBench
{
    // Realistic business operands (invoice-line magnitudes).
    private readonly decimal d1 = 49.95m, d2 = 7m, d3 = 12.5m, d4 = 0.0825m;
    private UcumExactFactor u1, u2;
    private BigRational r1, r2, r3, r4, r100;

    [GlobalSetup]
    public void Setup()
    {
        u1 = UcumExactFactor.Parse("49.95");
        u2 = UcumExactFactor.Parse("7");
        r1 = BigRational.Parse(d1);
        r2 = BigRational.Parse(d2);
        r3 = BigRational.Parse(d3);
        r4 = BigRational.Parse(d4);
        r100 = BigRational.FromInt(100);
    }

    // ---- Multiply ----
    [BenchmarkCategory("Multiply"), Benchmark(Baseline = true)] public decimal Dec_Mul() => d1 * d2;
    [BenchmarkCategory("Multiply"), Benchmark] public UcumExactFactor Ucum_Mul() => u1.Multiply(u2);
    [BenchmarkCategory("Multiply"), Benchmark] public BigRational Rat_Mul() => r1.Mul(r2);

    // ---- Divide ----
    [BenchmarkCategory("Divide"), Benchmark(Baseline = true)] public decimal Dec_Div() => d1 / d2;
    [BenchmarkCategory("Divide"), Benchmark] public UcumExactFactor Ucum_Div() => u1.Divide(u2);
    [BenchmarkCategory("Divide"), Benchmark] public BigRational Rat_Div() => r1.Div(r2);

    // ---- Add (decimal vs rational; UcumExactFactor has none) ----
    [BenchmarkCategory("Add"), Benchmark(Baseline = true)] public decimal Dec_Add() => d1 + d2;
    [BenchmarkCategory("Add"), Benchmark] public BigRational Rat_Add() => r1.Add(r2);

    // ---- Realistic computed-field chain (invoice-line-item.precept): 5 ops, includes add+sub ----
    [BenchmarkCategory("Chain5"), Benchmark(Baseline = true)]
    public decimal Dec_Chain()
    {
        var sub = d1 * d2;
        var disc = sub * d3 / 100m;
        var taxable = sub - disc;
        var tax = taxable * d4;
        return taxable + tax;
    }

    [BenchmarkCategory("Chain5"), Benchmark]
    public BigRational Rat_Chain()
    {
        var sub = r1.Mul(r2);
        var disc = sub.Mul(r3).Div(r100);
        var taxable = sub.Sub(disc);
        var tax = taxable.Mul(r4);
        return taxable.Add(tax);
    }
}
