using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Typed field declaration produced by the type checker.
/// Stub — carries minimal identity for SemanticIndex references.
/// </summary>
public sealed record class TypedField(
    string Name,
    TypeMeta Type,
    SourceSpan Span);

/// <summary>
/// Typed state declaration produced by the type checker.
/// Stub — carries minimal identity for SemanticIndex references.
/// </summary>
public sealed record class TypedState(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,
    SourceSpan Span);

/// <summary>
/// Typed event declaration produced by the type checker.
/// Stub — carries minimal identity for SemanticIndex references.
/// </summary>
public sealed record class TypedEvent(
    string Name,
    SourceSpan Span);
