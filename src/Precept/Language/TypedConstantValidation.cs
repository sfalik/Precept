namespace Precept.Language;

public static class TypedConstantValidation
{
    public static TypedConstantParseResult Validate(
        ContentValidation validation,
        string rawText,
        TypeKind targetType,
        TypedConstantContext? context = null) => validation switch
    {
        NodaTimeValidation noda => TemporalValidator.Validate(rawText, targetType, noda, context),
        ClosedSetValidation closed => ClosedSetValidator.Validate(rawText, closed),
        RegexValidation regex => RegexValidator.Validate(rawText, regex),
        UcumValidation ucum => UcumValidator.Validate(rawText, targetType, ucum, context),
        MoneyValidation money => MoneyValidator.Validate(rawText, money),
        QuantityValidation quantity => QuantityValidator.Validate(rawText, targetType, quantity, context),
        PriceValidation price => PriceValidator.Validate(rawText, price),
        ExchangeRateValidation exchangeRate => ExchangeRateValidator.Validate(rawText, exchangeRate),
        _ => TypedConstantParseResult.Accepted(rawText),
    };
}
