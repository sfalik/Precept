using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Dot access: <c>event.field</c>, <c>claim.amount</c>.</summary>
public sealed record MemberAccessExpression(
    SourceSpan Span,
    Expression Object,
    Token Member) : Expression(Span);
