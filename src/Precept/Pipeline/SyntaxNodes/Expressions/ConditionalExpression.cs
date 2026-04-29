namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Ternary conditional: <c>if Cond then A else B</c>.</summary>
public sealed record ConditionalExpression(
    SourceSpan Span,
    Expression Condition,
    Expression WhenTrue,
    Expression WhenFalse) : Expression(Span);
