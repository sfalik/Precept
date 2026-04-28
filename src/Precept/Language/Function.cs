namespace Precept.Language;

/// <summary>
/// Grouping of functions by semantic domain. Used by completions and MCP vocabulary
/// to present functions in contextually organized groups.
/// </summary>
public enum FunctionCategory
{
    Numeric,
    String,
    Temporal,
}

/// <summary>
/// A single typed overload for a built-in function.
/// Parameters are <see cref="ParameterMeta"/> instances (shared with the Operations catalog).
/// </summary>
public sealed record FunctionOverload(
    IReadOnlyList<ParameterMeta> Parameters,
    TypeKind ReturnType,
    QualifierMatch? Match = null,
    ProofRequirement[]? ProofRequirements = null)
{
    /// <summary>Proof obligations the type checker must verify at call sites.</summary>
    public ProofRequirement[] ProofRequirements { get; } = ProofRequirements ?? [];
}

/// <summary>
/// Metadata for a built-in function. Each <see cref="FunctionKind"/> maps to exactly one
/// <see cref="FunctionMeta"/> with one or more <see cref="FunctionOverload"/> entries.
/// </summary>
public sealed record FunctionMeta(
    FunctionKind Kind,
    string Name,
    string Description,
    IReadOnlyList<FunctionOverload> Overloads,
    FunctionCategory Category,
    string? UsageExample = null,
    string? SnippetTemplate = null,
    string? HoverDescription = null);
