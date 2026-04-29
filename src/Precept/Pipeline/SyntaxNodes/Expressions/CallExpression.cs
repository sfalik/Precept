using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Function call: <c>min(a, b)</c>, <c>now()</c>.</summary>
public sealed record CallExpression(
    SourceSpan Span,
    Token Name,
    ImmutableArray<Expression> Arguments) : Expression(Span);
