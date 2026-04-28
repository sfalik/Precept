using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// A state target in scoped constructs — either a named state or the <c>any</c> quantifier.
/// </summary>
public sealed record StateTargetNode(SourceSpan Span, Token Name, bool IsQuantifier) : SyntaxNode(Span);
