namespace Precept.Language;

public readonly record struct TemporalParseResult(
    bool IsValid,
    object? Value,
    string? CanonicalText,
    IReadOnlyList<TemporalDiagnostic> Diagnostics)
{
    public static TemporalParseResult Success(object value, string canonicalText) =>
        new(true, value, canonicalText, []);

    public static TemporalParseResult Failure(params TemporalDiagnostic[] diagnostics) =>
        new(false, null, null, diagnostics);
}
