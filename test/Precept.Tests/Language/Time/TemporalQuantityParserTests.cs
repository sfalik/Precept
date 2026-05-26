using FluentAssertions;
using NodaTime;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Time;

public class TemporalQuantityParserTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Existing context-free cases (preserved)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ReturnsPeriodForCalendarUnits()
    {
        var result = TemporalQuantityParser.Parse("2 years + 6 months");

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Period>();
    }

    [Fact]
    public void Parse_ReturnsDurationForTimeUnits()
    {
        var result = TemporalQuantityParser.Parse("72 hours");

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Duration>();
    }

    [Fact]
    public void Parse_RejectsNonIntegerMagnitudes()
    {
        TemporalQuantityParser.Parse("0.5 days").IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Context-aware classification (expectedType)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_WithDurationContext_ConvertsDaysToExactDuration()
    {
        var result = TemporalQuantityParser.Parse("365 days", TypeKind.Duration);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Duration>();
    }

    [Fact]
    public void Parse_WithDurationContext_ConvertsWeeksToDays()
    {
        var result = TemporalQuantityParser.Parse("2 weeks", TypeKind.Duration);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Duration>();
        ((Duration)result.Value!).TotalDays.Should().BeApproximately(14, 0.001);
    }

    [Fact]
    public void Parse_WithDurationContext_RejectsMonths()
    {
        var result = TemporalQuantityParser.Parse("3 months", TypeKind.Duration);

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(d => d.Code == "TEMP007");
    }

    [Fact]
    public void Parse_WithDurationContext_RejectsYears()
    {
        var result = TemporalQuantityParser.Parse("1 year", TypeKind.Duration);

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(d => d.Code == "TEMP007");
    }

    [Fact]
    public void Parse_WithPeriodContext_AcceptsTimeUnits()
    {
        var result = TemporalQuantityParser.Parse("3 hours", TypeKind.Period);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Period>();
    }

    [Fact]
    public void Parse_WithPeriodContext_AcceptsMixedUnits()
    {
        // Mixed units accepted when expectedType is Period
        var result = TemporalQuantityParser.Parse("1 day + 2 hours", TypeKind.Period);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Period>();
        var period = (Period)result.Value!;
        period.Days.Should().Be(1);
        period.Hours.Should().Be(2);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TEMP005 gated on ambiguous context
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_WithNoContext_RejectsMixedUnits_TEMP005()
    {
        var result = TemporalQuantityParser.Parse("1 day + 2 hours");

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(d => d.Code == "TEMP005");
    }

    [Fact]
    public void Parse_WithDurationContext_AcceptsMixedDayAndHour()
    {
        // When duration context, '1 day' converts to exact Duration (24h), '2 hours' is exact
        var result = TemporalQuantityParser.Parse("1 day + 2 hours", TypeKind.Duration);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Duration>();
        ((Duration)result.Value!).TotalHours.Should().BeApproximately(26, 0.001);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Canonical field-default cases (closes BUG-010 type-inference family)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_PeriodFieldDefault_30Days_ReturnsPeriod()
    {
        // 'field G as period default '30 days'' — period context from field declaration
        var result = TemporalQuantityParser.Parse("30 days", TypeKind.Period);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Period>();
        ((Period)result.Value!).Days.Should().Be(30);
    }

    [Fact]
    public void Parse_DurationFieldDefault_30Days_ReturnsDuration()
    {
        // 'field D as duration default '30 days'' — duration context from field declaration
        var result = TemporalQuantityParser.Parse("30 days", TypeKind.Duration);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeOfType<Duration>();
    }
}
