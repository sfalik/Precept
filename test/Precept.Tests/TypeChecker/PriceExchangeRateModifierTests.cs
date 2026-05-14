using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Regression anchors for price and exchangerate modifier applicability gaps.
///
/// George's fix extended the modifier catalog to allow zero-bound modifiers
/// (nonnegative, positive, nonzero) and maxplaces on both price and exchangerate,
/// and range-bound modifiers (min, max) on price only.
///
/// min/max remain invalid on exchangerate because ordering is undefined for
/// currency-pair rates (spec § "Business-domain comparison").
/// </summary>
public class PriceExchangeRateModifierTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Zero-bound modifiers on price — must produce 0 errors (gaps now fixed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Nonnegative_OnPriceField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Positive_OnPriceField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Nonzero_OnPriceField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' nonzero
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Range-bound modifiers on price — must produce 0 errors (gaps now fixed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Min_OnPriceField_WithTypedConstant_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' min '1 USD/kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Max_OnPriceField_WithTypedConstant_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' max '100 USD/kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MinMax_OnPriceField_WithTypedConstants_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' min '1 USD/kg' max '100 USD/kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsRequireQualifier_PriceWithoutIn_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price of 'mass' min '1 USD/kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsRequireQualifier);
    }

    [Fact]
    public void BoundsRequireQualifier_PriceWithIn_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' min '1 USD/kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  maxplaces on price — must produce 0 errors (gap now fixed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Maxplaces_OnPriceField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as price in 'USD' of 'mass' maxplaces 4
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Zero-bound modifiers on exchangerate — must produce 0 errors (gaps now fixed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Nonnegative_OnExchangeRateField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Positive_OnExchangeRateField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Nonzero_OnExchangeRateField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' nonzero
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  maxplaces on exchangerate — must produce 0 errors (gap now fixed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Maxplaces_OnExchangeRateField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' maxplaces 4
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  min/max on exchangerate — STILL invalid (ordering undefined by design)
    //  Spec: "exchangerate supports only ==/!= — ordering operators are type errors"
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Min_OnExchangeRateField_StillEmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' min '0.9 USD/EUR'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void Max_OnExchangeRateField_StillEmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' max '1.1 USD/EUR'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void BoundsRequireQualifier_ExchangeRateWithBounds_NoDiagnostic()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as exchangerate min '0.9 USD/EUR'
            state Open initial
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.BoundsRequireQualifier));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression guards — old wrong behavior must NOT recur
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Nonnegative_OnPriceField_MustNotProduceInvalidModifierForType()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as price in 'USD/each' nonnegative
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "nonnegative must be valid on price fields after catalog extension");
    }

    [Fact]
    public void Positive_OnPriceField_MustNotProduceInvalidModifierForType()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as price in 'USD/each' positive
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "positive must be valid on price fields after catalog extension");
    }

    [Fact]
    public void Nonnegative_OnExchangeRateField_MustNotProduceInvalidModifierForType()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as exchangerate in 'USD' to 'EUR' nonnegative
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "nonnegative must be valid on exchangerate fields after catalog extension");
    }

    [Fact]
    public void Min_OnPriceField_MustNotProduceInvalidModifierForType()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field X as price in 'USD/each' min '1 USD/each'
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "min must be valid on price fields after catalog extension");
    }
}
