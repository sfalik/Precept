using Precept.Language;
using Precept.Pipeline;

namespace Precept.Runtime;

/// <summary>
/// The executable model — a sealed, immutable artifact constructed from an
/// error-free <see cref="Compilation"/>. A single instance serves all
/// entity versions of the same precept definition.
/// </summary>
/// <remarks>
/// Thread-safe, shareable, cacheable. Mirrors CEL's Program (one compiled
/// program evaluated against many activations) and OPA's PreparedEvalQuery.
///
/// Internal structure (dispatch tables, slot arrays, constraint buckets) is
/// pending D8/R4 (executable model contract).
///
/// Catalog-driven construction: use Constraints.GetMeta(kind) to route each
/// constraint into its activation bucket; pattern-match on ConstraintMeta DU
/// subtypes (not ConstraintKind enum values directly). ConstraintMeta.StateAnchored
/// groups the three state-scoped subtypes for shared graph-analysis paths.
/// See: docs/language/catalog-system.md § Evaluator-catalog integration pattern
/// </remarks>
public sealed class Precept
{
    private Precept() { }

    // ── Construction ────────────────────────────────────────────────

    public static Precept From(Compilation compilation)
    {
        if (compilation.HasErrors)
            throw new InvalidOperationException("Cannot create a Precept from a compilation with errors.");

        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates the initial entity version. If the precept declares an initial event,
    /// fires it atomically as part of construction — args are passed through to the
    /// event pipeline. If no initial event is declared, constructs from defaults
    /// (always succeeds by compile-time guarantee).
    /// </summary>
    /// <remarks>
    /// Internally: build hollow version (defaults + initial state + omitted fields) →
    /// if initial event: Fire(initialEvent, args) on hollow version → return outcome.
    /// If no initial event: evaluate computed fields, run rules/ensures → Applied.
    ///
    /// The compiler enforces (C100) that precepts with required fields lacking defaults
    /// declare an initial event, and (C101) that the initial event assigns those fields.
    /// </remarks>
    public EventOutcome Create(IReadOnlyDictionary<string, object?>? args = null)
        => throw new NotImplementedException();

    /// <summary>
    /// Progressive inspection of construction — same model as <see cref="Version.InspectFire"/>.
    /// Returns the annotated landscape for the initial event: row matching, constraint
    /// results, field snapshots. If no initial event, returns the default-based landscape.
    /// </summary>
    public EventInspection InspectCreate(IReadOnlyDictionary<string, object?>? args = null)
        => throw new NotImplementedException();

    /// <summary>
    /// Reconstitutes a Version from persisted data. Validates against the current
    /// definition: recomputes computed fields, evaluates global rules and state ensures.
    /// Access modes are bypassed — all stored fields are accepted regardless of the
    /// restored state's write/read/omit declarations.
    /// </summary>
    /// <remarks>
    /// Internally: build working copy with state + all fields → recompute computed fields
    /// → evaluate constraints → return outcome. No event matching, no mutations, no
    /// access mode checks. Future migration logic runs before the validation pipeline.
    /// </remarks>
    public RestoreOutcome Restore(string? state, IReadOnlyDictionary<string, object?> fields)
        => throw new NotImplementedException();

    // ── Definition-level queries (structural — precomputed) ─────────

    public IReadOnlyList<StateDescriptor> States
        => throw new NotImplementedException();

    public IReadOnlyList<FieldDescriptor> Fields
        => throw new NotImplementedException();

    public IReadOnlyList<EventDescriptor> Events
        => throw new NotImplementedException();

    /// <summary>The initial state descriptor, or <c>null</c> for stateless precepts.</summary>
    public StateDescriptor? InitialState
        => throw new NotImplementedException();

    /// <summary>
    /// The initial event descriptor, or <c>null</c> if the precept does not declare one.
    /// When non-null, <see cref="Create"/> fires this event atomically.
    /// </summary>
    public EventDescriptor? InitialEvent
        => throw new NotImplementedException();

    public bool IsStateless
        => throw new NotImplementedException();

    // ── Constraint catalog (Tier 1 — all declared constraints) ──────
    // The full constraint catalog: every rule, every ensure, every anchor.
    // Unfiltered by state. This is the definition-level truth surface.

    public IReadOnlyList<ConstraintDescriptor> Constraints              // TODO D8/R4: backed by executable model
        => throw new NotImplementedException();
}
