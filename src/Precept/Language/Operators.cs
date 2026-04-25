using System.Collections.Frozen;

namespace Precept.Language;

/// <summary>
/// Catalog of all expression-level operators. Precedence values and associativity
/// match the Pratt parser's binding-power table (language spec § 2.1).
/// </summary>
public static class Operators
{
    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
    {
        // ── Logical ────────────────────────────────────────────────
        OperatorKind.Or => new(
            kind, Tokens.GetMeta(TokenKind.Or),
            "Logical disjunction",
            Arity.Binary, Associativity.Left, Precedence: 10),

        OperatorKind.And => new(
            kind, Tokens.GetMeta(TokenKind.And),
            "Logical conjunction",
            Arity.Binary, Associativity.Left, Precedence: 20),

        OperatorKind.Not => new(
            kind, Tokens.GetMeta(TokenKind.Not),
            "Logical negation",
            Arity.Unary, Associativity.Right, Precedence: 25),

        // ── Comparison ─────────────────────────────────────────────
        OperatorKind.Equals => new(
            kind, Tokens.GetMeta(TokenKind.DoubleEquals),
            "Equality comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.NotEquals => new(
            kind, Tokens.GetMeta(TokenKind.NotEquals),
            "Inequality comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.CaseInsensitiveEquals => new(
            kind, Tokens.GetMeta(TokenKind.CaseInsensitiveEquals),
            "Case-insensitive equality (string-only)",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.CaseInsensitiveNotEquals => new(
            kind, Tokens.GetMeta(TokenKind.CaseInsensitiveNotEquals),
            "Case-insensitive not-equals (string-only)",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.LessThan => new(
            kind, Tokens.GetMeta(TokenKind.LessThan),
            "Less-than comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.GreaterThan => new(
            kind, Tokens.GetMeta(TokenKind.GreaterThan),
            "Greater-than comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.LessThanOrEqual => new(
            kind, Tokens.GetMeta(TokenKind.LessThanOrEqual),
            "Less-than-or-equal comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        OperatorKind.GreaterThanOrEqual => new(
            kind, Tokens.GetMeta(TokenKind.GreaterThanOrEqual),
            "Greater-than-or-equal comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30),

        // ── Membership ─────────────────────────────────────────────
        OperatorKind.Contains => new(
            kind, Tokens.GetMeta(TokenKind.Contains),
            "Collection membership test",
            Arity.Binary, Associativity.Left, Precedence: 40),

        // ── Arithmetic (binary) ────────────────────────────────────
        OperatorKind.Plus => new(
            kind, Tokens.GetMeta(TokenKind.Plus),
            "Addition",
            Arity.Binary, Associativity.Left, Precedence: 50),

        OperatorKind.Minus => new(
            kind, Tokens.GetMeta(TokenKind.Minus),
            "Subtraction",
            Arity.Binary, Associativity.Left, Precedence: 50),

        OperatorKind.Times => new(
            kind, Tokens.GetMeta(TokenKind.Star),
            "Multiplication",
            Arity.Binary, Associativity.Left, Precedence: 60),

        OperatorKind.Divide => new(
            kind, Tokens.GetMeta(TokenKind.Slash),
            "Division",
            Arity.Binary, Associativity.Left, Precedence: 60),

        OperatorKind.Modulo => new(
            kind, Tokens.GetMeta(TokenKind.Percent),
            "Modulo (remainder)",
            Arity.Binary, Associativity.Left, Precedence: 60),

        // ── Arithmetic (unary) ─────────────────────────────────────
        OperatorKind.Negate => new(
            kind, Tokens.GetMeta(TokenKind.Minus),
            "Arithmetic negation",
            Arity.Unary, Associativity.Right, Precedence: 65),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — flat list
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<OperatorMeta> All { get; } =
        Enum.GetValues<OperatorKind>().Select(GetMeta).ToArray();

    // ════════════════════════════════════════════════════════════════════════════
    //  ByToken — (TokenKind, Arity) → OperatorMeta
    //
    //  Keyed by both token and arity because Minus/Negate share TokenKind.Minus.
    //  The parser always knows whether it is in prefix (unary) or infix (binary)
    //  position, so the two-key lookup is natural.
    // ════════════════════════════════════════════════════════════════════════════

    public static FrozenDictionary<(TokenKind, Arity), OperatorMeta> ByToken { get; } =
        All.ToFrozenDictionary(m => (m.Token.Kind, m.Arity));
}
