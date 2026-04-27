using Precept.Language;

namespace Precept.Runtime;

/// <summary>
/// Field descriptor — the runtime face of a declared <c>field</c>.
/// Produced by the Precept Builder; carried by the executable model.
/// Every runtime surface that references a field does so through this descriptor.
/// </summary>
public sealed record FieldDescriptor(
    string Name,
    TypeKind Type,
    int SlotIndex,
    IReadOnlyList<ModifierKind> Modifiers,
    string? DefaultExpression,
    bool IsComputed,
    int SourceLine);

/// <summary>
/// State descriptor — the runtime face of a declared <c>state</c>.
/// Produced by the Precept Builder; carried by the executable model.
/// </summary>
public sealed record StateDescriptor(
    string Name,
    IReadOnlyList<ModifierKind> Modifiers,
    int SourceLine);

/// <summary>
/// Arg descriptor — the runtime face of a declared event argument.
/// Carried by <see cref="EventDescriptor.Args"/>.
/// </summary>
public sealed record ArgDescriptor(
    string Name,
    TypeKind Type,
    bool IsOptional,
    string? DefaultExpression,
    int SourceLine);

/// <summary>
/// Event descriptor — the runtime face of a declared <c>event</c>.
/// Produced by the Precept Builder; carried by the executable model.
/// </summary>
public sealed record EventDescriptor(
    string Name,
    IReadOnlyList<ModifierKind> Modifiers,
    IReadOnlyList<ArgDescriptor> Args,
    int SourceLine);

/// <summary>
/// Fault-site descriptor — runtime residue of a proof obligation that the compiler
/// could not statically discharge. Planted by the Precept Builder at defense-in-depth
/// backstop sites in the evaluator. Should be unreachable in correct programs.
/// </summary>
/// <param name="FaultCode">The runtime fault classification.</param>
/// <param name="PreventedBy">
/// The compiler-owned diagnostic that should have blocked this site.
/// Derived from the <see cref="StaticallyPreventableAttribute"/> on
/// <paramref name="FaultCode"/> at Precept Builder time.
/// </param>
public sealed record FaultSiteDescriptor(
    FaultCode FaultCode,
    DiagnosticCode PreventedBy,
    int SourceLine);
