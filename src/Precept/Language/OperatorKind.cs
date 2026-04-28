namespace Precept.Language;

/// <summary>
/// Every operator the language supports. Members map to expression-level operators
/// in the Pratt parser's precedence table. See language spec § 2.1.
/// </summary>
public enum OperatorKind
{
    // ── Logical ────────────────────────────────────────────────────
    Or,
    And,
    Not,

    // ── Comparison ─────────────────────────────────────────────────
    Equals,
    NotEquals,
    CaseInsensitiveEquals,
    CaseInsensitiveNotEquals,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,

    // ── Membership ─────────────────────────────────────────────────
    Contains,

    // ── Arithmetic (binary) ────────────────────────────────────────
    Plus,
    Minus,
    Times,
    Divide,
    Modulo,

    // ── Arithmetic (unary) ─────────────────────────────────────────
    Negate,
}
