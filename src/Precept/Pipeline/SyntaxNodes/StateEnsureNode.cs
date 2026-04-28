using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>(in|to|from) State ensure Expr because Msg</c></summary>
public sealed record StateEnsureNode(
    SourceSpan Span,
    Token Preposition,
    StateTargetNode State,
    Expression? Guard,
    Expression Condition,
    Expression Message) : Declaration(Span);
