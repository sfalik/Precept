using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Regression anchors for the money/quantity modifier extension.
/// Covers: zero-bound modifiers (nonnegative, positive, nonzero) and ranged
/// bound modifiers (min, max) on money and quantity fields.
///
/// Design reference: docs/Working/frank-money-modifiers.md § D
/// </summary>
public class MoneyQuantityModifierRegressionTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Zero-bound modifiers on money and quantity — must be 0 errors
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Nonnegative_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as money in 'USD' nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Positive_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as money in 'USD' positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Nonzero_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as money in 'USD' nonzero
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Nonnegative_OnQuantityField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as quantity in 'kg' nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Positive_OnQuantityField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as quantity in 'kg' positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Ranged bound modifiers on money and quantity — must be 0 errors
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Min_OnMoneyField_WithTypedConstant_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as money in 'USD' min '100.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Max_OnMoneyField_WithTypedConstant_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as money in 'USD' max '500.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MinMax_OnMoneyField_WithTypedConstants_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as money in 'USD' min '100.00 USD' max '500.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Min_OnQuantityField_WithTypedConstant_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as quantity in 'kg' min '1.0 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pre-existing gaps — qualifier mismatch and plain-number pass silently
    //  (consistent with default modifier behavior — not new regressions)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Min_OnMoneyField_QualifierMismatch_NoDiagnostic_PreExistingGap()
    {
        // EUR is a valid ISO 4217 code; qualifier alignment is not checked at
        // compile time (same gap exists for `default`). This anchor documents
        // the known gap and must stay 0 errors until the gap is fixed uniformly.
        var precept = """
            precept Widget
            field X as money in 'USD' min '100.00 EUR'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Min_OnMoneyField_PlainNumber_NoDiagnostic_PreExistingGap()
    {
        // A plain integer bound on a money field passes silently (same gap
        // exists for `default 100` on a money field). Documents the known gap.
        var precept = """
            precept Widget
            field X as money in 'USD' min 100
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Invalid typed-constant content — must emit InvalidTypedConstantContent
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Min_OnMoneyField_InvalidTypedConstantContent_EmitsError()
    {
        // 'not-valid-currency' is not a valid typed-constant for a money field.
        var precept = """
            precept Widget
            field X as money in 'USD' min 'not-valid-currency'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidTypedConstantContent);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression guards — old wrong behavior must NOT recur
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Nonnegative_OnMoneyField_MustNotProduceInvalidModifierForType()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as money in 'USD' nonnegative
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "nonnegative must be valid on money fields after catalog extension");
    }

    [Fact]
    public void Positive_OnQuantityField_MustNotProduceInvalidModifierForType()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as quantity in 'kg' positive
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "positive must be valid on quantity fields after catalog extension");
    }
}
