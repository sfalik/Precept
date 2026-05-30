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
    //  period declared with a temporal DIMENSION (`of 'time'`/`of 'date'`) or a
    //  `duration`. A period declared only by BASIS (`in 'hours'`) does NOT yet
    //  cancel — the price×period chain resolves the period's TemporalDimension,
    //  which a declared TemporalUnit basis does not surface (pinned as a gap below).
    // ════════════════════════════════════════════════════════════════════════

    private static bool HasQualifierChainError(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));

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
    public void PricePerHours_TimesPeriodInHours_NotYetProven()
    {
        // KNOWN GAP: per § D15 `period in 'hours'` should cancel `price in 'USD/hours'`, but the
        // price×period chain resolves the period's TemporalDimension and a declared TemporalUnit
        // basis does not surface one on that path — so it currently fails. Pinned so the gap is
        // visible; a future fix flips this to BeFalse. Authors use `period of 'time'` meanwhile.
        HasQualifierChainError("""
            precept Billing
            field Rate as price in 'USD/hours'
            field Worked as period in 'hours'
            field Pay as money in 'USD'
            state Open initial
            state Done
            event Submit
            from Open on Submit -> set Pay = Rate * Worked -> transition Done
            """).Should().BeTrue("a declared-basis period does not yet resolve its temporal dimension for the chain (gap)");
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
