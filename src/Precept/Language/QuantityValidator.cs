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

        if (context?.DeclaredQualifiers is { } qualifiers && !qualifiers.IsDefaultOrEmpty)
        {
            var literalDimension = UnitDimensionHelper.DeriveUnitDimensionName(unitResult.Unit!);
            foreach (var qualifier in qualifiers)
            {
                if (qualifier is DeclaredQualifierMeta.Dimension { DimensionName: var compoundUnitDimension }
                    && UnitDimensionHelper.TryGetCanonicalCompoundUnitCode(compoundUnitDimension, out var requiredCompoundUnit))
                {
                    if (!UnitDimensionHelper.TryGetCanonicalCompoundUnitCode(match.Groups[2].Value, out var actualCompoundUnit)
                        || !string.Equals(actualCompoundUnit, requiredCompoundUnit, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedConstantParseResult.Failed(
                            validation.FormatDescription,
                            new TypedConstantDiagnostic(
                                DiagnosticCode.QualifierMismatch.ToString(),
                                $"Unit '{match.Groups[2].Value}' does not match compound qualifier '{compoundUnitDimension}'"));
                    }

                    continue;
                }

                string? requiredDimension = qualifier switch
                {
                    DeclaredQualifierMeta.Dimension { DimensionName: var dimensionName } => dimensionName,
                    DeclaredQualifierMeta.Unit { DimensionName: var dimensionName } => dimensionName,
                    _ => null,
                };

                if (requiredDimension is not null
                    && !string.IsNullOrEmpty(literalDimension)
                    && !string.Equals(literalDimension, requiredDimension, System.StringComparison.OrdinalIgnoreCase))
                {
                    return TypedConstantParseResult.Failed(
                        validation.FormatDescription,
                        new TypedConstantDiagnostic(
                            DiagnosticCode.DimensionCategoryMismatch.ToString(),
                            $"Unit '{unitResult.Unit!.CanonicalCode}' has dimension '{literalDimension}' but field requires '{requiredDimension}'"));
                }
            }
        }

        var canonicalText = $"{amount.ToString(CultureInfo.InvariantCulture)} {unitResult.Unit!.CanonicalCode}";
        return new TypedConstantParseResult(true, (amount, unitResult.Unit), canonicalText, validation.FormatDescription, []);
    }
}
