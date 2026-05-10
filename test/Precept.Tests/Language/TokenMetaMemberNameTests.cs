using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class TokenMetaMemberNameTests
{
    [Theory]
    [InlineData(TokenKind.Min)]
    [InlineData(TokenKind.Max)]
    [InlineData(TokenKind.Countof)]
    [InlineData(TokenKind.Peekby)]
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
    public void AccessorKeyword_AppearsInParserMemberNameVocabulary(TokenKind kind)
        => Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(kind);

    [Theory]
    [MemberData(nameof(AllKeywordKindsExceptValidMemberNames))]
    public void NonAccessorKeyword_DoesNotAppearInParserMemberNameVocabulary(TokenKind kind)
        => Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().NotContain(kind);

    public static TheoryData<TokenKind> AllKeywordKindsExceptValidMemberNames()
    {
        var data = new TheoryData<TokenKind>();
        var validMemberNames = Precept.Pipeline.Parser.KeywordsValidAsMemberName.ToHashSet();

        foreach (var kind in Tokens.Keywords.Values.Distinct().Order())
            if (!validMemberNames.Contains(kind))
                data.Add(kind);

        return data;
    }
}
