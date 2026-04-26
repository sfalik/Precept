namespace Precept.Language;

/// <summary>
/// Grammar forms / declaration shapes — every construct the parser can produce.
/// </summary>
public enum ConstructKind
{
    // ── Wrapper ─────────────────────────────────────────────────────
    /// <summary><c>precept Name</c> — the file-level header.</summary>
    PreceptHeader,

    // ── Top-level declarations ──────────────────────────────────────
    /// <summary><c>field Name as Type Modifiers</c></summary>
    FieldDeclaration,

    /// <summary><c>state Name Modifiers</c></summary>
    StateDeclaration,

    /// <summary><c>event Name (Args)? initial?</c></summary>
    EventDeclaration,

    /// <summary><c>rule Expr because Msg</c></summary>
    RuleDeclaration,

    // ── Nested in state scope ───────────────────────────────────────
    /// <summary><c>from StateTarget on Event ... -> Outcome</c></summary>
    TransitionRow,

    /// <summary><c>(in|to|from) StateTarget ensure Expr because Msg</c></summary>
    StateEnsure,

    /// <summary><c>(in StateTarget)? (write|read|omit) FieldTarget</c></summary>
    AccessMode,

    /// <summary><c>(to|from) StateTarget -> Actions</c></summary>
    StateAction,

    // ── Nested in event scope ───────────────────────────────────────
    /// <summary><c>on Event ensure Expr because Msg</c></summary>
    EventEnsure,

    // ── Stateless ───────────────────────────────────────────────────
    /// <summary><c>on Event -> Actions</c> (no state machine)</summary>
    EventHandler,
}
