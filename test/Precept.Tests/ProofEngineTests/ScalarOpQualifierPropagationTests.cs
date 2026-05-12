using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for D4: Scalar-op qualifier propagation via
/// <see cref="Language.ResultQualifierPolicy.InheritFromQualifiedOperand"/>.
/// </summary>
public sealed class ScalarOpQualifierPropagationTests
{
    [Fact]
    public void ScalarOp_MoneyTimesDecimal_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field A as money in 'USD' default '10 USD'
            field B as decimal default 2
            field C as money in 'USD' <- A * B
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ScalarOp_MoneyDivideDecimal_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field A as money in 'USD' default '10 USD'
            field B as decimal default 2 nonzero
            field C as money in 'USD' <- A / B
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ScalarOp_QuantityTimesDecimal_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field Q as quantity of 'mass' default '1 kg'
            field S as decimal default 2
            field R as quantity of 'mass' <- Q * S
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ScalarOp_QuantityDivideDecimal_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field Q as quantity of 'mass' default '1 kg'
            field S as decimal default 2 nonzero
            field R as quantity of 'mass' <- Q / S
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ScalarOp_PriceTimesDecimal_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field P as price in 'USD' of 'mass' default '0.00 USD/kg'
            field S as decimal default 2
            field R as price in 'USD' of 'mass' <- P * S
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ScalarOp_PriceDivideDecimal_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field P as price in 'USD' of 'mass' default '0.00 USD/kg'
            field S as decimal default 2 nonzero
            field R as price in 'USD' of 'mass' <- P / S
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ChainedScalarOps_PreservesQualifier()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field A as money in 'USD' default '10 USD'
            field B as decimal default 2
            field C as money in 'USD' <- A * B * B
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MoneyScaledSubtraction_PreservesQualifier()
    {
        // The original repro case from D4 root cause analysis
        var compilation = Compiler.Compile("""
            precept Test
            field TotalCost as money in 'USD' default '10 USD'
            field DiscountPercent as decimal default 0
            field FinalCost as money in 'USD' <- TotalCost - (TotalCost * DiscountPercent / 100)
            """);

        compilation.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void CrossCurrencyScalarResult_Diagnostic()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field A as money in 'USD' default '10 USD'
            field B as decimal default 2
            field C as money in 'EUR' <- A * B
            """);

        compilation.HasErrors.Should().BeTrue("USD ≠ EUR should produce PRE0114");
    }

    [Fact]
    public void BidirectionalScalarOrder_DecimalOnLeft()
    {
        var compilation = Compiler.Compile("""
            precept Test
            field A as money in 'USD' default '10 USD'
            field B as decimal default 2
            field C as money in 'USD' <- B * A
            """);

        compilation.HasErrors.Should().BeFalse();
    }
}
