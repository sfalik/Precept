using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>A literal value: integer, decimal, string, boolean.</summary>
public sealed record LiteralExpression(SourceSpan Span, Token Value) : Expression(Span);
