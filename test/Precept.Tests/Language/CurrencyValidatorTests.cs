using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class CurrencyValidatorTests
{
    [Fact]
    public void Validate_AcceptsKnownCurrencyCodes()
    {
        CurrencyValidator.Validate("usd").IsValid.Should().BeTrue();
        CurrencyValidator.Validate("XYZ").IsValid.Should().BeFalse();
    }
}
