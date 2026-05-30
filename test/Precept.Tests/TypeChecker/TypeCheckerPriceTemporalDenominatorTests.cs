using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Temporal-unit price denominators: <c>price in 'USD/hours'</c> and siblings.
/// A price's denominator may be a NodaTime temporal unit (hours/minutes/seconds/
/// days/weeks/months/years) — the vocabulary spec § D15 mandates for time-unit
/// denominators — not only a UCUM physical unit or a count unit. The denominator's
/// dimension is derived from <see cref="TemporalUnits.TemporalUnitEntry.IsCalendarBased"/>
/// (clock units → "time"; calendar units → "date"), matching the period/duration
/// temporal-dimension spellings so the existing cancellation chain resolves.
/// </summary>
public class TypeCheckerPriceTemporalDenominatorTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Parses (the headline) — temporal-unit denominators are no longer PRE0075
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("hours")]
    [InlineData("minutes")]
    [InlineData("seconds")]
    [InlineData("days")]
    [InlineData("weeks")]
    [InlineData("months")]
    [InlineData("years")]
    public void PriceIn_TemporalDenominator_Parses(string unit)
    {
        var precept = $"""
            precept Billing
            field Rate as price in 'USD/{unit}'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PriceIn_SingularTemporalDenominator_Parses()
    {
        // TemporalUnits matches singular and plural; 'hour' resolves like 'hours'.
        var precept = """
            precept Billing
            field Rate as price in 'USD/hour'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Dimension carried — clock → "time", calendar → "date"
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("hours", "time")]
    [InlineData("minutes", "time")]
    [InlineData("seconds", "time")]
    [InlineData("days", "date")]
    [InlineData("weeks", "date")]
    [InlineData("months", "date")]
    [InlineData("years", "date")]
    public void PriceIn_TemporalDenominator_CarriesDerivedDimension(string unit, string expectedDimension)
    {
        var precept = $"""
            precept Billing
            field Rate as price in 'USD/{unit}'
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        diagnostics.Where(d => d.Severity == Severity.Error)
            .Where(d => d.Code != DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString())
            .Should().BeEmpty();

        var rateField = index.Fields.First(f => f.Name == "Rate");
        var compound = (DeclaredQualifierMeta.CompoundPrice)rateField.DeclaredQualifiers
            .First(q => q is DeclaredQualifierMeta.CompoundPrice);
        compound.CurrencyCode.Should().Be("USD");
        compound.UnitCode.Should().Be(unit);
        compound.DimensionName.Should().Be(expectedDimension);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cancellation (FULL pipeline — Compiler.Compile, so the proof-stage chain
    //  obligation is actually exercised). A temporal-denominator price cancels a
    //  period declared with a temporal DIMENSION (`of 'time'`/`of 'date'`), a
    //  `duration`, or a single-basis BASIS (`in 'hours'`) — the price×period chain
    //  derives the period's TemporalDimension from its single declared basis. A
    //  composite basis (`in 'hours + minutes'`) does NOT cancel a single-unit
    //  denominator; it is a compile error (PRE0074, CompoundPeriodDenominator).
    // ════════════════════════════════════════════════════════════════════════

    private static bool HasQualifierChainError(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));

    private static bool HasCompoundPeriodDenominatorError(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.CompoundPeriodDenominator));

    [Fact]
    public void PricePerHours_TimesPeriodOfTime_Cancels()
    {
        HasQualifierChainError("""
            precept Billing
            field Rate as price in 'USD/hours'
            field Worked as period of 'time'
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Worked -> transition Done
            """).Should().BeFalse("a time-dimension period cancels the USD/hours denominator");
    }

    [Fact]
    public void PricePerHours_TimesDuration_Cancels()
    {
        HasQualifierChainError("""
            precept Billing
            field Rate as price in 'USD/hours'
            field Elapsed as duration
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Elapsed -> transition Done
            """).Should().BeFalse("a duration carries the time dimension and cancels the USD/hours denominator");
    }

    [Fact]
    public void PricePerHours_TimesPeriodInHours_Cancels()
    {
        // Per § D15 `period in 'hours'` cancels `price in 'USD/hours'`: the price×period chain
        // derives the period's TemporalDimension from its single declared basis ('hours' → time),
        // so the chain obligation discharges. The single-basis guard is what makes this sound —
        // a composite basis cannot derive a single dimension this way (see compound tests below).
        HasQualifierChainError("""
            precept Billing
            field Rate as price in 'USD/hours'
            field Worked as period in 'hours'
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Worked -> transition Done
            """).Should().BeFalse("a single-basis period resolves its temporal dimension and cancels the USD/hours denominator");
    }

    [Fact]
    public void PricePerMonths_TimesPeriodInMonths_Cancels()
    {
        // Calendar single-basis: 'months' derives the date dimension and cancels USD/months.
        HasQualifierChainError("""
            precept Billing
            field Rate as price in 'USD/months'
            field Worked as period in 'months'
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Worked -> transition Done
            """).Should().BeFalse("a single calendar-basis period resolves the date dimension and cancels the USD/months denominator");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Composite basis rejection — a multi-component period cannot cancel a
    //  single-unit denominator (PRE0074, CompoundPeriodDenominator), and must NOT
    //  fall through to the generic UnprovedQualifierCompatibility.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PricePerHours_TimesCompositePeriod_EmitsCompoundPeriodDenominator()
    {
        var source = """
            precept Billing
            field Rate as price in 'USD/hours'
            field Worked as period in 'hours + minutes'
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Worked -> transition Done
            """;

        HasCompoundPeriodDenominatorError(source)
            .Should().BeTrue("a composite period cannot cancel a single-unit denominator");
        HasQualifierChainError(source)
            .Should().BeFalse("the composite case emits the precise PRE0074, not the generic chain error");
    }

    [Fact]
    public void PricePerDays_TimesDatetimeSpanningCompositePeriod_EmitsCompoundPeriodDenominator()
    {
        // 'days + hours' spans calendar and clock atoms (a Datetime-spanning composite);
        // it still cannot cancel the single-unit USD/days denominator.
        HasCompoundPeriodDenominatorError("""
            precept Billing
            field Rate as price in 'USD/days'
            field Worked as period in 'days + hours'
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Worked -> transition Done
            """).Should().BeTrue("a datetime-spanning composite period cannot cancel a single-unit denominator");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Negative — a non-temporal, non-UCUM denominator still rejects
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_NonsenseDenominator_StillRejects()
    {
        var precept = """
            precept Billing
            field Rate as price in 'USD/banana'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidUnitString);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression — UCUM physical and count denominators unchanged
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceIn_UcumDenominator_StillResolvesPhysicalDimension()
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

        var compound = (DeclaredQualifierMeta.CompoundPrice)index.Fields.First(f => f.Name == "Cost")
            .DeclaredQualifiers.First(q => q is DeclaredQualifierMeta.CompoundPrice);
        compound.DimensionName.Should().Be("mass");
    }

    [Fact]
    public void PriceIn_CountDenominator_StillResolvesCount()
    {
        var precept = """
            precept Product
            field Cost as price in 'USD/each'
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        diagnostics.Where(d => d.Severity == Severity.Error)
            .Where(d => d.Code != DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString())
            .Should().BeEmpty();

        var compound = (DeclaredQualifierMeta.CompoundPrice)index.Fields.First(f => f.Name == "Cost")
            .DeclaredQualifiers.First(q => q is DeclaredQualifierMeta.CompoundPrice);
        compound.DimensionName.Should().Be("count");
    }
}
