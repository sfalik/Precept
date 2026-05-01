using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Precept0023Tests
{
    private const string OperatorStubs = @"
namespace Precept.Language
{
    public enum OperatorKind { Or, And, IsSet, IsNotSet, Extra1 }
    public enum TokenKind { Or, And, Is, Set, Not }
    public enum Arity { Unary = 1, Binary = 2 }
    public enum OperatorFamily { Logical = 1, Presence = 2 }
    public enum Associativity { Left = 1 }
    
    public sealed record TokenMeta(TokenKind Kind, string Text);
    
    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Or  => new(kind, ""or""),
            TokenKind.And => new(kind, ""and""),
            TokenKind.Is  => new(kind, ""is""),
            TokenKind.Set => new(kind, ""set""),
            TokenKind.Not => new(kind, ""not""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
    
    public abstract record OperatorMeta(
        OperatorKind Kind, string Description, Arity Arity,
        Associativity Associativity, int Precedence, OperatorFamily Family);
    
    public sealed record SingleTokenOp(
        OperatorKind Kind, TokenMeta Token, string Description, Arity Arity,
        Associativity Associativity, int Precedence, OperatorFamily Family)
        : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family);
    
    public sealed record MultiTokenOp(
        OperatorKind Kind, System.Collections.Generic.IReadOnlyList<TokenMeta> Tokens, string Description, Arity Arity,
        Associativity Associativity, int Precedence, OperatorFamily Family)
        : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family);
    
    public static class Operators
    {
";

    private const string CloseBrace = @"
    }
}";

    [Fact]
    public async Task GivenValidMultiTokenOps_NoDiagnostic()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or       => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.IsSet    => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Set)], ""Is Set"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Presence),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0023OperatorsDUShapeInvariants>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenMultiTokenOpWithOneToken_ReportsPRECEPT0023a()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.IsSet => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Is)], ""Is"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Presence),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0023OperatorsDUShapeInvariants>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0023OperatorsDUShapeInvariants.DiagnosticId_TooFewTokens)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("IsSet")
                .And.Contain("1")
                .And.Contain("at least 2");
    }

    [Fact]
    public async Task GivenMultiTokenOpWithZeroTokens_ReportsPRECEPT0023a()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.IsSet => new MultiTokenOp(kind, [], ""Empty"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Presence),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0023OperatorsDUShapeInvariants>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0023OperatorsDUShapeInvariants.DiagnosticId_TooFewTokens)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("IsSet")
                .And.Contain("0");
    }

    [Fact]
    public async Task GivenSingleTokenOpAndMultiTokenOpWithSameLeadToken_ReportsPRECEPT0023b()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or    => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.IsSet => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Or), Tokens.GetMeta(TokenKind.Set)], ""Or Set"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Presence),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0023OperatorsDUShapeInvariants>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0023OperatorsDUShapeInvariants.DiagnosticId_SingleMultiCollision)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("IsSet")
                .And.Contain("Or")
                .And.Contain("Or");
    }

    [Fact]
    public async Task GivenTwoMultiTokenOpsWithSameFullSequence_ReportsPRECEPT0023c()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.IsSet  => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Set)], ""Is Set"", Arity.Binary, Associativity.Left, 60, OperatorFamily.Presence),
            OperatorKind.Extra1 => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Set)], ""Also Is Set"", Arity.Binary, Associativity.Left, 60, OperatorFamily.Presence),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0023OperatorsDUShapeInvariants>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0023OperatorsDUShapeInvariants.DiagnosticId_MultiLeadCollision)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("Extra1")
                .And.Contain("Is,Set")
                .And.Contain("IsSet");
    }

    [Fact]
    public async Task GivenTwoMultiTokenOpsWithSameLeadButDifferentFullSequence_NoDiagnostic()
    {
        // IsSet=[Is,Set] and IsNotSet=[Is,Not,Set] share lead token Is but have distinct full sequences.
        // This is the real catalog pattern — must produce zero PRECEPT0023c diagnostics.
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.IsSet    => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Set)], ""Is Set"", Arity.Binary, Associativity.Left, 60, OperatorFamily.Presence),
            OperatorKind.IsNotSet => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Not), Tokens.GetMeta(TokenKind.Set)], ""Is Not Set"", Arity.Binary, Associativity.Left, 60, OperatorFamily.Presence),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0023OperatorsDUShapeInvariants>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0023OperatorsDUShapeInvariants.DiagnosticId_MultiLeadCollision)
            .Should().BeEmpty();
    }
}
