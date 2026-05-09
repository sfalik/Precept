using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumValidatorTests
{
    [Fact]
    public void Validate_AcceptsValidUcumExpression()
    {
        UcumValidator.Validate("kg.m/s^2", TypeKind.UnitOfMeasure, new UcumValidation("UCUM", ["kg"])).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsInvalidUcumExpression()
    {
        UcumValidator.Validate("parsecs", TypeKind.UnitOfMeasure, new UcumValidation("UCUM", ["kg"])).IsValid.Should().BeFalse();
    }
}
