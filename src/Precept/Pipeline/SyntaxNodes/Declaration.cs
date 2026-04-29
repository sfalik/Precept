namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// Base type for all top-level and scoped declaration nodes.
/// One sealed subtype per <see cref="Language.ConstructKind"/>.
/// </summary>
public abstract record Declaration(SourceSpan Span) : SyntaxNode(Span);
