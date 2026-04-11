using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

internal static class PreceptExpressionRuntimeEvaluator
{
    internal sealed record EvaluationResult(bool Success, object? Value, string? Error)
    {
        internal static EvaluationResult Ok(object? value) => new(true, value, null);
        internal static EvaluationResult Fail(string error) => new(false, null, error);
    }

    public static EvaluationResult Evaluate(
        PreceptExpression expression,
        IReadOnlyDictionary<string, object?> context,
        IReadOnlyDictionary<string, PreceptField>? fieldContracts = null)
    {
        return expression switch
        {
            PreceptLiteralExpression literal => EvaluationResult.Ok(literal.Value),
            PreceptIdentifierExpression identifier => EvaluateIdentifier(identifier, context),
            PreceptParenthesizedExpression parenthesized => Evaluate(parenthesized.Inner, context, fieldContracts),
            PreceptUnaryExpression unary => EvaluateUnary(unary, context),
            PreceptBinaryExpression binary => EvaluateBinary(binary, context, fieldContracts),
            PreceptRoundExpression round => EvaluateRound(round, context),
            PreceptChoiceOrdinalExpression ordinal => EvaluateChoiceOrdinal(ordinal, context),
            _ => EvaluationResult.Fail("unsupported expression node.")
        };
    }

    private static EvaluationResult EvaluateIdentifier(PreceptIdentifierExpression identifier, IReadOnlyDictionary<string, object?> context)
    {
        // Handle three-level dotted form: EventName.ArgName.length (e.g. Submit.Name.length)
        if (identifier.Member is not null && identifier.SubMember is not null)
        {
            if (identifier.SubMember == "length")
            {
                var argKey = $"{identifier.Name}.{identifier.Member}";
                if (!context.TryGetValue(argKey, out var argStrObj))
                    return EvaluationResult.Fail($"data key '{argKey}' was not provided.");
                if (argStrObj is null)
                    return EvaluationResult.Fail($"'{argKey}.length' failed: arg is null.");
                if (argStrObj is string argStr)
                    return EvaluationResult.Ok((double)argStr.Length);
                return EvaluationResult.Fail($"'{argKey}' is not a string.");
            }
            return EvaluationResult.Fail($"unsupported sub-member '{identifier.SubMember}'.");
        }

        // Handle collection property access: Collection.count, Collection.min, Collection.max, Collection.peek
        if (identifier.Member is not null)
        {
            var collectionKey = $"__collection__{identifier.Name}";
            if (context.TryGetValue(collectionKey, out var collectionObj) && collectionObj is CollectionValue collection)
            {
                return identifier.Member switch
                {
                    "count" => EvaluationResult.Ok((double)collection.Count),
                    "min" => collection.Kind == PreceptCollectionKind.Set
                        ? (collection.Count > 0
                            ? EvaluationResult.Ok(collection.Min()!)
                            : EvaluationResult.Fail($"'{identifier.Name}.min' failed: set is empty."))
                        : EvaluationResult.Fail($"'{identifier.Name}.min' is only valid on set<T> fields."),
                    "max" => collection.Kind == PreceptCollectionKind.Set
                        ? (collection.Count > 0
                            ? EvaluationResult.Ok(collection.Max()!)
                            : EvaluationResult.Fail($"'{identifier.Name}.max' failed: set is empty."))
                        : EvaluationResult.Fail($"'{identifier.Name}.max' is only valid on set<T> fields."),
                    "peek" => collection.Kind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack
                        ? (collection.Count > 0
                            ? EvaluationResult.Ok(collection.Peek()!)
                            : EvaluationResult.Fail($"'{identifier.Name}.peek' failed: collection is empty."))
                        : EvaluationResult.Fail($"'{identifier.Name}.peek' is only valid on queue<T> or stack<T> fields."),
                    _ => EvaluationResult.Fail($"unknown collection property '{identifier.Member}'.")
                };
            }

            // Handle string .length accessor: returns UTF-16 code unit count (matches .NET string.Length).
            if (identifier.Member == "length")
            {
                if (!context.TryGetValue(identifier.Name, out var strObj))
                    return EvaluationResult.Fail($"data key '{identifier.Name}' was not provided.");
                if (strObj is null)
                    return EvaluationResult.Fail($"'{identifier.Name}.length' failed: field is null.");
                if (strObj is string str)
                    return EvaluationResult.Ok((double)str.Length);
            }
        }

        var key = identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
        if (!context.TryGetValue(key, out var value))
            return EvaluationResult.Fail($"data key '{key}' was not provided.");

        return EvaluationResult.Ok(value);
    }

