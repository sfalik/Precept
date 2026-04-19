using System;
using System.Collections.Generic;

namespace Precept;

// Partial-class file for PreceptTypeChecker — general-purpose shared helpers.
// Contains stateless utility methods with no domain-specific validation logic.
// All other partial-class files depend on these helpers.
internal static partial class PreceptTypeChecker
{
    // ═══════════════════════════════════════════════════════════════
    // Mapping helpers
    // ═══════════════════════════════════════════════════════════════

    internal static StaticValueKind MapFieldContractKind(PreceptField field)
    {
        if (field.Type == PreceptScalarType.Choice)
        {
            var choiceKind = field.IsOrdered ? StaticValueKind.OrderedChoice : StaticValueKind.UnorderedChoice;
            return field.IsNullable ? choiceKind | StaticValueKind.Null : choiceKind;
        }
        return MapKind(field.Type, field.IsNullable);
    }

    internal static StaticValueKind MapFieldContractKind(PreceptEventArg arg)
    {
        if (arg.Type == PreceptScalarType.Choice)
        {
            var choiceKind = arg.IsOrdered ? StaticValueKind.OrderedChoice : StaticValueKind.UnorderedChoice;
            return arg.IsNullable ? choiceKind | StaticValueKind.Null : choiceKind;
        }
        return MapKind(arg.Type, arg.IsNullable);
    }

    internal static StaticValueKind MapScalarType(PreceptScalarType type) => MapScalarTypeToKind(type);

    private static StaticValueKind MapKind(PreceptScalarType type, bool isNullable)
    {
        var kind = MapScalarTypeToKind(type);
        if (isNullable)
            kind |= StaticValueKind.Null;

        return kind;
    }

    private static StaticValueKind MapScalarTypeToKind(PreceptScalarType type) => type switch
    {
        PreceptScalarType.String => StaticValueKind.String,
        PreceptScalarType.Number => StaticValueKind.Number,
        PreceptScalarType.Boolean => StaticValueKind.Boolean,
        PreceptScalarType.Null => StaticValueKind.Null,
        PreceptScalarType.Integer => StaticValueKind.Integer,
        PreceptScalarType.Decimal => StaticValueKind.Decimal,
        PreceptScalarType.Choice => StaticValueKind.String,  // choice values are strings at runtime
        _ => StaticValueKind.None
    };

    private static StaticValueKind MapLiteralKind(object? value) => value switch
    {
        null => StaticValueKind.Null,
        string => StaticValueKind.String,
        bool => StaticValueKind.Boolean,
        long => StaticValueKind.Integer,
        byte or sbyte or short or ushort or int or uint or ulong or float or double or decimal => StaticValueKind.Number,
        _ => StaticValueKind.None
    };

    // ═══════════════════════════════════════════════════════════════
    // Identifier/literal utility
    // ═══════════════════════════════════════════════════════════════

    internal static bool TryGetLiteralKind(string label, out StaticValueKind kind)
    {
        switch (label)
        {
            case "true":
            case "false":
                kind = StaticValueKind.Boolean;
                return true;
            case "null":
                kind = StaticValueKind.Null;
                return true;
            default:
                kind = StaticValueKind.None;
                return false;
        }
    }

    private static bool TryGetIdentifierKey(PreceptExpression expression, out string key)
    {
        var stripped = StripParentheses(expression);
        if (stripped is PreceptIdentifierExpression identifier)
        {
            key = identifier.Member is null
                ? identifier.Name
                : $"{identifier.Name}.{identifier.Member}";
            return true;
        }

        key = string.Empty;
        return false;
    }

    private static bool IsNullLiteral(PreceptExpression expression)
        => StripParentheses(expression) is PreceptLiteralExpression { Value: null };

    private static PreceptExpression StripParentheses(PreceptExpression expression)
    {
        while (expression is PreceptParenthesizedExpression parenthesized)
            expression = parenthesized.Inner;

        return expression;
    }

    // ═══════════════════════════════════════════════════════════════
    // Assignability/kind predicates
    // ═══════════════════════════════════════════════════════════════

