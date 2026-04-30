using System.Collections.Immutable;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>DU: type reference in field/argument declarations.</summary>
public abstract record TypeRefNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary><c>as string</c>, <c>as decimal maxplaces 2</c></summary>
public sealed record ScalarTypeRefNode(
    SourceSpan Span,
    Language.Token TypeName,
    TypeQualifierNode? Qualifier) : TypeRefNode(Span);

/// <summary><c>as set of string</c>, <c>as queue of integer</c></summary>
public sealed record CollectionTypeRefNode(
    SourceSpan Span,
    Language.Token CollectionKind,
    Language.Token ElementType,
    TypeQualifierNode? Qualifier) : TypeRefNode(Span);

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
