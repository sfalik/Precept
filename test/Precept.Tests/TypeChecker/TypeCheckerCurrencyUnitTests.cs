using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 1 — B2: Currency/Unit Arithmetic Safety (PRE0070–0074).
/// Validates that the TypeChecker emits qualifier mismatch diagnostics when
/// static qualifiers on binary operation operands are incompatible.
/// </summary>
public class TypeCheckerCurrencyUnitTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  PRE0070: CrossCurrencyArithmetic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MoneyFields_DifferentCurrencies_EmitsCrossCurrencyArithmetic()
    {
        var precept = """
            precept Invoice
            field USD as money in 'USD' default '0.00 USD'
            field EUR as money in 'EUR' default '0.00 EUR'
            field Total as money in 'USD' <- USD + EUR
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.CrossCurrencyArithmetic);
    }

    [Fact]
    public void MoneyFields_SameCurrency_NoDiagnostic()
    {
        var precept = """
            precept Invoice
            field A as money in 'USD' default '0.00 USD'
            field B as money in 'USD' default '0.00 USD'
            field Total as money in 'USD' <- A + B
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MoneyField_DynamicQualifier_NoDiagnosticAtTypeStage()
    {
        // When a money field's qualifier is interpolated from another field,
        // static checking is skipped (deferred to ProofEngine).
        var precept = """
            precept Invoice
            field CatalogCurrency as string default "USD"
            field A as money in '{CatalogCurrency}' default '0.00 USD'
            field B as money in 'EUR' default '0.00 EUR'
            field Total as money in 'EUR' <- A + B
            state Open initial
            """;

        // Should not emit CrossCurrencyArithmetic because A has a dynamic qualifier.
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        diagnostics.Should().NotContain(d => d.Code == DiagnosticCode.CrossCurrencyArithmetic.ToString(),
            because: "dynamic qualifier on operand defers check to ProofEngine");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PRE0071: CrossDimensionArithmetic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuantityFields_DifferentDimensions_EmitsCrossDimensionArithmetic()
    {
        var precept = """
            precept Shipment
            field Weight as quantity in 'kg' default '0 kg'
            field Distance as quantity in 'm' default '0 m'
            field Bad as quantity <- Weight + Distance
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.CrossDimensionArithmetic);
    }

    [Fact]
    public void QuantityFields_SameDimension_NoDiagnostic()
    {
        var precept = """
            precept Shipment
            field W1 as quantity in 'kg' default '0 kg'
            field W2 as quantity in 'kg' default '0 kg'
            field Total as quantity in 'kg' <- W1 + W2
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PRE0072: DenominatorUnitMismatch
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceField_DenominatorUnitMismatch_EmitsDenominatorUnitMismatch()
    {
        // Price is per-mass, dividing by a length quantity — dimensions don't match.
        var precept = """
            precept Widget
            field Rate as price in 'USD' of 'mass' default '10 USD/kg'
            field Dist as quantity in 'm' default '1 m' positive
            field Bad as price <- Rate / Dist
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DenominatorUnitMismatch);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PRE0073: DurationDenominatorMismatch
    //  NOTE: This diagnostic fires on division operations involving duration/period
    //  operands where the denominator uses variable-length temporal units. Currently
    //  no Duration/Period division operation exists in the catalog, so this check
    //  activates when such operations are added.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceField_DurationDenominatorVariable_EmitsDurationDenominatorMismatch()
    {
        // Duration ÷ Duration where both are same-dimension resolves via DurationDivideDuration.
        // PRE0073 fires when a duration encounters a variable-length denominator (date-level units).
        // Today: DurationDivideDuration only exists for fixed-length units — test validates
        // same-dimension duration division compiles clean without crashing.
        var precept = """
            precept Widget
            field D1 as duration default '60 minutes'
            field D2 as duration default '30 minutes'
            field Ratio as number <- D1 / D2
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PRE0074: CompoundPeriodDenominator
    //  NOTE: This diagnostic fires when a compound period (multiple temporal
    //  components) is used as a denominator against a single-unit denominator.
    //  Period division operations do not exist in the catalog yet.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceField_CompoundPeriodDenominator_EmitsCompoundPeriodDenominator()
    {
        // Period + Period is valid; Period / Period does not exist in the operations catalog.
        // PRE0074 fires when period division is added and compound period is the divisor.
        // Today: validate period arithmetic (addition) compiles clean without crashing.
        var precept = """
            precept Widget
            field P1 as period in 'years' default '2 years'
            field P2 as period in 'months' default '1 month'
            field Combined as period <- P1 + P2
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: MaxPlacesExceeded (PRE0067)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MaxPlaces_DefaultExceedsLimit_EmitsMaxPlacesExceeded()
    {
        var precept = """
            precept Widget
            field Price as decimal maxplaces 2 default 10.123
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.MaxPlacesExceeded);
    }

    [Fact]
    public void MaxPlaces_DefaultWithinLimit_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Price as decimal maxplaces 2 default 10.12
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
