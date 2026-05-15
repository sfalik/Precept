using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 11 (G9): Exchange rate assignment qualifier validation — FromCurrency/ToCurrency
/// Slice 10 (G7): Assignment expression qualifier propagation — binary expression operands
/// </summary>
public class TypeCheckerAssignmentQualifierTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Slice 11 (G9) — Exchange Rate assignment qualifier validation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExchangeRate_Assignment_MatchingFromTo_NoDiagnostic()
    {
        var precept = """
            precept FxTest
            field rate1 as exchangerate in 'USD' to 'EUR'
            field rate2 as exchangerate in 'USD' to 'EUR'
            state Open initial
            state Closed
            event Sync
            from Open on Sync
                -> set rate1 = rate2
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void ExchangeRate_Assignment_MismatchedFromCurrency_Diagnostic()
    {
        var precept = """
            precept FxTest
            field rate1 as exchangerate in 'USD' to 'EUR'
            field rate2 as exchangerate in 'GBP' to 'EUR'
            state Open initial
            state Closed
            event Sync
            from Open on Sync
                -> set rate1 = rate2
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void ExchangeRate_Assignment_MismatchedToCurrency_Diagnostic()
    {
        var precept = """
            precept FxTest
            field rate1 as exchangerate in 'USD' to 'EUR'
            field rate2 as exchangerate in 'USD' to 'GBP'
            state Open initial
            state Closed
            event Sync
            from Open on Sync
                -> set rate1 = rate2
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void Regression_ExistingDimensionUnitCurrency_CasesUnaffected()
    {
        // Currency mismatch (existing behavior)
        var currencyPrecept = """
            precept Widget
            field m as money in 'USD'
            state Open initial
            state Closed
            event E(p as money in 'EUR')
            from Open on E
                -> set m = p
                -> transition Closed
            """;
        TypeCheckerTestHelpers.CheckExpectingError(currencyPrecept, DiagnosticCode.QualifierMismatch);

        // Dimension mismatch (existing behavior)
        var dimensionPrecept = """
            precept Widget
            field q1 as quantity of 'length'
            field q2 as quantity of 'mass'
            state Open initial
            state Closed
            event E
            from Open on E
                -> set q1 = q2
                -> transition Closed
            """;
        TypeCheckerTestHelpers.CheckExpectingError(dimensionPrecept, DiagnosticCode.DimensionCategoryMismatch);

        // Matching qualifiers (existing behavior)
        var matchingPrecept = """
            precept Widget
            field m1 as money in 'USD'
            field m2 as money in 'USD'
            state Open initial
            state Closed
            event E
            from Open on E
                -> set m1 = m2
                -> transition Closed
            """;
        TypeCheckerTestHelpers.CheckExpectingClean(matchingPrecept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 10 (G7) — Assignment expression qualifier propagation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetUsdField_FromUsdPlusUsd_Expression_NoDiagnostic()
    {
        var precept = """
            precept Invoice
            field total as money in 'USD'
            field a as money in 'USD'
            field b as money in 'USD'
            state Open initial
            state Closed
            event Calc
            from Open on Calc
                -> set total = a + b
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetUsdField_FromEurPlusEur_Expression_Diagnostic()
    {
        var precept = """
            precept Invoice
            field total as money in 'USD'
            field a as money in 'EUR'
            field b as money in 'EUR'
            state Open initial
            state Closed
            event Calc
            from Open on Calc
                -> set total = a + b
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void SetUsdField_FromDirectRef_StillCaught()
    {
        // Regression: direct ref mismatch is not broken by the expression handling
        var precept = """
            precept Invoice
            field total as money in 'USD'
            field eurField as money in 'EUR'
            state Open initial
            state Closed
            event Calc
            from Open on Calc
                -> set total = eurField
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void SetUsdField_ScaleByDecimal_NoDiagnostic()
    {
        var precept = """
            precept Invoice
            field total as money in 'USD'
            state Open initial
            state Closed
            event Calc
            from Open on Calc
                -> set total = total * 2.0
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetMassQuantity_FromLengthPlusLength_Expression_Diagnostic()
    {
        var precept = """
            precept Sensor
            field massQty as quantity of 'mass'
            field a as quantity of 'length'
            field b as quantity of 'length'
            state Open initial
            state Closed
            event Calc
            from Open on Calc
                -> set massQty = a + b
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DimensionCategoryMismatch);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 23 — Static qualifier routing: assignment validation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetQuantityField_FromStaticUnitInterpolated_Matching_NoDiagnostic()
    {
        // '{n} kg' is InterpolatedTypedConstant with StaticQualifier = StaticUnitQualifier(kg).
        // When target is 'in kg', qualifiers match — no PRE0134.
        var precept = """
            precept Widget
            field Qty as quantity in 'kg' default '0 kg' writable
            state Open initial
            state Closed
            event Update(n as decimal)
            from Open on Update
                -> set Qty = '{n} kg'
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetQuantityField_FromStaticUnitInterpolated_Mismatch_QualifierMismatch()
    {
        // '{n} g' has StaticQualifier = StaticUnitQualifier(g), target is 'in kg' → PRE0134.
        var precept = """
            precept Widget
            field Qty as quantity in 'kg' default '0 kg' writable
            state Open initial
            state Closed
            event Update(n as decimal)
            from Open on Update
                -> set Qty = '{n} g'
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void SetMoneyField_FromStaticCurrencyInterpolated_Matching_NoDiagnostic()
    {
        // '{n} USD' has StaticQualifier = StaticCurrencyQualifier(USD), target is 'in USD' → clean.
        var precept = """
            precept Widget
            field Total as money in 'USD' default '0.00 USD' writable
            state Open initial
            state Closed
            event Update(n as decimal)
            from Open on Update
                -> set Total = '{n} USD'
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetMoneyField_FromStaticCurrencyInterpolated_Mismatch_QualifierMismatch()
    {
        // '{n} EUR' has StaticQualifier = StaticCurrencyQualifier(EUR), target is 'in USD' → PRE0134.
        var precept = """
            precept Widget
            field Total as money in 'USD' default '0.00 USD' writable
            state Open initial
            state Closed
            event Update(n as decimal)
            from Open on Update
                -> set Total = '{n} EUR'
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }
}
