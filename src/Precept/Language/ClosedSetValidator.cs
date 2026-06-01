namespace Precept.Language;

public static class ClosedSetValidator
{
    /// <summary>
    /// Validates closed-set membership. Context-free by contract: the caller selects which
    /// <see cref="ClosedSetValidation"/> to validate against (e.g., a partition-specific set);
    /// this validator only tests membership. <paramref name="failureCode"/> carries a
    /// domain-specific diagnostic the caller owns (e.g., a cross-partition dimension value routes
    /// to <see cref="DiagnosticCode.InvalidDimensionString"/>); <c>null</c> falls to the family's
    /// catalog format/semantic mapping.
    /// </summary>
    public static TypedConstantParseResult Validate(
        string rawText,
        ClosedSetValidation validation,
        DiagnosticCode? failureCode = null)
    {
        return validation.AllowedValues.Contains(rawText)
            ? new TypedConstantParseResult(true, rawText, rawText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic($"Expected a value from {validation.SetName}.", SpecificCode: failureCode));
    }
}
