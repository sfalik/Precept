using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class RegexValidatorTests
{
    [Fact]
    public void Validate_UsesPatternMatching()
    {
        var validation = new RegexValidation("^[A-Z]{3}$", "three letters", ["USD"]);
        RegexValidator.Validate("USD", validation).IsValid.Should().BeTrue();
        RegexValidator.Validate("usd", validation).IsValid.Should().BeFalse();
    }
}
