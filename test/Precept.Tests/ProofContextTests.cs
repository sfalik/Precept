using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Wrapper-contract tests for <see cref="ProofContext"/>.
/// Verifies that ProofContext correctly delegates to the underlying proof infrastructure
/// for all query methods and preserves copy-on-write semantics for mutation methods.
/// </summary>
public class ProofContextTests
{
    // ── Symbols property ─────────────────────────────────────────────────────

    [Fact]
    public void Symbols_ExposesUnderlyingDictionary()
    {
        var dict = new Dictionary<string, StaticValueKind>
        {
            ["$positive:Price"] = StaticValueKind.Boolean,
            ["$nonneg:Amount"]  = StaticValueKind.Boolean,
        };
        var ctx = new ProofContext(dict);

        ctx.Symbols.Should().BeEquivalentTo(dict);
    }

    // ── IntervalOf ───────────────────────────────────────────────────────────

    [Fact]
    public void IntervalOf_FieldWithPositiveMarker_ReturnsPositiveInterval()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>
        {
            ["$positive:Price"] = StaticValueKind.Boolean,
        });
        var expr = PreceptParser.ParseExpression("Price");

        var interval = ctx.IntervalOf(expr);

        interval.IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_LiteralExpression_ReturnsSingleton()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("42");

        var interval = ctx.IntervalOf(expr);

        interval.Lower.Should().Be(42);
        interval.Upper.Should().Be(42);
        interval.LowerInclusive.Should().BeTrue();
        interval.UpperInclusive.Should().BeTrue();
    }

    // ── KnowsNonzero ─────────────────────────────────────────────────────────

    [Fact]
    public void KnowsNonzero_FieldWithGtMarker_ReturnsTrue()
    {
        // $gt:A:B → A > B → A - B > 0 (strictly positive, hence nonzero)
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>
        {
            ["$gt:A:B"] = StaticValueKind.Boolean,
        });
        var expr = PreceptParser.ParseExpression("A - B");

        ctx.KnowsNonzero(expr).Should().BeTrue();
    }

    [Fact]
    public void KnowsNonzero_UnknownField_ReturnsFalse()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("X");

        ctx.KnowsNonzero(expr).Should().BeFalse();
    }

    // ── KnowsNonnegative ─────────────────────────────────────────────────────

    [Fact]
    public void KnowsNonnegative_FieldWithNonnegMarker_ReturnsTrue()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>
        {
            ["$nonneg:Amount"] = StaticValueKind.Boolean,
        });
        var expr = PreceptParser.ParseExpression("Amount");

        ctx.KnowsNonnegative(expr).Should().BeTrue();
    }

    [Fact]
    public void KnowsNonnegative_UnconstrainedField_ReturnsFalse()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("X");

        ctx.KnowsNonnegative(expr).Should().BeFalse();
    }

    // ── SignOf ────────────────────────────────────────────────────────────────

    [Fact]
    public void SignOf_PositiveField_ReturnsPositive()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>
        {
            ["$positive:Rate"] = StaticValueKind.Boolean,
        });
        var expr = PreceptParser.ParseExpression("Rate");

        ctx.SignOf(expr).Should().Be(ProofSign.Positive);
    }

    [Fact]
    public void SignOf_UnknownField_ReturnsUnknown()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("X");

        ctx.SignOf(expr).Should().Be(ProofSign.Unknown);
    }

    // ── WithNarrowing ─────────────────────────────────────────────────────────

    [Fact]
    public void WithNarrowing_ReturnsNewContext_DoesNotMutateOriginal()
    {
        // Narrowing "Price > 0" (assumeTrue) injects $positive:Price, $nonneg:Price, $nonzero:Price
        // into a copy — the original empty context must remain unchanged.
        var original = new ProofContext(new Dictionary<string, StaticValueKind>());
        var condition = PreceptParser.ParseExpression("Price > 0");

        var narrowed = original.WithNarrowing(condition, assumeTrue: true);

        narrowed.Should().NotBeSameAs(original);
        narrowed.Symbols.Should().ContainKey("$positive:Price");
        original.Symbols.Should().BeEmpty();
    }
}