    private static EvaluationResult EvaluateUnary(PreceptUnaryExpression unary, IReadOnlyDictionary<string, object?> context)
    {
        var operand = Evaluate(unary.Operand, context);
        if (!operand.Success)
            return operand;

        return unary.Operator switch
        {
            "not" => operand.Value is bool b
                ? EvaluationResult.Ok(!b)
                : EvaluationResult.Fail("operator 'not' requires boolean operand."),
            "-" => operand.Value is long l
                ? EvaluationResult.Ok(-l)
                : TryToNumber(operand.Value, out var number)
                    ? EvaluationResult.Ok(-number)
                    : EvaluationResult.Fail("unary '-' requires numeric operand."),
            _ => EvaluationResult.Fail($"unsupported unary operator '{unary.Operator}'.")
        };
    }

    private static EvaluationResult EvaluateBinary(
        PreceptBinaryExpression binary,
        IReadOnlyDictionary<string, object?> context,
        IReadOnlyDictionary<string, PreceptField>? fieldContracts = null)
    {
        if (binary.Operator == "contains")
            return EvaluateContains(binary, context);

        if (binary.Operator == "and")
        {
            var left = Evaluate(binary.Left, context, fieldContracts);
            if (!left.Success)
                return left;

            if (left.Value is not bool leftBool)
                return EvaluationResult.Fail("operator 'and' requires boolean operands.");

            if (!leftBool)
                return EvaluationResult.Ok(false);

            var right = Evaluate(binary.Right, context, fieldContracts);
            if (!right.Success)
                return right;

            return right.Value is bool rightBool
                ? EvaluationResult.Ok(rightBool)
                : EvaluationResult.Fail("operator 'and' requires boolean operands.");
        }

        if (binary.Operator == "or")
        {
            var left = Evaluate(binary.Left, context, fieldContracts);
            if (!left.Success)
                return left;

            if (left.Value is not bool leftBool)
                return EvaluationResult.Fail("operator 'or' requires boolean operands.");

            if (leftBool)
                return EvaluationResult.Ok(true);

            var right = Evaluate(binary.Right, context, fieldContracts);
            if (!right.Success)
                return right;

            return right.Value is bool rightBool
                ? EvaluationResult.Ok(rightBool)
                : EvaluationResult.Fail("operator 'or' requires boolean operands.");
        }

        var leftValue = Evaluate(binary.Left, context, fieldContracts);
        if (!leftValue.Success)
            return leftValue;

        var rightValue = Evaluate(binary.Right, context, fieldContracts);
        if (!rightValue.Success)
            return rightValue;

        var leftOperand = leftValue.Value;
        var rightOperand = rightValue.Value;

        switch (binary.Operator)
        {
            case "+":
                if (leftOperand is string leftString && rightOperand is string rightString)
                    return EvaluationResult.Ok(leftString + rightString);

                if (leftOperand is long la && rightOperand is long ra)
                    return EvaluationResult.Ok(la + ra);

                if (TryToNumber(leftOperand, out var leftNumberForAdd) && TryToNumber(rightOperand, out var rightNumberForAdd))
                    return EvaluationResult.Ok(leftNumberForAdd + rightNumberForAdd);

                return EvaluationResult.Fail("operator '+' requires number+number or string+string.");

            case "-":
                if (leftOperand is long ls && rightOperand is long rs)
                    return EvaluationResult.Ok(ls - rs);

                if (TryToNumber(leftOperand, out var leftNumberForSub) && TryToNumber(rightOperand, out var rightNumberForSub))
                    return EvaluationResult.Ok(leftNumberForSub - rightNumberForSub);

                return EvaluationResult.Fail("operator '-' requires numeric operands.");

            case "*":
                if (leftOperand is long lm && rightOperand is long rm)
                    return EvaluationResult.Ok(lm * rm);

                if (TryToNumber(leftOperand, out var leftNumberForMul) && TryToNumber(rightOperand, out var rightNumberForMul))
                    return EvaluationResult.Ok(leftNumberForMul * rightNumberForMul);

                return EvaluationResult.Fail("operator '*' requires numeric operands.");

            case "/":
                if (leftOperand is long ld && rightOperand is long rd)
                {
                    if (rd == 0L) return EvaluationResult.Fail("integer division by zero.");
                    return EvaluationResult.Ok(ld / rd); // C# truncates toward zero
                }

                if (TryToNumber(leftOperand, out var leftNumberForDiv) && TryToNumber(rightOperand, out var rightNumberForDiv))
                    return EvaluationResult.Ok(leftNumberForDiv / rightNumberForDiv);

                return EvaluationResult.Fail("operator '/' requires numeric operands.");

            case "%":
                if (leftOperand is long lmod && rightOperand is long rmod)
                {
                    if (rmod == 0L) return EvaluationResult.Fail("integer modulo by zero.");
                    return EvaluationResult.Ok(lmod % rmod);
                }

                if (TryToNumber(leftOperand, out var leftNumberForMod) && TryToNumber(rightOperand, out var rightNumberForMod))
                    return EvaluationResult.Ok(leftNumberForMod % rightNumberForMod);

                return EvaluationResult.Fail("operator '%' requires numeric operands.");

            case "==":
                if (TryToNumber(leftOperand, out var leftEq) && TryToNumber(rightOperand, out var rightEq))
                    return EvaluationResult.Ok(leftEq == rightEq);
                return EvaluationResult.Ok(Equals(leftOperand, rightOperand));

            case "!=":
                if (TryToNumber(leftOperand, out var leftNeq) && TryToNumber(rightOperand, out var rightNeq))
                    return EvaluationResult.Ok(leftNeq != rightNeq);
                return EvaluationResult.Ok(!Equals(leftOperand, rightOperand));

            case ">":
                if (leftOperand is long lgt && rightOperand is long rgt)
                    return EvaluationResult.Ok(lgt > rgt);

                if (TryToNumber(leftOperand, out var leftNumberForGt) && TryToNumber(rightOperand, out var rightNumberForGt))
                    return EvaluationResult.Ok(leftNumberForGt > rightNumberForGt);

                if (TryGetChoiceOrdinals(binary.Left, leftOperand, rightOperand, fieldContracts, out var liGt, out var riGt))
                {
                    if (liGt < 0) return EvaluationResult.Fail($"'{leftOperand}' is not a member of the ordered choice set.");
                    if (riGt < 0) return EvaluationResult.Fail($"'{rightOperand}' is not a member of the ordered choice set.");
                    return EvaluationResult.Ok(liGt > riGt);
                }

                return EvaluationResult.Fail("operator '>' requires numeric operands.");

            case ">=":
                if (leftOperand is long lgte && rightOperand is long rgte)
                    return EvaluationResult.Ok(lgte >= rgte);

                if (TryToNumber(leftOperand, out var leftNumberForGte) && TryToNumber(rightOperand, out var rightNumberForGte))
                    return EvaluationResult.Ok(leftNumberForGte >= rightNumberForGte);

                if (TryGetChoiceOrdinals(binary.Left, leftOperand, rightOperand, fieldContracts, out var liGte, out var riGte))
                {
                    if (liGte < 0) return EvaluationResult.Fail($"'{leftOperand}' is not a member of the ordered choice set.");
                    if (riGte < 0) return EvaluationResult.Fail($"'{rightOperand}' is not a member of the ordered choice set.");
                    return EvaluationResult.Ok(liGte >= riGte);
                }

                return EvaluationResult.Fail("operator '>=' requires numeric operands.");

            case "<":
                if (leftOperand is long llt && rightOperand is long rlt)
                    return EvaluationResult.Ok(llt < rlt);

                if (TryToNumber(leftOperand, out var leftNumberForLt) && TryToNumber(rightOperand, out var rightNumberForLt))
                    return EvaluationResult.Ok(leftNumberForLt < rightNumberForLt);

                if (TryGetChoiceOrdinals(binary.Left, leftOperand, rightOperand, fieldContracts, out var liLt, out var riLt))
                {
                    if (liLt < 0) return EvaluationResult.Fail($"'{leftOperand}' is not a member of the ordered choice set.");
                    if (riLt < 0) return EvaluationResult.Fail($"'{rightOperand}' is not a member of the ordered choice set.");
                    return EvaluationResult.Ok(liLt < riLt);
                }

                return EvaluationResult.Fail("operator '<' requires numeric operands.");

            case "<=":
                if (leftOperand is long llte && rightOperand is long rlte)
                    return EvaluationResult.Ok(llte <= rlte);

                if (TryToNumber(leftOperand, out var leftNumberForLte) && TryToNumber(rightOperand, out var rightNumberForLte))
                    return EvaluationResult.Ok(leftNumberForLte <= rightNumberForLte);

                if (TryGetChoiceOrdinals(binary.Left, leftOperand, rightOperand, fieldContracts, out var liLte, out var riLte))
                {
                    if (liLte < 0) return EvaluationResult.Fail($"'{leftOperand}' is not a member of the ordered choice set.");
                    if (riLte < 0) return EvaluationResult.Fail($"'{rightOperand}' is not a member of the ordered choice set.");
                    return EvaluationResult.Ok(liLte <= riLte);
                }

                return EvaluationResult.Fail("operator '<=' requires numeric operands.");

            default:
                return EvaluationResult.Fail($"unsupported binary operator '{binary.Operator}'.");
        }
    }

