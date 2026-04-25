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
///
/// TODO D8/R4: All string parameters (field names, event names) and string-based
/// return types are provisional. The runtime API will use typed metadata descriptors
/// from the executable model — not raw strings — once D8/R4 defines those descriptors.
/// Every string placeholder below is a known gap.
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

    public object? this[string fieldName]                               // TODO D8/R4: field descriptor, not string
        => throw new NotImplementedException();                         // TODO R3: slot array or dictionary

    public IReadOnlyList<FieldAccessInfo> FieldAccess                    // TODO D8/R4: field descriptors carry access mode
        => throw new NotImplementedException();                         // omit = absent from list

    // ── Structural queries (precomputed — zero evaluation cost) ─────

    public IReadOnlyList<string> AvailableEvents                        // TODO D8/R4: event descriptors, not strings
        => throw new NotImplementedException();                         // events with rows in current state

    public IReadOnlyList<ArgInfo> RequiredArgs(string eventName)         // TODO D8/R4: event descriptor param, arg descriptors returned
        => throw new NotImplementedException();                         // name + type per arg

    // ── Applicable constraints (Tier 2 — filtered for current state) ─
    // Constraints active for this entity's current state:
    //   - All global rules (always apply)
    //   - `in <CurrentState> ensure` (residency truth)
    //   - `from <CurrentState> ensure` (checked on exit)
    //   - Event ensures for available events
    // Precomputed during construction — zero evaluation cost.

    public IReadOnlyList<ConstraintDescriptor> ApplicableConstraints     // TODO D8/R4: backed by executable model scope index
        => throw new NotImplementedException();

    // ── Commit ──────────────────────────────────────────────────────

    public EventOutcome Fire(string eventName, IReadOnlyDictionary<string, object?> args)       // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    public UpdateOutcome Update(IReadOnlyDictionary<string, object?> fields)                    // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    // ── Inspect ─────────────────────────────────────────────────────

    public EventInspection InspectFire(string eventName, IReadOnlyDictionary<string, object?>? args = null)     // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();

    public UpdateInspection InspectUpdate(IReadOnlyDictionary<string, object?>? fields = null)                  // TODO D8/R4: descriptor-keyed
        => throw new NotImplementedException();
}
