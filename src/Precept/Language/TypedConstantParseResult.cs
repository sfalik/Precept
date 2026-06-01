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

/// <summary>
/// A typed-constant validation failure. <see cref="SpecificCode"/> is the domain-specific
/// diagnostic the validator owns (e.g. a quantity's dimension/qualifier mismatch); it is
/// <c>null</c> when the validator has no specific code, in which case the type checker maps the
/// failure through the family's catalog <c>FormatErrorCode</c>/<c>SemanticErrorCode</c> by
/// <see cref="ErrorKind"/>.
/// </summary>
public sealed record TypedConstantDiagnostic(
    string Message,
    DiagnosticCode? SpecificCode = null,
    string? Suggestion = null,
    TypedConstantErrorKind ErrorKind = TypedConstantErrorKind.Format);

public sealed record TypedConstantContext(
    TypeKind? PeerType = null,
    OperatorKind? Operator = null,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers = null,
    /// <summary>
    /// The dimension registry a <c>dimension</c> typed constant validates against, when the
    /// constant is compared against a <c>.dimension</c> accessor that declares one. Lets a
    /// <c>period.dimension</c> comparison validate the literal against the temporal partition
    /// (<c>date</c>/<c>time</c>/<c>datetime</c>) while a quantity/uom/price comparison validates
    /// against the UCUM partition. Null when the constant is not a partitioned dimension.
    /// </summary>
    DimensionPartition? DimensionPartition = null);
