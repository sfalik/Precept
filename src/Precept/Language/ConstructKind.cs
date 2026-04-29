namespace Precept.Language;

/// <summary>
/// Grammar forms / declaration shapes — every construct the parser can produce.
/// </summary>
public enum ConstructKind
{
    // ── Wrapper ─────────────────────────────────────────────────────
    /// <summary><c>precept Name</c> — the file-level header.</summary>
    PreceptHeader     =  1,

    // ── Top-level declarations ──────────────────────────────────────
    /// <summary><c>field Name as Type Modifiers</c></summary>
    FieldDeclaration  =  2,

    /// <summary><c>state Name Modifiers</c></summary>
    StateDeclaration  =  3,

    /// <summary><c>event Name (Args)? initial?</c></summary>
    EventDeclaration  =  4,

    /// <summary><c>rule Expr because Msg</c></summary>
    RuleDeclaration   =  5,

    // ── Nested in state scope ───────────────────────────────────────
    /// <summary><c>from StateTarget on Event ... -> Outcome</c></summary>
    TransitionRow     =  6,

    /// <summary><c>(in|to|from) StateTarget ensure Expr because Msg</c></summary>
    StateEnsure       =  7,

    /// <summary><c>in StateTarget modify FieldTarget readonly|editable [when Guard]</c></summary>
    AccessMode        =  8,

    /// <summary><c>in StateTarget omit FieldTarget</c> — structural exclusion, no guard.</summary>
    OmitDeclaration   =  9,

    /// <summary><c>(to|from) StateTarget -> Actions</c></summary>
    StateAction       = 10,

    // ── Nested in event scope ───────────────────────────────────────
    /// <summary><c>on Event ensure Expr because Msg</c></summary>
    EventEnsure       = 11,

    // ── Stateless ───────────────────────────────────────────────────
    /// <summary><c>on Event -> Actions</c> (no state machine)</summary>
    EventHandler      = 12,
}
