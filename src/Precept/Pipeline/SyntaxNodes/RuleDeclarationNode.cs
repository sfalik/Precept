namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>rule Expr [when Guard] because Msg</c></summary>
public sealed record RuleDeclarationNode(
    SourceSpan Span,
    Expression Condition,
    Expression? Guard,
    Expression Message) : Declaration(Span);
