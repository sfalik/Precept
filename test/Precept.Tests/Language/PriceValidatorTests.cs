using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class PriceValidatorTests
{
    [Fact]
    public void Validate_ParsesPriceLiterals()
    {
        PriceValidator.Validate("4.17 USD/each", new PriceValidation("price", ["4.17 USD/each"])).IsValid.Should().BeTrue();
        PriceValidator.Validate("10.00 EUR/kg", new PriceValidation("price", ["4.17 USD/each"])).IsValid.Should().BeTrue();
    }
}
