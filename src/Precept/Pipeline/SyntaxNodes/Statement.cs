namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// Base type for action statements within event handlers and transitions.
/// </summary>
public abstract record Statement(SourceSpan Span) : SyntaxNode(Span);
