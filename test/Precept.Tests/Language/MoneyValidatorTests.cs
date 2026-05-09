using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class MoneyValidatorTests
{
    [Fact]
    public void Validate_ParsesMoneyLiterals()
    {
        MoneyValidator.Validate("100 USD", new MoneyValidation("money", ["100 USD"])).IsValid.Should().BeTrue();
        MoneyValidator.Validate("50.25 EUR", new MoneyValidation("money", ["100 USD"])).IsValid.Should().BeTrue();
        MoneyValidator.Validate("100", new MoneyValidation("money", ["100 USD"])).IsValid.Should().BeFalse();
        MoneyValidator.Validate("100 XYZ", new MoneyValidation("money", ["100 USD"])).IsValid.Should().BeFalse();
    }
}
