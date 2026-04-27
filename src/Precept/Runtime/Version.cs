namespace Precept.Runtime;

/// <summary>
/// Immutable snapshot of an entity at a point in time. Every operation returns
/// a new Version — the input is never mutated.
/// </summary>
/// <remarks>
/// Instance methods delegate to the static <see cref="Evaluator"/>. Version is
/// a thin façade: identity (Precept + State + field data) plus the four-method
/// API surface decided in R5.
///
/// Internal representation (slot array vs. dictionary) is pending R3.
/// </remarks>
public sealed record Version
{
    internal Version(Precept precept, string? state)
    {
        Precept = precept;
        State = state;
    }

    // ── Identity ────────────────────────────────────────────────────

    public Precept Precept { get; }
    public string? State { get; }                                       // null for stateless precepts

    // ── Field access ────────────────────────────────────────────────

    public object? this[string fieldName]                               // TODO R3: slot array or dictionary
        => throw new NotImplementedException();

    public IReadOnlyList<FieldAccessInfo> FieldAccess                   // omit = absent from list
        => throw new NotImplementedException();

    // ── Structural queries (precomputed — zero evaluation cost) ─────

    public IReadOnlyList<EventDescriptor> AvailableEvents
        => throw new NotImplementedException();                         // events with rows in current state

    public IReadOnlyList<ArgDescriptor> RequiredArgs(EventDescriptor @event)
        => throw new NotImplementedException();

    // ── Applicable constraints (Tier 2 — filtered for current state) ─
    // Constraints active for this entity's current state:
    //   - All global rules (always apply)
    //   - `in <CurrentState> ensure` (residency truth)
    //   - `from <CurrentState> ensure` (checked on exit)
    //   - Event ensures for available events
    // Precomputed during construction — zero evaluation cost.

    public IReadOnlyList<ConstraintDescriptor> ApplicableConstraints
        => throw new NotImplementedException();

    // ── Commit ──────────────────────────────────────────────────────
    // Fire and Update use string-keyed arguments — these remain string-based
    // until the Evaluator is implemented, as they require descriptor resolution
    // at dispatch time (R3/R5).

    public EventOutcome Fire(string eventName, IReadOnlyDictionary<string, object?> args)
        => throw new NotImplementedException();

    public UpdateOutcome Update(IReadOnlyDictionary<string, object?> fields)
        => throw new NotImplementedException();

    // ── Inspect ─────────────────────────────────────────────────────

    public EventInspection InspectFire(string eventName, IReadOnlyDictionary<string, object?>? args = null)
        => throw new NotImplementedException();

    public UpdateInspection InspectUpdate(IReadOnlyDictionary<string, object?>? fields = null)
        => throw new NotImplementedException();
}
