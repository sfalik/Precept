using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerFunctionQualifierTests
{
    [Fact]
    public void Max_WithDifferentCountingUnits_EmitsCrossCountingUnitOperation()
    {
        var precept = """
            precept Inventory
            field EachQty as quantity in 'each' default '1 each'
            field BoxQty as quantity in 'box' default '1 box'
            field Peak as quantity <- clamp(EachQty, BoxQty, EachQty)
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.CrossCountingUnitOperation);
    }

    [Fact]
    public void Min_WithDifferentCurrencies_EmitsCrossCurrencyArithmetic()
    {
        var precept = """
            precept Invoice
            field USD as money in 'USD' default '1 USD'
            field EUR as money in 'EUR' default '1 EUR'
            field Lowest as money <- clamp(USD, EUR, USD)
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.CrossCurrencyArithmetic);
    }

    [Fact]
    public void Abs_WithSingleQuantity_EmitsNoQualifierDiagnostic()
    {
        var precept = """
            precept Inventory
            field Qty as quantity in 'each' default '1 each'
            field AbsQty as quantity in 'each' <- abs(Qty)
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Max_WithSameDimensionUnits_EmitsNoQualifierDiagnostic()
    {
        var precept = """
            precept Shipment
            field Kg as quantity in 'kg' default '1 kg'
            field G as quantity in 'g' default '1 g'
            field Peak as quantity <- clamp(Kg, G, Kg)
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
