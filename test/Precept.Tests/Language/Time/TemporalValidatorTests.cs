using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Time;

public class TemporalValidatorTests
{
    [Fact]
    public void Validate_ReturnsTypedConstantResultForDate()
    {
        var result = TemporalValidator.Validate(
            "2026-04-15",
            TypeKind.Date,
            new NodaTimeValidation(TemporalLiteralKind.Date, "uuuu'-'MM'-'dd", "ISO 8601 date", ["2026-04-15"]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllowsIsoPeriodFallback()
    {
        var result = TemporalValidator.Validate(
            "P30D",
            TypeKind.Period,
            new NodaTimeValidation(TemporalLiteralKind.TemporalQuantity, "NormalizingIso", "period", ["P30D"]));

        result.IsValid.Should().BeTrue();
    }
}
