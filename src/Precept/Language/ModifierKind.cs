namespace Precept.Language;

/// <summary>
/// Each member identifies a distinct modifier across all 5 declaration surfaces.
/// Grouped by DU subtype: field (15), state (7), event (1), access (3), anchor (3).
/// </summary>
public enum ModifierKind
{
    // ── Field modifiers (15) — FieldModifierMeta ────────────────────────────────

    /// <summary>Flag: field is nullable (presence tested via is set / is not set).</summary>
    Optional,
    /// <summary>Flag: choice field supports ordinal comparison.</summary>
    Ordered,
    /// <summary>Flag: value ≥ 0.</summary>
    Nonnegative,
    /// <summary>Flag: value &gt; 0.</summary>
    Positive,
    /// <summary>Flag: value ≠ 0.</summary>
    Nonzero,
    /// <summary>Flag: string is non-empty.</summary>
    Notempty,
    /// <summary>Value: default value expression.</summary>
    Default,
    /// <summary>Value: minimum value.</summary>
    Min,
    /// <summary>Value: maximum value.</summary>
    Max,
    /// <summary>Value: minimum string length.</summary>
    Minlength,
    /// <summary>Value: maximum string length.</summary>
    Maxlength,
    /// <summary>Value: minimum collection count.</summary>
    Mincount,
    /// <summary>Value: maximum collection count.</summary>
    Maxcount,
    /// <summary>Value: maximum decimal places.</summary>
    Maxplaces,
    /// <summary>Flag: field is directly editable (write baseline); defaults to read-only without this modifier.</summary>
    Writable,

    // ── State modifiers (7) — StateModifierMeta ─────────────────────────────────

    /// <summary>Structural: the precept starts in this state.</summary>
    InitialState,
    /// <summary>Structural: no outgoing transitions.</summary>
    Terminal,
    /// <summary>Structural: all initial→terminal paths visit this state (dominator).</summary>
    Required,
    /// <summary>Structural: no path from this state back to any ancestor.</summary>
    Irreversible,
    /// <summary>Semantic: success outcome state.</summary>
    Success,
    /// <summary>Semantic: warning outcome state.</summary>
    Warning,
    /// <summary>Semantic: error outcome state.</summary>
    Error,

    // ── Event modifiers (1 v2) — EventModifierMeta ──────────────────────────────

    /// <summary>Keyword: this event is the auto-fire entry point.</summary>
    InitialEvent,

    // ── Access modifiers (3) — AccessModifierMeta ───────────────────────────────

    /// <summary>Field is present and writable.</summary>
    Write,
    /// <summary>Field is present and read-only.</summary>
    Read,
    /// <summary>Field is structurally absent.</summary>
    Omit,

    // ── Anchor modifiers (3) — AnchorModifierMeta ───────────────────────────────

    /// <summary>Anchor: in-state scope.</summary>
    In,
    /// <summary>Anchor: on-entry scope (to).</summary>
    To,
    /// <summary>Anchor: on-exit scope (from).</summary>
    From,
}
