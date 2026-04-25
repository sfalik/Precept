using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class TokensTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllTokenKinds))]
    public void GetMeta_ReturnsWithoutThrowing_ForEveryTokenKind(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Should().NotBeNull();
    }

    [Fact]
    public void All_ContainsExactlyAsManyEntries_AsEnumValues()
    {
        var expected = Enum.GetValues<TokenKind>().Length;
        Tokens.All.Should().HaveCount(expected);
    }

    [Theory]
    [MemberData(nameof(AllTokenKinds))]
    public void GetMeta_EveryEntry_HasNonNullKindAndNonEmptyDescription(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Kind.Should().Be(kind);
        meta.Description.Should().NotBeNullOrWhiteSpace();
    }

    // ── Text field rules ────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_KeywordTokens_HaveNonNullText()
    {
        // Synthetic value-bearing literal tokens (NumberLiteral, StringLiteral, etc.)
        // share the Literal category but carry null Text at catalog level — they are
        // excluded here. The SyntheticTokens test asserts their null Text separately.
        var syntheticKinds = SyntheticTokenKindSet();

        Tokens.All
            .Where(m => !syntheticKinds.Contains(m.Kind)
                && m.Categories.Any(c => c is
                    TokenCategory.Declaration or TokenCategory.Preposition or
                    TokenCategory.Control or TokenCategory.Action or
                    TokenCategory.Outcome or TokenCategory.AccessMode or
                    TokenCategory.LogicalOperator or TokenCategory.Membership or
                    TokenCategory.Quantifier or TokenCategory.StateModifier or
                    TokenCategory.Constraint or TokenCategory.Type or
                    TokenCategory.Literal))
            .Should().AllSatisfy(m =>
                m.Text.Should().NotBeNull($"keyword token {m.Kind} should have non-null Text"));
    }

    [Theory]
    [MemberData(nameof(SyntheticTokenKinds))]
    public void GetMeta_SyntheticTokens_HaveNullText(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Text.Should().BeNull($"{kind} is a synthetic token and should have null Text");
    }

    [Fact]
    public void GetMeta_OperatorTokens_HaveNonNullText()
    {
        Tokens.All
            .Where(m => m.Categories.Any(c => c == TokenCategory.Operator))
            .Should().AllSatisfy(m =>
                m.Text.Should().NotBeNull($"operator token {m.Kind} should have non-null Text"));
    }

    [Fact]
    public void GetMeta_PunctuationTokens_HaveNonNullText()
    {
        Tokens.All
            .Where(m => m.Categories.Any(c => c == TokenCategory.Punctuation))
            .Should().AllSatisfy(m =>
                m.Text.Should().NotBeNull($"punctuation token {m.Kind} should have non-null Text"));
    }

    // ── Category rules ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllTokenKinds))]
    public void GetMeta_EveryEntry_HasAtLeastOneCategory(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public void GetMeta_SetToken_HasBothActionAndTypeCategories()
    {
        var meta = Tokens.GetMeta(TokenKind.Set);
        meta.Categories.Should().Contain(TokenCategory.Action);
        meta.Categories.Should().Contain(TokenCategory.Type);
    }

    [Theory]
    [InlineData(TokenKind.Min)]
    [InlineData(TokenKind.Max)]
    public void GetMeta_MinAndMax_HaveBothConstraintAndControlCategories(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Categories.Should().Contain(TokenCategory.Constraint);
        meta.Categories.Should().Contain(TokenCategory.Control);
    }

    // ── Keywords dictionary ─────────────────────────────────────────────────────

    [Fact]
    public void Keywords_ContainsAllKeywordCategoryTokensWithNonNullText()
    {
        // Keywords must contain exactly the distinct texts of all keyword-category tokens
        // with non-null text, excluding SetType (parser-synthesized, never emitted by lexer).
        var expectedKeys = Tokens.All
            .Where(m => m.Text is not null
                && m.Kind != TokenKind.SetType
                && m.Categories.Any(c => c is
                    TokenCategory.Declaration or TokenCategory.Preposition or
                    TokenCategory.Control or TokenCategory.Action or
                    TokenCategory.Outcome or TokenCategory.AccessMode or
                    TokenCategory.LogicalOperator or TokenCategory.Membership or
                    TokenCategory.Quantifier or TokenCategory.StateModifier or
                    TokenCategory.Constraint or TokenCategory.Type or
                    TokenCategory.Literal))
            .Select(m => m.Text!)
            .ToHashSet();

        Tokens.Keywords.Keys.Should().BeEquivalentTo(expectedKeys);
    }

    [Fact]
    public void Keywords_ExcludesSetType()
    {
        Tokens.Keywords.Values.Should().NotContain(TokenKind.SetType,
            "SetType is parser-synthesized and must not appear in the Keywords dictionary");
    }

    [Fact]
    public void Keywords_HasNoNullOrEmptyKeys()
    {
        Tokens.Keywords.Keys.Should().AllSatisfy(k =>
            k.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void Keywords_HasNoDuplicateKeys()
    {
        var keys = Tokens.Keywords.Keys.ToList();
        keys.Should().OnlyHaveUniqueItems("each keyword text maps to exactly one TokenKind");
    }

    [Fact]
    public void Keywords_ValueRoundtrips_ThroughGetMeta()
    {
        foreach (var (text, kind) in Tokens.Keywords)
        {
            Tokens.GetMeta(kind).Text.Should().Be(text,
                $"GetMeta({kind}).Text should equal the Keywords key '{text}'");
        }
    }

    // ── Structural ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TokenKind.EndOfSource)]
    [InlineData(TokenKind.NewLine)]
    [InlineData(TokenKind.Comment)]
    public void EndOfSource_NewLine_Comment_AreStructural(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Categories.Should().ContainSingle()
            .Which.Should().Be(TokenCategory.Structural,
                $"{kind} should have only the Structural category");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static TheoryData<TokenKind> AllTokenKinds()
    {
        var data = new TheoryData<TokenKind>();
        foreach (var kind in Enum.GetValues<TokenKind>())
            data.Add(kind);
        return data;
    }

    public static TheoryData<TokenKind> SyntheticTokenKinds() => new()
    {
        TokenKind.Identifier,
        TokenKind.NumberLiteral,
        TokenKind.StringLiteral,
        TokenKind.StringStart,
        TokenKind.StringMiddle,
        TokenKind.StringEnd,
        TokenKind.TypedConstant,
        TokenKind.TypedConstantStart,
        TokenKind.TypedConstantMiddle,
        TokenKind.TypedConstantEnd,
        TokenKind.EndOfSource,
        TokenKind.NewLine,
        TokenKind.Comment,
    };

    private static HashSet<TokenKind> SyntheticTokenKindSet() =>
    [
        TokenKind.Identifier,
        TokenKind.NumberLiteral,
        TokenKind.StringLiteral,
        TokenKind.StringStart,
        TokenKind.StringMiddle,
        TokenKind.StringEnd,
        TokenKind.TypedConstant,
        TokenKind.TypedConstantStart,
        TokenKind.TypedConstantMiddle,
        TokenKind.TypedConstantEnd,
        TokenKind.EndOfSource,
        TokenKind.NewLine,
        TokenKind.Comment,
    ];
}
