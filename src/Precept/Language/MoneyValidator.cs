using System.Globalization;
using System.Text.RegularExpressions;

namespace Precept.Language;

public static class MoneyValidator
{
    private static readonly Regex Pattern =
        new(@"^([+-]?\d+(?:\.\d+)?)\s+([A-Za-z]{3})$", RegexOptions.Compiled);

    public static TypedConstantParseResult Validate(string rawText, MoneyValidation validation)
    {
        var match = Pattern.Match(rawText.Trim());
        if (!match.Success)
            return TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC011", "Money must be '<decimal> <ISO-4217>'."));

        var amount = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var currencyResult = CurrencyValidator.Validate(match.Groups[2].Value);
        if (!currencyResult.IsValid)
            return TypedConstantParseResult.Failed(validation.FormatDescription, currencyResult.Diagnostics.ToArray());

        var canonicalText = $"{amount.ToString(CultureInfo.InvariantCulture)} {currencyResult.CanonicalText}";
        return new TypedConstantParseResult(true, (amount, currencyResult.Value), canonicalText, validation.FormatDescription, []);
    }
}
