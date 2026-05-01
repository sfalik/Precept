using System.Collections.Immutable;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>List literal: <c>[elem, elem, ...]</c>.</summary>
public sealed record ListLiteralExpression(
    SourceSpan Span,
    ImmutableArray<Expression> Elements) : Expression(Span);
