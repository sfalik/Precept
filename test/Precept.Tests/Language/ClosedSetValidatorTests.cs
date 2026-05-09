using System;
using System.Collections.Frozen;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class ClosedSetValidatorTests
{
    [Fact]
    public void Validate_UsesSetMembership()
    {
        var validation = new ClosedSetValidation("values", new[] { "USD", "EUR" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase), "currencies", ["USD"]);
        ClosedSetValidator.Validate("USD", validation).IsValid.Should().BeTrue();
        ClosedSetValidator.Validate("JPY", validation).IsValid.Should().BeFalse();
    }
}
