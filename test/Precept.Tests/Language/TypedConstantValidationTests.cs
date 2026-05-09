using System;
using System.Collections.Frozen;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class TypedConstantValidationTests
{
    [Fact]
    public void Validate_RoutesTemporalValidation()
    {
        var result = TypedConstantValidation.Validate(
            new NodaTimeValidation(TemporalLiteralKind.Date, "uuuu'-'MM'-'dd", "ISO 8601 date", ["2026-04-15"]),
            "2026-04-15",
            TypeKind.Date);

        result.IsValid.Should().BeTrue();
        result.CanonicalText.Should().Be("2026-04-15");
    }

    [Fact]
    public void Validate_RoutesClosedSetValidation()
    {
        var result = TypedConstantValidation.Validate(
            new ClosedSetValidation("values", new[] { "USD", "EUR" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase), "currency", ["USD"]),
            "USD",
            TypeKind.Currency);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RoutesRegexValidation()
    {
        var result = TypedConstantValidation.Validate(
            new RegexValidation("^[A-Z]{3}$", "three letters", ["USD"]),
            "USD",
            TypeKind.String);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RoutesUcumValidation()
    {
        var result = TypedConstantValidation.Validate(
            new UcumValidation("UCUM expression", ["kg"]),
            "kg.m/s^2",
            TypeKind.UnitOfMeasure);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DefaultArmReturnsAccepted()
    {
        var result = TypedConstantValidation.Validate(
            new UnknownValidation("unknown", ["x"]),
            "x",
            TypeKind.String);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be("x");
    }

    private sealed record UnknownValidation(string FormatDescription, string[] Examples)
        : ContentValidation(FormatDescription, Examples);
}
