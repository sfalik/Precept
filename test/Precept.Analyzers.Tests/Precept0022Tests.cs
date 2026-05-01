using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Precept0022Tests
{
    private const string OperatorStubs = @"
namespace Precept.Language
{
    public enum OperatorKind { Or, And, Plus }
    public enum TokenKind { Or, And, Plus }
    public enum Arity { Binary = 2 }
    public enum OperatorFamily { Logical = 1, Arithmetic = 2 }
    public enum Associativity { Left = 1 }
    
    public sealed record TokenMeta(TokenKind Kind, string Text);
    
    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Or   => new(kind, ""or""),
            TokenKind.And  => new(kind, ""and""),
            TokenKind.Plus => new(kind, ""+""),
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
    
    public static class Operators
    {
";

    private const string CloseBrace = @"
    }
}";

    [Fact]
    public async Task GivenAllOperatorsUseTokensGetMeta_NoDiagnostic()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or   => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.And  => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.And), ""And"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Logical),
            OperatorKind.Plus => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Plus), ""Plus"", Arity.Binary, Associativity.Left, 30, OperatorFamily.Arithmetic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0022OperatorsInlineToken>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenOperatorWithInlineTokenMeta_ReportsPRECEPT0022()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or   => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.Plus => new SingleTokenOp(kind, new TokenMeta(TokenKind.Plus, ""+""), ""Plus"", Arity.Binary, Associativity.Left, 30, OperatorFamily.Arithmetic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0022OperatorsInlineToken>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0022OperatorsInlineToken.DiagnosticId)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("Plus")
                .And.Contain("Tokens.GetMeta");
    }

    [Fact]
    public async Task GivenMultipleOperatorsWithInlineTokenMeta_ReportsMultipleDiagnostics()
    {
        var source = OperatorStubs + @"
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Or   => new SingleTokenOp(kind, new TokenMeta(TokenKind.Or, ""or""), ""Or"", Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
            OperatorKind.And  => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.And), ""And"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Logical),
            OperatorKind.Plus => new SingleTokenOp(kind, new TokenMeta(TokenKind.Plus, ""+""), ""Plus"", Arity.Binary, Associativity.Left, 30, OperatorFamily.Arithmetic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0022OperatorsInlineToken>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0022OperatorsInlineToken.DiagnosticId)
            .Should().HaveCount(2);
    }
}
