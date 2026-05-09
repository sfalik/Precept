namespace Precept.Language;

public static class UcumValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        TypeKind targetType,
        UcumValidation validation,
        TypedConstantContext? context = null)
    {
        var result = UcumCatalog.Parse(rawText);
        return result.IsValid
            ? new TypedConstantParseResult(true, result.Unit, result.Unit?.CanonicalCode, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                result.Diagnostics.Select(diagnostic => new TypedConstantDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Suggestion)).ToArray());
    }
}
