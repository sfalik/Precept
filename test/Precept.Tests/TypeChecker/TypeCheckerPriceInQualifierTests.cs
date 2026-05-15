using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// QS-2: PriceIn axis resolution and OfRequiresCurrencyIn enforcement.
/// Validates that the TypeChecker correctly resolves polymorphic 'in' values
/// on price fields (currency, unit, compound) and enforces 'of' constraints.
/// </summary>
public class TypeCheckerPriceInQualifierTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Currency-only: price in 'USD' → Currency (regression)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_CurrencyCode_ResolvesAsCurrency()
    {
        var precept = """
            precept Product
            field Cost as price in 'USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Unit-only: price in 'kg' → Unit (new behavior)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_UnitCode_ResolvesAsUnit()
    {
        var precept = """
            precept Product
            field Cost as price in 'kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Compound: price in 'USD/kg' → CompoundPrice
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_CompoundCurrencyUnit_ResolvesAsCompoundPrice()
    {
        var precept = """
            precept Product
            field Cost as price in 'USD/kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PriceIn_CompoundCurrencyUnit_ProducesCompoundPriceMeta()
    {
        var precept = """
            precept Product
            field Cost as price in 'USD/kg'
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        diagnostics.Where(d => d.Severity == Severity.Error)
            .Where(d => d.Code != DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString())
            .Should().BeEmpty();

        var costField = index.Fields.First(f => f.Name == "Cost");
        costField.DeclaredQualifiers.Should().ContainSingle(q =>
            q is DeclaredQualifierMeta.CompoundPrice);
        var compound = (DeclaredQualifierMeta.CompoundPrice)costField.DeclaredQualifiers.First(q => q is DeclaredQualifierMeta.CompoundPrice);
        compound.CurrencyCode.Should().Be("USD");
        compound.UnitCode.Should().Be("kg");
        compound.DimensionName.Should().Be("mass");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  OfRequiresCurrencyIn: price in 'kg' of '...' → InvalidQualifierCoexistence
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_UnitWithOf_EmitsInvalidQualifierCoexistence()
    {
        var precept = """
            precept Product
            field Cost as price in 'kg' of 'SaleUnit'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidQualifierCoexistence);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Currency + of: price in 'USD' of '...' → valid (existing behavior)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_CurrencyWithOf_IsValid()
    {
        var precept = """
            precept Product
            field Cost as price in 'USD' of 'mass'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Compound + of: price in 'USD/kg' of '...' → InvalidQualifierCoexistence
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_CompoundWithOf_EmitsInvalidQualifierCoexistence()
    {
        var precept = """
            precept Product
            field Cost as price in 'USD/kg' of 'mass'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidQualifierCoexistence);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Malformed compounds: trailing/leading slash → InvalidPriceQualifier
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_TrailingSlash_EmitsDiagnostic()
    {
        // 'USD/' — compound guard rejects (slash at end), falls through to currency check,
        // 'USD/' is not a valid currency code, then not a valid UCUM unit, emits InvalidPriceQualifier
        var precept = """
            precept Product
            field Cost as price in 'USD/'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidPriceQualifier);
    }

    [Fact]
    public void PriceIn_LeadingSlash_EmitsDiagnostic()
    {
        // '/kg' — compound guard rejects (slash at start), falls through, '/kg' not a currency,
        // UCUM parse of '/kg' likely fails, emits InvalidPriceQualifier
        var precept = """
            precept Product
            field Cost as price in '/kg'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidPriceQualifier);
    }
}
