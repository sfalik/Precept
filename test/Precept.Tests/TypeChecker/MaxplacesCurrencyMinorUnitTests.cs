using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// F-LANG-BIZ-10 (Phase 4 W-H): currency-derived `maxplaces currency.minorUnit`
/// on money fields. Tests cover the design's six acceptance criteria — the
/// contextual identifier resolves at compile time when the field's currency
/// qualifier is static, and emits teachable diagnostics on the failure modes.
/// </summary>
public class MaxplacesCurrencyMinorUnitTests
{
    [Fact]
    public void MaxplacesCurrencyMinorUnit_StaticCurrency_CompilesClean()
    {
        var (_, diagnostics) = Check("""
            precept Invoice
            field Amount as money in 'USD' maxplaces currency.minorUnit
            state Open initial
            """);

        diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.InvalidModifierValue)
            || d.Code == nameof(DiagnosticCode.MaxplacesCurrencyQualifierNotStatic));
    }

    [Fact]
    public void MaxplacesCurrencyMinorUnit_ResolvesToCatalogValueOnDefault()
    {
        // USD's minor-unit count is 2; a 3-decimal-place default literal should be
        // rejected by EnforceMaxplacesAgainstDefault via the catalog-resolved bound.
        var (_, diagnostics) = Check("""
            precept Invoice
            field Amount as money in 'USD' maxplaces currency.minorUnit default '99.999 USD'
            state Open initial
            """);

        diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.MaxPlacesExceeded),
            because: "USD.minorUnit = 2, so a 3-decimal-place literal exceeds the catalog-resolved bound");
    }

    [Fact]
    public void MaxplacesCurrencyMinorUnit_InterpolatedCurrency_EmitsNotStatic()
    {
        var (_, diagnostics) = Check("""
            precept Invoice
            field FieldCur as currency default 'USD'
            field Amount as money in '{FieldCur}' maxplaces currency.minorUnit
            state Open initial
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.MaxplacesCurrencyQualifierNotStatic),
            because: "interpolated currency cannot be resolved at compile time");
    }

    [Fact]
    public void MaxplacesCurrencyAccessor_OnNonWhitelistedAccessor_EmitsInvalidModifierValue()
    {
        // currency.numericCode returns integer but is semantically wrong for precision.
        // The catalog's UseInModifierValueContext flag gates only minorUnit.
        var (_, diagnostics) = Check("""
            precept Invoice
            field Amount as money in 'USD' maxplaces currency.numericCode
            state Open initial
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InvalidModifierValue),
            because: "only currency.minorUnit is whitelisted in modifier-value position");
    }

    [Fact]
    public void MaxplacesCurrencyMinorUnit_LiteralIntegerForm_StillCompilesClean()
    {
        // Back-compat: the literal-integer form continues to work.
        var (_, diagnostics) = Check("""
            precept Invoice
            field Amount as money in 'USD' maxplaces 2
            state Open initial
            """);

        diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.InvalidModifierValue));
    }
}
