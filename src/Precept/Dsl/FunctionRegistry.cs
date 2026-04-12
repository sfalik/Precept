using System.Collections.Generic;

namespace Precept;

/// <summary>
/// Constraint on a function argument beyond its type.
/// </summary>
internal enum FunctionArgConstraint
{
    None,
    MustBeIntegerLiteral
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

        Register(new FunctionDefinition("abs", "Returns the absolute value.",
        [
            new FunctionOverload([new FunctionParameter("value", Numeric)], Numeric)
        ]));

        Register(new FunctionDefinition("floor", "Rounds toward negative infinity, returning an integer.",
        [
            new FunctionOverload([new FunctionParameter("value", Numeric)], StaticValueKind.Integer)
        ]));

        Register(new FunctionDefinition("ceil", "Rounds toward positive infinity, returning an integer.",
        [
            new FunctionOverload([new FunctionParameter("value", Numeric)], StaticValueKind.Integer)
        ]));

        Register(new FunctionDefinition("round", "Rounds a numeric value. 1-arg: banker's rounding to nearest integer. 2-arg: rounds to specified decimal places.",
        [
            // round(numeric) → decimal  (1-arg, banker's rounding to nearest integer)
            new FunctionOverload(
                [new FunctionParameter("value", Numeric)],
                StaticValueKind.Decimal),
            // round(numeric, places) → decimal  (2-arg, precision rounding)
            new FunctionOverload(
                [
                    new FunctionParameter("value", Numeric),
                    new FunctionParameter("places", StaticValueKind.Number | StaticValueKind.Integer, FunctionArgConstraint.MustBeIntegerLiteral)
                ],
                StaticValueKind.Decimal)
        ]));

        Register(new FunctionDefinition("truncate", "Truncates toward zero, returning an integer.",
        [
            new FunctionOverload([new FunctionParameter("value", Numeric)], StaticValueKind.Integer)
        ]));

        // ── Numeric: aggregation / comparison ───────────────────

        Register(new FunctionDefinition("min", "Returns the smallest of two or more values.",
        [
            new FunctionOverload(
                [new FunctionParameter("a", Numeric), new FunctionParameter("b", Numeric)],
                Numeric, MinArity: 2)
        ]));

        Register(new FunctionDefinition("max", "Returns the largest of two or more values.",
        [
            new FunctionOverload(
                [new FunctionParameter("a", Numeric), new FunctionParameter("b", Numeric)],
                Numeric, MinArity: 2)
        ]));

        Register(new FunctionDefinition("clamp", "Constrains a value to a range [min, max].",
        [
            new FunctionOverload(
                [new FunctionParameter("value", Numeric), new FunctionParameter("min", Numeric), new FunctionParameter("max", Numeric)],
                Numeric)
        ]));

        // ── Numeric: exponentiation ─────────────────────────────

        Register(new FunctionDefinition("pow", "Raises a value to an integer power.",
        [
            new FunctionOverload(
                [new FunctionParameter("base", Numeric), new FunctionParameter("exponent", StaticValueKind.Integer)],
                Numeric)
        ]));

        Register(new FunctionDefinition("sqrt", "Returns the square root. Requires non-negative argument (compile-time proof).",
        [
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.Number | StaticValueKind.Decimal)],
                StaticValueKind.Number | StaticValueKind.Decimal)
        ]));

        // ── String: case / whitespace ───────────────────────────

        Register(new FunctionDefinition("toLower", "Converts to lowercase using invariant culture.",
        [
            new FunctionOverload([new FunctionParameter("value", StaticValueKind.String)], StaticValueKind.String)
        ]));

        Register(new FunctionDefinition("toUpper", "Converts to uppercase using invariant culture.",
        [
            new FunctionOverload([new FunctionParameter("value", StaticValueKind.String)], StaticValueKind.String)
        ]));

        Register(new FunctionDefinition("trim", "Removes leading and trailing whitespace.",
        [
            new FunctionOverload([new FunctionParameter("value", StaticValueKind.String)], StaticValueKind.String)
        ]));

        // ── String: prefix / suffix ─────────────────────────────

        Register(new FunctionDefinition("startsWith", "Tests whether a string starts with a prefix. Case-sensitive.",
        [
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.String), new FunctionParameter("prefix", StaticValueKind.String)],
                StaticValueKind.Boolean)
        ]));

        Register(new FunctionDefinition("endsWith", "Tests whether a string ends with a suffix. Case-sensitive.",
        [
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.String), new FunctionParameter("suffix", StaticValueKind.String)],
                StaticValueKind.Boolean)
        ]));

        // ── String: extraction ──────────────────────────────────

        Register(new FunctionDefinition("left", "Returns the leftmost N characters. 1-indexed, clamping.",
        [
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.String), new FunctionParameter("count", Numeric)],
                StaticValueKind.String)
        ]));

        Register(new FunctionDefinition("right", "Returns the rightmost N characters. Clamping.",
        [
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.String), new FunctionParameter("count", Numeric)],
                StaticValueKind.String)
        ]));

        Register(new FunctionDefinition("mid", "Returns a substring starting at position for a given length. 1-indexed, clamping.",
        [
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.String), new FunctionParameter("start", Numeric), new FunctionParameter("length", Numeric)],
                StaticValueKind.String)
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
