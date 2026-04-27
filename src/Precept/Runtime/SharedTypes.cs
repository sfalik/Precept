using Precept.Language;

namespace Precept.Runtime;

/// <summary>Field access mode in a given state: read-only or read-write.</summary>
public enum FieldAccessMode { Read, Write }

/// <summary>
/// Describes a non-omitted field in the current state: descriptor, access mode,
/// and current value.
/// </summary>
public sealed record FieldAccessInfo(
    FieldDescriptor Field,
    FieldAccessMode Mode,
    object? CurrentValue);

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
/// TODO G1/G9: ReferencedFields and ConstraintViolation.FieldNames are
/// provisional flat string lists. The prototype carries a rich target
/// hierarchy (FieldTarget, EventArgTarget, EventTarget, StateTarget,
/// DefinitionTarget) that distinguishes WHERE a violation lands. The
/// clean-room model needs an equivalent typed target model referencing
/// descriptor identity.
/// </remarks>
public sealed record ConstraintDescriptor(
    ConstraintKind Kind,
    string? ScopeTarget,                        // state name (for state ensures) or event name (for event ensures); null for invariants
    string ExpressionText,                      // the source expression text — enables side-by-side display with the precept source
    string Because,                             // the mandatory rationale — the `because` clause text
    IReadOnlyList<string> ReferencedFields,     // TODO G1/G9: should reference FieldDescriptor — currently provisional flat list
    bool HasGuard,                              // whether a `when` guard is present
    int SourceLine);                            // 1-based line number in the .precept source — enables go-to-definition

/// <summary>
/// Constraint violation produced by the collect-all constraint evaluator.
/// References the <see cref="ConstraintDescriptor"/> that was violated.
/// </summary>
/// <remarks>
/// TODO G1/G9: FieldNames is a provisional flat list. The prototype uses
/// a typed ConstraintTarget hierarchy (field, event-arg, event, state,
/// definition) for rich UI attribution. Should become a typed target list
/// referencing descriptor identity.
/// </remarks>
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames);          // TODO G1/G9: typed target list — transitive expansion of computed field refs