    /// <summary>
    /// Attempts to resolve declaration-position ordinal indices for an ordered choice field comparison.
    /// Returns true when the left-side expression is an ordered choice field and both operands are strings.
    /// </summary>
    private static bool TryGetChoiceOrdinals(
        PreceptExpression leftExpr,
        object? leftOperand,
        object? rightOperand,
        IReadOnlyDictionary<string, PreceptField>? fieldContracts,
        out int leftIdx,
        out int rightIdx)
    {
        leftIdx = rightIdx = -1;
        if (fieldContracts is null) return false;
        if (leftOperand is not string leftStr) return false;
        if (rightOperand is not string rightStr) return false;

        // Strip parentheses to find the underlying identifier.
        while (leftExpr is PreceptParenthesizedExpression p) leftExpr = p.Inner;
        if (leftExpr is not PreceptIdentifierExpression { Member: null } leftId) return false;
        if (!fieldContracts.TryGetValue(leftId.Name, out var field)) return false;
        if (field.Type != PreceptScalarType.Choice || !field.IsOrdered) return false;
        if (field.ChoiceValues is not { Count: > 0 } values) return false;

        leftIdx = FindChoiceIndex(values, leftStr);
        rightIdx = FindChoiceIndex(values, rightStr);
        return true;
    }

