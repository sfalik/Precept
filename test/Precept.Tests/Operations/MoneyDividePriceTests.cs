using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.OperationsSuite;

/// <summary>
/// F-LANG-BIZ-01: <c>money / price → quantity</c>. The inverse-of-inverse
/// completion alongside the existing <c>price × quantity → money</c> and
/// <c>money ÷ quantity → price</c> entries. Currency-axis match is enforced
/// via QualifierChainProofRequirement; the result quantity inherits the
/// price's denominator unit (e.g. <c>'USD' / 'USD/each' → 'each'</c>).
/// </summary>
public class MoneyDividePriceTests
{
    [Fact]
    public void MoneyDividePrice_MatchingCurrency_TypeChecksAsQuantity()
    {
        // Catalog entry exists with the right type signature.
        var meta = Operations.GetMeta(OperationKind.MoneyDividePrice);
        var binary = meta.Should().BeOfType<BinaryOperationMeta>().Subject;

        binary.Op.Should().Be(OperatorKind.Divide);
        binary.Result.Should().Be(TypeKind.Quantity);
        binary.Lhs.Kind.Should().Be(TypeKind.Money);
        binary.Rhs.Kind.Should().Be(TypeKind.Price);
    }

    [Fact]
    public void MoneyDividePrice_HasCurrencyMatchAndDivisorProofs()
    {
        var meta = Operations.GetMeta(OperationKind.MoneyDividePrice);
        var binaryMeta = meta.Should().BeOfType<BinaryOperationMeta>().Subject;

        binaryMeta.ProofRequirements.Should().HaveCount(2);

        var currencyChain = binaryMeta.ProofRequirements
            .OfType<QualifierChainProofRequirement>()
            .ToList();
        currencyChain.Should().ContainSingle(
            because: "currency must match across money and price");
        currencyChain.Single().LeftAxis.Should().Be(QualifierAxis.Currency);
        currencyChain.Single().RightAxis.Should().Be(QualifierAxis.Currency);

        var divisorNonzero = binaryMeta.ProofRequirements
            .OfType<NumericProofRequirement>()
            .Where(n => n.Comparison == OperatorKind.NotEquals && n.Threshold == 0m)
            .ToList();
        divisorNonzero.Should().ContainSingle(
            because: "divisor must be non-zero");
    }

    [Fact]
    public void MoneyDividePrice_ResultQualifierPolicy_InheritsPriceDenominatorUnit()
    {
        var meta = Operations.GetMeta(OperationKind.MoneyDividePrice);
        var binaryMeta = meta.Should().BeOfType<BinaryOperationMeta>().Subject;
        binaryMeta.ResultQualifierPolicy.Should().Be(ResultQualifierPolicy.InheritPriceDenominatorUnit);
    }

    [Fact]
    public void MoneyDividePrice_AppearsInBinaryOperationCatalog()
    {
        var match = Operations.All
            .OfType<BinaryOperationMeta>()
            .Where(bm => bm.Lhs.Kind == TypeKind.Money
                      && bm.Rhs.Kind == TypeKind.Price
                      && bm.Op == OperatorKind.Divide)
            .ToList();
        match.Should().ContainSingle();
    }

    [Fact]
    public void MoneyDividePrice_AuthoredCleanly_NoTypeMismatchDiagnostic()
    {
        // The natural author shape: money TotalCost, price UnitPrice → quantity TotalUnits.
        // Same-currency case — currency-chain obligation should be discharged from
        // the static qualifiers.
        var (_, diagnostics) = Check("""
            precept InventoryAdjust
            field TotalCost as money in 'USD' default '0 USD' editable
            field UnitPrice as price in 'USD' of 'count' default '1 USD/count' editable
            field TotalUnits as quantity in 'count' default '0 count' <- TotalCost / UnitPrice
            state Open initial
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.TypeMismatch),
            because: "money ÷ price now type-checks to quantity via the new MoneyDividePrice operation");
    }
}
