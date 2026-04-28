using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>from State on Event [when Guard] [-> Actions] -> Outcome</c></summary>
public sealed record TransitionRowNode(
    SourceSpan Span,
    StateTargetNode FromState,
    Token EventName,
    Expression? Guard,
    ImmutableArray<Statement> Actions,
    OutcomeNode Outcome) : Declaration(Span);
