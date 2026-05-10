using System;
using System.Linq;
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



    public static TheoryData<TokenKind> AllKindsExceptValidMemberNames()
    {
        var data = new TheoryData<TokenKind>();
        var validMemberNames = Tokens.All
            .Where(meta => meta.IsValidAsMemberName)
            .Select(meta => meta.Kind)
            .ToHashSet();

        foreach (var kind in Enum.GetValues<TokenKind>())
            if (!validMemberNames.Contains(kind))
                data.Add(kind);
        return data;
    }
}
