using System.Text.RegularExpressions;
using NodaTime.Text;

namespace Precept.Language;

public static class TypedConstantValidation
{
    public static TypedConstantParseResult Validate(
        ContentValidation validation,
        string rawText,
        TypeKind targetType,
        TypedConstantContext? context = null) => validation switch
    {
        NodaTimeValidation noda => ValidateTemporal(rawText, noda),
        ClosedSetValidation closed => ValidateClosedSet(rawText, closed),
        RegexValidation regex => ValidateRegex(rawText, regex),
        UcumValidation => ValidateUcum(rawText),
        MoneyValidation money => TypedConstantParseResult.Accepted(rawText) with { FormatDescription = money.FormatDescription },
        QuantityValidation quantity => TypedConstantParseResult.Accepted(rawText) with { FormatDescription = quantity.FormatDescription },
        PriceValidation price => TypedConstantParseResult.Accepted(rawText) with { FormatDescription = price.FormatDescription },
        ExchangeRateValidation exchangeRate => TypedConstantParseResult.Accepted(rawText) with { FormatDescription = exchangeRate.FormatDescription },
        _ => TypedConstantParseResult.Accepted(rawText),
    };

    private static TypedConstantParseResult ValidateTemporal(string rawText, NodaTimeValidation validation)
    {
        var result = TemporalParser.Parse(validation.LiteralKind, rawText);
        return result.IsValid
            ? new TypedConstantParseResult(true, result.Value, result.CanonicalText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                result.Diagnostics.Select(diagnostic => new TypedConstantDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Suggestion)).ToArray());
    }

    private static TypedConstantParseResult ValidateClosedSet(string rawText, ClosedSetValidation validation)
    {
        return validation.AllowedValues.Contains(rawText)
            ? new TypedConstantParseResult(true, rawText, rawText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC002", $"Expected a value from {validation.SetName}."));
    }

    private static TypedConstantParseResult ValidateRegex(string rawText, RegexValidation validation)
    {
        return Regex.IsMatch(rawText, validation.Pattern)
            ? new TypedConstantParseResult(true, rawText, rawText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC003", $"Value does not match {validation.FormatDescription}."));
    }

    private static TypedConstantParseResult ValidateUcum(string rawText)
    {
        var result = UcumCatalog.Parse(rawText);
        return result.IsValid
            ? new TypedConstantParseResult(true, result.Unit, result.Unit?.CanonicalCode, "UCUM expression", [])
            : TypedConstantParseResult.Failed(
                "UCUM expression",
                result.Diagnostics.Select(diagnostic => new TypedConstantDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Suggestion)).ToArray());
    }
}
