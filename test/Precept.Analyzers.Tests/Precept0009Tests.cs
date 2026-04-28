using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0009Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────

    private const string OpStubs = @"
namespace Precept.Language
{
    public enum OperationKind { NegInt, NegDec, IntPlusInt, IntMinusInt, IntPlusInt2, DecPlusDec, IntPlusDec, MoneyPlusMoney }
    public enum OperatorKind { Negate, Plus, Minus }
    public enum TypeKind { Integer, Decimal, Money }
    public enum QualifierMatch { Any, Same }

    public sealed record ParameterMeta(TypeKind Kind, string Name = null);

    public abstract record OperationMeta(
        OperationKind Kind, OperatorKind Op, TypeKind Result, string Description);

    public sealed record UnaryOperationMeta(
        OperationKind Kind, OperatorKind Op, ParameterMeta Operand, TypeKind Result, string Description)
        : OperationMeta(Kind, Op, Result, Description);

    public sealed record BinaryOperationMeta(
        OperationKind Kind, OperatorKind Op, ParameterMeta Lhs, ParameterMeta Rhs, TypeKind Result,
        string Description, bool BidirectionalLookup = false, QualifierMatch Match = QualifierMatch.Any)
        : OperationMeta(Kind, Op, Result, Description);
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0009a — Duplicate binary operation key
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Binary_DistinctKeys_NoDiagnostic()
    {
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntPlusInt => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""int+int""),
            OperationKind.IntMinusInt => new BinaryOperationMeta(
                kind, OperatorKind.Minus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""int-int""),
            OperationKind.DecPlusDec => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Decimal), new(TypeKind.Decimal), TypeKind.Decimal, ""dec+dec""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupBinaryKey)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Binary_DuplicateKey_ReportsError()
    {
        // Two arms with same (Plus, Integer, Integer, Any) key.
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntPlusInt => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""int+int first""),
            OperationKind.IntPlusInt2 => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""int+int duplicate""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        var dupDiags = diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupBinaryKey).ToList();
        dupDiags.Should().ContainSingle();
        dupDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        dupDiags[0].GetMessage().Should().Contain("Plus");
        dupDiags[0].GetMessage().Should().Contain("Integer");
    }

    [Fact]
    public async Task Binary_SameOpDifferentMatch_NoDiagnostic()
    {
        // Same (Plus, Money, Money) but different Match — distinct keys.
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntPlusInt => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Money), new(TypeKind.Money), TypeKind.Money, ""money+money any""),
            OperationKind.MoneyPlusMoney => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Money), new(TypeKind.Money), TypeKind.Money, ""money+money same"",
                Match: QualifierMatch.Same),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupBinaryKey)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Binary_SwappedOperands_NoDiagnostic()
    {
        // (Plus, Integer, Decimal) and (Plus, Decimal, Integer) — different keys.
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntPlusDec => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Decimal), TypeKind.Decimal, ""int+dec""),
            OperationKind.DecPlusDec => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Decimal), new(TypeKind.Integer), TypeKind.Decimal, ""dec+int""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupBinaryKey)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0009b — Duplicate unary operation key
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Unary_DistinctKeys_NoDiagnostic()
    {
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.NegInt => new UnaryOperationMeta(
                kind, OperatorKind.Negate, new(TypeKind.Integer), TypeKind.Integer, ""-int""),
            OperationKind.NegDec => new UnaryOperationMeta(
                kind, OperatorKind.Negate, new(TypeKind.Decimal), TypeKind.Decimal, ""-dec""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupUnaryKey)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Unary_DuplicateKey_ReportsError()
    {
        // Two arms with same (Negate, Integer) key.
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.NegInt => new UnaryOperationMeta(
                kind, OperatorKind.Negate, new(TypeKind.Integer), TypeKind.Integer, ""-int first""),
            OperationKind.NegDec => new UnaryOperationMeta(
                kind, OperatorKind.Negate, new(TypeKind.Integer), TypeKind.Integer, ""-int duplicate""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        var dupDiags = diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupUnaryKey).ToList();
        dupDiags.Should().ContainSingle();
        dupDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        dupDiags[0].GetMessage().Should().Contain("Negate");
        dupDiags[0].GetMessage().Should().Contain("Integer");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Mixed — unary and binary in same switch
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Mixed_UnaryAndBinary_NoDiagnostic()
    {
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.NegInt => new UnaryOperationMeta(
                kind, OperatorKind.Negate, new(TypeKind.Integer), TypeKind.Integer, ""-int""),
            OperationKind.IntPlusInt => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""int+int""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WrongEnumKind_NoDiagnostic()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { String }
    public sealed record TypeMeta(TypeKind Kind, string Description);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String => new(kind, ""text""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum OperationKind { A, B }
    public enum OperatorKind { Plus }
    public enum TypeKind { Integer }
    public enum QualifierMatch { Any }

    public sealed record ParameterMeta(TypeKind Kind, string Name = null);
    public abstract record OperationMeta(
        OperationKind Kind, OperatorKind Op, TypeKind Result, string Description);
    public sealed record BinaryOperationMeta(
        OperationKind Kind, OperatorKind Op, ParameterMeta Lhs, ParameterMeta Rhs,
        TypeKind Result, string Description, bool BidirectionalLookup = false,
        QualifierMatch Match = QualifierMatch.Any)
        : OperationMeta(Kind, Op, Result, Description);

    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.A => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""dup1""),
            OperationKind.B => new BinaryOperationMeta(
                kind, OperatorKind.Plus, new(TypeKind.Integer), new(TypeKind.Integer), TypeKind.Integer, ""dup2""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: shared static field references
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Binary_SharedStaticFields_DistinctKeys_NoDiagnostic()
    {
        // Uses shared static ParameterMeta fields like the real catalog.
        var source = OpStubs + @"
    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);
        private static readonly ParameterMeta PDecimal = new(TypeKind.Decimal);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntPlusInt => new BinaryOperationMeta(
                kind, OperatorKind.Plus, PInteger, PInteger, TypeKind.Integer, ""int+int""),
            OperationKind.IntMinusInt => new BinaryOperationMeta(
                kind, OperatorKind.Minus, PInteger, PInteger, TypeKind.Integer, ""int-int""),
            OperationKind.DecPlusDec => new BinaryOperationMeta(
                kind, OperatorKind.Plus, PDecimal, PDecimal, TypeKind.Decimal, ""dec+dec""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupBinaryKey)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Binary_SharedStaticFields_DuplicateKey_ReportsError()
    {
        // Two arms sharing same static PInteger with same operator → duplicate.
        var source = OpStubs + @"
    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntPlusInt => new BinaryOperationMeta(
                kind, OperatorKind.Plus, PInteger, PInteger, TypeKind.Integer, ""int+int first""),
            OperationKind.IntPlusInt2 => new BinaryOperationMeta(
                kind, OperatorKind.Plus, PInteger, PInteger, TypeKind.Integer, ""int+int dup""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        var dupDiags = diagnostics.Where(d => d.Id == PRECEPT0009OperationsCrossRef.DiagnosticId_DupBinaryKey).ToList();
        dupDiags.Should().ContainSingle();
        dupDiags[0].GetMessage().Should().Contain("Plus");
        dupDiags[0].GetMessage().Should().Contain("Integer");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: discard arm
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = OpStubs + @"
    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            _ => new UnaryOperationMeta(kind, OperatorKind.Negate, new(TypeKind.Integer), TypeKind.Integer, ""fallback""),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0009OperationsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }
}
