namespace Precept.Language;

/// <summary>
/// State-machine action verbs — the keywords that appear after <c>-></c> in
/// transition rows and state-action hooks.
/// </summary>
public enum ActionKind
{
    // ── Scalar ──────────────────────────────────────────────────────
    Set,

    // ── Set collection ──────────────────────────────────────────────
    Add,
    Remove,

    // ── Queue collection ────────────────────────────────────────────
    Enqueue,
    Dequeue,

    // ── Stack collection ────────────────────────────────────────────
    Push,
    Pop,

    // ── Universal (collections + optional scalars) ──────────────────
    Clear,
}
