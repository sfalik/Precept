namespace Precept.Runtime;

// ─── Inspection types — returned by InspectFire / InspectUpdate ─────

/// <summary>Row-level certainty in first-match routing.</summary>
public enum Prospect { Certain = 1, Possible = 2, Impossible = 3 }

/// <summary>Per-constraint evaluation result under partial information.</summary>
public enum ConstraintStatus { Satisfied = 1, Violated = 2, Unresolvable = 3 }

/// <summary>
/// Progressive inspection of an event. Returned by <see cref="Version.InspectFire"/>
/// and nested inside <see cref="UpdateInspection.Events"/>.
/// </summary>
public sealed record EventInspection(
    string EventName,
    Prospect OverallProspect,
    IReadOnlyList<ConstraintResult> EventEnsures,
    IReadOnlyList<RowInspection> Rows);

/// <summary>
/// Progressive inspection of a field update. Returned by <see cref="Version.InspectUpdate"/>.
/// Includes event prospects for all events in the current state, evaluated against the
/// hypothetical post-patch field state.
/// </summary>
public sealed record UpdateInspection(
    IReadOnlyList<FieldSnapshot> Fields,
    IReadOnlyList<ConstraintResult> Constraints,
    IReadOnlyList<EventInspection> Events,
    Version? HypotheticalResult);

/// <summary>
/// Per-row inspection result within an event. Carries prospect, effect,
/// resulting field values, and constraint evaluation.
/// </summary>
public sealed record RowInspection(
    Prospect Prospect,
    RowEffect Effect,
    IReadOnlyList<FieldSnapshot> ResultingFields,
    IReadOnlyList<ConstraintResult> Constraints,
    Version? HypotheticalResult);

// ─── Row effects ────────────────────────────────────────────────────

public abstract record RowEffect;
public sealed record TransitionTo(string TargetState) : RowEffect;
public sealed record NoTransition() : RowEffect;
public sealed record Rejection(string Reason) : RowEffect;

// ─── Shared inspection primitives ───────────────────────────────────

/// <summary>
/// Post-mutation field value with access mode and resolvability.
/// <see cref="IsResolved"/> is <c>false</c> when a required arg dependency
/// is missing — <see cref="Value"/> is meaningless in that case.
/// </summary>
/// <remarks>
/// TODO D8/R4: <c>FieldName</c> and <c>FieldType</c> become a typed field descriptor
/// from the executable model, carrying slot index, access modes, and constraints.
/// </remarks>
public sealed record FieldSnapshot(
    string FieldName,                           // TODO D8/R4: field descriptor
    FieldAccessMode Mode,
    string FieldType,                           // TODO D8/R4: carried by descriptor
    bool IsResolved,
    object? Value);

/// <summary>
/// Constraint evaluation result with field attribution. <see cref="FieldNames"/>
/// identifies which fields the constraint relates to (empty for entity-level constraints).
/// </summary>
/// <remarks>
/// References the <see cref="ConstraintDescriptor"/> that was evaluated —
/// callers can access kind, scope, anchor, guard, and <c>because</c> rationale
/// through the descriptor. This is Tier 3 of the constraint exposure model.
/// </remarks>
public sealed record ConstraintResult(
    ConstraintDescriptor Constraint,            // the declared constraint that was evaluated
    IReadOnlyList<string> FieldNames,           // TODO D8/R4: field descriptors — transitive expansion
    ConstraintStatus Status);
