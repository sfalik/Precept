using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Unary prefix operation: <c>not x</c>, <c>-amount</c>.</summary>
public sealed record UnaryExpression(
    SourceSpan Span,
    Token Operator,
    Expression Operand) : Expression(Span);
