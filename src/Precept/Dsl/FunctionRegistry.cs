using System.Collections.Generic;

namespace Precept;

/// <summary>
/// Constraint on a function argument beyond its type.
/// </summary>
internal enum FunctionArgConstraint
{
    None,
    MustBeIntegerLiteral,
    RequiresNonNegativeProof
}

/// <summary>
/// Describes a single parameter of a function overload.
/// </summary>
internal sealed record FunctionParameter(
    string Name,
    StaticValueKind AcceptedTypes,
    FunctionArgConstraint Constraint = FunctionArgConstraint.None);

/// <summary>
/// Describes one overload of a built-in function.
/// </summary>
internal sealed record FunctionOverload(
    FunctionParameter[] Parameters,
    StaticValueKind ReturnType,
    int? MinArity = null);

/// <summary>
/// Describes a built-in function with all its overloads.
/// </summary>
internal sealed record FunctionDefinition(
    string Name,
    string Description,
    FunctionOverload[] Overloads);

/// <summary>
/// Registry of all built-in functions available in the Precept DSL.
/// Designed for multiple overloads, variable arity, return-type lookup,
/// argument-type validation, and special argument constraints.
/// </summary>
internal static class FunctionRegistry
{
    private static readonly Dictionary<string, FunctionDefinition> Functions = new(System.StringComparer.Ordinal);

    private static readonly StaticValueKind Numeric =
        StaticValueKind.Number | StaticValueKind.Integer | StaticValueKind.Decimal;

    static FunctionRegistry()
    {
        // ── Numeric: rounding / truncation ──────────────────────
        // Overloads ordered by specificity: integer → decimal → number.
        // Resolution picks the first arity-and-type match.

        Register(new FunctionDefinition("abs", "Returns the absolute value.",
        [
            new([new("value", StaticValueKind.Integer)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Decimal),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Number),
        ]));

        Register(new FunctionDefinition("floor", "Rounds toward negative infinity, returning an integer.",
        [
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Integer),
        ]));

        Register(new FunctionDefinition("ceil", "Rounds toward positive infinity, returning an integer.",
        [
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Integer),
        ]));

        Register(new FunctionDefinition("round", "Rounds a numeric value. 1-arg: banker's rounding to nearest integer. 2-arg: rounds to specified decimal places.",
        [
            // 1-arg: type-specific overloads
            new([new("value", StaticValueKind.Integer)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Number),
            // 2-arg: precision rounding (accepts any numeric, returns decimal)
            new(
            [
                new("value", Numeric),
                new("places", StaticValueKind.Number | StaticValueKind.Integer, FunctionArgConstraint.MustBeIntegerLiteral),
            ], StaticValueKind.Decimal),
        ]));

        Register(new FunctionDefinition("truncate", "Truncates toward zero, returning an integer.",
        [
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Integer),
        ]));

        // ── Numeric: aggregation / comparison ───────────────────

        Register(new FunctionDefinition("min", "Returns the smallest of two or more values.",
        [
            new([new("value", StaticValueKind.Integer)], StaticValueKind.Integer, MinArity: 2),
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Decimal, MinArity: 2),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Number, MinArity: 2),
        ]));

        Register(new FunctionDefinition("max", "Returns the largest of two or more values.",
        [
            new([new("value", StaticValueKind.Integer)], StaticValueKind.Integer, MinArity: 2),
            new([new("value", StaticValueKind.Decimal)], StaticValueKind.Decimal, MinArity: 2),
            new([new("value", StaticValueKind.Number)], StaticValueKind.Number, MinArity: 2),
        ]));

        Register(new FunctionDefinition("clamp", "Constrains a value to a range [min, max].",
        [
            new([new("value", StaticValueKind.Integer), new("min", StaticValueKind.Integer), new("max", StaticValueKind.Integer)], StaticValueKind.Integer),
            new([new("value", StaticValueKind.Decimal), new("min", StaticValueKind.Decimal), new("max", StaticValueKind.Decimal)], StaticValueKind.Decimal),
            new([new("value", StaticValueKind.Number), new("min", StaticValueKind.Number), new("max", StaticValueKind.Number)], StaticValueKind.Number),
        ]));

        // ── Numeric: exponentiation ─────────────────────────────

        Register(new FunctionDefinition("pow", "Raises a value to an integer power.",
        [
            new([new("base", StaticValueKind.Integer), new("exponent", StaticValueKind.Integer)], StaticValueKind.Integer),
            new([new("base", StaticValueKind.Decimal), new("exponent", StaticValueKind.Integer)], StaticValueKind.Decimal),
            new([new("base", StaticValueKind.Number), new("exponent", StaticValueKind.Integer)], StaticValueKind.Number),
        ]));

        Register(new FunctionDefinition("sqrt", "Returns the square root. Requires non-negative argument (compile-time proof).",
        [
            new([new("value", StaticValueKind.Decimal, FunctionArgConstraint.RequiresNonNegativeProof)], StaticValueKind.Decimal),
            new([new("value", StaticValueKind.Number, FunctionArgConstraint.RequiresNonNegativeProof)], StaticValueKind.Number),
        ]));

        // ── String: case / whitespace ───────────────────────────

        Register(new FunctionDefinition("toLower", "Converts to lowercase using invariant culture.",
        [
            new([new("value", StaticValueKind.String)], StaticValueKind.String),
        ]));

        Register(new FunctionDefinition("toUpper", "Converts to uppercase using invariant culture.",
        [
            new([new("value", StaticValueKind.String)], StaticValueKind.String),
        ]));

        Register(new FunctionDefinition("trim", "Removes leading and trailing whitespace.",
        [
            new([new("value", StaticValueKind.String)], StaticValueKind.String),
        ]));

        // ── String: prefix / suffix ─────────────────────────────

        Register(new FunctionDefinition("startsWith", "Tests whether a string starts with a prefix. Case-sensitive.",
        [
            new([new("value", StaticValueKind.String), new("prefix", StaticValueKind.String)], StaticValueKind.Boolean),
        ]));

        Register(new FunctionDefinition("endsWith", "Tests whether a string ends with a suffix. Case-sensitive.",
        [
            new([new("value", StaticValueKind.String), new("suffix", StaticValueKind.String)], StaticValueKind.Boolean),
        ]));

        // ── String: extraction ──────────────────────────────────

        Register(new FunctionDefinition("left", "Returns the leftmost N characters. Clamping.",
        [
            new([new("value", StaticValueKind.String), new("count", StaticValueKind.Number | StaticValueKind.Integer)], StaticValueKind.String),
        ]));

        Register(new FunctionDefinition("right", "Returns the rightmost N characters. Clamping.",
        [
            new([new("value", StaticValueKind.String), new("count", StaticValueKind.Number | StaticValueKind.Integer)], StaticValueKind.String),
        ]));

        Register(new FunctionDefinition("mid", "Returns a substring starting at position for a given length. 1-indexed, clamping.",
        [
            new(
            [
                new("value", StaticValueKind.String),
                new("start", StaticValueKind.Number | StaticValueKind.Integer),
                new("length", StaticValueKind.Number | StaticValueKind.Integer),
            ], StaticValueKind.String),
        ]));
    }

    public static bool IsFunction(string name) => Functions.ContainsKey(name);

    public static bool TryGetFunction(string name, out FunctionDefinition definition)
        => Functions.TryGetValue(name, out definition!);

    public static IReadOnlyCollection<string> FunctionNames => Functions.Keys;

    public static IReadOnlyCollection<FunctionDefinition> AllFunctions => Functions.Values;

    private static void Register(FunctionDefinition definition)
        => Functions[definition.Name] = definition;
}
