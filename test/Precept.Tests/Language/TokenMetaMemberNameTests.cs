using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class TokenMetaMemberNameTests
{
    [Fact]
    public void TokenMeta_Min_IsValidAsMemberName_True()
        => Tokens.GetMeta(TokenKind.Min).IsValidAsMemberName.Should().BeTrue();

    [Fact]
    public void TokenMeta_Max_IsValidAsMemberName_True()
        => Tokens.GetMeta(TokenKind.Max).IsValidAsMemberName.Should().BeTrue();

    [Theory]
    [MemberData(nameof(AllKindsExceptMinMax))]
    public void TokenMeta_AllOtherKeywords_IsValidAsMemberName_False(TokenKind kind)
        => Tokens.GetMeta(kind).IsValidAsMemberName.Should().BeFalse();

    [Fact]
    public void Parser_KeywordsValidAsMemberName_ContainsMinAndMax()
    {
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.Min);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.Max);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().HaveCount(2);
    }

    public static TheoryData<TokenKind> AllKindsExceptMinMax()
    {
        var data = new TheoryData<TokenKind>();
        foreach (var kind in Enum.GetValues<TokenKind>())
            if (kind != TokenKind.Min && kind != TokenKind.Max)
                data.Add(kind);
        return data;
    }
}
