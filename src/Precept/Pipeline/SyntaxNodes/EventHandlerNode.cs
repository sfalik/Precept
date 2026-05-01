using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>on Event -> Actions (ensure PostConditionGuard)?</c> (stateless precepts).</summary>
public sealed record EventHandlerNode(
    SourceSpan Span,
    Token EventName,
    ImmutableArray<Statement> Actions,
    Expression? PostConditionGuard) : Declaration(Span);
