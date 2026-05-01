using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Precept0021Tests
{
    private const string TokenStubs = @"
namespace Precept.Language
{
    public enum TokenKind { Precept, State, Event, Set, Extra1, Identifier }
    public enum TokenCategory { Declaration = 1, Action = 2 }
    
    public sealed record TokenMeta(TokenKind Kind, string? Text);
    
    public static class Tokens
    {
";

    private const string CloseBrace = @"
    }
}";

    [Fact]
    public async Task GivenTokensWithAllDistinctText_NoDiagnostic()
    {
        var source = TokenStubs + @"
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Precept => new(kind, ""precept""),
            TokenKind.State   => new(kind, ""state""),
            TokenKind.Event   => new(kind, ""event""),
            TokenKind.Set     => new(kind, ""set""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0021TokensDuplicateText>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenSyntheticTokensWithNullText_NoDiagnostic()
    {
        var source = TokenStubs + @"
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Precept    => new(kind, ""precept""),
            TokenKind.Identifier => new(kind, null),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0021TokensDuplicateText>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenTwoArmsWithSameText_ReportsPRECEPT0021()
    {
        var source = TokenStubs + @"
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Precept => new(kind, ""state""),
            TokenKind.State   => new(kind, ""state""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0021TokensDuplicateText>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0021TokensDuplicateText.DiagnosticId)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("State")
                .And.Contain("state")
                .And.Contain("Precept");
    }

    [Fact]
    public async Task GivenThreeArmsWithSameText_ReportsTwoDiagnostics()
    {
        var source = TokenStubs + @"
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Precept => new(kind, ""set""),
            TokenKind.State   => new(kind, ""set""),
            TokenKind.Set     => new(kind, ""set""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0021TokensDuplicateText>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0021TokensDuplicateText.DiagnosticId)
            .Should().HaveCount(2);
    }
}
