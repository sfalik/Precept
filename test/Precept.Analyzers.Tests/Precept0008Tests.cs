using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0008Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────

    private const string TypeStubs = @"
namespace Precept.Language
{
    public enum TypeKind { String, Integer, Decimal, Number, Error }
    public enum TokenKind { StringType, IntegerType, DecimalType, NumberType }
    public enum TypeCategory { Scalar, Special }
    public enum ModifierKind { Notempty, Nonnegative }

    [System.Flags]
    public enum TypeTrait { None = 0, Orderable = 1, EqualityComparable = 2 }

    public sealed record TokenMeta(TokenKind Kind, string Text);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.StringType  => new(kind, ""string""),
            TokenKind.IntegerType => new(kind, ""integer""),
            TokenKind.DecimalType => new(kind, ""decimal""),
            TokenKind.NumberType  => new(kind, ""number""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public record TypeMeta(
        TypeKind Kind,
        TokenMeta Token,
        string Description,
        TypeCategory Category,
        string DisplayName,
        TypeTrait Traits = TypeTrait.None,
        TypeKind[] WidensTo = null,
        ModifierKind[] ImpliedModifiers = null);
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  X05 — WidensTo must not self-reference
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WidensTo_OtherTypes_NoDiagnostic()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",    TypeCategory.Scalar, ""string""),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"",   TypeCategory.Scalar, ""integer"",
                WidensTo: [TypeKind.Decimal, TypeKind.Number]),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""decimal"", TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""number"",  TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_WidensSelf)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task WidensTo_SelfReference_ReportsError()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",    TypeCategory.Scalar, ""string""),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"",   TypeCategory.Scalar, ""integer"",
                WidensTo: [TypeKind.Integer, TypeKind.Decimal]),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""decimal"", TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""number"",  TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        var selfDiags = diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_WidensSelf).ToList();
        selfDiags.Should().ContainSingle();
        selfDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        selfDiags[0].GetMessage().Should().Contain("Integer");
    }

    [Fact]
    public async Task WidensTo_NoWidensTo_NoDiagnostic()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",    TypeCategory.Scalar, ""string""),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"",   TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""decimal"", TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""number"",  TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_WidensSelf)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  X06 — ImpliedModifiers must not have duplicates
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImpliedModifiers_NoDuplicates_NoDiagnostic()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",    TypeCategory.Scalar, ""string"",
                ImpliedModifiers: [ModifierKind.Notempty]),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"",   TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""decimal"", TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""number"",  TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_DupModifier)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task ImpliedModifiers_Duplicate_ReportsError()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",    TypeCategory.Scalar, ""string"",
                ImpliedModifiers: [ModifierKind.Notempty, ModifierKind.Notempty]),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"",   TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""decimal"", TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""number"",  TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        var dupDiags = diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_DupModifier).ToList();
        dupDiags.Should().ContainSingle();
        dupDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        dupDiags[0].GetMessage().Should().Contain("String");
        dupDiags[0].GetMessage().Should().Contain("Notempty");
    }

    [Fact]
    public async Task ImpliedModifiers_DistinctMultiple_NoDiagnostic()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",    TypeCategory.Scalar, ""string"",
                ImpliedModifiers: [ModifierKind.Notempty, ModifierKind.Nonnegative]),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"",   TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""decimal"", TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""number"",  TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_DupModifier)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S13 — Token should use Tokens.GetMeta()
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Token_ViaGetMeta_NoDiagnostic()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",  TypeCategory.Scalar, ""string""),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"", TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""dec"",   TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""num"",   TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_InlineToken)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Token_Null_NoDiagnostic()
    {
        // Error/StateRef types legitimately pass null for Token.
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, Tokens.GetMeta(TokenKind.StringType),  ""text"",  TypeCategory.Scalar, ""string""),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"", TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""dec"",   TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""num"",   TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_InlineToken)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Token_InlineNew_ReportsWarning()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, new TokenMeta(TokenKind.StringType, ""string""), ""text"",  TypeCategory.Scalar, ""string""),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"", TypeCategory.Scalar, ""integer""),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""dec"",   TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""num"",   TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        var inlineDiags = diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_InlineToken).ToList();
        inlineDiags.Should().ContainSingle();
        inlineDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        inlineDiags[0].GetMessage().Should().Contain("String");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NonTypeKindSwitch_NoDiagnostic()
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum TypeKind { A, B }
    public enum TokenKind { X }
    public enum TypeCategory { Scalar }
    public enum ModifierKind { Notempty }
    public enum TypeTrait { None }

    public sealed record TokenMeta(TokenKind Kind, string Text);
    public record TypeMeta(TypeKind Kind, TokenMeta Token, string Description, TypeCategory Category, string DisplayName,
        TypeTrait Traits = TypeTrait.None, TypeKind[] WidensTo = null, ModifierKind[] ImpliedModifiers = null);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.A => new(kind, null, ""a"", TypeCategory.Scalar, ""a"", WidensTo: [TypeKind.A]),
            TypeKind.B => new(kind, null, ""b"", TypeCategory.Scalar, ""b""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Combined: multiple violations in one switch
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Combined_AllViolations_ReportsAll()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, new TokenMeta(TokenKind.StringType, ""string""), ""text"", TypeCategory.Scalar, ""string"",
                ImpliedModifiers: [ModifierKind.Notempty, ModifierKind.Notempty]),
            TypeKind.Integer => new(kind, Tokens.GetMeta(TokenKind.IntegerType), ""whole"", TypeCategory.Scalar, ""integer"",
                WidensTo: [TypeKind.Integer]),
            TypeKind.Decimal => new(kind, Tokens.GetMeta(TokenKind.DecimalType), ""dec"",   TypeCategory.Scalar, ""decimal""),
            TypeKind.Number  => new(kind, Tokens.GetMeta(TokenKind.NumberType),  ""num"",   TypeCategory.Scalar, ""number""),
            TypeKind.Error   => new(kind, null, ""error"", TypeCategory.Special, ""error""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_InlineToken)
            .Should().ContainSingle("String arm has inline new TokenMeta(...)");
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_DupModifier)
            .Should().ContainSingle("String arm has duplicate Notempty");
        diagnostics.Where(d => d.Id == PRECEPT0008TypesCrossRef.DiagnosticId_WidensSelf)
            .Should().ContainSingle("Integer arm widens to itself");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: discard arm → no diagnostic
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = TypeStubs + @"
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            _ => new(kind, null, ""fallback"", TypeCategory.Special, ""error""),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0008TypesCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }
}
