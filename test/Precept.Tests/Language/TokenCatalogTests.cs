using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public sealed class TokenCatalogTests
{
    [Fact]
    public void TokenMeta_HasNoTextMateScopeField()
        => typeof(TokenMeta).GetProperty("TextMateScope").Should().BeNull();

    [Fact]
    public void TokenMeta_HasNoSemanticTokenTypeField()
        => typeof(TokenMeta).GetProperty("SemanticTokenType").Should().BeNull();

    [Fact]
    public void TokenMeta_VisualCategory_WhenSet_MapsToValidKind()
    {
        foreach (var kind in Enum.GetValues<TokenKind>())
        {
            var meta = Tokens.GetMeta(kind);
            if (!meta.VisualCategory.HasValue) continue;

            var catalogEntry = SemanticTokenTypes.GetMeta(meta.VisualCategory.Value);
            catalogEntry.Should().NotBeNull();
        }
    }
}
