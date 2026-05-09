using System.Globalization;
using System.Text.RegularExpressions;

namespace Precept.Language;

public static class PriceValidator
{
    private static readonly Regex Pattern =
        new(@"^([+-]?\d+(?:\.\d+)?)\s+([A-Za-z]{3})/(.+)$", RegexOptions.Compiled);

    public static TypedConstantParseResult Validate(string rawText, PriceValidation validation)
    {
        var match = Pattern.Match(rawText.Trim());
        if (!match.Success)
            return TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC013", "Price must be '<decimal> <ISO-4217>/<UCUM-unit>'."));

        var amount = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var currencyResult = CurrencyValidator.Validate(match.Groups[2].Value);
        if (!currencyResult.IsValid)
            return TypedConstantParseResult.Failed(validation.FormatDescription, currencyResult.Diagnostics.ToArray());

        var unitResult = UcumCatalog.Parse(match.Groups[3].Value);
        if (!unitResult.IsValid)
            return TypedConstantParseResult.Failed(
                validation.FormatDescription,
                unitResult.Diagnostics.Select(diagnostic => new TypedConstantDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Suggestion)).ToArray());

        var canonicalText = $"{amount.ToString(CultureInfo.InvariantCulture)} {currencyResult.CanonicalText}/{unitResult.Unit!.CanonicalCode}";
        return new TypedConstantParseResult(true, (amount, currencyResult.Value, unitResult.Unit), canonicalText, validation.FormatDescription, []);
    }
}
