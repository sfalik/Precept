using System.Linq;
using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerInterpolatedStaticQualifierTests
{
    [Fact]
    public void InterpolatedTypedConstant_StaticCurrencyQualifier_IsCaptured()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Test
            field Amount as decimal default '1'
            field Price as money default '0 USD'
            event Go initial
            on Go
                -> set Price = '{Amount} USD'
            """);

        var value = GetAssignedConstant(index, "Price");
        value.StaticQualifier.Should().BeOfType<StaticCurrencyQualifier>()
            .Which.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void InterpolatedTypedConstant_StaticUnitQualifier_IsCaptured()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Test
            field Amount as decimal default '1'
            field Qty as quantity in 'kg' default '0 kg'
            event Go initial
            on Go
                -> set Qty = '{Amount} kg'
            """);

        var value = GetAssignedConstant(index, "Qty");
        value.StaticQualifier.Should().BeOfType<StaticUnitQualifier>()
            .Which.Unit.CanonicalCode.Should().Be("kg");
    }

    [Fact]
    public void InterpolatedTypedConstant_StaticCurrencyAndUnitQualifier_IsCaptured()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Test
            field Amount as decimal default '1'
            field Cost as price default '0 USD/kg'
            event Go initial
            on Go
                -> set Cost = '{Amount} USD/kg'
            """);

        var value = GetAssignedConstant(index, "Cost");
        var qualifier = value.StaticQualifier.Should().BeOfType<StaticCurrencyAndUnitQualifier>().Subject;
        qualifier.CurrencyCode.Should().Be("USD");
        qualifier.Unit.CanonicalCode.Should().Be("kg");
    }

    [Fact]
    public void InterpolatedTypedConstant_StaticFromToCurrenciesQualifier_IsCaptured()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Test
            field Amount as decimal default '1'
            field Rate as exchangerate default '1 USD/EUR'
            event Go initial
            on Go
                -> set Rate = '{Amount} USD/EUR'
            """);

        var value = GetAssignedConstant(index, "Rate");
        var qualifier = value.StaticQualifier.Should().BeOfType<StaticFromToCurrenciesQualifier>().Subject;
        qualifier.FromCode.Should().Be("USD");
        qualifier.ToCode.Should().Be("EUR");
    }

    [Fact]
    public void InterpolatedTypedConstant_WholeValueSlot_HasNullStaticQualifier()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Test
            field Source as money default '1 USD'
            field Target as money default '0 USD'
            event Go initial
            on Go
                -> set Target = '{Source}'
            """);

        var value = GetAssignedConstant(index, "Target");
        value.StaticQualifier.Should().BeNull();
    }

    private static InterpolatedTypedConstant GetAssignedConstant(SemanticIndex index, string fieldName)
    {
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(action => action.FieldName == fieldName);
        return assign.InputExpression.Should().BeOfType<InterpolatedTypedConstant>().Subject;
    }
}
