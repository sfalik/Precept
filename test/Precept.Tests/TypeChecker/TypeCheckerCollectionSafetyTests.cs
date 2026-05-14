using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests;

/// <summary>
/// PRE0104 (MissingOrderingKey) — emitted when .min/.max is called on a
/// collection whose element type lacks the Orderable trait.
/// </summary>
public class TypeCheckerCollectionSafetyTests
{
    // ── PRE0104: MissingOrderingKey ─────────────────────────────────────────

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void SetOfString_MinMax_EmitsMissingOrderingKey(string accessor)
    {
        var (_, diagnostics) = Check($$"""
            precept Widget
            field Tags as set of string
            field Result as string optional <- Tags.{{accessor}}
            """);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.MissingOrderingKey),
            because: $"string lacks Orderable trait, so .{accessor} should emit PRE0104");
    }

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void SetOfInteger_MinMax_NoDiagnostic(string accessor)
    {
        var (_, diagnostics) = Check($$"""
            precept Widget
            field Scores as set of integer
            field Result as integer optional <- Scores.{{accessor}}
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.MissingOrderingKey),
            because: $"integer has Orderable trait, so .{accessor} is structurally valid");
    }

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void SetOfDecimal_MinMax_NoDiagnostic(string accessor)
    {
        var (_, diagnostics) = Check($$"""
            precept Widget
            field Prices as set of number
            field Result as number optional <- Prices.{{accessor}}
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.MissingOrderingKey),
            because: $"decimal has Orderable trait, so .{accessor} is structurally valid");
    }

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void SetOfBoolean_MinMax_EmitsMissingOrderingKey(string accessor)
    {
        var (_, diagnostics) = Check($$"""
            precept Widget
            field Flags as set of boolean
            field Result as boolean optional <- Flags.{{accessor}}
            """);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.MissingOrderingKey),
            because: $"boolean lacks Orderable trait, so .{accessor} should emit PRE0104");
    }

    // ── PRE0100: IndexBoundsGuard (routing validation) ──────────────────────

    [Fact]
    public void ListAt_WithoutGuard_EmitsIndexBoundsGuard()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Steps as list of string
            field First as string optional <- Steps.at(0)
            """);

        compilation.Diagnostics.Should().ContainSingle(
            d => d.Code == nameof(DiagnosticCode.IndexBoundsGuard),
            because: ".at() index access without bounds proof should emit PRE0100");
    }

    [Fact]
    public void ListAt_DoesNotEmit_UnguardedCollectionAccess()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Steps as list of string
            field First as string optional <- Steps.at(0)
            """);

        compilation.Diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.UnguardedCollectionAccess),
            because: ".at() should route to IndexBoundsGuard (PRE0100), not UnguardedCollectionAccess (PRE0063)");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: CollectionInnerTypeError (PRE0105)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Collection_AddWrongType_EmitsCollectionInnerTypeError()
    {
        var precept = """
            precept Widget
            field Tags as list of string default []
            field Count as integer default 0
            state Open initial
            state Done
            event Submit(Value as integer)
            from Open on Submit -> add Tags Submit.Value -> transition Done
            """;

        CheckExpectingError(precept, DiagnosticCode.CollectionInnerTypeError);
    }

    [Fact]
    public void Collection_AddCorrectType_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Tags as list of string default []
            state Open initial
            state Done
            event Submit(Value as string)
            from Open on Submit -> add Tags Submit.Value -> transition Done
            """;

        CheckExpectingClean(precept);
    }
}