    private static int FindChoiceIndex(IReadOnlyList<string> values, string target)
    {
        for (var i = 0; i < values.Count; i++)
            if (string.Equals(values[i], target, StringComparison.Ordinal))
                return i;
        return -1;
    }

    private static EvaluationResult EvaluateChoiceOrdinal(
        PreceptChoiceOrdinalExpression ordinal,
        IReadOnlyDictionary<string, object?> context)
    {
        if (!context.TryGetValue(ordinal.FieldName, out var v) || v is not string s)
            return EvaluationResult.Fail($"Cannot resolve ordinal for '{ordinal.FieldName}'.");
        var idx = FindChoiceIndex(ordinal.ChoiceValues, s);
        if (idx < 0)
            return EvaluationResult.Fail($"Value '{s}' is not in the ordered choice set for '{ordinal.FieldName}'.");
        return EvaluationResult.Ok((long)idx);
    }

    private static EvaluationResult EvaluateContains(PreceptBinaryExpression binary, IReadOnlyDictionary<string, object?> context)
    {
        // Left side must be a collection identifier
        if (binary.Left is not PreceptIdentifierExpression collectionIdentifier || collectionIdentifier.Member is not null)
            return EvaluationResult.Fail("'contains' requires a collection field on the left side.");

        var collectionKey = $"__collection__{collectionIdentifier.Name}";
        if (!context.TryGetValue(collectionKey, out var collectionObj) || collectionObj is not CollectionValue collection)
            return EvaluationResult.Fail($"'{collectionIdentifier.Name}' is not a collection field.");

        var rightResult = Evaluate(binary.Right, context);
        if (!rightResult.Success)
            return rightResult;

        return EvaluationResult.Ok(collection.Contains(rightResult.Value));
    }

