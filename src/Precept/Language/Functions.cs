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
    private static readonly ParameterMeta PDate = new(TypeKind.Date);
    private static readonly ParameterMeta PTime = new(TypeKind.Time);
    private static readonly ParameterMeta PDuration = new(TypeKind.Duration);
    private static readonly ParameterMeta PDateTime = new(TypeKind.DateTime);

    // Named parameters for overloads that carry a proof requirement — the same instance
    // must appear in both Parameters and ParamSubject to satisfy reference-equality (PRECEPT0005).
    private static readonly ParameterMeta PSqrtInteger = new(TypeKind.Integer, "value");
    private static readonly ParameterMeta PSqrtDecimal = new(TypeKind.Decimal, "value");
    private static readonly ParameterMeta PSqrtNumber  = new(TypeKind.Number,  "value");

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
        ],
        FunctionCategory.Numeric,
        UsageExample: "min(score, 100)",
        SnippetTemplate: "min(${1:a}, ${2:b})",
        HoverDescription: "Returns the smaller of two values. For money and quantity, both arguments must share the same qualifier."),

        FunctionKind.Max => new(kind, "max", "Maximum of two values (common numeric type)",
        [
            new([PInteger, PInteger], TypeKind.Integer),
            new([PDecimal, PDecimal], TypeKind.Decimal),
            new([PNumber, PNumber], TypeKind.Number),
            new([PMoney, PMoney], TypeKind.Money, QualifierMatch.Same),
            new([PQuantity, PQuantity], TypeKind.Quantity, QualifierMatch.Same),
        ],
        FunctionCategory.Numeric,
        UsageExample: "max(score, 0)",
        SnippetTemplate: "max(${1:a}, ${2:b})",
        HoverDescription: "Returns the larger of two values. For money and quantity, both arguments must share the same qualifier."),

        // ── Numeric: abs ────────────────────────────────────────────────────────
        FunctionKind.Abs => new(kind, "abs", "Absolute value (same type as input)",
        [
            new([new(TypeKind.Integer, "value")], TypeKind.Integer),
            new([new(TypeKind.Decimal, "value")], TypeKind.Decimal),
            new([new(TypeKind.Number,  "value")], TypeKind.Number),
            new([new(TypeKind.Money,   "value")], TypeKind.Money, QualifierMatch.Same),
            new([new(TypeKind.Quantity,"value")], TypeKind.Quantity, QualifierMatch.Same),
        ],
        FunctionCategory.Numeric,
        UsageExample: "abs(balance)",
        SnippetTemplate: "abs(${1:value})",
        HoverDescription: "Returns the absolute (non-negative) value. The result type matches the input type, including money and quantity qualifiers."),

        // ── Numeric: clamp ──────────────────────────────────────────────────────
        FunctionKind.Clamp => new(kind, "clamp", "Clamp value between lo and hi (common numeric type)",
        [
            new([new(TypeKind.Integer, "value"), new(TypeKind.Integer, "lo"), new(TypeKind.Integer, "hi")], TypeKind.Integer),
            new([new(TypeKind.Decimal, "value"), new(TypeKind.Decimal, "lo"), new(TypeKind.Decimal, "hi")], TypeKind.Decimal),
            new([new(TypeKind.Number,  "value"), new(TypeKind.Number,  "lo"), new(TypeKind.Number,  "hi")], TypeKind.Number),
            new([new(TypeKind.Money,   "value"), new(TypeKind.Money,   "lo"), new(TypeKind.Money,   "hi")], TypeKind.Money, QualifierMatch.Same),
            new([new(TypeKind.Quantity,"value"), new(TypeKind.Quantity,"lo"), new(TypeKind.Quantity,"hi")], TypeKind.Quantity, QualifierMatch.Same),
        ],
        FunctionCategory.Numeric,
        UsageExample: "clamp(score, 0, 100)",
        SnippetTemplate: "clamp(${1:value}, ${2:min}, ${3:max})",
        HoverDescription: "Constrains a value to the range [lo, hi]. Returns lo if below, hi if above, or the value itself if in range."),

        // ── Numeric: rounding family (decimal|number → integer) ─────────────────
        FunctionKind.Floor => new(kind, "floor", "Round toward negative infinity → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ],
        FunctionCategory.Numeric,
        UsageExample: "floor(rate)",
        SnippetTemplate: "floor(${1:value})",
        HoverDescription: "Rounds toward negative infinity and returns an integer. Use to truncate decimal values downward."),

        FunctionKind.Ceil => new(kind, "ceil", "Round toward positive infinity → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ],
        FunctionCategory.Numeric,
        UsageExample: "ceil(rate)",
        SnippetTemplate: "ceil(${1:value})",
        HoverDescription: "Rounds toward positive infinity and returns an integer. Use to round decimal values up to the next whole number."),

        FunctionKind.Truncate => new(kind, "truncate", "Round toward zero → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ],
        FunctionCategory.Numeric,
        UsageExample: "truncate(amount)",
        SnippetTemplate: "truncate(${1:value})",
        HoverDescription: "Rounds toward zero and returns an integer. Discards the fractional part without rounding direction."),

        FunctionKind.Round => new(kind, "round", "Banker's rounding → integer",
        [
            new([PDecimal], TypeKind.Integer),
            new([PNumber], TypeKind.Integer),
        ],
        FunctionCategory.Numeric,
        UsageExample: "round(amount)",
        SnippetTemplate: "round(${1:value})",
        HoverDescription: "Rounds to the nearest integer using banker's rounding (round half to even). Produces an integer result."),

        // ── Numeric: round with places (numeric, integer → decimal) ─────────────
        FunctionKind.RoundPlaces => new(kind, "round",
            "Round to N decimal places (explicit bridge: number → decimal)",
        [
            new([new(TypeKind.Integer, "value"), new(TypeKind.Integer, "places")], TypeKind.Decimal),
            new([new(TypeKind.Decimal, "value"), new(TypeKind.Integer, "places")], TypeKind.Decimal),
            new([new(TypeKind.Number,  "value"), new(TypeKind.Integer, "places")], TypeKind.Decimal),
            new([new(TypeKind.Money,   "value"), new(TypeKind.Integer, "places")], TypeKind.Money, QualifierMatch.Same),
            new([new(TypeKind.Quantity,"value"), new(TypeKind.Integer, "places")], TypeKind.Quantity, QualifierMatch.Same),
        ],
        FunctionCategory.Numeric,
        UsageExample: "round(amount, 2)",
        SnippetTemplate: "round(${1:value}, ${2:places})",
        HoverDescription: "Rounds a numeric value to N decimal places. The result is always a decimal — use to lock precision on computed values."),

        // ── Numeric: lane bridges ─────────────────────────────────────────────────────
        FunctionKind.Approximate => new(kind, "approximate",
            "Explicit bridge: decimal → number (makes precision loss visible)",
        [
            new([new(TypeKind.Decimal, "value")], TypeKind.Number),
        ],
        FunctionCategory.Numeric,
        UsageExample: "approximate(total)",
        SnippetTemplate: "approximate(${1:value})",
        HoverDescription: "Explicitly converts a decimal to a number (floating-point). Makes precision loss visible and prevents accidental precision widening."),

        // ── Numeric: power / root ───────────────────────────────────────────────
        FunctionKind.Pow => new(kind, "pow",
            "Raise base to integer exponent (same type as base)",
        [
            new([new(TypeKind.Integer, "base"), new(TypeKind.Integer, "exp")], TypeKind.Integer),
            new([new(TypeKind.Decimal, "base"), new(TypeKind.Integer, "exp")], TypeKind.Decimal),
            new([new(TypeKind.Number,  "base"), new(TypeKind.Integer, "exp")], TypeKind.Number),
        ],
        FunctionCategory.Numeric,
        UsageExample: "pow(base, 2)",
        SnippetTemplate: "pow(${1:base}, ${2:exp})",
        HoverDescription: "Raises a value to an integer power. The result type matches the base type."),

        FunctionKind.Sqrt => new(kind, "sqrt",
            "Square root → number (proof engine checks non-negativity)",
        [
            new([PSqrtInteger], TypeKind.Number,
                ProofRequirements:
                [
                    new NumericProofRequirement(new ParamSubject(PSqrtInteger), OperatorKind.GreaterThanOrEqual, 0m,
                        "Argument must be non-negative"),
                ]),
            new([PSqrtDecimal], TypeKind.Number,
                ProofRequirements:
                [
                    new NumericProofRequirement(new ParamSubject(PSqrtDecimal), OperatorKind.GreaterThanOrEqual, 0m,
                        "Argument must be non-negative"),
                ]),
            new([PSqrtNumber], TypeKind.Number,
                ProofRequirements:
                [
                    new NumericProofRequirement(new ParamSubject(PSqrtNumber), OperatorKind.GreaterThanOrEqual, 0m,
                        "Argument must be non-negative"),
                ]),
        ],
        FunctionCategory.Numeric,
        UsageExample: "sqrt(area)",
        SnippetTemplate: "sqrt(${1:value})",
        HoverDescription: "Returns the square root as a number. The proof engine verifies the input is non-negative at design time."),

        // ── String ───────────────────────────────────────────────────────────────────
        FunctionKind.Trim => new(kind, "trim", "Remove leading and trailing whitespace",
        [
            new([new(TypeKind.String, "str")], TypeKind.String),
        ],
        FunctionCategory.String,
        UsageExample: "trim(notes)",
        SnippetTemplate: "trim(${1:str})",
        HoverDescription: "Removes leading and trailing whitespace from a text value."),

        FunctionKind.StartsWith => new(kind, "startsWith", "Case-sensitive prefix test",
        [
            new([new(TypeKind.String, "str"), new(TypeKind.String, "prefix")], TypeKind.Boolean),
        ],
        FunctionCategory.String,
        UsageExample: "startsWith(email, \"info@\")",
        SnippetTemplate: "startsWith(${1:str}, ${2:prefix})",
        HoverDescription: "Returns true if the text value begins with the given prefix. Case-sensitive."),

        FunctionKind.EndsWith => new(kind, "endsWith", "Case-sensitive suffix test",
        [
            new([new(TypeKind.String, "str"), new(TypeKind.String, "suffix")], TypeKind.Boolean),
        ],
        FunctionCategory.String,
        UsageExample: "endsWith(email, \".com\")",
        SnippetTemplate: "endsWith(${1:str}, ${2:suffix})",
        HoverDescription: "Returns true if the text value ends with the given suffix. Case-sensitive."),

        FunctionKind.ToLower => new(kind, "toLower", "Lowercase (invariant culture)",
        [
            new([new(TypeKind.String, "str")], TypeKind.String),
        ],
        FunctionCategory.String,
        UsageExample: "toLower(code)",
        SnippetTemplate: "toLower(${1:str})",
        HoverDescription: "Converts a text value to lowercase using invariant culture."),

        FunctionKind.ToUpper => new(kind, "toUpper", "Uppercase (invariant culture)",
        [
            new([new(TypeKind.String, "str")], TypeKind.String),
        ],
        FunctionCategory.String,
        UsageExample: "toUpper(code)",
        SnippetTemplate: "toUpper(${1:str})",
        HoverDescription: "Converts a text value to uppercase using invariant culture."),

        FunctionKind.Left => new(kind, "left",
            "Leftmost N code units (clamped to string length)",
        [
            new([new(TypeKind.String, "str"), new(TypeKind.Integer, "n")], TypeKind.String),
        ],
        FunctionCategory.String,
        UsageExample: "left(name, 3)",
        SnippetTemplate: "left(${1:str}, ${2:n})",
        HoverDescription: "Returns the first N characters of a text value. Clamped to the string's length — never errors on short strings."),

        FunctionKind.Right => new(kind, "right",
            "Rightmost N code units (clamped to string length)",
        [
            new([new(TypeKind.String, "str"), new(TypeKind.Integer, "n")], TypeKind.String),
        ],
        FunctionCategory.String,
        UsageExample: "right(code, 4)",
        SnippetTemplate: "right(${1:str}, ${2:n})",
        HoverDescription: "Returns the last N characters of a text value. Clamped to the string's length."),

        FunctionKind.Mid => new(kind, "mid",
            "1-indexed substring (clamped); start and length must be positive integer",
        [
            new([new(TypeKind.String, "str"), new(TypeKind.Integer, "start"), new(TypeKind.Integer, "length")], TypeKind.String),
        ],
        FunctionCategory.String,
        UsageExample: "mid(code, 3, 2)",
        SnippetTemplate: "mid(${1:str}, ${2:start}, ${3:length})",
        HoverDescription: "Returns a substring starting at a 1-indexed position. Both start and length must be positive. Clamped to string bounds."),

        // ── Temporal ────────────────────────────────────────────────────────────
        FunctionKind.Now => new(kind, "now", "Current instant (UTC)",
        [
            new([], TypeKind.Instant),
        ],
        FunctionCategory.Temporal,
        UsageExample: "now()",
        SnippetTemplate: "now()",
        HoverDescription: "Returns the current UTC instant. Use .inZone(timezone) to convert to local time for display."),

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
