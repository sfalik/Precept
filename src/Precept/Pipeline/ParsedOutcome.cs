namespace Precept.Pipeline;

/// <summary>
/// Discriminated union for the resolved outcome form in a transition row.
/// Outcomes are a closed 3-member vocabulary resolved at parse time —
/// they are NOT expressions and do not participate in expression resolution.
/// </summary>
public abstract record ParsedOutcome(SourceSpan Span);

/// <summary>Transition to a named target state: -> transition StateName</summary>
public sealed record TransitionOutcome(string StateName, SourceSpan Span)
    : ParsedOutcome(Span)
{
    public SourceSpan StateSpan { get; init; } = Span;
}

/// <summary>Explicit no-transition: -> no transition</summary>
public sealed record NoTransitionOutcome(SourceSpan Span)
    : ParsedOutcome(Span);

/// <summary>Rejection with reason: -> reject "reason"</summary>
public sealed record RejectOutcome(string Reason, SourceSpan Span)
    : ParsedOutcome(Span);

/// <summary>
/// Malformed or missing outcome (error recovery sentinel).
/// Used when the outcome arrow is present but the form is unrecognizable,
/// or when the outcome slot is structurally required but absent from source.
/// </summary>
public sealed record MalformedOutcome(SourceSpan Span)
    : ParsedOutcome(Span);
