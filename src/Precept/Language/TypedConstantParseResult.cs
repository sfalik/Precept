namespace Precept.Language;

public readonly record struct TypedConstantParseResult(
    bool IsValid,
    object? Value,
    string? CanonicalText,
    string FormatDescription,
    IReadOnlyList<TypedConstantDiagnostic> Diagnostics)
{
    public static TypedConstantParseResult Accepted(string rawText) =>
        new(true, rawText, rawText, string.Empty, []);

    public static TypedConstantParseResult Failed(string formatDescription, params TypedConstantDiagnostic[] diagnostics) =>
        new(false, null, null, formatDescription, diagnostics);
}

public sealed record TypedConstantDiagnostic(string Code, string Message, string? Suggestion = null);

public sealed record TypedConstantContext(TypeKind? PeerType = null, OperatorKind? Operator = null);