    internal static bool IsAssignableKind(StaticValueKind actual, StaticValueKind expected) => IsAssignable(actual, expected);

    private static bool IsAssignable(StaticValueKind actual, StaticValueKind expected)
    {
        // Normalize choice kinds: choice values are strings at runtime and are assignment-compatible with string.
        actual = NormalizeChoiceKind(actual);
        expected = NormalizeChoiceKind(expected);

        var actualNonNull = actual & ~StaticValueKind.Null;
        var expectedNonNull = expected & ~StaticValueKind.Null;

        if (!HasFlag(expected, StaticValueKind.Null) && HasFlag(actual, StaticValueKind.Null))
            return false;

        // Integer widens to Number and Decimal (explicit widening rules from #29)
        if (actualNonNull == StaticValueKind.Integer &&
            (expectedNonNull == StaticValueKind.Number || expectedNonNull == StaticValueKind.Decimal))
            return true;

        if ((actualNonNull & ~expectedNonNull) != StaticValueKind.None)
            return false;

        if (actual == StaticValueKind.Null)
            return HasFlag(expected, StaticValueKind.Null);

        return true;
    }

    /// <summary>Normalizes OrderedChoice and UnorderedChoice to String for assignability and equality checks.
    /// Choice values are strings at runtime; the distinction only matters for ordinal comparison (>/<).
    /// </summary>
    private static StaticValueKind NormalizeChoiceKind(StaticValueKind k)
    {
        var withoutNull = k & ~StaticValueKind.Null;
        var nullBit = k & StaticValueKind.Null;
        if (withoutNull == StaticValueKind.OrderedChoice || withoutNull == StaticValueKind.UnorderedChoice)
            return StaticValueKind.String | nullBit;
        return k;
    }

    private static bool HasFlag(StaticValueKind kind, StaticValueKind flag)
        => (kind & flag) == flag;

    private static bool IsExactly(StaticValueKind kind, StaticValueKind expected)
        => kind == expected;

    /// <summary>Returns true when <paramref name="k"/> is a pure numeric kind (Number, Integer, or Decimal, non-nullable, no other flags).</summary>
    private static bool IsNumericKind(StaticValueKind k)
        => IsExactly(k, StaticValueKind.Number) || IsExactly(k, StaticValueKind.Integer) || IsExactly(k, StaticValueKind.Decimal);

    // ═══════════════════════════════════════════════════════════════
    // Formatting/message builders
    // ═══════════════════════════════════════════════════════════════

    internal static string FormatKinds(StaticValueKind kinds)
    {
        if (kinds == StaticValueKind.None)
            return "unknown";

        var labels = new List<string>(6);
        if (HasFlag(kinds, StaticValueKind.OrderedChoice)) labels.Add("ordered choice");
        else if (HasFlag(kinds, StaticValueKind.UnorderedChoice)) labels.Add("choice");
        else if (HasFlag(kinds, StaticValueKind.String)) labels.Add("string");
        if (HasFlag(kinds, StaticValueKind.Number)) labels.Add("number");
        if (HasFlag(kinds, StaticValueKind.Integer)) labels.Add("integer");
        if (HasFlag(kinds, StaticValueKind.Decimal)) labels.Add("decimal");
        if (HasFlag(kinds, StaticValueKind.Boolean)) labels.Add("boolean");
        if (HasFlag(kinds, StaticValueKind.Null)) labels.Add("null");
        return string.Join("|", labels);
    }

