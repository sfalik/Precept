using Precept.Language;

namespace Precept.Runtime;

/// <summary>
/// Runtime expression evaluator. Walks the executable model's expression trees
/// against entity data. Pure: same inputs, same result, no side effects.
/// </summary>
/// <remarks>
/// Static by design — aligned with the pipeline pattern (Lexer.Lex, Parser.Parse,
/// TypeChecker.Check). The evaluator holds no per-invocation state; the working copy
/// is allocated inside the evaluation function and never escapes the operation boundary.
///
/// Survey grounding (R1): CEL's Interpretable.Eval(Activation), XState's
/// transition(machine, state, event), and Dhall's eval(env, expr) all converge on
/// stateless/functional evaluation for pure-expression languages.
///
/// Every failure path must go through <see cref="Fail"/> with a classified
/// <see cref="FaultCode"/>. This is enforced by PRECEPT0001. Every FaultCode
/// must carry <see cref="StaticallyPreventableAttribute"/> linking it to the
/// <see cref="Pipeline.DiagnosticCode"/> the compiler should have emitted —
/// enforced by PRECEPT0002. Together these guarantee fault–diagnostic correspondence:
/// if the compiler emits no errors, the evaluator should never fault.
/// </remarks>
// CATALOG-DRIVEN IMPLEMENTATION GUIDE
//
// Derive the following from catalogs rather than hardcoding:
//
//   Today (catalog metadata available):
//     Constraint activation timing → Constraints.GetMeta(kind) + ConstraintMeta DU subtypes
//     Modifier boundary validation → ValueModifierMeta.ApplicableTo, HasValue
//     Accessor signatures          → TypeMeta.Accessors, TypeAccessor.ParameterType, RequiredTraits
//
//   Future (pending executable model design D8/R4):
//     Operation dispatch  → Operations.FindUnary / FindCandidates
//     Function dispatch   → Functions.GetMeta(kind)
//     Action dispatch     → Actions.GetMeta(kind)
//
// Do NOT use Operations.Resolve() — that API does not exist.
// Do NOT claim catalog-driven execution dispatch until delegate fields exist in catalog metadata.
//
// See: docs/language/catalog-system.md § Evaluator-catalog integration pattern
public static class Evaluator
{
    // ── Commit ──────────────────────────────────────────────────────
    // TODO Phase 3: implement Fire/Update once the executable model is designed (D8/R4)
    // TODO D8/R4: All string parameters become typed metadata descriptors from
    // the executable model. The evaluator consumes descriptors, not strings.

    /// <summary>
    /// Fires an event on the entity, applying all matching transition actions.
    /// </summary>
    /// <remarks>
    /// PHASE 3 ENFORCEMENT OBLIGATION — ACCESS MODE CONSTRAINTS:
    /// Before applying any action in a transition row, resolve the entity's current state
    /// and look up the access mode for the action's target field in that state.
    ///
    /// If <see cref="ModifierKind.Omit"/>: the field is structurally absent in the current state.
    /// Writing to it is a runtime fault — call <see cref="Fail"/> with a new
    /// <see cref="FaultCode"/> member <c>WriteToOmittedField</c> (to be added with
    /// <c>[StaticallyPreventable(DiagnosticCode.WriteToOmittedField)]</c>).
    /// This indicates either a compiler bug (the type checker should have caught this via
    /// <see cref="DiagnosticCode.WriteToOmittedField"/>) or API misuse (firing events on
    /// an entity whose definition was not type-checked).
    ///
    /// If <see cref="ModifierKind.Read"/>: the field is read-only in the current state.
    /// Writing to it is a runtime fault — call <see cref="Fail"/> with a new
    /// <see cref="FaultCode"/> member <c>WriteToReadOnlyField</c> (to be added with
    /// <c>[StaticallyPreventable(DiagnosticCode.WriteToReadOnlyField)]</c>).
    /// Same root cause as above.
    ///
    /// Access mode lookup: use the compiled <c>TypeCheckResult.AccessModes</c> indexed by
    /// (currentStateName, fieldName). Guarded access modes require evaluating the guard
    /// expression against current entity state before determining effective mode.
    ///
    /// These faults are non-recoverable (severity: Fatal). They indicate a static guarantee
    /// was violated at runtime — the transition must be aborted with no state changes committed.
    /// </remarks>
    internal static EventOutcome Fire(Precept precept, Version version, string eventName, IReadOnlyDictionary<string, object?> args)       // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    /// <summary>
    /// Applies direct field updates to the entity outside of event-driven transitions.
    /// </summary>
    /// <remarks>
    /// PHASE 3 ENFORCEMENT OBLIGATION — ACCESS MODE CONSTRAINTS:
    /// Before applying each field update, resolve the entity's current state and look up
    /// the access mode for the target field in that state.
    ///
    /// If <see cref="ModifierKind.Omit"/>: fault via <see cref="Fail"/> with
    /// <c>FaultCode.WriteToOmittedField</c> (to be added in Phase 3).
    /// If <see cref="ModifierKind.Read"/>: fault via <see cref="Fail"/> with
    /// <c>FaultCode.WriteToReadOnlyField</c> (to be added in Phase 3).
    ///
    /// Same semantics as <see cref="Fire"/>: these faults are non-recoverable and indicate
    /// a compiler bug or API misuse. See Fire remarks for full rationale.
    /// </remarks>
    internal static UpdateOutcome Update(Precept precept, Version version, IReadOnlyDictionary<string, object?> fields)                    // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    // ── Inspect ─────────────────────────────────────────────────────
    // TODO Phase 3: implement InspectFire/InspectUpdate once the executable model is designed (D8/R4)

