namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// Base type for all expression nodes (literals, binary ops, field refs, etc.).
/// </summary>
public abstract record Expression(SourceSpan Span) : SyntaxNode(Span);
