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
    FunctionOverload[] Overloads);

/// <summary>
/// Registry of all built-in functions available in the Precept DSL.
/// Designed for multiple overloads, variable arity, return-type lookup,
/// argument-type validation, and special argument constraints.
/// </summary>
internal static class FunctionRegistry
{
    private static readonly Dictionary<string, FunctionDefinition> Functions = new(System.StringComparer.Ordinal);

    static FunctionRegistry()
    {
        Register(new FunctionDefinition("round",
        [
            // round(numeric) → decimal  (1-arg, banker's rounding to nearest integer)
            new FunctionOverload(
                [new FunctionParameter("value", StaticValueKind.Number | StaticValueKind.Integer | StaticValueKind.Decimal)],
                StaticValueKind.Decimal),
            // round(numeric, places) → decimal  (2-arg, precision rounding)
            new FunctionOverload(
                [
                    new FunctionParameter("value", StaticValueKind.Number | StaticValueKind.Integer | StaticValueKind.Decimal),
                    new FunctionParameter("places", StaticValueKind.Number | StaticValueKind.Integer, FunctionArgConstraint.MustBeIntegerLiteral)
                ],
                StaticValueKind.Decimal)
        ]));
    }

    public static bool IsFunction(string name) => Functions.ContainsKey(name);

    public static bool TryGetFunction(string name, out FunctionDefinition definition)
        => Functions.TryGetValue(name, out definition!);

    public static IReadOnlyCollection<string> FunctionNames => Functions.Keys;

    private static void Register(FunctionDefinition definition)
        => Functions[definition.Name] = definition;
}