    private static EvaluationResult EvaluateRound(PreceptRoundExpression round, IReadOnlyDictionary<string, object?> context)
    {
        var valResult = Evaluate(round.Value, context);
        if (!valResult.Success)
            return valResult;

        if (!TryToDecimal(valResult.Value, out var d))
            return EvaluationResult.Fail("round() requires a numeric argument.");

        var result = Math.Round(d, round.Places, MidpointRounding.ToEven);
        return EvaluationResult.Ok(result);
    }

    private static bool TryToDecimal(object? value, out decimal d)
    {
        switch (value)
        {
            case decimal dec: d = dec; return true;
            case double dbl: d = (decimal)dbl; return true;
            case float flt: d = (decimal)flt; return true;
            case long l: d = l; return true;
            case int i: d = i; return true;
            case short s: d = s; return true;
            case byte b: d = b; return true;
            case sbyte sb: d = sb; return true;
            default: d = default; return false;
        }
    }

    private static bool TryToNumber(object? value, out double number)
    {
        switch (value)
        {
            case byte b: number = b; return true;
            case sbyte sb: number = sb; return true;
            case short s: number = s; return true;
            case ushort us: number = us; return true;
            case int i: number = i; return true;
            case uint ui: number = ui; return true;
            case long l: number = l; return true;
            case ulong ul: number = ul; return true;
            case float f: number = f; return true;
            case double d: number = d; return true;
            case decimal dec: number = (double)dec; return true;
            default:
                number = default;
                return false;
        }
    }
}

/// <summary>
/// Runtime backing store for collection fields (set/queue/stack).
/// This class is mutable and supports clone for working-copy semantics.
/// </summary>
public sealed class CollectionValue
{
    public PreceptCollectionKind Kind { get; }
    public PreceptScalarType InnerType { get; }

    // set<T> backed by SortedSet keyed on IComparable
    private SortedSet<object>? _set;
    // queue<T> / stack<T> backed by List (front=0 for queue, top=last for stack)
    private List<object>? _list;

    public CollectionValue(PreceptCollectionKind kind, PreceptScalarType innerType)
    {
        Kind = kind;
        InnerType = innerType;

        if (kind == PreceptCollectionKind.Set)
            _set = new SortedSet<object>(CollectionComparer.Instance);
        else
            _list = new List<object>();
    }

