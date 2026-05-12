using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public sealed class SemanticTokenTypesCatalogTests
{
    [Fact]
    public void All_ContainsThirteenEntries()
        => SemanticTokenTypes.All.Should().HaveCount(13);

    [Fact]
    public void GetMeta_EachKind_ReturnsNonNullEntry()
    {
        foreach (var kind in Enum.GetValues<SemanticTokenTypeKind>())
        {
            var meta = SemanticTokenTypes.GetMeta(kind);
            meta.Should().NotBeNull();
            meta.CustomType.Should().NotBeNullOrEmpty();
            meta.TextMateScope.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void CustomTypes_AreUnique()
        => SemanticTokenTypes.All.Select(m => m.CustomType).Should().OnlyHaveUniqueItems();

    [Fact]
    public void TextMateScopes_AreUnique()
        => SemanticTokenTypes.All.Select(m => m.TextMateScope).Should().OnlyHaveUniqueItems();

    [Fact]
    public void SupportsConstrainedModifier_SetOnlyForCorrectKinds()
    {
        var constrained = new[]
        {
            SemanticTokenTypeKind.State,
            SemanticTokenTypeKind.Event,
            SemanticTokenTypeKind.FieldName,
            SemanticTokenTypeKind.ArgName
        };

        foreach (var kind in Enum.GetValues<SemanticTokenTypeKind>())
        {
            var expected = constrained.Contains(kind);
            SemanticTokenTypes.GetMeta(kind).SupportsConstrainedModifier
                .Should().Be(expected, because: $"{kind} SupportsConstrainedModifier should be {expected}");
        }
    }

    [Fact]
    public void Comment_IsItalic()
        => SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Comment).Italic.Should().BeTrue();

    [Fact]
    public void KeywordSemantic_IsBold()
        => SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordSemantic).Bold.Should().BeTrue();
}
