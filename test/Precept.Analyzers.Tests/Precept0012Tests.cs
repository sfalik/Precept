using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0012Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────

    private const string FuncStubs = @"
namespace Precept.Language
{
    public enum FunctionKind { Min, Max, Round, RoundPlaces, Sqrt }
    public enum TypeKind { Integer, Decimal, Number }
    public enum FunctionCategory { Numeric }
    public enum OperatorKind { GreaterThanOrEqual }

    public sealed record ParameterMeta(TypeKind Kind, string Name = null);
    public abstract record ProofSubject;
    public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;
    public abstract record ProofRequirement(ProofSubject Subject, string Description);
    public sealed record NumericProofRequirement(
        ProofSubject Subject, OperatorKind Comparison, decimal Threshold, string Description
    ) : ProofRequirement(Subject, Description);

    public enum QualifierMatch { Same }

    public sealed record FunctionOverload(
        System.Collections.Generic.IReadOnlyList<ParameterMeta> Parameters,
        TypeKind ReturnType,
        QualifierMatch? Match = null,
        ProofRequirement[] ProofRequirements = null);

    public sealed record FunctionMeta(
        FunctionKind Kind,
        string Name,
        string Description,
        System.Collections.Generic.IReadOnlyList<FunctionOverload> Overloads,
        FunctionCategory Category,
        string UsageExample = null,
        string SnippetTemplate = null,
        string HoverDescription = null);
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0012a — Same-name arity collision
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SameName_DistinctArities_NoDiagnostic()
    {
        // Round (1-param) and RoundPlaces (2-param) share name "round" but different arities.
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Round => new(kind, ""round"", ""Round to integer"",
                new FunctionOverload[] { new([new(TypeKind.Decimal)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            FunctionKind.RoundPlaces => new(kind, ""round"", ""Round to N places"",
                new FunctionOverload[] { new([new(TypeKind.Decimal), new(TypeKind.Integer, ""places"")], TypeKind.Decimal) },
                FunctionCategory.Numeric),
            FunctionKind.Min => new(kind, ""min"", ""Minimum"",
                new FunctionOverload[] { new([new(TypeKind.Integer), new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_ArityCollision)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task SameName_SameArity_ReportsError()
    {
        // Both map to "round" with 1-param overloads → collision.
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Round => new(kind, ""round"", ""Round to integer"",
                new FunctionOverload[] { new([new(TypeKind.Decimal)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            FunctionKind.RoundPlaces => new(kind, ""round"", ""Round to decimal"",
                new FunctionOverload[] { new([new(TypeKind.Number)], TypeKind.Decimal) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        var collisions = diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_ArityCollision).ToList();
        collisions.Should().ContainSingle();
        collisions[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        collisions[0].GetMessage().Should().Contain("round");
        collisions[0].GetMessage().Should().Contain("1");
    }

    [Fact]
    public async Task DifferentNames_SameArity_NoDiagnostic()
    {
        // Different names with same arity — no collision.
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Min => new(kind, ""min"", ""Minimum"",
                new FunctionOverload[] { new([new(TypeKind.Integer), new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            FunctionKind.Max => new(kind, ""max"", ""Maximum"",
                new FunctionOverload[] { new([new(TypeKind.Integer), new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_ArityCollision)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task SingleArm_NoDiagnostic()
    {
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Min => new(kind, ""min"", ""Minimum"",
                new FunctionOverload[] { new([new(TypeKind.Integer), new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0012b — Empty overloads
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Overloads_NonEmpty_NoDiagnostic()
    {
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Min => new(kind, ""min"", ""Minimum"",
                new FunctionOverload[] { new([new(TypeKind.Integer), new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_EmptyOverloads)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Overloads_Empty_ReportsError()
    {
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Min => new(kind, ""min"", ""Minimum"",
                new FunctionOverload[] { },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        var emptyDiags = diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_EmptyOverloads).ToList();
        emptyDiags.Should().ContainSingle();
        emptyDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        emptyDiags[0].GetMessage().Should().Contain("Min");
    }

    [Fact]
    public async Task Overloads_MultipleOverloads_NoDiagnostic()
    {
        // Multiple overloads in an array — not empty.
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Min => new(kind, ""min"", ""Minimum"",
                new FunctionOverload[]
                {
                    new([new(TypeKind.Integer), new(TypeKind.Integer)], TypeKind.Integer),
                    new([new(TypeKind.Decimal), new(TypeKind.Decimal)], TypeKind.Decimal),
                },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_EmptyOverloads)
            .Should().BeEmpty();
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
    public enum OperatorKind { Plus, Minus }
    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(kind, ""Addition""),
            OperatorKind.Minus => new(kind, ""Subtraction""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum FunctionKind { Foo, Bar }
    public enum TypeKind { Integer }
    public enum FunctionCategory { Numeric }

    public sealed record ParameterMeta(TypeKind Kind, string Name = null);
    public sealed record FunctionOverload(
        System.Collections.Generic.IReadOnlyList<ParameterMeta> Parameters,
        TypeKind ReturnType);
    public sealed record FunctionMeta(
        FunctionKind Kind, string Name, string Description,
        System.Collections.Generic.IReadOnlyList<FunctionOverload> Overloads,
        FunctionCategory Category);

    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Foo => new(kind, ""foo"", ""Foo"",
                new FunctionOverload[] { new([new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            FunctionKind.Bar => new(kind, ""foo"", ""Bar"",
                new FunctionOverload[] { new([new(TypeKind.Integer)], TypeKind.Integer) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge cases
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            _ => new(kind, ""fallback"", ""Fallback"",
                new FunctionOverload[] { new(new ParameterMeta[] { }, TypeKind.Integer) },
                FunctionCategory.Numeric),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Combined_EmptyAndCollision_ReportsBoth()
    {
        // Round has empty overloads AND RoundPlaces shares name "round" (though no arity comparison possible).
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Round => new(kind, ""round"", ""Round"",
                new FunctionOverload[] { },
                FunctionCategory.Numeric),
            FunctionKind.RoundPlaces => new(kind, ""round"", ""Round to places"",
                new FunctionOverload[] { new([new(TypeKind.Decimal), new(TypeKind.Integer, ""places"")], TypeKind.Decimal) },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_EmptyOverloads)
            .Should().ContainSingle("Round has empty Overloads");
        // No arity collision since Round has no overloads (no arities to collide with).
        diagnostics.Where(d => d.Id == PRECEPT0012FunctionsCrossRef.DiagnosticId_ArityCollision)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleOverloads_MultipleArities_DistinctAcrossArms_NoDiagnostic()
    {
        // Round has arity 1, RoundPlaces has arities 2. Even with multiple overloads, no collision.
        var source = FuncStubs + @"
    public static class Functions
    {
        public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
        {
            FunctionKind.Round => new(kind, ""round"", ""Round to integer"",
                new FunctionOverload[]
                {
                    new([new(TypeKind.Decimal)], TypeKind.Integer),
                    new([new(TypeKind.Number)], TypeKind.Integer),
                },
                FunctionCategory.Numeric),
            FunctionKind.RoundPlaces => new(kind, ""round"", ""Round to N places"",
                new FunctionOverload[]
                {
                    new([new(TypeKind.Decimal), new(TypeKind.Integer, ""places"")], TypeKind.Decimal),
                    new([new(TypeKind.Number), new(TypeKind.Integer, ""places"")], TypeKind.Decimal),
                },
                FunctionCategory.Numeric),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0012FunctionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }
}
