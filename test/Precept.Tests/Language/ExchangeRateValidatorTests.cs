using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class ExchangeRateValidatorTests
{
    [Fact]
    public void Validate_ParsesExchangeRates()
    {
        ExchangeRateValidator.Validate("1.08 USD/EUR", new ExchangeRateValidation("fx", ["1.08 USD/EUR"])).IsValid.Should().BeTrue();
        ExchangeRateValidator.Validate("0.92 EUR/USD", new ExchangeRateValidation("fx", ["1.08 USD/EUR"])).IsValid.Should().BeTrue();
    }
}
