using FluentAssertions;
using NodaTime;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Time;

public class TemporalParserTests
{
    [Fact]
    public void Parse_Date_ValidatesIsoDate()
    {
        TemporalParser.Parse(TemporalLiteralKind.Date, "2026-04-15").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.Date, "2026/04/15").IsValid.Should().BeFalse();
        TemporalParser.Parse(TemporalLiteralKind.Date, "2026-13-01").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Time_ValidatesExtendedAndShortForms()
    {
        TemporalParser.Parse(TemporalLiteralKind.Time, "14:30:00").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.Time, "14:30").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.Time, "25:00:00").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_DateTime_ValidatesIsoDateTime()
    {
        TemporalParser.Parse(TemporalLiteralKind.DateTime, "2026-04-15T14:30:00").IsValid.Should().BeTrue();
    }

    [Fact]
    public void Parse_Instant_RequiresUtcSuffix()
    {
        TemporalParser.Parse(TemporalLiteralKind.Instant, "2026-04-15T14:30:00Z").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.Instant, "2026-04-15T14:30:00").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_ZonedDateTime_ValidatesTimezone()
    {
        TemporalParser.Parse(TemporalLiteralKind.ZonedDateTime, "2026-04-15T14:30:00[America/New_York]").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.ZonedDateTime, "2026-04-15T14:30:00[Invalid/Zone]").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Timezone_ValidatesIanaIds()
    {
        TemporalParser.Parse(TemporalLiteralKind.Timezone, "America/New_York").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.Timezone, "Mars/Olympus").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_TemporalQuantity_ValidatesQuantityForms()
    {
        TemporalParser.Parse(TemporalLiteralKind.TemporalQuantity, "30 days").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.TemporalQuantity, "72 hours").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.TemporalQuantity, "2 years + 6 months").IsValid.Should().BeTrue();
        TemporalParser.Parse(TemporalLiteralKind.TemporalQuantity, "0.5 days").IsValid.Should().BeFalse();
        TemporalParser.Parse(TemporalLiteralKind.TemporalQuantity, "30 parsecs").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Date_ReturnsLocalDate()
    {
        var result = TemporalParser.Parse(TemporalLiteralKind.Date, "2026-04-15");

        result.Value.Should().BeOfType<LocalDate>();
        result.CanonicalText.Should().Be("2026-04-15");
    }
}
