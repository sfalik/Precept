using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>A single-quoted typed constant literal: <c>'USD'</c>, <c>'2026-04-15'</c>.</summary>
public sealed record TypedConstantExpression(
    SourceSpan Span,
    Token Value) : Expression(Span);
