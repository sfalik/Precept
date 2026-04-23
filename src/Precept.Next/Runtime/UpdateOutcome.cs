namespace Precept.Runtime;

// ─── Update outcomes — returned by Version.Update ───────────────────

/// <summary>
/// Sealed hierarchy for field update results. Callers pattern-match;
/// any unhandled variant produces a compiler warning.
/// </summary>
public abstract record UpdateOutcome;

/// <summary>Patch applied, constraints passed, new Version committed.</summary>
public sealed record FieldWriteCommitted(Version Result) : UpdateOutcome;

/// <summary>Patch applied to working copy but constraints violated.</summary>
public sealed record UpdateConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : UpdateOutcome;

/// <summary>Field is not <c>write</c>-accessible in current state.</summary>
public sealed record AccessDenied(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;   // TODO D8/R4: field descriptor

/// <summary>Type mismatch or structurally invalid patch.</summary>
public sealed record InvalidInput(string Reason) : UpdateOutcome;
