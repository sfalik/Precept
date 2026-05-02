using System.Collections.Immutable;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>DU: type reference in field/argument declarations.</summary>
public abstract record TypeRefNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary><c>as string</c>, <c>as decimal maxplaces 2</c></summary>
public sealed record ScalarTypeRefNode(
    SourceSpan Span,
    Language.Token TypeName,
    ImmutableArray<TypeQualifierNode> Qualifiers,
    bool CaseInsensitive = false) : TypeRefNode(Span);

/// <summary><c>as set of string</c>, <c>as queue of integer</c>, <c>as set of ~string</c></summary>
public sealed record CollectionTypeRefNode(
    SourceSpan Span,
    Language.Token CollectionKind,
    Language.Token ElementType,
    ImmutableArray<TypeQualifierNode> Qualifiers,
    bool CaseInsensitive = false) : TypeRefNode(Span);

/// <summary><c>as choice of string("A", "B", "C")</c></summary>
public sealed record ChoiceTypeRefNode(
    SourceSpan Span,
    Language.Token? ElementType,
    ImmutableArray<Expression> Options) : TypeRefNode(Span);

/// <summary>Type qualifier: <c>in USD</c>, <c>of mass</c></summary>
public sealed record TypeQualifierNode(
    SourceSpan Span,
    Language.Token Keyword,
    Expression Value) : SyntaxNode(Span);

public enum SortDirection { Ascending = 1, Descending = 2 }

public sealed record LogByTypeRefNode(
    SourceSpan Span,
    Language.Token ElementType,
    Language.Token OrderingKeyType,
    bool CaseInsensitive,
    ImmutableArray<TypeQualifierNode> Qualifiers
) : TypeRefNode(Span);

public sealed record QueueByTypeRefNode(
    SourceSpan Span,
    Language.Token ElementType,
    Language.Token OrderingKeyType,
    SortDirection SortDirection,
    bool CaseInsensitive,
    ImmutableArray<TypeQualifierNode> Qualifiers
) : TypeRefNode(Span);

public sealed record LookupTypeRefNode(
    SourceSpan Span,
    Language.Token KeyType,
    Language.Token ValueType,
    bool CaseInsensitive,
    ImmutableArray<TypeQualifierNode> Qualifiers
) : TypeRefNode(Span);
