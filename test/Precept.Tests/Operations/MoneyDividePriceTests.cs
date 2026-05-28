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

    [Fact]
    public void MoneyDividePrice_ResultInheritsPriceDenominatorUnit_NoQualifierMismatch()
    {
        // Decision A wiring: PriceDenominatorInherited subtype + assignment-side
        // resolver should derive the result's Unit-axis qualifier from the price's
        // denominator unit ('each' from 'USD/each'), so the target field's declared
        // 'quantity in 'each'' qualifier matches and no QualifierMismatch fires.
        // Uses the canonical compound-price syntax `price in 'USD/each'`.
        var (_, diagnostics) = Check("""
            precept Reorder
            field Budget as money in 'USD' default '0 USD' editable
            field UnitCost as price in 'USD/each' default '1 USD/each' editable
            field UnitsToBuy as quantity in 'each' default '0 each' <- Budget / UnitCost
            state Open initial
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.QualifierMismatch),
            because: "result quantity inherits 'each' from the price's denominator unit, matching the target's qualifier");
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility),
            because: "PriceDenominatorInherited resolver provides the unit-axis qualifier for proof discharge");
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility),
            because: "the assignment-side resolver returns Resolved (not Unknown) for the Unit/Dimension axes");
    }

    [Fact]
    public void MoneyDividePrice_TransitiveInheritance_PriceSideWrappedInBinaryOp()
    {
        // Transitive resolution: the price operand itself is a binary op
        // (`UnitCost + Markup`). The resolver must walk into that nested op to
        // find the CompoundPrice qualifier on the leaf field references.
        // Exercises ResolveQualifierFromExpression's recursive descent through
        // a SameQualifierRequired binding to reach the price.
        var (_, diagnostics) = Check("""
            precept Reorder
            field Budget as money in 'USD' default '0 USD' editable
            field UnitCost as price in 'USD/each' default '1 USD/each' editable
            field Markup as price in 'USD/each' default '0 USD/each' editable
            field UnitsToBuy as quantity in 'each' default '0 each' <- Budget / (UnitCost + Markup)
            state Open initial
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.QualifierMismatch),
            because: "transitive resolution: outer / projects denominator from the inner (UnitCost + Markup) price sum");
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility),
            because: "the resolver must walk into the nested binary op on the price side and still find the denominator");
    }

    [Fact]
    public void MoneyDividePrice_MoneySideWrappedInBinaryOp_ResolutionStillCorrect()
    {
        // The money operand wrapped in a binary op: (Budget - Spent) / UnitCost.
        // Tests that the operand-side identification `binary.Left.ResultType ==
        // TypeKind.Price ? binary.Left : binary.Right` correctly picks the price
        // operand even when the money side is itself a TypedBinaryOp.
        var (_, diagnostics) = Check("""
            precept Reorder
            field Budget as money in 'USD' default '0 USD' editable
            field Spent as money in 'USD' default '0 USD' editable
            field UnitCost as price in 'USD/each' default '1 USD/each' editable
            field Remaining as quantity in 'each' default '0 each' <- (Budget - Spent) / UnitCost
            state Open initial
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.QualifierMismatch),
            because: "wrapping the money side does not change the price-side denominator inheritance");
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility),
            because: "operand-side identification correctly picks the Price operand when Money is nested");
    }
}
