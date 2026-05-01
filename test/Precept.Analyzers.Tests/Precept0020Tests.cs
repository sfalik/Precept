using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Precept0020Tests
{
    private const string OperatorStubs = @"
namespace Precept.Language
{
    public enum OperatorKind { Or, And, Not, Plus, Minus, Negate, IsSet, IsNotSet, Extra1, Extra2 }
    public enum TokenKind { Or, And, Not, Plus, Minus, Is, Set }
    public enum Arity { Unary = 1, Binary = 2, Postfix = 3 }
    public enum OperatorFamily { Arithmetic = 1, Logical = 2, Presence = 3 }
    public enum Associativity { Left = 1, Right = 2, NonAssociative = 3 }
    
    public sealed record TokenMeta(TokenKind Kind, string Text);
    
    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Or  => new(kind, ""or""),
            TokenKind.And => new(kind, ""and""),
            TokenKind.Not => new(kind, ""not""),
            TokenKind.Plus  => new(kind, ""+""),
            TokenKind.Minus => new(kind, ""-""),
            TokenKind.Is    => new(kind, ""is""),
            TokenKind.Set   => new(kind, ""set""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
    
    public abstract record OperatorMeta(
        OperatorKind Kind, string Description, Arity Arity,
        Associativity Associativity, int Precedence, OperatorFamily Family,
        bool IsKeywordOperator = false, string HoverDescription = null, string UsageExample = null);
    
    public sealed record SingleTokenOp(
        OperatorKind Kind, TokenMeta Token, string Description, Arity Arity,
        Associativity Associativity, int Precedence, OperatorFamily Family,
        bool IsKeywordOperator = false, string HoverDescription = null, string UsageExample = null)
        : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family, IsKeywordOperator, HoverDescription, UsageExample);
    
    public sealed record MultiTokenOp(
        OperatorKind Kind, System.Collections.Generic.IReadOnlyList<TokenMeta> Tokens, string Description, Arity Arity,
        Associativity Associativity, int Precedence, OperatorFamily Family,
        bool IsKeywordOperator = false, string HoverDescription = null, string UsageExample = null)
        : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family, IsKeywordOperator, HoverDescription, UsageExample);
";

    private const string CloseBrace = @"
}";

    [Fact]
    public async Task GivenOperatorsWithAllDistinctTokenArityPairs_NoDiagnostic()
    {
        var source = OperatorStubs + @"
    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or     => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or),   ""Or"",  Arity.Binary,  Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.And    => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.And),  ""And"", Arity.Binary,  Associativity.Left, 20, OperatorFamily.Logical),
            OperatorKind.Plus   => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Plus), ""Plus"", Arity.Binary,  Associativity.Left, 30, OperatorFamily.Arithmetic),
            OperatorKind.Negate => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Minus), ""Negate"", Arity.Unary, Associativity.Right, 40, OperatorFamily.Arithmetic),
            OperatorKind.Minus  => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Minus), ""Minus"",  Arity.Binary, Associativity.Left, 50, OperatorFamily.Arithmetic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
" + CloseBrace;

        await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
    }

    [Fact]
    public async Task GivenTwoUnaryAndOneBinaryOnSameToken_NoDiagnostic()
    {
        var source = OperatorStubs + @"
    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Minus  => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Minus), ""Minus"", Arity.Binary,  Associativity.Left, 10, OperatorFamily.Arithmetic),
            OperatorKind.Negate => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Minus), ""Negate"", Arity.Unary, Associativity.Right, 20, OperatorFamily.Arithmetic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
" + CloseBrace;

        await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
    }

    [Fact]
    public async Task GivenTwoArmsWithSameTokenAndSameArity_ReportsPRECEPT0020a()
    {
        var source = OperatorStubs + @"
    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Not    => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Not), ""Not"", Arity.Unary, Associativity.Right, 10, OperatorFamily.Logical),
            OperatorKind.Extra1 => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Not), ""Extra"", Arity.Unary, Associativity.Right, 20, OperatorFamily.Logical),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0020OperatorsTokenCollision.DiagnosticId_ByTokenCollision)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("Extra1")
                .And.Contain("Not")
                .And.Contain("Unary")
                .And.Contain("Not");
    }

    [Fact]
    public async Task GivenTwoBinaryArmsWithSameToken_ReportsPRECEPT0020b()
    {
        var source = OperatorStubs + @"
    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or     => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.Extra1 => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Extra"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Logical),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0020OperatorsTokenCollision.DiagnosticId_PrecedenceCollision)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("Extra1")
                .And.Contain("Or")
                .And.Contain("Or");
    }

    [Fact]
    public async Task GivenTwoBinaryArmsWithSameToken_ReportsBoth0020a_And_0020b()
    {
        var source = OperatorStubs + @"
    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or     => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.Extra1 => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Extra"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Logical),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
        diagnostics.Where(d => d.Id == PRECEPT0020OperatorsTokenCollision.DiagnosticId_ByTokenCollision)
            .Should().ContainSingle();
        diagnostics.Where(d => d.Id == PRECEPT0020OperatorsTokenCollision.DiagnosticId_PrecedenceCollision)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task GivenOperatorWithInlineToken_DoesNotCrash()
    {
        var source = OperatorStubs + @"
    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus => new SingleTokenOp(kind, new TokenMeta(TokenKind.Plus, ""+""), ""Plus"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Arithmetic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
" + CloseBrace;

        await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
    }
}