    /// <summary>Returns a human-readable label for a <see cref="StaticValueKind"/> (used in function diagnostic messages).</summary>
    private static string KindLabel(StaticValueKind k) => (k & ~StaticValueKind.Null) switch
    {
        StaticValueKind.Integer => "integer",
        StaticValueKind.Decimal => "decimal",
        StaticValueKind.Number => "number",
        StaticValueKind.String => "string",
        StaticValueKind.Boolean => "boolean",
        StaticValueKind.Number | StaticValueKind.Integer | StaticValueKind.Decimal => "numeric",
        StaticValueKind.Number | StaticValueKind.Integer => "number or integer",
        _ => k.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// Resolves the result kind for a numeric binary operation.
    /// Integer × Integer → Integer; Decimal × Decimal|×Integer → Decimal; anything involving Number → Number.
    /// </summary>
    private static StaticValueKind ResolveNumericResultKind(StaticValueKind left, StaticValueKind right)
    {
        if (IsExactly(left, StaticValueKind.Integer) && IsExactly(right, StaticValueKind.Integer))
            return StaticValueKind.Integer;
        if ((IsExactly(left, StaticValueKind.Decimal) || IsExactly(left, StaticValueKind.Integer)) &&
            (IsExactly(right, StaticValueKind.Decimal) || IsExactly(right, StaticValueKind.Integer)))
            return StaticValueKind.Decimal;
        return StaticValueKind.Number;
    }

    /// <summary>Returns true when assigning a Number or Decimal to an Integer target (narrowing, requires explicit conversion).</summary>
    private static bool IsNarrowingToInteger(StaticValueKind actual, StaticValueKind expected)
    {
        var expectedNonNull = expected & ~StaticValueKind.Null;
        var actualNonNull = actual & ~StaticValueKind.Null;
        return expectedNonNull == StaticValueKind.Integer &&
               (actualNonNull == StaticValueKind.Number || actualNonNull == StaticValueKind.Decimal);
    }

    /// <summary>
    /// Returns a numeric-specific author-oriented mismatch message for C39,
    /// or <see langword="null"/> when the mismatch is not between numeric kinds.
    /// </summary>
    private static string? TryBuildNumericMismatchMessage(
        StaticValueKind actual, StaticValueKind expected, string expectedLabel)
    {
        var actualBase   = actual   & ~StaticValueKind.Null;
        var expectedBase = expected & ~StaticValueKind.Null;

        // decimal → number: intentionally unsupported (would silently discard precision)
        if (actualBase == StaticValueKind.Decimal && expectedBase == StaticValueKind.Number)
            return $"{expectedLabel} type mismatch: decimal cannot be assigned to number — " +
                   "this conversion is intentionally unsupported to prevent silent precision loss. " +
                   "Change the field type to decimal, or use floor(), ceil(), truncate(), or round() " +
                   "to drop the fractional part explicitly.";

        // number → decimal: requires explicit authored normalization path
        if (actualBase == StaticValueKind.Number && expectedBase == StaticValueKind.Decimal)
            return $"{expectedLabel} type mismatch: number cannot be implicitly assigned to decimal. " +
                   "Use round(expr, N) to normalize to a specific decimal precision, " +
                   "or change the field type to number.";

        return null;
    }

    /// <summary>
    /// Builds a C60 narrowing-assignment message that advertises only conversion functions
    /// whose return type is <c>integer</c> for the given source kind.
    /// <list type="bullet">
    ///   <item><c>floor()</c>, <c>ceil()</c>, <c>truncate()</c> — return <c>integer</c> for both <c>number</c> and <c>decimal</c> sources.</item>
    ///   <item><c>round(decimal)</c> — returns <c>integer</c>; advertised for <c>decimal</c> sources.</item>
    ///   <item><c>round(number)</c> — returns <c>integer</c>; advertised for <c>number</c> sources.</item>
    /// </list>
    /// </summary>
    private static string BuildC60Message(StaticValueKind actualKind, string fieldName)
    {
        var actualLabel = FormatKinds(actualKind);
        return $"Narrowing assignment: {actualLabel} cannot be implicitly narrowed to integer field '{fieldName}'. Use floor(), ceil(), truncate(), or round() to produce an integer value.";
    }

    /// <summary>
    /// Builds a C79 branch-type-mismatch message, providing numeric widening guidance
    /// when both branches are numeric kinds that do not unify.
    /// </summary>
    private static string BuildC79Message(StaticValueKind thenKind, StaticValueKind elseKind)
    {
        var thenBase = thenKind & ~StaticValueKind.Null;
        var elseBase = elseKind & ~StaticValueKind.Null;

        var bothNumeric = IsNumericKind(thenBase) && IsNumericKind(elseBase);
        if (bothNumeric)
            return DiagnosticCatalog.C79.FormatMessage(
                ("thenType", FormatKinds(thenKind)),
                ("elseType", FormatKinds(elseKind)),
                ("hint", "integer widens to both number and decimal, but number and decimal " +
                         "do not unify with each other. Make both branches the same numeric kind."));

        return DiagnosticCatalog.C79.FormatMessage(
            ("thenType", FormatKinds(thenKind)),
            ("elseType", FormatKinds(elseKind)),
            ("hint", "Make both branches the same scalar type."));
    }

    // ═══════════════════════════════════════════════════════════════
    // Copy helpers (copy-on-write proof context builders)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed relational fact store.
    /// Used by <see cref="TryApplyNumericComparisonNarrowing"/> to produce copy-on-write updates.
    /// </summary>
    private static Dictionary<LinearForm, RelationalFact> CopyRelationalFacts(GlobalProofContext context)
    {
        var copy = new Dictionary<LinearForm, RelationalFact>();
        foreach (var kvp in context.RelationalFacts)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed field interval store.
    /// Used for copy-on-write propagation when constructing derived <see cref="GlobalProofContext"/> instances.
    /// </summary>
    private static Dictionary<string, NumericInterval> CopyFieldIntervals(GlobalProofContext context)
    {
        var copy = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
        foreach (var kvp in context.FieldIntervals)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed numeric flags store.
    /// </summary>
    private static Dictionary<string, NumericFlags> CopyFlags(GlobalProofContext context)
    {
        var copy = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
        foreach (var kvp in context.Flags)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    /// <summary>
    /// Returns a mutable copy of <paramref name="context"/>'s typed expression-level interval store.
    /// </summary>
    private static Dictionary<LinearForm, NumericInterval> CopyExprFacts(GlobalProofContext context)
    {
        var copy = new Dictionary<LinearForm, NumericInterval>();
        foreach (var kvp in context.ExprFacts)
            copy[kvp.Key] = kvp.Value;
        return copy;
    }

    // ═══════════════════════════════════════════════════════════════
    // Symbol/row builders
    // ═══════════════════════════════════════════════════════════════

    private static IEnumerable<string> ExpandRowStates(PreceptTransitionRow row, IReadOnlyList<string> allStates)
    {
        if (string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase))
            return allStates;

        return [row.FromState];
    }

    private static Dictionary<string, StaticValueKind> BuildSymbolKinds(
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName,
        IReadOnlyList<PreceptCollectionField> collectionFields,
        IReadOnlyDictionary<string, StaticValueKind>? stateSymbols)
    {
        var symbols = stateSymbols is not null
            ? new Dictionary<string, StaticValueKind>(stateSymbols, StringComparer.Ordinal)
            : new Dictionary<string, StaticValueKind>(dataFieldKinds, StringComparer.Ordinal);

        if (eventArgKinds.TryGetValue(eventName, out var eventArgs))
        {
            foreach (var pair in eventArgs)
            {
                // Only dotted form (EventName.ArgName) is valid in transition-row scope.
                // Bare arg names are valid only in event-ensure scope (see BuildEventEnsureSymbols).
                symbols[$"{eventName}.{pair.Key}"] = pair.Value;
                if (HasFlag(pair.Value, StaticValueKind.String))
                    symbols[$"{eventName}.{pair.Key}.length"] = StaticValueKind.Number;
            }
        }

        foreach (var col in collectionFields)
        {
            var innerKind = MapScalarTypeToKind(col.InnerType);
            symbols[$"{col.Name}.count"] = StaticValueKind.Number;

            if (col.CollectionKind == PreceptCollectionKind.Set)
            {
                symbols[$"{col.Name}.min"] = innerKind;
                symbols[$"{col.Name}.max"] = innerKind;
            }

            if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                symbols[$"{col.Name}.peek"] = innerKind;
        }

        foreach (var pair in dataFieldKinds)
        {
            if (HasFlag(pair.Value, StaticValueKind.String))
                symbols[$"{pair.Key}.length"] = StaticValueKind.Number;
        }

        return symbols;
    }
}
