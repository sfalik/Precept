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
    public void GetMeta_SetToken_HasActionCategory_NotTypeCategory()
    {
        var meta = Tokens.GetMeta(TokenKind.Set);
        meta.Categories.Should().Contain(TokenCategory.Action);
        meta.Categories.Should().NotContain(TokenCategory.Type);
    }

    [Fact]
    public void GetMeta_SetTypeToken_HasTypeCategory_NotActionCategory()
    {
        var meta = Tokens.GetMeta(TokenKind.SetType);
        meta.Categories.Should().Contain(TokenCategory.Type);
        meta.Categories.Should().NotContain(TokenCategory.Action);
    }

    [Theory]
    [InlineData(TokenKind.Min)]
    [InlineData(TokenKind.Max)]
    public void GetMeta_MinAndMax_HaveConstraintCategory(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);
        meta.Categories.Should().Contain(TokenCategory.Constraint);
        meta.Categories.Should().NotContain(TokenCategory.Control,
            "min/max are constraint keywords — their function usage is cataloged in FunctionKind, not TokenCategory");
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

    // M7 ── VisualCategory ──────────────────────────────────────────────────

    [Fact]
    public void AllTextualTokens_HaveVisualCategory()
    {
        Tokens.All
            .Where(m => m.Text is not null)
            .Should().AllSatisfy(m =>
                m.VisualCategory.Should().HaveValue(
                    $"textual token {m.Kind} should have a VisualCategory"));
    }

    [Fact]
    public void TypeKeywords_HaveTypeVisualCategory()
    {
        var syntheticKinds = SyntheticTokenKindSet();
        Tokens.All
            .Where(m => !syntheticKinds.Contains(m.Kind) && m.Categories.Any(c => c == TokenCategory.Type))
            .Should().AllSatisfy(m =>
                m.VisualCategory.Should().Be(SemanticTokenTypeKind.Type,
                    $"{m.Kind} is a type keyword"));
    }

    [Fact]
    public void StateModifiers_HaveKeywordSemanticVisualCategory()
    {
        Tokens.All
            .Where(m => m.Categories.Any(c => c == TokenCategory.StateModifier))
            .Should().AllSatisfy(m =>
                m.VisualCategory.Should().Be(SemanticTokenTypeKind.KeywordSemantic,
                    $"{m.Kind} is a state modifier"));
    }

    [Fact]
    public void StructuralTokens_HaveNullVisualCategory()
    {
        Tokens.GetMeta(TokenKind.EndOfSource).VisualCategory.Should().BeNull(
            "EndOfSource is synthetic and has no visual category");
        Tokens.GetMeta(TokenKind.NewLine).VisualCategory.Should().BeNull(
            "NewLine is synthetic and has no visual category");
    }

    [Theory]
    [InlineData(TokenKind.And)]
    [InlineData(TokenKind.Or)]
    [InlineData(TokenKind.Not)]
    public void LogicalOperators_HaveKeywordGrammarVisualCategory(TokenKind kind)
    {
        Tokens.GetMeta(kind).VisualCategory.Should().Be(SemanticTokenTypeKind.KeywordGrammar,
            $"{kind} is a logical operator keyword");
    }

    [Fact]
    public void Operators_HaveOperatorVisualCategory()
    {
        Tokens.All
            .Where(m => m.Categories.Any(c => c == TokenCategory.Operator))
            .Should().AllSatisfy(m =>
                m.VisualCategory.Should().Be(SemanticTokenTypeKind.Operator,
                    $"{m.Kind} is an operator"));
    }

    [Fact]
    public void BooleanLiterals_HaveValueVisualCategory()
    {
        Tokens.GetMeta(TokenKind.True).VisualCategory.Should().Be(SemanticTokenTypeKind.Value);
        Tokens.GetMeta(TokenKind.False).VisualCategory.Should().Be(SemanticTokenTypeKind.Value);
    }

    [Theory]
    [InlineData(TokenKind.NumberLiteral)]
    [InlineData(TokenKind.StringLiteral)]
    [InlineData(TokenKind.StringStart)]
    [InlineData(TokenKind.StringMiddle)]
    [InlineData(TokenKind.StringEnd)]
    [InlineData(TokenKind.TypedConstant)]
    [InlineData(TokenKind.TypedConstantStart)]
    [InlineData(TokenKind.TypedConstantMiddle)]
    [InlineData(TokenKind.TypedConstantEnd)]
    public void LiteralFragments_HaveNoVisualCategory(TokenKind kind)
    {
        Tokens.GetMeta(kind).VisualCategory.Should().BeNull(
            $"{kind} has no semantic-token visual category");
    }

    // ── Ordering invariants ────────────────────────────────────────────────────────

    // X12
    [Fact]
    public void All_IsInAscendingOrder()
    {
        var kinds = Tokens.All.Select(m => (int)m.Kind).ToList();
        kinds.Should().BeInAscendingOrder("Tokens.All should be ordered by TokenKind value");
    }

    // ── Boundary category tests ──────────────────────────────────────────────────

    // X16
    [Fact]
    public void Tilde_IsOperator_NotType()
    {
        var meta = Tokens.GetMeta(TokenKind.Tilde);
        meta.Categories.Should().Contain(TokenCategory.Operator,
            "Tilde '~' is the case-insensitive collection inner-type prefix operator");
        meta.Categories.Should().NotContain(TokenCategory.Type,
            "Tilde is not a type keyword");
    }

    // X17
    [Fact]
    public void Arrow_IsOperator_NotStructural()
    {
        // Arrow '->' is the action-chain / outcome separator. Spec §1.1 places it in
        // the Operators table. It must be categorized as Operator so TwoCharOperators
        // and operator-driven consumers include it correctly (GAP-017 fix, iter 7).
        var meta = Tokens.GetMeta(TokenKind.Arrow);
        meta.Categories.Should().Contain(TokenCategory.Operator,
            "spec §1.1 places Arrow '->' in the Operators table");
        meta.Categories.Should().NotContain(TokenCategory.Structural,
            "Arrow is not structural — recategorized to Operator (GAP-017 fix)");
        meta.Categories.Should().NotContain(TokenCategory.Punctuation,
            "Arrow is not punctuation");
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
        TokenKind.SetType,  // Parser-synthesized alias from TokenKind.Set
    ];

    // ── AccessModeKeywords ───────────────────────────────────────────────────────

    [Fact]
    public void Tokens_AccessModeKeywords_ContainsReadonlyAndEditable()
    {
        Tokens.AccessModeKeywords.Should().Contain(TokenKind.Readonly);
        Tokens.AccessModeKeywords.Should().Contain(TokenKind.Editable);
    }
}
