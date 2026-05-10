using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class TokenCatalogCapabilityTests
{
    [Fact]
    public void Any_IsStateWildcard_True()
        => CatalogCapabilityReflection.GetInstanceValue(Tokens.GetMeta(TokenKind.Any), "IsStateWildcard")
            .Should().Be(true);

    [Fact]
    public void All_IsBroadcastFieldTarget_True()
        => CatalogCapabilityReflection.GetInstanceValue(Tokens.GetMeta(TokenKind.All), "IsBroadcastFieldTarget")
            .Should().Be(true);

    [Fact]
    public void Min_IsAlsoBuiltinFunction_True()
        => CatalogCapabilityReflection.GetInstanceValue(Tokens.GetMeta(TokenKind.Min), "IsAlsoBuiltinFunction")
            .Should().Be(true);

    [Fact]
    public void Max_IsAlsoBuiltinFunction_True()
        => CatalogCapabilityReflection.GetInstanceValue(Tokens.GetMeta(TokenKind.Max), "IsAlsoBuiltinFunction")
            .Should().Be(true);

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
    public void TypeKeywords_IsValidAsMemberName_True(TokenKind kind)
        => Tokens.GetMeta(kind).IsValidAsMemberName.Should().BeTrue(
            $"{kind} should be available as a keyword member/accessor name");

    [Fact]
    public void KeywordsValidAsMemberName_DerivedFromCatalog()
    {
        var expected = Tokens.All
            .Where(meta => meta.IsValidAsMemberName)
            .Select(meta => meta.Kind);

        CatalogCapabilityReflection.GetStaticSequence<TokenKind>(
                typeof(Precept.Pipeline.Parser), "KeywordsValidAsMemberName")
            .Should().BeEquivalentTo(expected);
    }
}
