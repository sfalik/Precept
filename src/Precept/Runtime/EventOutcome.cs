namespace Precept.Runtime;

// ─── Event outcomes — returned by Version.Fire ──────────────────────

/// <summary>
/// Sealed hierarchy for event execution results. Callers pattern-match;
/// any unhandled variant produces a compiler warning.
/// </summary>
public abstract record EventOutcome;

/// <summary>State change succeeded — new Version in target state. <see cref="Args"/> carries the submitted event args.</summary>
public sealed record Transitioned(Version Result, FiredArgs Args) : EventOutcome;

/// <summary>No-transition row or stateless event succeeded — mutations committed. <see cref="Args"/> carries the submitted event args.</summary>
public sealed record Applied(Version Result, FiredArgs Args) : EventOutcome;

/// <summary>Authored <c>reject</c> row matched — business prohibition. <see cref="Args"/> carries the submitted event args.</summary>
public sealed record Rejected(string Reason, FiredArgs Args) : EventOutcome;

/// <summary>Arg validation failure — wrong type, unknown key, missing required arg.</summary>
public sealed record InvalidArgs(string Reason) : EventOutcome;

/// <summary>
/// Post-mutation constraints violated (global rules, state ensures, event ensures).
/// The <c>Event</c> prefix disambiguates from <see cref="UpdateConstraintsFailed"/>,
/// not scope-limits to event-level constraints.
/// </summary>
public sealed record EventConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : EventOutcome;

/// <summary>All guards failed (including <c>when</c> precondition) — no row matched.</summary>
public sealed record Unmatched() : EventOutcome;

/// <summary>No transition rows or hooks for this event in current state.</summary>
public sealed record UndefinedEvent() : EventOutcome;
