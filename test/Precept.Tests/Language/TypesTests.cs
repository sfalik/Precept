using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class TypesTests
{
    [Fact]
    public void TypedLiteralTypes_HaveExpectedContentValidation()
    {
        Types.GetMeta(TypeKind.Date).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.Time).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.DateTime).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.Instant).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.Duration).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.Period).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.Timezone).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.ZonedDateTime).ContentValidation.Should().BeOfType<NodaTimeValidation>();
        Types.GetMeta(TypeKind.Currency).ContentValidation.Should().BeOfType<ClosedSetValidation>();
        Types.GetMeta(TypeKind.UnitOfMeasure).ContentValidation.Should().BeOfType<UcumValidation>();
        Types.GetMeta(TypeKind.Dimension).ContentValidation.Should().BeOfType<ClosedSetValidation>();
        Types.GetMeta(TypeKind.Money).ContentValidation.Should().BeOfType<MoneyValidation>();
        Types.GetMeta(TypeKind.Quantity).ContentValidation.Should().BeOfType<QuantityValidation>();
        Types.GetMeta(TypeKind.Price).ContentValidation.Should().BeOfType<PriceValidation>();
        Types.GetMeta(TypeKind.ExchangeRate).ContentValidation.Should().BeOfType<ExchangeRateValidation>();
    }
}
