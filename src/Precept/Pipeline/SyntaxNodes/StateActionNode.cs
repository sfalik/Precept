using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>(to|from) State [when Guard] -> Actions</c></summary>
public sealed record StateActionNode(
    SourceSpan Span,
    Token Preposition,
    StateTargetNode State,
    Expression? Guard,
    ImmutableArray<Statement> Actions) : Declaration(Span);
