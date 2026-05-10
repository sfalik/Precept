using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseATokenCatalogTests
{
    [Fact]
    public void Any_IsStateWildcard_True()
        => Tokens.GetMeta(TokenKind.Any).IsStateWildcard.Should().BeTrue();

    [Fact]
    public void All_IsBroadcastFieldTarget_True()
        => Tokens.GetMeta(TokenKind.All).IsBroadcastFieldTarget.Should().BeTrue();

    [Theory]
    [InlineData(TokenKind.Min)]
    [InlineData(TokenKind.Max)]
    public void MinAndMax_AreAlsoBuiltinFunctions(TokenKind kind)
        => Tokens.GetMeta(kind).IsAlsoBuiltinFunction.Should().BeTrue();

    [Theory]
    [InlineData(TokenKind.CurrencyType)]
    [InlineData(TokenKind.DateType)]
    [InlineData(TokenKind.TimeType)]
    [InlineData(TokenKind.InstantType)]
    [InlineData(TokenKind.TimezoneType)]
    [InlineData(TokenKind.DateTimeType)]
    [InlineData(TokenKind.DimensionType)]
    [InlineData(TokenKind.From)]
    [InlineData(TokenKind.To)]
    [InlineData(TokenKind.At)]
    public void KeywordAccessors_AreValidMemberNames(TokenKind kind)
        => Tokens.GetMeta(kind).IsValidAsMemberName.Should().BeTrue();

    [Fact]
    public void KeywordsValidAsMemberName_DerivesFromTokenCatalog()
    {
        var expected = Tokens.All
            .Where(meta => meta.IsValidAsMemberName)
            .Select(meta => meta.Kind);

        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().BeEquivalentTo(expected);
    }
}
