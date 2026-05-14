using System.Collections.Immutable;

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

/// <summary>
/// Classifies a typed-constant validation failure as either a format mismatch
/// (input doesn't match the expected structural pattern) or a semantic violation
/// (input matches the pattern but contains invalid domain values).
/// </summary>
public enum TypedConstantErrorKind
{
    /// <summary>The input does not match the expected structural format.</summary>
    Format = 1,
    /// <summary>The input matches the format but contains invalid domain values (e.g., Feb 30).</summary>
    Semantic = 2,
}

public sealed record TypedConstantDiagnostic(
    string Code,
    string Message,
    string? Suggestion = null,
    TypedConstantErrorKind ErrorKind = TypedConstantErrorKind.Format);

public sealed record TypedConstantContext(
    TypeKind? PeerType = null,
    OperatorKind? Operator = null,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers = null);
