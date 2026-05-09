using NodaTime.Text;

namespace Precept.Language;

public static class TemporalValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        TypeKind targetType,
        NodaTimeValidation validation,
        TypedConstantContext? context = null)
    {
        var temporalResult = TemporalParser.Parse(validation.LiteralKind, rawText);
        if (temporalResult.IsValid)
            return Success(validation.FormatDescription, temporalResult.Value, temporalResult.CanonicalText);

        if (validation.LiteralKind == TemporalLiteralKind.TemporalQuantity &&
            (targetType == TypeKind.Period || validation.NodaTimePattern == "NormalizingIso"))
        {
            var periodResult = PeriodPattern.NormalizingIso.Parse(rawText);
            if (periodResult.Success)
                return Success(validation.FormatDescription, periodResult.Value, rawText);
        }

        return TypedConstantParseResult.Failed(
            validation.FormatDescription,
            temporalResult.Diagnostics.Select(diagnostic => new TypedConstantDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Suggestion)).ToArray());
    }

    private static TypedConstantParseResult Success(string formatDescription, object? value, string? canonicalText) =>
        new(true, value, canonicalText, formatDescription, []);
}
