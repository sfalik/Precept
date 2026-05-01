using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Null-check postfix: <c>field is not set</c>. Returns true when the optional field has no value.</summary>
public sealed record IsNotSetExpression(
    SourceSpan Span,
    Expression Operand) : Expression(Span);
