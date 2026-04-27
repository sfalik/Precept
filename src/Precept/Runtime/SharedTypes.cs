using Precept.Language;

namespace Precept.Runtime;

// ─── Shared types used across commit and inspection surfaces ────────
//
// TODO D8/R4: All string identifiers (field names, event names, arg names) are
// provisional placeholders. The runtime API will use typed metadata descriptors
// from the executable model. Each descriptor carries compiled metadata (name,
// type, slot index, access mode, constraints, arg-dependency sets). Callers
// obtain descriptors from the model and pass them back to operations.

/// <summary>Field access mode in a given state: read-only or read-write.</summary>
public enum FieldAccessMode { Read, Write }

/// <summary>
/// Describes a non-omitted field in the current state: name, access mode,
/// declared type, and current value.
/// </summary>
/// <remarks>
/// TODO D8/R4: This record becomes (or wraps) a typed field descriptor from
/// the executable model. The descriptor is the canonical field identity.
/// </remarks>
public sealed record FieldAccessInfo(
    string FieldName,                           // TODO D8/R4: field descriptor
    FieldAccessMode Mode,                       // TODO D8/R4: carried by descriptor per state
    string FieldType,                           // TODO D8/R4: carried by descriptor
    object? CurrentValue);

/// <summary>Event argument descriptor: name and declared type.</summary>
/// <remarks>
/// TODO D8/R4: Becomes a typed arg descriptor from the executable model,
/// carrying validation constraints and dependency sets.
/// </remarks>
public sealed record ArgInfo(
    string Name,                                // TODO D8/R4: arg descriptor
    string Type);                               // TODO D8/R4: carried by descriptor

// ─── Constraint descriptors ─────────────────────────────────────────
//
// Three-tier constraint exposure:
//   Tier 1: Precept.Constraints   — full catalog of all declared constraints
//   Tier 2: Version.ApplicableConstraints — constraints active for current state
//            (global rules + in/from ensures + event ensures for available events)
//            NOTE: `to <State> ensure` constraints are NOT in Tier 2 — they are
//            transitional and only surface in Tier 3 during inspect/fire when
//            the target state is known from row matching.
//   Tier 3: ConstraintResult / ConstraintViolation — evaluation outcomes referencing descriptors

/// <summary>
/// Metadata descriptor for a declared constraint. Created during
/// <see cref="Precept.From"/> and owned by the executable model.
/// This is the canonical identity for a constraint — evaluation results
/// and violations reference it, not string descriptions.
/// </summary>
/// <remarks>
/// TODO D8/R4: Internal representation will carry the compiled expression
/// tree, guard expression, slot indices, and arg-dependency sets. The
/// public surface exposes the semantic metadata callers need.
///
/// TODO G1/G9: ReferencedFields and ConstraintViolation.FieldNames are
/// provisional flat string lists. The prototype carries a rich target
/// hierarchy (FieldTarget, EventArgTarget, EventTarget, StateTarget,
/// DefinitionTarget) that distinguishes WHERE a violation lands. The
/// clean-room model needs an equivalent typed target model — but the
/// metadata descriptors (D8/R4) must be defined first, since targets
/// should reference descriptors, not strings.
/// </remarks>
public sealed record ConstraintDescriptor(
    ConstraintKind Kind,
    string? ScopeTarget,                        // state name (for state ensures) or event name (for event ensures); null for invariants
    string ExpressionText,                      // the source expression text — enables side-by-side display with the precept source
    string Because,                             // the mandatory rationale — the `because` clause text
    IReadOnlyList<string> ReferencedFields,     // TODO D8/R4: field descriptors — semantic subjects
    bool HasGuard,                              // whether a `when` guard is present
    int SourceLine);                            // 1-based line number in the .precept source — enables go-to-definition

/// <summary>
/// Constraint violation produced by the collect-all constraint evaluator.
/// References the <see cref="ConstraintDescriptor"/> that was violated.
/// Stub — full shape pending R6 (constraint evaluation attribution model).
/// </summary>
/// <remarks>
/// TODO G1/G9: FieldNames is a provisional flat list. The prototype uses
/// a typed ConstraintTarget hierarchy (field, event-arg, event, state,
/// definition) for rich UI attribution. When metadata descriptors (D8/R4)
/// are defined, this should become a typed target list referencing those
/// descriptors — not just field name strings.
/// </remarks>
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames);           // TODO D8/R4 + G1/G9: typed target list — transitive expansion of computed field refs
