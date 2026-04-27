namespace Precept.Language;

/// <summary>
/// Metadata for a <see cref="ConstraintKind"/> value. Discriminated union — DU as identity:
/// the subtype IS the semantic signal. Consumers pattern-match exhaustively; the compiler
/// catches unhandled cases when new kinds are added.
///
/// The <see cref="StateAnchored"/> intermediate abstract layer groups the three state-anchored
/// kinds so consumers can check <c>meta is ConstraintMeta.StateAnchored</c> without testing
/// each kind individually.
/// </summary>
public abstract record ConstraintMeta(ConstraintKind Kind, string Description)
{
    /// <summary>Global invariant — <c>rule</c> — always enforced.</summary>
    public sealed record Invariant()
        : ConstraintMeta(ConstraintKind.Invariant, "Global invariant — always enforced");

    /// <summary>
    /// Abstract intermediate grouping for the three state-anchored constraint kinds.
    /// Consumers can check <c>meta is StateAnchored</c> to test for any state-scoped constraint
    /// without switching on individual kinds.
    /// </summary>
    public abstract record StateAnchored(ConstraintKind Kind, string Description)
        : ConstraintMeta(Kind, Description);

    /// <summary>State residency — <c>in &lt;State&gt; ensure</c> — enforced while in state.</summary>
    public sealed record StateResident()
        : StateAnchored(ConstraintKind.StateResident, "State residency — enforced while in state");

    /// <summary>State entry — <c>to &lt;State&gt; ensure</c> — enforced on transition into state.</summary>
    public sealed record StateEntry()
        : StateAnchored(ConstraintKind.StateEntry, "State entry — enforced on transition into state");

    /// <summary>State exit — <c>from &lt;State&gt; ensure</c> — enforced on transition out of state.</summary>
    public sealed record StateExit()
        : StateAnchored(ConstraintKind.StateExit, "State exit — enforced on transition out of state");

    /// <summary>Event precondition — <c>on &lt;Event&gt; ensure</c> — enforced before event fires.</summary>
    public sealed record EventPrecondition()
        : ConstraintMeta(ConstraintKind.EventPrecondition, "Event precondition — enforced before event fires");
}
