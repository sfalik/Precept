using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Binary operation: <c>a + b</c>, <c>x == y</c>, <c>p and q</c>.</summary>
public sealed record BinaryExpression(
    SourceSpan Span,
    Expression Left,
    Token Operator,
    Expression Right) : Expression(Span);
