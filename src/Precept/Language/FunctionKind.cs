namespace Precept.Language;

/// <summary>
/// Each member identifies a distinct built-in function (or overload family).
/// Round and RoundPlaces are separate because they have different return types.
/// </summary>
public enum FunctionKind
{
    // ── Numeric ─────────────────────────────────────────────────────────────────
    Min,
    Max,
    Abs,
    Clamp,
    Floor,
    Ceil,
    Truncate,
    Round,
    RoundPlaces,
    Approximate,
    Pow,
    Sqrt,

    // ── String ──────────────────────────────────────────────────────────────────
    Trim,
    StartsWith,
    EndsWith,
    ToLower,
    ToUpper,
    Left,
    Right,
    Mid,

    // ── Temporal ────────────────────────────────────────────────────────────────
    Now,
}
