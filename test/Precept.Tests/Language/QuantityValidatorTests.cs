using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class QuantityValidatorTests
{
    [Fact]
    public void Validate_ParsesQuantityLiterals()
    {
        QuantityValidator.Validate("5 kg", TypeKind.Quantity, new QuantityValidation("quantity", ["5 kg"])).IsValid.Should().BeTrue();
        QuantityValidator.Validate("2.5 mg/dL", TypeKind.Quantity, new QuantityValidation("quantity", ["5 kg"])).IsValid.Should().BeTrue();
        QuantityValidator.Validate("5 parsecs", TypeKind.Quantity, new QuantityValidation("quantity", ["5 kg"])).IsValid.Should().BeFalse();
    }
}
