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

    public static EvaluationResult Evaluate(PreceptExpression expression, IReadOnlyDictionary<string, object?> context)
    {
        return expression switch
        {
            PreceptLiteralExpression literal => EvaluationResult.Ok(literal.Value),
            PreceptIdentifierExpression identifier => EvaluateIdentifier(identifier, context),
            PreceptParenthesizedExpression parenthesized => Evaluate(parenthesized.Inner, context),
            PreceptUnaryExpression unary => EvaluateUnary(unary, context),
            PreceptBinaryExpression binary => EvaluateBinary(binary, context),
            _ => EvaluationResult.Fail("unsupported expression node.")
        };
    }

    private static EvaluationResult EvaluateIdentifier(PreceptIdentifierExpression identifier, IReadOnlyDictionary<string, object?> context)
    {
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
            "!" => operand.Value is bool b
                ? EvaluationResult.Ok(!b)
                : EvaluationResult.Fail("operator '!' requires boolean operand."),
            "-" => TryToNumber(operand.Value, out var number)
                ? EvaluationResult.Ok(-number)
                : EvaluationResult.Fail("unary '-' requires numeric operand."),
            _ => EvaluationResult.Fail($"unsupported unary operator '{unary.Operator}'.")
        };
    }

    private static EvaluationResult EvaluateBinary(PreceptBinaryExpression binary, IReadOnlyDictionary<string, object?> context)
    {
        if (binary.Operator == "contains")
            return EvaluateContains(binary, context);

        if (binary.Operator == "&&")
        {
            var left = Evaluate(binary.Left, context);
            if (!left.Success)
                return left;

            if (left.Value is not bool leftBool)
                return EvaluationResult.Fail("operator '&&' requires boolean operands.");

            if (!leftBool)
                return EvaluationResult.Ok(false);

            var right = Evaluate(binary.Right, context);
            if (!right.Success)
                return right;

            return right.Value is bool rightBool
                ? EvaluationResult.Ok(rightBool)
                : EvaluationResult.Fail("operator '&&' requires boolean operands.");
        }

        if (binary.Operator == "||")
        {
            var left = Evaluate(binary.Left, context);
            if (!left.Success)
                return left;

            if (left.Value is not bool leftBool)
                return EvaluationResult.Fail("operator '||' requires boolean operands.");

            if (leftBool)
                return EvaluationResult.Ok(true);

            var right = Evaluate(binary.Right, context);
            if (!right.Success)
                return right;

            return right.Value is bool rightBool
                ? EvaluationResult.Ok(rightBool)
                : EvaluationResult.Fail("operator '||' requires boolean operands.");
        }

        var leftValue = Evaluate(binary.Left, context);
        if (!leftValue.Success)
            return leftValue;

        var rightValue = Evaluate(binary.Right, context);
        if (!rightValue.Success)
            return rightValue;

        var leftOperand = leftValue.Value;
        var rightOperand = rightValue.Value;

        switch (binary.Operator)
        {
            case "+":
                if (leftOperand is string leftString && rightOperand is string rightString)
                    return EvaluationResult.Ok(leftString + rightString);

                if (TryToNumber(leftOperand, out var leftNumberForAdd) && TryToNumber(rightOperand, out var rightNumberForAdd))
                    return EvaluationResult.Ok(leftNumberForAdd + rightNumberForAdd);

                return EvaluationResult.Fail("operator '+' requires number+number or string+string.");

            case "-":
                if (TryToNumber(leftOperand, out var leftNumberForSub) && TryToNumber(rightOperand, out var rightNumberForSub))
                    return EvaluationResult.Ok(leftNumberForSub - rightNumberForSub);

                return EvaluationResult.Fail("operator '-' requires numeric operands.");

            case "*":
                if (TryToNumber(leftOperand, out var leftNumberForMul) && TryToNumber(rightOperand, out var rightNumberForMul))
                    return EvaluationResult.Ok(leftNumberForMul * rightNumberForMul);

                return EvaluationResult.Fail("operator '*' requires numeric operands.");

            case "/":
                if (TryToNumber(leftOperand, out var leftNumberForDiv) && TryToNumber(rightOperand, out var rightNumberForDiv))
                    return EvaluationResult.Ok(leftNumberForDiv / rightNumberForDiv);

                return EvaluationResult.Fail("operator '/' requires numeric operands.");

            case "%":
                if (TryToNumber(leftOperand, out var leftNumberForMod) && TryToNumber(rightOperand, out var rightNumberForMod))
                    return EvaluationResult.Ok(leftNumberForMod % rightNumberForMod);

                return EvaluationResult.Fail("operator '%' requires numeric operands.");

            case "==":
                if (TryToNumber(leftOperand, out var leftNumberForEq) && TryToNumber(rightOperand, out var rightNumberForEq))
                    return EvaluationResult.Ok(leftNumberForEq == rightNumberForEq);

                return EvaluationResult.Ok(Equals(leftOperand, rightOperand));

            case "!=":
                if (TryToNumber(leftOperand, out var leftNumberForNeq) && TryToNumber(rightOperand, out var rightNumberForNeq))
                    return EvaluationResult.Ok(leftNumberForNeq != rightNumberForNeq);

                return EvaluationResult.Ok(!Equals(leftOperand, rightOperand));

            case ">":
                if (TryToNumber(leftOperand, out var leftNumberForGt) && TryToNumber(rightOperand, out var rightNumberForGt))
                    return EvaluationResult.Ok(leftNumberForGt > rightNumberForGt);

                return EvaluationResult.Fail("operator '>' requires numeric operands.");

            case ">=":
                if (TryToNumber(leftOperand, out var leftNumberForGte) && TryToNumber(rightOperand, out var rightNumberForGte))
                    return EvaluationResult.Ok(leftNumberForGte >= rightNumberForGte);

                return EvaluationResult.Fail("operator '>=' requires numeric operands.");

            case "<":
                if (TryToNumber(leftOperand, out var leftNumberForLt) && TryToNumber(rightOperand, out var rightNumberForLt))
                    return EvaluationResult.Ok(leftNumberForLt < rightNumberForLt);

                return EvaluationResult.Fail("operator '<' requires numeric operands.");

            case "<=":
                if (TryToNumber(leftOperand, out var leftNumberForLte) && TryToNumber(rightOperand, out var rightNumberForLte))
                    return EvaluationResult.Ok(leftNumberForLte <= rightNumberForLte);

                return EvaluationResult.Fail("operator '<=' requires numeric operands.");

            default:
                return EvaluationResult.Fail($"unsupported binary operator '{binary.Operator}'.");
        }
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
