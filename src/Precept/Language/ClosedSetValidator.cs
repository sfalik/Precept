namespace Precept.Language;

public static class ClosedSetValidator
{
    public static TypedConstantParseResult Validate(string rawText, ClosedSetValidation validation)
    {
        return validation.AllowedValues.Contains(rawText)
            ? new TypedConstantParseResult(true, rawText, rawText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC002", $"Expected a value from {validation.SetName}."));
    }
}
