namespace Precept.Pipeline.SyntaxNodes;

/// <summary>DU: field modifier (flag or value-bearing).</summary>
public abstract record FieldModifierNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Flag modifier: <c>optional</c>, <c>nonnegative</c>, <c>positive</c>, etc.</summary>
public sealed record FlagModifierNode(SourceSpan Span, Language.Token Keyword) : FieldModifierNode(Span);

/// <summary>Value modifier: <c>default 0</c>, <c>min 1</c>, <c>max 100</c>, etc.</summary>
public sealed record ValueModifierNode(SourceSpan Span, Language.Token Keyword, Expression Value) : FieldModifierNode(Span);
