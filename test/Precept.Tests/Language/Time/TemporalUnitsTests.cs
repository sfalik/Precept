using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Time;

public class TemporalUnitsTests
{
    [Fact]
    public void AllEntries_ContainExpectedUnitWords()
    {
        TemporalUnits.TryGet("years", out var years).Should().BeTrue();
        TemporalUnits.TryGet("months", out var months).Should().BeTrue();
        TemporalUnits.TryGet("weeks", out var weeks).Should().BeTrue();
        TemporalUnits.TryGet("days", out var days).Should().BeTrue();
        TemporalUnits.TryGet("hours", out var hours).Should().BeTrue();
        TemporalUnits.TryGet("minutes", out var minutes).Should().BeTrue();
        TemporalUnits.TryGet("seconds", out var seconds).Should().BeTrue();

        years.IsPeriod.Should().BeTrue();
        months.IsPeriod.Should().BeTrue();
        weeks.IsPeriod.Should().BeTrue();
        days.IsPeriod.Should().BeTrue();
        hours.IsDuration.Should().BeTrue();
        minutes.IsDuration.Should().BeTrue();
        seconds.IsDuration.Should().BeTrue();
    }
}
