using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

// ── ParsedTypeReference DU ─────────────────────────────────────────────────────
//
// Discriminated union capturing the full structural type information from source.
// The parser produces these; the type checker resolves them into typed forms.
// Derived from Types catalog metadata (TypeCategory, TypeMeta properties).

/// <summary>
/// Abstract base of the ParsedTypeReference discriminated union.
/// Captures the full type structure as written in source, preserving collection
/// wrappers, choice domains, and CI qualifiers that would be lost with a bare TypeMeta.
/// </summary>
public abstract record ParsedTypeReference(SourceSpan Span);

/// <summary>
/// A simple (non-parameterized) type reference: string, integer, money, etc.
/// Covers all scalar, temporal, and business-domain types that don't take
/// a structural type parameter.
/// </summary>
public sealed record SimpleTypeReference(TypeMeta Type, SourceSpan Span)
    : ParsedTypeReference(Span);

/// <summary>
/// A collection type with an inner element type: list of money, set of string, etc.
/// The CollectionType is the outer type (list, set, queue, etc.); ElementType is the inner.
/// For keyed collections (lookup, log by, queue by), the KeyType captures the ordering/key type.
/// </summary>
public sealed record CollectionTypeReference(
    TypeMeta CollectionType,
    ParsedTypeReference ElementType,
    ParsedTypeReference? KeyType,
    SourceSpan Span)
    : ParsedTypeReference(Span);

/// <summary>
/// A choice type with an explicit domain: choice of string("Draft", "Submitted").
/// ElementType captures the element type (string, integer, boolean, etc.).
/// Domain contains the literal values as they appear in source.
/// </summary>
public sealed record ChoiceTypeReference(
    TypeMeta Type,
    TypeMeta? ElementType,
    ImmutableArray<string> Domain,
    SourceSpan Span)
    : ParsedTypeReference(Span);

/// <summary>
/// A case-insensitive type marker: ~string.
/// The InnerType is the underlying type with CI semantics applied.
/// </summary>
public sealed record CITypeReference(TypeMeta Type, SourceSpan Span)
    : ParsedTypeReference(Span);

/// <summary>
/// Missing type reference sentinel — used when type parsing fails or is absent.
/// </summary>
public sealed record MissingTypeReference(SourceSpan Span)
    : ParsedTypeReference(Span);
