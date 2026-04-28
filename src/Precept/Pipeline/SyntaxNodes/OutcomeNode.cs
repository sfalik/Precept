namespace Precept.Pipeline.SyntaxNodes;

/// <summary>DU: transition outcome.</summary>
public abstract record OutcomeNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary><c>-> transition TargetState</c></summary>
public sealed record TransitionOutcomeNode(SourceSpan Span, Language.Token TargetState) : OutcomeNode(Span);

/// <summary><c>-> no transition</c></summary>
public sealed record NoTransitionOutcomeNode(SourceSpan Span) : OutcomeNode(Span);

/// <summary><c>-> reject "reason"</c></summary>
public sealed record RejectOutcomeNode(SourceSpan Span, Expression Message) : OutcomeNode(Span);
