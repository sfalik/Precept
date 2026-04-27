using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0010Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────
    // Both Types.GetMeta(TypeKind) and Operations.GetMeta(OperationKind) must
    // exist in the same compilation for cross-catalog checks.

    private const string SharedTypes = @"
namespace Precept.Language
{
    public enum TypeKind { Integer, String, Boolean }
    public enum TokenKind { Integer, String, Boolean }
    public enum OperatorKind { Equals, NotEquals, LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual, Plus }
    public enum TypeCategory { Scalar }

    [System.Flags]
    public enum TypeTrait
    {
        None              = 0,
        Orderable         = 1,
        EqualityComparable = 2,
    }

    public enum OperationKind
    {
        IntEqInt, IntNeInt,
        IntLtInt, IntGtInt, IntLteInt, IntGteInt,
        StrEqStr, StrNeStr,
    }

    public sealed record TokenMeta(TokenKind Kind, string Text);
    public record TypeMeta(
        TypeKind Kind, TokenMeta Token, string Description,
        TypeCategory Category, string DisplayName,
        TypeTrait Traits = TypeTrait.None);

    public record ParameterMeta(TypeKind Kind, string Name = null);

    public abstract record OperationMeta(
        OperationKind Kind, OperatorKind Op, TypeKind Result, string Description);

