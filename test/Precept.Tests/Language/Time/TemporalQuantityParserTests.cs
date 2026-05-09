using FluentAssertions;
using NodaTime;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Time;

public class TemporalQuantityParserTests
{
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
}
