using System.Globalization;
using System.Text.RegularExpressions;

namespace Precept.Language;

public static class ExchangeRateValidator
{
    private static readonly Regex Pattern =
        new(@"^([+-]?\d+(?:\.\d+)?)\s+([A-Za-z]{3})/([A-Za-z]{3})$", RegexOptions.Compiled);

    public static TypedConstantParseResult Validate(string rawText, ExchangeRateValidation validation)
    {
        var match = Pattern.Match(rawText.Trim());
        if (!match.Success)
            return TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC014", "Exchange rate must be '<decimal> <ISO-4217>/<ISO-4217>'."));

        var rate = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var fromResult = CurrencyValidator.Validate(match.Groups[2].Value);
        if (!fromResult.IsValid)
            return TypedConstantParseResult.Failed(validation.FormatDescription, fromResult.Diagnostics.ToArray());

        var toResult = CurrencyValidator.Validate(match.Groups[3].Value);
        if (!toResult.IsValid)
            return TypedConstantParseResult.Failed(validation.FormatDescription, toResult.Diagnostics.ToArray());

        var canonicalText = $"{rate.ToString(CultureInfo.InvariantCulture)} {fromResult.CanonicalText}/{toResult.CanonicalText}";
        return new TypedConstantParseResult(true, (rate, fromResult.Value, toResult.Value), canonicalText, validation.FormatDescription, []);
    }
}
