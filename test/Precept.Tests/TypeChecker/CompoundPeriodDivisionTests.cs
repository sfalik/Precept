using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// D15 compound-period divisor rejection on the division path. A compound (multi-component)
/// period cannot be a divisor: <c>money ÷ period in 'hours + minutes'</c> is ill-defined because
/// NodaTime stores period components separately with no single-magnitude total — so it is a
/// compile error via <c>CompoundPeriodDenominator</c>. A single-basis period divisor
/// (<c>period in 'hours'</c>) and a <c>duration</c> divisor are sound. This is the division
/// analogue of the multiplication-path rejection shipped earlier; both reuse the same diagnostic.
///
/// Full pipeline via <see cref="Compiler.Compile(string)"/> so the type-stage denominator check runs.
/// </summary>
public class CompoundPeriodDivisionTests
{
    private static bool HasCompoundPeriodDenominator(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.CompoundPeriodDenominator));

    [Fact]
    public void MoneyDivideCompoundPeriod_ClockComposite_EmitsCompoundPeriodDenominator()
    {
        var source = """
            precept Billing
            field M as money in 'USD'
            field P as period in 'hours + minutes'
            field R as price in 'USD/hours' <- M / P
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeTrue("a compound period has no single magnitude to divide by (D15)");
    }

    [Fact]
    public void MoneyDivideCompoundPeriod_CalendarComposite_EmitsCompoundPeriodDenominator()
    {
        var source = """
            precept Billing
            field M as money in 'USD'
            field P as period in 'months + days'
            field R as price in 'USD/months' <- M / P
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeTrue("a compound calendar period cannot be reduced to a single divisor unit");
    }

    [Fact]
    public void QuantityDivideCompoundPeriod_EmitsCompoundPeriodDenominator()
    {
        var source = """
            precept Throughput
            field Q as quantity in 'kg'
            field P as period in 'hours + minutes'
            field R as quantity in 'kg/hours' <- Q / P
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeTrue("quantity ÷ compound period is the same ill-defined divisor problem");
    }

    [Fact]
    public void MoneyDivideSingleBasisPeriod_NoCompoundPeriodDenominator()
    {
        var source = """
            precept Billing
            field M as money in 'USD'
            field P as period in 'hours'
            field R as price in 'USD/hours' <- M / P
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeFalse("a single-basis period is a valid divisor");
    }

    [Fact]
    public void MoneyDivideSingleCalendarBasisPeriod_NoCompoundPeriodDenominator()
    {
        var source = """
            precept Billing
            field M as money in 'USD'
            field P as period in 'months'
            field R as price in 'USD/months' <- M / P
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeFalse("a single calendar-basis period is a valid divisor");
    }

    [Fact]
    public void MoneyDivideDuration_NoCompoundPeriodDenominator()
    {
        var source = """
            precept Billing
            field M as money in 'USD'
            field D as duration
            field R as price in 'USD/hours' <- M / D
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeFalse("a duration is a single scalar and is exempt (D15)");
    }

    [Fact]
    public void QuantityDivideSingleBasisPeriod_NoCompoundPeriodDenominator()
    {
        var source = """
            precept Throughput
            field Q as quantity in 'kg'
            field P as period in 'hours'
            field R as quantity in 'kg/hours' <- Q / P
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeFalse("a single-basis period is a valid quantity divisor");
    }

    [Fact]
    public void DurationDivideDuration_NoCompoundPeriodDenominator()
    {
        // duration ÷ duration → number ratio; a duration carries no TemporalUnit basis, so the
        // compound-period denominator check is correctly inapplicable.
        var source = """
            precept Ratio
            field D1 as duration
            field D2 as duration
            field R as number <- D1 / D2
            state Open initial
            """;

        HasCompoundPeriodDenominator(source)
            .Should().BeFalse("duration ÷ duration has no period basis to reject");
    }
}
