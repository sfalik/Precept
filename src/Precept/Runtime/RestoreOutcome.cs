namespace Precept.Runtime;

// ─── Restore outcomes — returned by Precept.Restore ─────────────────

/// <summary>
/// Sealed hierarchy for entity restoration results. Callers pattern-match;
/// any unhandled variant produces a compiler warning.
/// </summary>
public abstract record RestoreOutcome;

/// <summary>Data is valid — constraints passed, Version ready for operations.</summary>
public sealed record Restored(Version Result) : RestoreOutcome;

/// <summary>
/// Post-recomputation constraints violated (global rules, state ensures).
/// The persisted data does not satisfy the current definition's constraints.
/// </summary>
public sealed record RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : RestoreOutcome;

/// <summary>
/// Structural mismatch between persisted data and current definition:
/// undefined state, unknown fields, missing required fields, type mismatch.
/// </summary>
public sealed record RestoreInvalidInput(string Reason) : RestoreOutcome;
