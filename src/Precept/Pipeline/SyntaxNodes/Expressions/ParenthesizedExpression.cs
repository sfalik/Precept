namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Parenthesized expression: <c>(a + b)</c>.</summary>
public sealed record ParenthesizedExpression(
    SourceSpan Span,
    Expression Inner) : Expression(Span);
