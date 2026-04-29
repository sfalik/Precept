using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>A bare identifier reference: <c>amount</c>, <c>reviewer</c>.</summary>
public sealed record IdentifierExpression(SourceSpan Span, Token Name) : Expression(Span);
