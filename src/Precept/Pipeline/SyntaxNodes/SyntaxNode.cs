namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// Base type for all AST nodes produced by the parser.
/// </summary>
public abstract record SyntaxNode(SourceSpan Span);
