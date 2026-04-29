namespace Precept.Language;

/// <summary>
/// Each member identifies a distinct built-in function (or overload family).
/// Round and RoundPlaces are separate because they have different return types.
/// </summary>
public enum FunctionKind
{
    // ── Numeric ─────────────────────────────────────────────────────────────────
    Min          =  1,
    Max          =  2,
    Abs          =  3,
    Clamp        =  4,
    Floor        =  5,
    Ceil         =  6,
    Truncate     =  7,
    Round        =  8,
    RoundPlaces  =  9,
    Approximate  = 10,
    Pow          = 11,
    Sqrt         = 12,

    // ── String ──────────────────────────────────────────────────────────────────
    Trim         = 13,
    StartsWith   = 14,
    EndsWith     = 15,
    ToLower      = 16,
    ToUpper      = 17,
    Left         = 18,
    Right        = 19,
    Mid          = 20,

    // ── Temporal ────────────────────────────────────────────────────────────────
    Now          = 21,
}
