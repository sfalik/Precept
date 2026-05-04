using System.Text.Json;

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

    public PreceptValue this[string fieldName]                          // TODO R3: slot array or dictionary
        => throw new NotImplementedException();

    public T Get<T>(string fieldName)
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

    // JSON lane — wire callers that already have JsonElement on hand
    public EventOutcome Fire(string eventName, JsonElement? args = null)
        => throw new NotImplementedException();

    public UpdateOutcome Update(JsonElement? fields = null)
        => throw new NotImplementedException();

    // Typed lane — in-process callers using fluent builders
    public EventOutcome Fire(string eventName, Action<IArgBuilder>? args = null)
        => throw new NotImplementedException();

    public UpdateOutcome Update(Action<IFieldBuilder>? fields = null)
        => throw new NotImplementedException();

    // ── Inspect ─────────────────────────────────────────────────────

    // JSON lane
    public EventInspection InspectFire(string eventName, JsonElement? args = null)
        => throw new NotImplementedException();

    public UpdateInspection InspectUpdate(JsonElement? fields = null)
        => throw new NotImplementedException();

    // Typed lane
    public EventInspection InspectFire(string eventName, Action<IArgBuilder>? args = null)
        => throw new NotImplementedException();

    public UpdateInspection InspectUpdate(Action<IFieldBuilder>? fields = null)
        => throw new NotImplementedException();
}
