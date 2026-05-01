using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Null-check postfix: <c>field is set</c>. Returns true when the optional field has a value.</summary>
public sealed record IsSetExpression(
    SourceSpan Span,
    Expression Operand) : Expression(Span);
