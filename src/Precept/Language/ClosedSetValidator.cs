namespace Precept.Language;

public static class ClosedSetValidator
{
    /// <summary>
    /// Validates closed-set membership. Context-free by contract: the caller selects which
    /// <see cref="ClosedSetValidation"/> to validate against (e.g., a partition-specific set);
    /// this validator only tests membership. <paramref name="failureCode"/> overrides the
    /// generic <c>TC002</c> failure code so the caller can route a domain-specific diagnostic
    /// (e.g., a cross-partition dimension value to <c>InvalidDimensionString</c>).
    /// </summary>
    public static TypedConstantParseResult Validate(
        string rawText,
        ClosedSetValidation validation,
        string failureCode = "TC002")
    {
        return validation.AllowedValues.Contains(rawText)
            ? new TypedConstantParseResult(true, rawText, rawText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic(failureCode, $"Expected a value from {validation.SetName}."));
    }
}
