using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class ParserCatalogDerivationTests
{
    [Fact]
    public void KeywordsValidAsMemberName_DerivedFromCatalog()
    {
        var expected = Tokens.All
            .Where(meta => meta.IsValidAsMemberName)
            .Select(meta => meta.Kind);

        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().BeEquivalentTo(expected);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.At);
    }
}
