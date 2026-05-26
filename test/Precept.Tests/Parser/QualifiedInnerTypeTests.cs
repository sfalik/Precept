using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.Parser;

/// <summary>
/// Phase 4 W-C / F-LANG-COLL-06: qualified inner types in collections.
/// Set, bag, list, queue, log, and lookup-value can carry qualifier metadata
/// (`set of money in 'USD'`, `lookup of K to quantity of 'mass'`, etc.). The
/// parser routes inner types through <see cref="TryParseQualifiers"/>; the
/// type checker builds a <see cref="TypedQualifiedElement"/> on the typed field.
/// Root-cause fix for BUG-005 (was symptom-fix-emit-PRE0105 in Phase 2).
/// </summary>
public class QualifiedInnerTypeTests
{
    [Theory]
    [InlineData("set of money in 'USD'")]
    [InlineData("bag of money in 'EUR'")]
    [InlineData("list of money in 'JPY'")]
    [InlineData("queue of money in 'GBP'")]
    [InlineData("log of money in 'USD'")]
    public void Collection_OfQualifiedMoney_CompilesClean(string innerType)
    {
        var precept = $$"""
            precept Widget
            field Amounts as {{innerType}}
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: $"{innerType} is a valid qualified inner type per Phase 4 W-C");
    }

    [Theory]
    [InlineData("set of quantity of 'mass'")]
    [InlineData("bag of quantity of 'length'")]
    [InlineData("list of quantity of 'volume'")]
    public void Collection_OfQualifiedQuantity_CompilesClean(string innerType)
    {
        var precept = $$"""
            precept Widget
            field Measurements as {{innerType}}
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: $"{innerType} is a valid qualified-quantity inner type");
    }

    [Fact]
    public void Lookup_QualifiedValue_Money_CompilesClean()
    {
        // Original BUG-005 repro shape — now compiles clean.
        var precept = """
            precept Widget
            field Prices as lookup of string to money in 'USD'
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: "BUG-005 root-cause fix: qualified-value lookup compiles clean");
    }

    [Fact]
    public void Lookup_QualifiedValue_Quantity_CompilesClean()
    {
        var precept = """
            precept Widget
            field Weights as lookup of string to quantity of 'mass'
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError));
    }

    [Fact]
    public void TypedField_QualifiedElement_CarriesQualifierMetadata()
    {
        // Verify the TypedElementType DU is populated correctly: a qualified inner type
        // produces TypedQualifiedElement carrying the qualifier metadata, not TypedScalarElement.
        var precept = """
            precept Widget
            field Prices as set of money in 'USD'
            state Open initial
            """;

        var index = CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Prices");

        field.ElementType.Should().BeOfType<TypedQualifiedElement>(
            because: "qualified inner type produces TypedQualifiedElement variant of the DU");

        var qualified = (TypedQualifiedElement)field.ElementType!;
        qualified.ResolvedTypeKind.Should().Be(TypeKind.Money);
        var currencyQual = qualified.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.Currency>()
            .Single();
        currencyQual.CurrencyCode.Should().Be("USD",
            because: "the 'USD' currency qualifier flows from the inner type into TypedQualifiedElement");
    }

    [Fact]
    public void TypedField_ScalarElement_HasNoQualifierMetadata()
    {
        // Regression guard: an unqualified inner type produces TypedScalarElement, not TypedQualifiedElement.
        var precept = """
            precept Widget
            field Tags as set of string
            state Open initial
            """;

        var index = CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Tags");

        field.ElementType.Should().BeOfType<TypedScalarElement>(
            because: "unqualified inner type produces TypedScalarElement");
        field.ElementType!.ResolvedTypeKind.Should().Be(TypeKind.String);
    }

    [Fact]
    public void LookupAccess_Result_InheritsElementQualifier_ForArithmetic()
    {
        // End-to-end: `(F for K)` on a `lookup of K to money in 'USD'` produces a
        // typed expression that the proof engine recognizes as carrying the 'USD'
        // currency qualifier — so currency-arithmetic compatibility checks succeed.
        var precept = """
            precept Widget
            field Prices as lookup of string to money in 'USD'
            field Total as money in 'USD' default '0.00 USD'
            state Open initial
            state Done terminal
            event Sum(Key as string)
            from Open on Sum when Prices contains Sum.Key
                -> set Total = Total + (Prices for Sum.Key)
                -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility),
            because: "the lookup-access result inherits the lookup's element-type 'USD' qualifier");
    }
}
