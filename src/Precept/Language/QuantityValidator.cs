using System.Globalization;
using System.Text.RegularExpressions;

namespace Precept.Language;

public static class QuantityValidator
{
    private static readonly Regex Pattern =
        new(@"^([+-]?\d+(?:\.\d+)?)\s+(.+)$", RegexOptions.Compiled);

    public static TypedConstantParseResult Validate(
        string rawText,
        TypeKind targetType,
        QuantityValidation validation,
        TypedConstantContext? context = null)
    {
        var match = Pattern.Match(rawText.Trim());
        if (!match.Success)
            return TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC012", "Quantity must be '<decimal> <UCUM-unit>'."));

        var amount = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unitResult = UcumParser.Parse(match.Groups[2].Value);
        if (!unitResult.IsValid)
            return TypedConstantParseResult.Failed(
                validation.FormatDescription,
                unitResult.Diagnostics.Select(diagnostic => new TypedConstantDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Suggestion)).ToArray());

        var canonicalText = $"{amount.ToString(CultureInfo.InvariantCulture)} {unitResult.Unit!.CanonicalCode}";
        return new TypedConstantParseResult(true, (amount, unitResult.Unit), canonicalText, validation.FormatDescription, []);
    }
}
