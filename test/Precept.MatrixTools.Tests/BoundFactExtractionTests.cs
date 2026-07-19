using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Per-term bound-fact extraction from guard conjuncts (term-compare-constant).
/// Extraction only — the interval-arithmetic derivation these facts feed is the
/// prover's job, not this calculator's.
/// </summary>
public class BoundFactExtractionTests
{
    [Fact]
    public void UpperAndLowerBoundsExtract()
    {
        var guard = Px.Expr("A <= 500 and B >= 10");
        var facts = WpCalculator.ExtractBoundFacts(guard);

        facts.Should().HaveCount(2);
        facts.Should().ContainSingle(f =>
            f.Term.Key == "pre(A)" && f.Kind == BoundKind.UpperInclusive && f.Bound == 500m);
        facts.Should().ContainSingle(f =>
            f.Term.Key == "pre(B)" && f.Kind == BoundKind.LowerInclusive && f.Bound == 10m);
    }

    [Fact]
    public void ConstantOnTheLeftIsMirrored()
    {
        // 500 >= A is the same bound fact as A <= 500.
        var facts = WpCalculator.ExtractBoundFacts(Px.Expr("500 >= A"));
        facts.Should().ContainSingle(f =>
            f.Term.Key == "pre(A)" && f.Kind == BoundKind.UpperInclusive && f.Bound == 500m);
    }

    [Fact]
    public void StrictAndEqualityKindsExtract()
    {
        var facts = WpCalculator.ExtractBoundFacts(Px.Expr("A < 5 and B > 1 and C == 3"));
        facts.Should().ContainSingle(f => f.Term.Key == "pre(A)" && f.Kind == BoundKind.UpperExclusive && f.Bound == 5m);
        facts.Should().ContainSingle(f => f.Term.Key == "pre(B)" && f.Kind == BoundKind.LowerExclusive && f.Bound == 1m);
        facts.Should().ContainSingle(f => f.Term.Key == "pre(C)" && f.Kind == BoundKind.Equal && f.Bound == 3m);
    }

    [Fact]
    public void CompoundTermsAreStillBoundFacts()
    {
        // The term side may be compound: (A + B) <= 1000 is a bound on the sum.
        var facts = WpCalculator.ExtractBoundFacts(Px.Expr("A + B <= 1000"));
        facts.Should().ContainSingle(f =>
            f.Term.Key == "(+ pre(A) pre(B))" && f.Kind == BoundKind.UpperInclusive && f.Bound == 1000m);
    }

    [Fact]
    public void NonConstantConjunctsAreNotBoundFacts()
    {
        WpCalculator.ExtractBoundFacts(Px.Expr("A <= B")).Should().BeEmpty();
    }

    [Fact]
    public void DisjunctionsYieldNoFacts()
    {
        // A disjunction guarantees neither side; only top-level conjuncts are facts.
        WpCalculator.ExtractBoundFacts(Px.Expr("A <= 5 or B <= 5")).Should().BeEmpty();
    }
}
