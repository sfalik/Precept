namespace Precept.Language;

/// <summary>
/// State-machine action verbs — the keywords that appear after <c>-></c> in
/// transition rows and state-action hooks.
/// </summary>
public enum ActionKind
{
    // ── Scalar ──────────────────────────────────────────────────────
    Set = 1,

    // ── Set collection ──────────────────────────────────────────────
    Add     = 2,
    Remove  = 3,

    // ── Queue collection ────────────────────────────────────────────
    Enqueue = 4,
    Dequeue = 5,

    // ── Stack collection ────────────────────────────────────────────
    Push    = 6,
    Pop     = 7,

    // ── Universal (collections + optional scalars) ──────────────────
    Clear   = 8,

    // ── New collection actions ───────────────────────────────────────
    Append      = 9,
    AppendBy    = 10,
    Insert      = 11,
    RemoveAt    = 12,
    Put         = 13,
    EnqueueBy   = 14,
    DequeueBy   = 15,
}
