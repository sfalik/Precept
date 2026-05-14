using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 10 — Qualifier Compatibility Checks.
/// Enforces that when both a field and its bound carry qualifiers, those qualifiers must match.
/// Emits <see cref="DiagnosticCode.BoundsQualifierMismatch"/> on mismatch.
/// Also enforces <see cref="DiagnosticCode.BoundsRequireQualifier"/> when a qualified field
/// has a plain numeric bound (the bound must specify its qualifier too).
/// </summary>
public class TypeCheckerQualifierCompatibilityTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Money / currency mismatch
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoundsQualifierMismatch_FieldUSD_BoundEUR_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Cost as money in 'USD' max '100 EUR'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsQualifierMismatch);
    }

    [Fact]
    public void BoundsQualifierMismatch_FieldUSD_BoundUSD_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Cost as money in 'USD' max '100 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsQualifierMismatch_FieldUSD_MinBoundEUR_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Cost as money in 'USD' min '10 EUR'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsQualifierMismatch);
    }

    [Fact]
    public void BoundsQualifierMismatch_FieldUSD_BothBoundsEUR_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Cost as money in 'USD' min '10 EUR' max '100 EUR'
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        diagnostics.Should().Contain(d => d.Code == DiagnosticCode.BoundsQualifierMismatch.ToString(),
            because: "both min and max bounds carry mismatched qualifier EUR");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Quantity / unit mismatch
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoundsQualifierMismatch_FieldKg_BoundLb_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' max '5 [lb_av]'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsQualifierMismatch);
    }

    [Fact]
    public void BoundsQualifierMismatch_FieldKg_BoundKg_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' max '5 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Plain numeric bound on qualified field → BoundsRequireQualifier
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoundsQualifierMismatch_FieldHasQualifier_BoundIsPlainNumeric_EmitsBoundsRequireQualifier()
    {
        // Field has 'in USD' but bound is plain numeric — the bound must specify its qualifier.
        var precept = """
            precept Widget
            field Cost as money in 'USD' max 500
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsRequireQualifier);
    }

    [Fact]
    public void BoundsQualifierMismatch_QuantityFieldHasQualifier_BoundIsPlainNumeric_EmitsBoundsRequireQualifier()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' min 0 max 100
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsRequireQualifier);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression: unqualified decimal field with plain numeric bound → clean
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoundsQualifierMismatch_DecimalFieldNoQualifier_PlainNumericBound_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Amount as decimal min 0 max 1000
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsQualifierMismatch_IntegerFieldNoQualifier_PlainNumericBound_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Count as integer min 0 max 100
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Qualifier-based dimension (of 'mass') — cross-axis check does not fire
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoundsQualifierMismatch_QuantityWithOfDimension_TypedConstantBound_NoDiagnostic()
    {
        // Field qualifier is on Dimension axis; bound qualifier is on Unit axis — no axis overlap,
        // so no BoundsQualifierMismatch should be emitted.
        var precept = """
            precept Widget
            field Weight as quantity of 'mass' max '5 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
