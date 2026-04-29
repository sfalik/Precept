using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>precept Name</c> — file-level header.</summary>
public sealed record PreceptHeaderNode(SourceSpan Span, Token Name) : Declaration(Span);
