namespace Precept.Language;

/// <summary>
/// A single typed overload for a built-in function.
/// Parameters are <see cref="ParameterMeta"/> instances (shared with the Operations catalog).
/// </summary>
public sealed record FunctionOverload(
    IReadOnlyList<ParameterMeta> Parameters,
    TypeKind ReturnType,
    QualifierMatch? Match = null);

/// <summary>
/// Metadata for a built-in function. Each <see cref="FunctionKind"/> maps to exactly one
/// <see cref="FunctionMeta"/> with one or more <see cref="FunctionOverload"/> entries.
/// </summary>
public sealed record FunctionMeta(
    FunctionKind Kind,
    string Name,
    string Description,
    IReadOnlyList<FunctionOverload> Overloads);
