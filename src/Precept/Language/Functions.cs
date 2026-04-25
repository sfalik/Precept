using System.Collections.Frozen;

namespace Precept.Language;

/// <summary>
/// Catalog of all built-in functions. Each entry describes a function's name,
/// typed overloads, and qualifier requirements. Source of truth for the type checker,
/// evaluator dispatch, LS completions/hover, and MCP vocabulary.
/// </summary>
public static class Functions
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared ParameterMeta instances — one per TypeKind used as a parameter
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly ParameterMeta PInteger = new(TypeKind.Integer);
    private static readonly ParameterMeta PDecimal = new(TypeKind.Decimal);
    private static readonly ParameterMeta PNumber = new(TypeKind.Number);
    private static readonly ParameterMeta PBoolean = new(TypeKind.Boolean);
    private static readonly ParameterMeta PString = new(TypeKind.String);
    private static readonly ParameterMeta PMoney = new(TypeKind.Money);
    private static readonly ParameterMeta PQuantity = new(TypeKind.Quantity);
    private static readonly ParameterMeta PInstant = new(TypeKind.Instant);

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static FunctionMeta GetMeta(FunctionKind kind) => kind switch
    {
        // ── Numeric: min / max ──────────────────────────────────────────────────
        FunctionKind.Min => new(kind, "min", "Minimum of two values (common numeric type)",
        [
            new([PInteger, PInteger], TypeKind.Integer),
            new([PDecimal, PDecimal], TypeKind.Decimal),
            new([PNumber, PNumber], TypeKind.Number),
            new([PMoney, PMoney], TypeKind.Money, QualifierMatch.Same),
            new([PQuantity, PQuantity], TypeKind.Quantity, QualifierMatch.Same),
        ]),

        FunctionKind.Max => new(kind, "max", "Maximum of two values (common numeric type)",
        [
            new([PInteger, PInteger], TypeKind.Integer),
            new([PDecimal, PDecimal], TypeKind.Decimal),
            new([PNumber, PNumber], TypeKind.Number),
            new([PMoney, PMoney], TypeKind.Money, QualifierMatch.Same),
            new([PQuantity, PQuantity], TypeKind.Quantity, QualifierMatch.Same),
        ]),

        // ── Numeric: abs ────────────────────────────────────────────────────────
        FunctionKind.Abs => new(kind, "abs", "Absolute value (same type as input)",
        [
            new([PInteger], TypeKind.Integer),
            new([PDecimal], TypeKind.Decimal),
            new([PNumber], TypeKind.Number),
            new([PMoney], TypeKind.Money, QualifierMatch.Same),
            new([PQuantity], TypeKind.Quantity, QualifierMatch.Same),
        ]),

        // ── Numeric: clamp ──────────────────────────────────────────────────────
        FunctionKind.Clamp => new(kind, "clamp", "Clamp value between lo and hi (common numeric type)",
        [
            new([PInteger, PInteger, PInteger], TypeKind.Integer),
            new([PDecimal, PDecimal, PDecimal], TypeKind.Decimal),
            new([PNumber, PNumber, PNumber], TypeKind.Number),
            new([PMoney, PMoney, PMoney], TypeKind.Money, QualifierMatch.Same),
            new([PQuantity, PQuantity, PQuantity], TypeKind.Quantity, QualifierMatch.Same),
        ]),

        // ── Numeric: rounding family (decimal|number → integer) ─────────────────
        FunctionKind.Floor => new(kind, "floor", "Round toward negative infinity → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ]),

        FunctionKind.Ceil => new(kind, "ceil", "Round toward positive infinity → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ]),

        FunctionKind.Truncate => new(kind, "truncate", "Round toward zero → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ]),

        FunctionKind.Round => new(kind, "round", "Banker's rounding → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ]),

        // ── Numeric: round with places (numeric, integer → decimal) ─────────────
        FunctionKind.RoundPlaces => new(kind, "round",
            "Round to N decimal places (explicit bridge: number → decimal)",
        [
            new([PInteger, PInteger], TypeKind.Decimal),
            new([PDecimal, PInteger], TypeKind.Decimal),
            new([PNumber, PInteger], TypeKind.Decimal),
            new([PMoney, PInteger], TypeKind.Money, QualifierMatch.Same),
            new([PQuantity, PInteger], TypeKind.Quantity, QualifierMatch.Same),
        ]),

        // ── Numeric: lane bridges ───────────────────────────────────────────────
        FunctionKind.Approximate => new(kind, "approximate",
            "Explicit bridge: decimal → number (makes precision loss visible)",
        [
            new([PDecimal], TypeKind.Number),
        ]),

        // ── Numeric: power / root ───────────────────────────────────────────────
        FunctionKind.Pow => new(kind, "pow",
            "Raise base to integer exponent (same type as base)",
        [
            new([PInteger, PInteger], TypeKind.Integer),
            new([PDecimal, PInteger], TypeKind.Decimal),
            new([PNumber, PInteger], TypeKind.Number),
        ]),

        FunctionKind.Sqrt => new(kind, "sqrt",
            "Square root → number (proof engine checks non-negativity)",
        [
            new([PInteger], TypeKind.Number),
            new([PDecimal], TypeKind.Number),
            new([PNumber], TypeKind.Number),
        ]),

        // ── String ──────────────────────────────────────────────────────────────
        FunctionKind.Trim => new(kind, "trim", "Remove leading and trailing whitespace",
        [
            new([PString], TypeKind.String),
        ]),

        FunctionKind.StartsWith => new(kind, "startsWith", "Case-sensitive prefix test",
        [
            new([PString, PString], TypeKind.Boolean),
        ]),

        FunctionKind.EndsWith => new(kind, "endsWith", "Case-sensitive suffix test",
        [
            new([PString, PString], TypeKind.Boolean),
        ]),

        FunctionKind.ToLower => new(kind, "toLower", "Lowercase (invariant culture)",
        [
            new([PString], TypeKind.String),
        ]),

        FunctionKind.ToUpper => new(kind, "toUpper", "Uppercase (invariant culture)",
        [
            new([PString], TypeKind.String),
        ]),

        FunctionKind.Left => new(kind, "left",
            "Leftmost N code units (clamped to string length)",
        [
            new([PString, PInteger], TypeKind.String),
        ]),

        FunctionKind.Right => new(kind, "right",
            "Rightmost N code units (clamped to string length)",
        [
            new([PString, PInteger], TypeKind.String),
        ]),

        FunctionKind.Mid => new(kind, "mid",
            "1-indexed substring (clamped); start and length must be positive integer",
        [
            new([PString, PInteger, PInteger], TypeKind.String),
        ]),

        // ── Temporal ────────────────────────────────────────────────────────────
        FunctionKind.Now => new(kind, "now", "Current instant (UTC)",
        [
            new([], TypeKind.Instant),
        ]),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown FunctionKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every FunctionMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<FunctionMeta> All { get; } =
        Enum.GetValues<FunctionKind>().Select(GetMeta).ToArray();

    // ════════════════════════════════════════════════════════════════════════════
    //  ByName — function name → FunctionMeta[] (handles Round/RoundPlaces)
    // ════════════════════════════════════════════════════════════════════════════

    public static FrozenDictionary<string, FunctionMeta[]> ByName { get; } =
        All.GroupBy(m => m.Name, StringComparer.Ordinal)
           .ToFrozenDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

    // ════════════════════════════════════════════════════════════════════════════
    //  Lookup helpers
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds all FunctionMeta entries that share the given name.
    /// Returns an empty span if the name is not recognized.
    /// </summary>
    public static ReadOnlySpan<FunctionMeta> FindByName(string name)
    {
        return ByName.TryGetValue(name, out var metas)
            ? metas.AsSpan()
            : ReadOnlySpan<FunctionMeta>.Empty;
    }
}
