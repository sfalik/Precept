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

    [Fact]
    public void TokenMeta_Countof_IsValidAsMemberName_True()
        => Tokens.GetMeta(TokenKind.Countof).IsValidAsMemberName.Should().BeTrue();

    [Fact]
    public void TokenMeta_Peekby_IsValidAsMemberName_True()
        => Tokens.GetMeta(TokenKind.Peekby).IsValidAsMemberName.Should().BeTrue();

    [Theory]
    [MemberData(nameof(AllKindsExceptValidMemberNames))]
    public void TokenMeta_AllOtherKeywords_IsValidAsMemberName_False(TokenKind kind)
        => Tokens.GetMeta(kind).IsValidAsMemberName.Should().BeFalse();

    [Fact]
    public void Parser_KeywordsValidAsMemberName_ContainsMinAndMax()
    {
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.Min);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.Max);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.Countof);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().Contain(TokenKind.Peekby);
        Precept.Pipeline.Parser.KeywordsValidAsMemberName.Should().HaveCount(4);
    }

    public static TheoryData<TokenKind> AllKindsExceptValidMemberNames()
    {
        var data = new TheoryData<TokenKind>();
        foreach (var kind in Enum.GetValues<TokenKind>())
            if (kind != TokenKind.Min && kind != TokenKind.Max &&
                kind != TokenKind.Countof && kind != TokenKind.Peekby)
                data.Add(kind);
        return data;
    }
}
