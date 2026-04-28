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
            Arity.Binary, Associativity.Left, Precedence: 10, OperatorFamily.Logical, IsKeywordOperator: true,
            HoverDescription: "Logical OR. Both operands must be boolean. True if either side is true."),

        OperatorKind.And => new(
            kind, Tokens.GetMeta(TokenKind.And),
            "Logical conjunction",
            Arity.Binary, Associativity.Left, Precedence: 20, OperatorFamily.Logical, IsKeywordOperator: true,
            HoverDescription: "Logical AND. Both operands must be boolean. True only if both sides are true."),

        OperatorKind.Not => new(
            kind, Tokens.GetMeta(TokenKind.Not),
            "Logical negation",
            Arity.Unary, Associativity.Right, Precedence: 25, OperatorFamily.Logical, IsKeywordOperator: true,
            HoverDescription: "Logical NOT. Operand must be boolean. Inverts true to false and false to true."),

        // ── Comparison ─────────────────────────────────────────────
        OperatorKind.Equals => new(
            kind, Tokens.GetMeta(TokenKind.DoubleEquals),
            "Equality comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Equality comparison. Returns true if both operands are equal. Cannot be chained."),

        OperatorKind.NotEquals => new(
            kind, Tokens.GetMeta(TokenKind.NotEquals),
            "Inequality comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Inequality comparison. Returns true if operands differ. Cannot be chained."),

        OperatorKind.CaseInsensitiveEquals => new(
            kind, Tokens.GetMeta(TokenKind.CaseInsensitiveEquals),
            "Case-insensitive equality (string-only)",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Case-insensitive text equality. Compares strings ignoring upper/lower case differences."),

        OperatorKind.CaseInsensitiveNotEquals => new(
            kind, Tokens.GetMeta(TokenKind.CaseInsensitiveNotEquals),
            "Case-insensitive not-equals (string-only)",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Case-insensitive text inequality. Returns true if strings differ ignoring case."),

        OperatorKind.LessThan => new(
            kind, Tokens.GetMeta(TokenKind.LessThan),
            "Less-than comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Less-than comparison. Requires orderable types. Cannot be chained."),

        OperatorKind.GreaterThan => new(
            kind, Tokens.GetMeta(TokenKind.GreaterThan),
            "Greater-than comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Greater-than comparison. Requires orderable types. Cannot be chained."),

        OperatorKind.LessThanOrEqual => new(
            kind, Tokens.GetMeta(TokenKind.LessThanOrEqual),
            "Less-than-or-equal comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Less-than-or-equal comparison. Requires orderable types. Cannot be chained."),

        OperatorKind.GreaterThanOrEqual => new(
            kind, Tokens.GetMeta(TokenKind.GreaterThanOrEqual),
            "Greater-than-or-equal comparison",
            Arity.Binary, Associativity.NonAssociative, Precedence: 30, OperatorFamily.Comparison,
            HoverDescription: "Greater-than-or-equal comparison. Requires orderable types. Cannot be chained."),

        // ── Membership ─────────────────────────────────────────────
        OperatorKind.Contains => new(
            kind, Tokens.GetMeta(TokenKind.Contains),
            "Collection membership test",
            Arity.Binary, Associativity.NonAssociative, Precedence: 40, OperatorFamily.Membership, IsKeywordOperator: true,
            HoverDescription: "Collection membership test. Left operand is the collection; right operand is the element to look for."),

        // ── Arithmetic (binary) ────────────────────────────────────
        OperatorKind.Plus => new(
            kind, Tokens.GetMeta(TokenKind.Plus),
            "Addition",
            Arity.Binary, Associativity.Left, Precedence: 50, OperatorFamily.Arithmetic,
            HoverDescription: "Addition or string concatenation. For temporal types, adds periods and durations to dates."),

        OperatorKind.Minus => new(
            kind, Tokens.GetMeta(TokenKind.Minus),
            "Subtraction",
            Arity.Binary, Associativity.Left, Precedence: 50, OperatorFamily.Arithmetic,
            HoverDescription: "Subtraction. For temporal types, subtracts periods and durations from dates."),

        OperatorKind.Times => new(
            kind, Tokens.GetMeta(TokenKind.Star),
            "Multiplication",
            Arity.Binary, Associativity.Left, Precedence: 60, OperatorFamily.Arithmetic,
            HoverDescription: "Multiplication. Cross-lane multiplication (decimal × number) is not allowed — use approximate() to bridge types."),

        OperatorKind.Divide => new(
            kind, Tokens.GetMeta(TokenKind.Slash),
            "Division",
            Arity.Binary, Associativity.Left, Precedence: 60, OperatorFamily.Arithmetic,
            HoverDescription: "Division. The proof engine checks for division-by-zero at design time."),

        OperatorKind.Modulo => new(
            kind, Tokens.GetMeta(TokenKind.Percent),
            "Modulo (remainder)",
            Arity.Binary, Associativity.Left, Precedence: 60, OperatorFamily.Arithmetic,
            HoverDescription: "Modulo (remainder) after integer division."),

        // ── Arithmetic (unary) ─────────────────────────────────────
        OperatorKind.Negate => new(
            kind, Tokens.GetMeta(TokenKind.Minus),
            "Arithmetic negation",
            Arity.Unary, Associativity.Right, Precedence: 65, OperatorFamily.Arithmetic,
            HoverDescription: "Arithmetic negation (unary minus). Returns the negative of the operand."),

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