    public sealed record BinaryOperationMeta(
        OperationKind Kind, OperatorKind Op,
        ParameterMeta Lhs, ParameterMeta Rhs,
        TypeKind Result, string Description)
        : OperationMeta(Kind, Op, Result, Description);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => new(kind, kind.ToString());
    }
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0010a — EqualityComparable trait → must have Equals + NotEquals ops
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EqTrait_WithEqOps_NoDiagnostic()
    {
        // Integer has EqualityComparable AND Equals+NotEquals ops → OK.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""int == int""),
            OperationKind.IntNeInt => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean, ""int != int""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_EqTraitMissingOps)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task EqTrait_MissingNotEquals_ReportsError()
    {
        // Integer has EqualityComparable but only Equals (no NotEquals) → error.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""int == int""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        var eqDiags = diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_EqTraitMissingOps).ToList();
        eqDiags.Should().ContainSingle();
        eqDiags[0].GetMessage().Should().Contain("Integer");
        eqDiags[0].GetMessage().Should().Contain("NotEquals");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0010b — Orderable trait → must have LT/GT/LTE/GTE ops
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrdTrait_WithAllOps_NoDiagnostic()
    {
        // Integer has Orderable AND all four ordering ops → OK.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""==""),
            OperationKind.IntNeInt => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean, ""!=""),
            OperationKind.IntLtInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThan, PInteger, PInteger, TypeKind.Boolean, ""<""),
            OperationKind.IntGtInt => new BinaryOperationMeta(
                kind, OperatorKind.GreaterThan, PInteger, PInteger, TypeKind.Boolean, "">""),
            OperationKind.IntLteInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThanOrEqual, PInteger, PInteger, TypeKind.Boolean, ""<=""),
            OperationKind.IntGteInt => new BinaryOperationMeta(
                kind, OperatorKind.GreaterThanOrEqual, PInteger, PInteger, TypeKind.Boolean, "">=""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_OrdTraitMissingOps)
            .Should().BeEmpty();
        diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_EqTraitMissingOps)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task OrdTrait_MissingGTE_ReportsError()
    {
        // Integer has Orderable but missing GreaterThanOrEqual → error.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""==""),
            OperationKind.IntNeInt => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean, ""!=""),
            OperationKind.IntLtInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThan, PInteger, PInteger, TypeKind.Boolean, ""<""),
            OperationKind.IntGtInt => new BinaryOperationMeta(
                kind, OperatorKind.GreaterThan, PInteger, PInteger, TypeKind.Boolean, "">""),
            OperationKind.IntLteInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThanOrEqual, PInteger, PInteger, TypeKind.Boolean, ""<=""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        var ordDiags = diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_OrdTraitMissingOps).ToList();
        ordDiags.Should().ContainSingle();
        ordDiags[0].GetMessage().Should().Contain("Integer");
        ordDiags[0].GetMessage().Should().Contain("GreaterThanOrEqual");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0010c — Equality ops → must have EqualityComparable trait
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EqOps_WithoutTrait_ReportsError()
    {
        // String has Equals/NotEquals ops but NO EqualityComparable trait → error.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String => new(kind, Tokens.GetMeta(TokenKind.String), ""str"",
                TypeCategory.Scalar, ""string""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PString = new(TypeKind.String);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.StrEqStr => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PString, PString, TypeKind.Boolean, ""str == str""),
            OperationKind.StrNeStr => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PString, PString, TypeKind.Boolean, ""str != str""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        var diags = diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_EqOpsMissingTrait).ToList();
        diags.Should().ContainSingle();
        diags[0].GetMessage().Should().Contain("String");
    }

    [Fact]
    public async Task EqOps_WithTrait_NoDiagnostic()
    {
        // String has Equals/NotEquals ops AND EqualityComparable trait → OK.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String => new(kind, Tokens.GetMeta(TokenKind.String), ""str"",
                TypeCategory.Scalar, ""string"",
                Traits: TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PString = new(TypeKind.String);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.StrEqStr => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PString, PString, TypeKind.Boolean, ""str == str""),
            OperationKind.StrNeStr => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PString, PString, TypeKind.Boolean, ""str != str""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_EqOpsMissingTrait)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0010d — Ordering ops → must have Orderable trait
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrdOps_WithoutTrait_ReportsError()
    {
        // Integer has LessThan ops but Traits = EqualityComparable (no Orderable) → error.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""==""),
            OperationKind.IntNeInt => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean, ""!=""),
            OperationKind.IntLtInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThan, PInteger, PInteger, TypeKind.Boolean, ""<""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        var diags = diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_OrdOpsMissingTrait).ToList();
        diags.Should().ContainSingle();
        diags[0].GetMessage().Should().Contain("Integer");
    }

    [Fact]
    public async Task OrdOps_AllModifierGated_NoDiagnostic()
    {
        // All ordering ops have ModifierRequirement proof obligations — trait absence is correct.
        var source = SharedTypes + @"
    public sealed record ModifierRequirement();

    public sealed record BinaryOperationMetaEx(
        OperationKind Kind, OperatorKind Op,
        ParameterMeta Lhs, ParameterMeta Rhs,
        TypeKind Result, string Description,
        ModifierRequirement[] ProofRequirements = null)
        : OperationMeta(Kind, Op, Result, Description);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""==""),
            OperationKind.IntNeInt => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean, ""!=""),
            OperationKind.IntLtInt => new BinaryOperationMetaEx(
                kind, OperatorKind.LessThan, PInteger, PInteger, TypeKind.Boolean, ""<"",
                ProofRequirements: [new ModifierRequirement()]),
            OperationKind.IntGtInt => new BinaryOperationMetaEx(
                kind, OperatorKind.GreaterThan, PInteger, PInteger, TypeKind.Boolean, "">"",
                ProofRequirements: [new ModifierRequirement()]),
            OperationKind.IntLteInt => new BinaryOperationMetaEx(
                kind, OperatorKind.LessThanOrEqual, PInteger, PInteger, TypeKind.Boolean, ""<="",
                ProofRequirements: [new ModifierRequirement()]),
            OperationKind.IntGteInt => new BinaryOperationMetaEx(
                kind, OperatorKind.GreaterThanOrEqual, PInteger, PInteger, TypeKind.Boolean, "">="",
                ProofRequirements: [new ModifierRequirement()]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_OrdOpsMissingTrait)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task OrdOps_WithTrait_NoDiagnostic()
    {
        // Integer has LT ops AND Orderable trait → OK.
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"",
                Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(TypeKind.Integer);

        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            OperationKind.IntEqInt => new BinaryOperationMeta(
                kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean, ""==""),
            OperationKind.IntNeInt => new BinaryOperationMeta(
                kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean, ""!=""),
            OperationKind.IntLtInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThan, PInteger, PInteger, TypeKind.Boolean, ""<""),
            OperationKind.IntGtInt => new BinaryOperationMeta(
                kind, OperatorKind.GreaterThan, PInteger, PInteger, TypeKind.Boolean, "">""),
            OperationKind.IntLteInt => new BinaryOperationMeta(
                kind, OperatorKind.LessThanOrEqual, PInteger, PInteger, TypeKind.Boolean, ""<=""),
            OperationKind.IntGteInt => new BinaryOperationMeta(
                kind, OperatorKind.GreaterThanOrEqual, PInteger, PInteger, TypeKind.Boolean, "">=""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Where(d => d.Id == PRECEPT0010TraitOperationConsistency.DiagnosticId_OrdOpsMissingTrait)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum TypeKind { Integer }
    public enum TypeCategory { Scalar }
    [System.Flags] public enum TypeTrait { None = 0, EqualityComparable = 2 }
    public enum OperationKind { IntEqInt }
    public enum OperatorKind { Equals }
    public enum TokenKind { Integer }
    public sealed record TokenMeta(TokenKind Kind, string Text);
    public record TypeMeta(TypeKind Kind, TokenMeta Token, string Description,
        TypeCategory Category, string DisplayName, TypeTrait Traits = TypeTrait.None);
    public record ParameterMeta(TypeKind Kind);
    public abstract record OperationMeta(OperationKind Kind, OperatorKind Op, TypeKind Result, string Desc);
    public sealed record BinaryOperationMeta(OperationKind Kind, OperatorKind Op,
        ParameterMeta Lhs, ParameterMeta Rhs, TypeKind Result, string Desc)
        : OperationMeta(Kind, Op, Result, Desc);
    public static class Tokens { public static TokenMeta GetMeta(TokenKind k) => new(k, k.ToString()); }

    // Integer with EqualityComparable trait but NO Equals/NotEquals ops.
    // Should NOT fire — wrong namespace.
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.Integer), ""int"",
                TypeCategory.Scalar, ""integer"", Traits: TypeTrait.EqualityComparable),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  No traits, no ops — clean slate
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NoTraitsNoOps_NoDiagnostic()
    {
        var source = SharedTypes + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Boolean => new(kind, Tokens.GetMeta(TokenKind.Boolean), ""bool"",
                TypeCategory.Scalar, ""boolean""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static class Operations
    {
        public static OperationMeta GetMeta(OperationKind kind) => kind switch
        {
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0010TraitOperationConsistency>(source);
        diagnostics.Should().BeEmpty();
    }
}
