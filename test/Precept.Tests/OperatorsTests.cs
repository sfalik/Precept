using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the Operators catalog: exhaustiveness, token references,
/// precedence/associativity from the language spec § 2.1, and ByToken index.
/// </summary>
public class OperatorsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_CoversEveryOperatorKind()
    {
        foreach (var kind in Enum.GetValues<OperatorKind>())
        {
            var meta = Operators.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void All_CountMatchesEnumValues()
    {
        Operators.All.Should().HaveCount(Enum.GetValues<OperatorKind>().Length);
    }

    [Fact]
    public void All_KindsAreDistinct()
    {
        Operators.All.Select(m => m.Kind).Should().OnlyHaveUniqueItems();
    }

    // ── Token references ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Plus, "+")] 
    [InlineData(OperatorKind.Minus, "-")]
    [InlineData(OperatorKind.Times, "*")]
    [InlineData(OperatorKind.Divide, "/")]
    [InlineData(OperatorKind.Modulo, "%")]
    [InlineData(OperatorKind.Equals, "==")]
    [InlineData(OperatorKind.NotEquals, "!=")]
    [InlineData(OperatorKind.CaseInsensitiveEquals, "~=")]
    [InlineData(OperatorKind.CaseInsensitiveNotEquals, "!~")]
    [InlineData(OperatorKind.LessThan, "<")]
    [InlineData(OperatorKind.GreaterThan, ">")]
    [InlineData(OperatorKind.LessThanOrEqual, "<=")]
    [InlineData(OperatorKind.GreaterThanOrEqual, ">=")]
    [InlineData(OperatorKind.And, "and")]
    [InlineData(OperatorKind.Or, "or")]
    [InlineData(OperatorKind.Not, "not")]
    [InlineData(OperatorKind.Negate, "-")]
    [InlineData(OperatorKind.Contains, "contains")]
    public void GetMeta_TokenTextMatchesSymbol(OperatorKind kind, string expectedText)
    {
        Operators.GetMeta(kind).Token.Text.Should().Be(expectedText);
    }

    [Fact]
    public void GetMeta_MinusAndNegate_ShareSameToken()
    {
        var minus = Operators.GetMeta(OperatorKind.Minus);
        var negate = Operators.GetMeta(OperatorKind.Negate);

        minus.Token.Kind.Should().Be(negate.Token.Kind);
        minus.Arity.Should().Be(Arity.Binary);
        negate.Arity.Should().Be(Arity.Unary);
    }

    // ── Arity ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Not)]
    [InlineData(OperatorKind.Negate)]
    public void GetMeta_UnaryOperators_HaveUnaryArity(OperatorKind kind)
    {
        Operators.GetMeta(kind).Arity.Should().Be(Arity.Unary);
    }

    [Theory]
    [InlineData(OperatorKind.Plus)]
    [InlineData(OperatorKind.Minus)]
    [InlineData(OperatorKind.Times)]
    [InlineData(OperatorKind.Divide)]
    [InlineData(OperatorKind.Modulo)]
    [InlineData(OperatorKind.Equals)]
    [InlineData(OperatorKind.NotEquals)]
    [InlineData(OperatorKind.CaseInsensitiveEquals)]
    [InlineData(OperatorKind.CaseInsensitiveNotEquals)]
    [InlineData(OperatorKind.LessThan)]
    [InlineData(OperatorKind.GreaterThan)]
    [InlineData(OperatorKind.LessThanOrEqual)]
    [InlineData(OperatorKind.GreaterThanOrEqual)]
    [InlineData(OperatorKind.And)]
    [InlineData(OperatorKind.Or)]
    [InlineData(OperatorKind.Contains)]
    public void GetMeta_BinaryOperators_HaveBinaryArity(OperatorKind kind)
    {
        Operators.GetMeta(kind).Arity.Should().Be(Arity.Binary);
    }

    // ── Precedence (from language spec § 2.1) ───────────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Or, 10)]
    [InlineData(OperatorKind.And, 20)]
    [InlineData(OperatorKind.Not, 25)]
    [InlineData(OperatorKind.Equals, 30)]
    [InlineData(OperatorKind.NotEquals, 30)]
    [InlineData(OperatorKind.CaseInsensitiveEquals, 30)]
    [InlineData(OperatorKind.CaseInsensitiveNotEquals, 30)]
    [InlineData(OperatorKind.LessThan, 30)]
    [InlineData(OperatorKind.GreaterThan, 30)]
    [InlineData(OperatorKind.LessThanOrEqual, 30)]
    [InlineData(OperatorKind.GreaterThanOrEqual, 30)]
    [InlineData(OperatorKind.Contains, 40)]
    [InlineData(OperatorKind.Plus, 50)]
    [InlineData(OperatorKind.Minus, 50)]
    [InlineData(OperatorKind.Times, 60)]
    [InlineData(OperatorKind.Divide, 60)]
    [InlineData(OperatorKind.Modulo, 60)]
    [InlineData(OperatorKind.Negate, 65)]
    public void GetMeta_Precedence_MatchesSpec(OperatorKind kind, int expectedPrecedence)
    {
        Operators.GetMeta(kind).Precedence.Should().Be(expectedPrecedence);
    }

    // ── Associativity ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Or)]
    [InlineData(OperatorKind.And)]
    [InlineData(OperatorKind.Plus)]
    [InlineData(OperatorKind.Minus)]
    [InlineData(OperatorKind.Times)]
    [InlineData(OperatorKind.Divide)]
    [InlineData(OperatorKind.Modulo)]
    public void GetMeta_LeftAssociativeOperators(OperatorKind kind)
    {
        Operators.GetMeta(kind).Associativity.Should().Be(Associativity.Left);
    }

    [Theory]
    [InlineData(OperatorKind.Not)]
    [InlineData(OperatorKind.Negate)]
    public void GetMeta_RightAssociativeOperators(OperatorKind kind)
    {
        Operators.GetMeta(kind).Associativity.Should().Be(Associativity.Right);
    }

    [Theory]
    [InlineData(OperatorKind.Equals)]
    [InlineData(OperatorKind.NotEquals)]
    [InlineData(OperatorKind.CaseInsensitiveEquals)]
    [InlineData(OperatorKind.CaseInsensitiveNotEquals)]
    [InlineData(OperatorKind.LessThan)]
    [InlineData(OperatorKind.GreaterThan)]
    [InlineData(OperatorKind.LessThanOrEqual)]
    [InlineData(OperatorKind.GreaterThanOrEqual)]
    [InlineData(OperatorKind.Contains)]
    public void GetMeta_NonAssociativeOperators(OperatorKind kind)
    {
        Operators.GetMeta(kind).Associativity.Should().Be(Associativity.NonAssociative);
    }

    // ── Precedence ordering invariants ──────────────────────────────────────────

    [Fact]
    public void Precedence_MultiplicativeBindsTighterThanAdditive()
    {
        Operators.GetMeta(OperatorKind.Times).Precedence
            .Should().BeGreaterThan(Operators.GetMeta(OperatorKind.Plus).Precedence);
    }

    [Fact]
    public void Precedence_AdditiveBindsTighterThanComparison()
    {
        Operators.GetMeta(OperatorKind.Plus).Precedence
            .Should().BeGreaterThan(Operators.GetMeta(OperatorKind.Equals).Precedence);
    }

    [Fact]
    public void Precedence_ComparisonBindsTighterThanLogicalAnd()
    {
        Operators.GetMeta(OperatorKind.Equals).Precedence
            .Should().BeGreaterThan(Operators.GetMeta(OperatorKind.And).Precedence);
    }

    [Fact]
    public void Precedence_AndBindsTighterThanOr()
    {
        Operators.GetMeta(OperatorKind.And).Precedence
            .Should().BeGreaterThan(Operators.GetMeta(OperatorKind.Or).Precedence);
    }

    [Fact]
    public void Precedence_UnaryNegateBindsTighterThanBinaryMultiplicative()
    {
        Operators.GetMeta(OperatorKind.Negate).Precedence
            .Should().BeGreaterThan(Operators.GetMeta(OperatorKind.Times).Precedence);
    }

    [Fact]
    public void Precedence_NotBindsBetweenAndAndComparison()
    {
        var not = Operators.GetMeta(OperatorKind.Not).Precedence;
        not.Should().BeGreaterThan(Operators.GetMeta(OperatorKind.And).Precedence);
        not.Should().BeLessThan(Operators.GetMeta(OperatorKind.Equals).Precedence);
    }

    [Fact]
    public void Precedence_ContainsBindsBetweenComparisonAndAdditive()
    {
        var contains = Operators.GetMeta(OperatorKind.Contains).Precedence;
        contains.Should().BeGreaterThan(Operators.GetMeta(OperatorKind.Equals).Precedence);
        contains.Should().BeLessThan(Operators.GetMeta(OperatorKind.Plus).Precedence);
    }

    // ── ByToken index ───────────────────────────────────────────────────────────

    [Fact]
    public void ByToken_CountMatchesAll()
    {
        // All has 18 entries. ByToken has 18 entries too — Minus and Negate
        // share TokenKind.Minus but differ on Arity, so both have unique keys.
        Operators.ByToken.Count.Should().Be(Operators.All.Count);
    }

    [Fact]
    public void ByToken_Lookup_ReturnsCorrectOperator()
    {
        var plus = Operators.ByToken[(TokenKind.Plus, Arity.Binary)];
        plus.Kind.Should().Be(OperatorKind.Plus);
    }

    [Fact]
    public void ByToken_Minus_ResolvesToBinaryOrUnaryByArity()
    {
        var binaryMinus = Operators.ByToken[(TokenKind.Minus, Arity.Binary)];
        binaryMinus.Kind.Should().Be(OperatorKind.Minus);

        var unaryMinus = Operators.ByToken[(TokenKind.Minus, Arity.Unary)];
        unaryMinus.Kind.Should().Be(OperatorKind.Negate);
    }

    [Fact]
    public void ByToken_RoundTrip_AllEntriesRetrievable()
    {
        foreach (var meta in Operators.All)
        {
            Operators.ByToken.TryGetValue((meta.Token.Kind, meta.Arity), out var found)
                .Should().BeTrue($"{meta.Kind} should be in ByToken");
            found!.Kind.Should().Be(meta.Kind);
        }
    }

    // ── Ordering invariants ──────────────────────────────────────────────────────

    // X13
    [Fact]
    public void All_IsInAscendingOrder()
    {
        var kinds = Operators.All.Select(m => (int)m.Kind).ToList();
        kinds.Should().BeInAscendingOrder("Operators.All should be ordered by OperatorKind value");
    }

    // ── Boundary: Arrow is not an expression operator ───────────────────────────────

    // X17
    [Fact]
    public void ByToken_DoesNotContainArrow()
    {
        // Arrow '->' is a structural action-chain separator, not an expression operator.
        // It must not appear in the Operators ByToken index under any Arity.
        var arrowInBinary = Operators.ByToken.ContainsKey((TokenKind.Arrow, Arity.Binary));
        var arrowInUnary  = Operators.ByToken.ContainsKey((TokenKind.Arrow, Arity.Unary));
        arrowInBinary.Should().BeFalse("Arrow '->' is not a binary expression operator");
        arrowInUnary.Should().BeFalse("Arrow '->' is not a unary expression operator");
    }
}
