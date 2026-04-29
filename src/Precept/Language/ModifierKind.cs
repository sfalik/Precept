namespace Precept.Language;

/// <summary>
/// Each member identifies a distinct modifier across all 5 declaration surfaces.
/// Grouped by DU subtype: field (15), state (7), event (1), access (3), anchor (3).
/// </summary>
public enum ModifierKind
{
    // ── Field modifiers (15) — FieldModifierMeta ────────────────────────────────

    /// <summary>Flag: field is nullable (presence tested via is set / is not set).</summary>
    Optional    =  1,
    /// <summary>Flag: choice field supports ordinal comparison.</summary>
    Ordered     =  2,
    /// <summary>Flag: value ≥ 0.</summary>
    Nonnegative =  3,
    /// <summary>Flag: value &gt; 0.</summary>
    Positive    =  4,
    /// <summary>Flag: value ≠ 0.</summary>
    Nonzero     =  5,
    /// <summary>Flag: string is non-empty.</summary>
    Notempty    =  6,
    /// <summary>Value: default value expression.</summary>
    Default     =  7,
    /// <summary>Value: minimum value.</summary>
    Min         =  8,
    /// <summary>Value: maximum value.</summary>
    Max         =  9,
    /// <summary>Value: minimum string length.</summary>
    Minlength   = 10,
    /// <summary>Value: maximum string length.</summary>
    Maxlength   = 11,
    /// <summary>Value: minimum collection count.</summary>
    Mincount    = 12,
    /// <summary>Value: maximum collection count.</summary>
    Maxcount    = 13,
    /// <summary>Value: maximum decimal places.</summary>
    Maxplaces   = 14,
    /// <summary>Flag: field is directly editable (write baseline); defaults to read-only without this modifier.</summary>
    Writable    = 15,

    // ── State modifiers (7) — StateModifierMeta ─────────────────────────────────

    /// <summary>Structural: the precept starts in this state.</summary>
    InitialState  = 16,
    /// <summary>Structural: no outgoing transitions.</summary>
    Terminal      = 17,
    /// <summary>Structural: all initial→terminal paths visit this state (dominator).</summary>
    Required      = 18,
    /// <summary>Structural: no path from this state back to any ancestor.</summary>
    Irreversible  = 19,
    /// <summary>Semantic: success outcome state.</summary>
    Success       = 20,
    /// <summary>Semantic: warning outcome state.</summary>
    Warning       = 21,
    /// <summary>Semantic: error outcome state.</summary>
    Error         = 22,

    // ── Event modifiers (1 v2) — EventModifierMeta ──────────────────────────────

    /// <summary>Keyword: this event is the auto-fire entry point.</summary>
    InitialEvent  = 23,

    // ── Access modifiers (3) — AccessModifierMeta ───────────────────────────────

    /// <summary>Field is present and writable.</summary>
    Write = 24,
    /// <summary>Field is present and read-only.</summary>
    Read  = 25,
    /// <summary>Field is structurally absent.</summary>
    Omit  = 26,

    // ── Anchor modifiers (3) — AnchorModifierMeta ───────────────────────────────

    /// <summary>Anchor: in-state scope.</summary>
    In   = 27,
    /// <summary>Anchor: on-entry scope (to).</summary>
    To   = 28,
    /// <summary>Anchor: on-exit scope (from).</summary>
    From = 29,
}
