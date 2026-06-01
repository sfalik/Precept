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
        // A dimension typed constant is partitioned by the type it is observed on (UCUM physical
        // family vs. temporal category). When the comparison peer's accessor declares a partition,
        // validate against that partition's registry rather than the default (UCUM) set, so
        // period.dimension == 'date' resolves and a cross-partition value is rejected.
        ClosedSetValidation closed when context?.DimensionPartition is { } partition =>
            ClosedSetValidator.Validate(rawText, Types.DimensionValidationFor(partition), DiagnosticCode.InvalidDimensionString),
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
