using System;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 11 (G9): Exchange rate assignment qualifier validation — FromCurrency/ToCurrency
/// Slice 10 (G7): Assignment expression qualifier propagation — binary expression operands
/// Regression: plain price typed constants must surface assignment qualifiers
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

    [Fact]
    public void SetPriceField_FromStaticCurrencyAndUnitInterpolated_BothMismatch_QualifierMismatch()
    {
        // '{n} EUR/g' has StaticQualifier = StaticCurrencyAndUnitQualifier(EUR, g).
        // Target is price in 'USD' of 'kg' — currency AND unit both differ → PRE0134.
        // This exercises BuildQualifiersFromStaticInterpolated → ValidateResolvedQualifiers
        // for the StaticCurrencyAndUnitQualifier subtype with a two-axis mismatch.
        var precept = """
            precept Widget
            field Cost as price in 'USD' of 'kg' default '0 USD/kg' writable
            state Open initial
            state Closed
            event Update(n as decimal)
            from Open on Update
                -> set Cost = '{n} EUR/g'
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression — Plain price typed constants at assignment sites
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetPriceField_FromTypedConstantCountUnitMismatch_EmitsQualifierMismatch()
    {
        const string precept = """
            precept Widget
            field Cost as price in 'each' default '1 USD/each' writable
            state A initial
            state B terminal
            event go initial
            from A on go
                -> set Cost = '4 USD/box'
                -> transition B
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        var errors = diagnostics.Where(d => d.Severity == Severity.Error).ToList();

        errors.Should().ContainSingle(
            because: "'4 USD/box' must be rejected for a field declared as price in 'each'");
        errors.Single().Code.Should().Be(DiagnosticCode.QualifierMismatch.ToString());
        errors.Single().Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void SetPriceField_FromTypedConstantCountUnitMatch_NoDiagnostic()
    {
        const string precept = """
            precept Widget
            field Cost as price in 'each' default '1 USD/each' writable
            state A initial
            state B terminal
            event go initial
            from A on go
                -> set Cost = '4 USD/each'
                -> transition B
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetPriceField_FromTypedConstantCurrencyMismatch_EmitsQualifierMismatch()
    {
        const string precept = """
            precept Widget
            field Cost as price in 'USD' of 'mass' default '1 USD/kg' writable
            state A initial
            state B terminal
            event go initial
            from A on go
                -> set Cost = '4 EUR/g'
                -> transition B
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().Contain(DiagnosticCode.QualifierMismatch.ToString(),
                because: "plain price literals must surface currency qualifiers during assignment validation");
    }

    [Fact]
    public void FieldDefault_PriceTypedConstantCountUnitMismatch_EmitsQualifierMismatch()
    {
        const string precept = """
            precept Widget
            field Cost as price in 'each' default '1 USD/box'
            state A initial
            state B terminal
            event go initial
            from A on go
                -> transition B
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        var errors = diagnostics.Where(d => d.Severity == Severity.Error).ToList();

        errors.Should().ContainSingle(
            because: "field defaults flow through the same assignment qualifier seam as set actions");
        errors.Single().Code.Should().Be(DiagnosticCode.QualifierMismatch.ToString());
        errors.Single().Severity.Should().Be(Severity.Error);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression — Interpolated price unit-slot assignment qualifiers
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetPriceField_FromInterpolatedUnitSlotCountUnitMismatch_EmitsQualifierMismatch()
    {
        const string precept = """
            precept InterpolatedMismatch
            field test3 as quantity in 'box' default '1 box'
            field test5 as price in 'each' default '1 USD/each' writable
            state Active initial
            state Done terminal
            event Update initial
            from Active on Update
                -> set test5 = '4.17 USD/{test3.unit}'
                -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void SetPriceField_FromInterpolatedUnitSlotCountUnitMatch_NoDiagnostic()
    {
        const string precept = """
            precept InterpolatedMatch
            field test3 as quantity in 'each' default '1 each'
            field test5 as price in 'each' default '1 USD/each' writable
            state Active initial
            state Done terminal
            event Update initial
            from Active on Update
                -> set test5 = '4.17 USD/{test3.unit}'
                -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetPriceField_FromInterpolatedUnitSlotCurrencyMismatch_EmitsQualifierMismatch()
    {
        const string precept = """
            precept InterpolatedCurrencyMismatch
            field test3 as quantity in 'kg' default '1 kg'
            field test5 as price in 'USD' of 'mass' default '1 USD/kg' writable
            state Active initial
            state Done terminal
            event Update initial
            from Active on Update
                -> set test5 = '4.17 EUR/{test3.unit}'
                -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression — Frank §6 qualifier enforcement matrix
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetExactUnitPriceField_FromBarePriceField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'each' default '1 USD/each'",
            assignment: "source",
            "field source as price default '1 USD/each'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetCurrencyPriceField_FromBarePriceField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "source",
            "field source as price default '1 USD/kg'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetQualifiedPriceField_FromWholeValueInterpolatedMismatchedPrice_EmitsQualifierMismatch()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "'{srcEurPrice}'",
            "field srcEurPrice as price in 'EUR' of 'mass' default '1 EUR/kg'");

        AssertContainsQualifierMismatch(result);
    }

    [Fact]
    public void SetQualifiedPriceField_FromWholeValueInterpolatedMatchingPrice_NoDiagnostic()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "'{srcUsdPrice}'",
            "field srcUsdPrice as price in 'USD' of 'mass' default '1 USD/kg'");

        AssertNoAssignmentQualifierDiagnostics(result);
    }

    [Fact]
    public void SetQualifiedPriceField_FromConditionalMismatchedBranches_EmitsDiagnostic()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "if flag then eurPrice else usdPrice",
            "field eurPrice as price in 'EUR' of 'mass' default '1 EUR/kg'",
            "field usdPrice as price in 'USD' of 'mass' default '1 USD/kg'",
            "field flag as boolean default true");

        AssertContainsQualifierMismatch(result);
    }

    [Fact]
    public void SetQualifiedPriceField_FromConditionalMatchingBranches_NoDiagnostic()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "if flag then usdPrice1 else usdPrice2",
            "field usdPrice1 as price in 'USD' of 'mass' default '1 USD/kg'",
            "field usdPrice2 as price in 'USD' of 'mass' default '2 USD/kg'",
            "field flag as boolean default true");

        AssertNoAssignmentQualifierDiagnostics(result);
    }

    [Fact]
    public void SetExactUnitPriceField_FromDimensionOnlyUnitSlot_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in '[lb_av]' default '1 USD/[lb_av]'",
            assignment: "'4 USD/{qty.unit}'",
            "field qty as quantity of 'mass' default '1 kg'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetDimensionPriceField_FromDimensionMatchingUnitSlot_NoDiagnostic()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price of 'mass' default '1 USD/kg'",
            assignment: "'4 USD/{qty.unit}'",
            "field qty as quantity of 'mass' default '1 kg'");

        AssertNoAssignmentQualifierDiagnostics(result);
    }

    [Fact]
    public void SetDimensionPriceField_FromBareQuantityUnitSlot_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price of 'mass' default '1 USD/kg'",
            assignment: "'4 USD/{qty.unit}'",
            "field qty as quantity default '1 kg'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetExactUnitPriceField_FromBareQuantityUnitSlot_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in '[lb_av]' default '1 USD/[lb_av]'",
            assignment: "'4 USD/{qty.unit}'",
            "field qty as quantity default '1 kg'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetUsdPriceField_FromInterpolatedCurrencySlotEurSource_EmitsQualifierMismatch()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "'{n} {eurPrice.currency}/kg'",
            "field eurPrice as price in 'EUR' of 'mass' default '1 EUR/kg'",
            "field n as integer default 4");

        AssertContainsQualifierMismatch(result);
    }

    [Fact]
    public void SetUsdPriceField_FromInterpolatedCurrencySlotMatchingSource_NoDiagnostic()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in 'USD' of 'mass' default '1 USD/kg'",
            assignment: "'{n} {usdPrice.currency}/kg'",
            "field usdPrice as price in 'USD' of 'mass' default '1 USD/kg'",
            "field n as integer default 4");

        AssertNoAssignmentQualifierDiagnostics(result);
    }

    [Fact]
    public void SetUsdMoneyField_FromBareMoneyField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as money in 'USD' default '1 USD'",
            assignment: "source",
            "field source as money default '1 USD'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetUsdMoneyField_FromInterpolatedCurrencySlotEurSource_EmitsQualifierMismatch()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as money in 'USD' default '1 USD'",
            assignment: "'{n} {eurMoney.currency}'",
            "field eurMoney as money in 'EUR' default '1 EUR'",
            "field n as integer default 4");

        AssertContainsQualifierMismatch(result);
    }

    [Fact]
    public void SetUsdMoneyField_FromInterpolatedCurrencySlotMatchingSource_NoDiagnostic()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as money in 'USD' default '1 USD'",
            assignment: "'{n} {usdMoney.currency}'",
            "field usdMoney as money in 'USD' default '1 USD'",
            "field n as integer default 4");

        AssertNoAssignmentQualifierDiagnostics(result);
    }

    [Fact]
    public void SetUsdEurRate_FromBareExchangeRateField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as exchangerate in 'USD' to 'EUR' default '1.08 USD/EUR'",
            assignment: "source",
            "field source as exchangerate default '1.08 USD/EUR'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SharedSurface_SetAction_FromBareMoneyField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileSetAssignment(
            targetDeclaration: "field target as money in 'USD' default '1 USD'",
            assignment: "source",
            "field source as money default '1 USD'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SharedSurface_FieldDefault_FromBareMoneyField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileFieldDefault(
            targetDeclaration: "field target as money in 'USD' default source",
            "field source as money default '1 USD'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SharedSurface_EventArgDefault_FromBareMoneyField_EmitsUnprovedAssignmentQualifier()
    {
        // Expected to fail until George's architectural fix lands
        var result = CompileEventArgDefault(
            argDeclaration: "amount as money in 'USD' default source",
            "field source as money default '1 USD'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SharedSurface_ComputedField_FromBareMoneyField_EmitsUnprovedAssignmentQualifier()
    {
        var result = CompileFieldDefault(
            targetDeclaration: "field target as money in 'USD' <- source",
            "field source as money default '1 USD'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetUsdMoneyField_FromBareMoneyArg_EmitsUnprovedAssignmentQualifier()
    {
        const string precept = """
            precept QualifierRegression
            field target as money in 'USD' default '1 USD'
            state Draft initial
            state Done terminal
            event Apply(source as money)
            from Draft on Apply
                -> set target = source
                -> transition Done
            """;

        var result = Compiler.Compile(precept);

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetUsdMoneyField_FromCurrencyConversionWithBareRate_EmitsUnprovedAssignmentQualifier()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as money in 'USD' default '1 USD'",
            assignment: "rate * amount",
            "field rate as exchangerate default '1.08 USD/EUR'",
            "field amount as money in 'EUR' default '1 EUR'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    [Fact]
    public void SetExactUnitPriceField_FromPriceDivideBareCompoundQuantity_EmitsUnprovedAssignmentQualifier()
    {
        var result = CompileSetAssignment(
            targetDeclaration: "field target as price in '[lb_av]' default '1 USD/[lb_av]'",
            assignment: "listPrice / factor",
            "field listPrice as price in 'USD' of 'mass' default '1 USD/kg'",
            "field factor as quantity default '1 each/kg'");

        AssertContainsUnprovedAssignmentQualifier(result);
    }

    private static void AssertContainsQualifierMismatch(Compilation result)
    {
        result.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.QualifierMismatch.ToString());
    }

    private static void AssertContainsUnprovedAssignmentQualifier(Compilation result)
    {
        result.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.UnprovedAssignmentQualifierCompatibility.ToString());
    }

    private static void AssertNoAssignmentQualifierDiagnostics(Compilation result)
    {
        result.Diagnostics.Should().NotContain(
            d => d.Code == DiagnosticCode.QualifierMismatch.ToString()
              || d.Code == DiagnosticCode.UnprovedAssignmentQualifierCompatibility.ToString());
        result.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
    }

    private static Compilation CompileSetAssignment(
        string targetDeclaration,
        string assignment,
        params string[] extraDeclarations)
    {
        var extra = string.Join(Environment.NewLine, extraDeclarations.Where(static s => !string.IsNullOrWhiteSpace(s)));

        var precept = $$"""
            precept QualifierRegression
            {{extra}}
            {{targetDeclaration}}
            state Draft initial
            state Done terminal
            event Apply
            from Draft on Apply
                -> set target = {{assignment}}
                -> transition Done
            """;

        return Compiler.Compile(precept);
    }

    private static Compilation CompileFieldDefault(string targetDeclaration, params string[] extraDeclarations)
    {
        var extra = string.Join(Environment.NewLine, extraDeclarations.Where(static s => !string.IsNullOrWhiteSpace(s)));

        var precept = $$"""
            precept QualifierRegression
            {{extra}}
            {{targetDeclaration}}
            """;

        return Compiler.Compile(precept);
    }

    private static Compilation CompileEventArgDefault(string argDeclaration, params string[] extraDeclarations)
    {
        var extra = string.Join(Environment.NewLine, extraDeclarations.Where(static s => !string.IsNullOrWhiteSpace(s)));

        var precept = $$"""
            precept QualifierRegression
            {{extra}}
            event Apply({{argDeclaration}})
            state Draft initial
            """;

        return Compiler.Compile(precept);
    }
}
