using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>on Event ensure Expr because Msg</c></summary>
public sealed record EventEnsureNode(
    SourceSpan Span,
    Token EventName,
    Expression? Guard,
    Expression Condition,
    Expression Message) : Declaration(Span);
