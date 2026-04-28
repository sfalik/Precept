namespace Precept.Language;

/// <summary>
/// The five constraint declaration forms in the Precept DSL.
/// </summary>
public enum ConstraintKind
{
    // ── Always-active ───────────────────────────────────────────────────
    /// <summary>Global invariant — <c>rule</c> declaration. Always enforced.</summary>
    Invariant,

    // ── State-anchored ──────────────────────────────────────────────────
    /// <summary>State residency — <c>in &lt;State&gt; ensure</c>. Enforced while in state.</summary>
    StateResident,

    /// <summary>State entry — <c>to &lt;State&gt; ensure</c>. Enforced on transition into state.</summary>
    StateEntry,

    /// <summary>State exit — <c>from &lt;State&gt; ensure</c>. Enforced on transition out of state.</summary>
    StateExit,

    // ── Event-anchored ──────────────────────────────────────────────────
    /// <summary>Event precondition — <c>on &lt;Event&gt; ensure</c>. Enforced before event fires.</summary>
    EventPrecondition,
}
