using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseATokenCatalogTests
{
    [Fact]
    public void TokenMeta_Shape_LocksTrack2Fields()
    {
        var propertyNames = typeof(TokenMeta)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        propertyNames.Should().HaveCount(12);
        propertyNames.Should().BeEquivalentTo(
        [
            nameof(TokenMeta.Kind),
            nameof(TokenMeta.Text),
            nameof(TokenMeta.Categories),
            nameof(TokenMeta.Description),
            nameof(TokenMeta.VisualCategory),
            nameof(TokenMeta.ValidAfter),
            nameof(TokenMeta.IsAccessModeAdjective),
            nameof(TokenMeta.IsStateWildcard),
            nameof(TokenMeta.IsFieldBroadcast),
            nameof(TokenMeta.IsFunctionCallLeader),
            nameof(TokenMeta.IsMessagePosition),
            nameof(TokenMeta.IsValidAsMemberName),
        ]);
    }

    [Fact]
    public void Any_IsStateWildcard_True()
        => Tokens.GetMeta(TokenKind.Any).IsStateWildcard.Should().BeTrue();

    [Fact]
    public void StateWildcardTokens_AreExactlyAny()
        => Tokens.All.Where(meta => meta.IsStateWildcard).Select(meta => meta.Kind)
            .Should().Equal(TokenKind.Any);

    [Fact]
    public void All_IsFieldBroadcast_True()
        => Tokens.GetMeta(TokenKind.All).IsFieldBroadcast.Should().BeTrue();

    [Fact]
    public void FieldBroadcastTokens_AreExactlyAll()
        => Tokens.All.Where(meta => meta.IsFieldBroadcast).Select(meta => meta.Kind)
            .Should().Equal(TokenKind.All);

    [Theory]
    [InlineData(TokenKind.Min)]
    [InlineData(TokenKind.Max)]
    public void MinAndMax_AreFunctionCallLeaders(TokenKind kind)
        => Tokens.GetMeta(kind).IsFunctionCallLeader.Should().BeTrue();

    [Fact]
    public void FunctionCallLeaderTokens_AreExactlyMinAndMax()
        => Tokens.All.Where(meta => meta.IsFunctionCallLeader).Select(meta => meta.Kind)
            .Should().Equal(TokenKind.Min, TokenKind.Max);

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
    public void AccessorKeywords_AppearInParserMemberNameVocabulary(TokenKind kind)
        => Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(kind);

    [Fact]
    public void KeywordsValidAsMemberName_DerivesFromTypeAccessors()
    {
        var expected = Types.All
            .SelectMany(meta => meta.Accessors)
            .Select(accessor => accessor.Name)
            .Distinct()
            .Select(name => Tokens.Keywords.TryGetValue(name, out var kind) ? kind : (TokenKind?)null)
            .Where(kind => kind is not null)
            .Select(kind => kind!.Value);

        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().BeEquivalentTo(expected);
    }
}