    /// <summary>
    /// Dry-run inspection of what firing an event would produce, without committing changes.
    /// </summary>
    /// <remarks>
    /// PHASE 3 ENFORCEMENT OBLIGATION — ACCESS MODE CONSTRAINTS:
    /// Inspect methods must enforce the same access mode constraints as their commit
    /// counterparts (<see cref="Fire"/>). If a transition row would write to an omitted
    /// or read-only field, the inspection result must report the fault — not silently
    /// skip the action. The inspection should surface the <c>FaultCode.WriteToOmittedField</c>
    /// or <c>FaultCode.WriteToReadOnlyField</c> fault (both to be added in Phase 3)
    /// in the inspection result rather than throwing, so callers can observe what would fail.
    ///
    /// This ensures inspect and commit paths have identical enforcement semantics —
    /// an inspection that reports success must mean the corresponding commit will not
    /// fault on access mode violations.
    /// </remarks>
    internal static EventInspection InspectFire(Precept precept, Version version, string eventName, IReadOnlyDictionary<string, object?>? args)     // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    /// <summary>
    /// Dry-run inspection of what a direct field update would produce, without committing changes.
    /// </summary>
    /// <remarks>
    /// PHASE 3 ENFORCEMENT OBLIGATION — ACCESS MODE CONSTRAINTS:
    /// Same obligation as <see cref="InspectFire"/>: access mode violations must appear
    /// in the inspection result. See InspectFire remarks for full rationale.
    /// </remarks>
    internal static UpdateInspection InspectUpdate(Precept precept, Version version, IReadOnlyDictionary<string, object?>? fields)                  // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    // ── Restore ─────────────────────────────────────────────────────
    // TODO Phase 3: implement Restore once the executable model is designed (D8/R4)

    /// <summary>
    /// Restores an entity to a known state from persisted data.
    /// </summary>
    /// <remarks>
    /// PHASE 3 DESIGN NOTE — ACCESS MODE CONSTRAINTS:
    /// Restore reconstructs entity state from persisted field values — it does not apply
    /// transition actions. Access mode enforcement does NOT apply to Restore: the persisted
    /// data represents a previously valid state snapshot, and the restore path must accept
    /// it without re-validating action-level constraints. The field values were validated
    /// at the time they were written (by Fire or Update).
    ///
    /// However, Restore MUST validate structural consistency: if the restored state declares
    /// <see cref="ModifierKind.Omit"/> for a field, the persisted data should not contain a
    /// value for that field. If it does, this indicates data corruption or schema drift —
    /// the implementation should decide whether to fault or silently discard the value
    /// (design decision deferred to Phase 3).
    /// </remarks>
    internal static RestoreOutcome Restore(Precept precept, string? state, IReadOnlyDictionary<string, object?> fields)                            // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    // ── Fault production ────────────────────────────────────────────

    internal static Fault Fail(FaultCode code, params object?[] args)
        => Faults.Create(code, args);
}
