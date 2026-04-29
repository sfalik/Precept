namespace Precept.Language;

/// <summary>
/// Every operator the language supports. Members map to expression-level operators
/// in the Pratt parser's precedence table. See language spec § 2.1.
/// </summary>
public enum OperatorKind
{
    // ── Logical ────────────────────────────────────────────────────
    Or                        =  1,
    And                       =  2,
    Not                       =  3,

    // ── Comparison ─────────────────────────────────────────────────
    Equals                    =  4,
    NotEquals                 =  5,
    CaseInsensitiveEquals     =  6,
    CaseInsensitiveNotEquals  =  7,
    LessThan                  =  8,
    GreaterThan               =  9,
    LessThanOrEqual           = 10,
    GreaterThanOrEqual        = 11,

    // ── Membership ─────────────────────────────────────────────────
    Contains                  = 12,

    // ── Arithmetic (binary) ────────────────────────────────────────
    Plus                      = 13,
    Minus                     = 14,
    Times                     = 15,
    Divide                    = 16,
    Modulo                    = 17,

    // ── Arithmetic (unary) ─────────────────────────────────────────
    Negate                    = 18,
}