    private CollectionValue(PreceptCollectionKind kind, PreceptScalarType innerType, SortedSet<object>? set, List<object>? list)
    {
        Kind = kind;
        InnerType = innerType;
        _set = set;
        _list = list;
    }

    public int Count => Kind == PreceptCollectionKind.Set ? _set!.Count : _list!.Count;

    public object? Min() => _set?.Min;
    public object? Max() => _set?.Max;

    public object? Peek()
    {
        if (Kind == PreceptCollectionKind.Queue)
            return _list!.Count > 0 ? _list![0] : null;
        if (Kind == PreceptCollectionKind.Stack)
            return _list!.Count > 0 ? _list![^1] : null;
        return null;
    }

    public bool Contains(object? value)
    {
        if (value is null) return false;
        var normalized = NormalizeValue(value);

        if (Kind == PreceptCollectionKind.Set)
            return _set!.Contains(normalized);

        return _list!.Any(item => CollectionComparer.Instance.Compare(item, normalized) == 0);
    }

    /// <summary>Add to set (idempotent — no-op if duplicate).</summary>
    public void Add(object value) => _set!.Add(NormalizeValue(value));

    /// <summary>Remove from set by value (idempotent — no-op if absent).</summary>
    public void Remove(object value) => _set!.Remove(NormalizeValue(value));

    /// <summary>Enqueue to queue (append to back).</summary>
    public void Enqueue(object value) => _list!.Add(NormalizeValue(value));

    /// <summary>Dequeue from queue (remove from front). Returns false if empty.</summary>
    public bool Dequeue()
    {
        if (_list!.Count == 0) return false;
        _list.RemoveAt(0);
        return true;
    }

    /// <summary>Push onto stack (add to top/end).</summary>
    public void Push(object value) => _list!.Add(NormalizeValue(value));

    /// <summary>Pop from stack (remove from top/end). Returns false if empty.</summary>
    public bool Pop()
    {
        if (_list!.Count == 0) return false;
        _list.RemoveAt(_list.Count - 1);
        return true;
    }

    /// <summary>Clear all elements.</summary>
    public void Clear()
    {
        if (Kind == PreceptCollectionKind.Set)
            _set!.Clear();
        else
            _list!.Clear();
    }

    /// <summary>Deep-clone for working-copy semantics.</summary>
    public CollectionValue Clone()
    {
        if (Kind == PreceptCollectionKind.Set)
            return new CollectionValue(Kind, InnerType, new SortedSet<object>(_set!, CollectionComparer.Instance), null);

        return new CollectionValue(Kind, InnerType, null, new List<object>(_list!));
    }

    /// <summary>Serialize to a list for JSON output.</summary>
    public List<object> ToSerializableList()
    {
        if (Kind == PreceptCollectionKind.Set)
            return _set!.ToList();

        return new List<object>(_list!);
    }

    /// <summary>Populate from a deserialized list.</summary>
    public void LoadFrom(IEnumerable<object> items)
    {
        if (Kind == PreceptCollectionKind.Set)
        {
            _set!.Clear();
            foreach (var item in items)
                _set.Add(NormalizeValue(item));
        }
        else
        {
            _list!.Clear();
            foreach (var item in items)
                _list.Add(NormalizeValue(item));
        }
    }

    /// <summary>Normalize numeric values to double for consistent comparison.</summary>
    private static object NormalizeValue(object value)
    {
        if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or decimal)
            return Convert.ToDouble(value);
        return value;
    }

    /// <summary>Comparer that handles mixed types by converting numerics to double.</summary>
    private sealed class CollectionComparer : IComparer<object>
    {
        public static readonly CollectionComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            if (x is double xd && y is double yd)
                return xd.CompareTo(yd);

            if (x is string xs && y is string ys)
                return string.Compare(xs, ys, StringComparison.Ordinal);

            if (x is bool xb && y is bool yb)
                return xb.CompareTo(yb);

            // Fallback: convert both to string for comparison
            return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
        }
    }
}
