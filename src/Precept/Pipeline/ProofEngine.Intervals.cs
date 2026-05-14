using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // Interval proof methods are in this file

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 2: Interval Computation and Obligation Collection
    // ════════════════════════════════════════════════════════════════════════

    private static NumericInterval IntervalOf(TypedExpression expr, SemanticIndex semantics)
        => IntervalOfNarrowed(expr, semantics, null);

    private static NumericInterval IntervalOfNarrowed(
        TypedExpression expr,
        SemanticIndex semantics,
        ImmutableDictionary<string, NumericInterval>? narrowed)
    {
        switch (expr)
        {
            case TypedLiteral { Value: decimal d }:
                return NumericInterval.Point(d);
            case TypedLiteral { Value: int i }:
                return NumericInterval.Point((decimal)i);
            case TypedLiteral { Value: long l }:
                return NumericInterval.Point((decimal)l);

            case TypedFieldRef fieldRef:
                if (narrowed is not null && narrowed.TryGetValue(fieldRef.FieldName, out var narrowedInterval))
                    return narrowedInterval;
                return ExtractFieldInterval(fieldRef.FieldName, semantics);

            case TypedArgRef argRef:
                return ExtractArgInterval(argRef.ArgName, argRef.EventName, semantics);

            case TypedBinaryOp bin:
            {
                var opMeta = Operations.GetMeta(bin.ResolvedOp);
                if (opMeta is BinaryOperationMeta bom && bom.IntervalTransfer is { } transfer)
                {
                    var leftInterval = IntervalOfNarrowed(bin.Left, semantics, narrowed);
                    var rightInterval = IntervalOfNarrowed(bin.Right, semantics, narrowed);
                    return transfer(leftInterval, rightInterval);
                }
                return NumericInterval.Unbounded;
            }

            case TypedUnaryOp un:
            {
                var opMeta = Operations.GetMeta(un.ResolvedOp);
                if (opMeta is UnaryOperationMeta uom && uom.IntervalTransfer is { } transfer)
                    return transfer(IntervalOfNarrowed(un.Operand, semantics, narrowed));
                return NumericInterval.Unbounded;
            }

            case TypedFunctionCall call:
            {
                var overload = ResolveFunctionOverload(call);
                if (overload?.IntervalTransfer is { } transfer)
                {
                    var argIntervals = call.Arguments
                        .Select(a => IntervalOfNarrowed(a, semantics, narrowed))
                        .ToArray();
                    return transfer(argIntervals);
                }
                return NumericInterval.Unbounded;
            }

            case TypedConditional cond:
            {
                var thenInterval = IntervalOfNarrowed(cond.ThenBranch, semantics, narrowed);
                var elseInterval = IntervalOfNarrowed(cond.ElseBranch, semantics, narrowed);
                return thenInterval.Union(elseInterval);
            }

            default:
                return NumericInterval.Unbounded;
        }
    }

    private static NumericInterval ExtractFieldInterval(string fieldName, SemanticIndex semantics)
    {
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field))
            return NumericInterval.Unbounded;
        var (min, max) = GetFieldBounds(field);
        if (!min.HasValue && !max.HasValue) return NumericInterval.Unbounded;
        return new NumericInterval(min ?? decimal.MinValue, max ?? decimal.MaxValue);
    }

    private static NumericInterval ExtractArgInterval(string argName, string eventName, SemanticIndex semantics)
    {
        if (!semantics.EventsByName.TryGetValue(eventName, out var evt))
            return NumericInterval.Unbounded;

        foreach (var arg in evt.Args)
        {
            if (!string.Equals(arg.Name, argName, StringComparison.Ordinal))
                continue;

            if (!arg.DeclaredMin.HasValue && !arg.DeclaredMax.HasValue)
                return NumericInterval.Unbounded;
            return new NumericInterval(arg.DeclaredMin ?? decimal.MinValue, arg.DeclaredMax ?? decimal.MaxValue);
        }
        return NumericInterval.Unbounded;
    }

    internal static (decimal? min, decimal? max) GetFieldBounds(TypedField field)
        => (field.DeclaredMin, field.DeclaredMax);

    private static bool TryIntervalContainmentProof(
        ProofObligation obligation,
        SemanticIndex semantics,
        out NumericInterval? computedInterval)
    {
        computedInterval = null;
        if (obligation.Requirement is not IntervalContainmentProofRequirement intervalReq)
            return false;

        var resultInterval = IntervalOf(obligation.Site, semantics);
        computedInterval = resultInterval;

        if (resultInterval.IsUnbounded) return false;

        if (intervalReq.DeclaredMin.HasValue && resultInterval.Min < intervalReq.DeclaredMin.Value)
            return false;
        if (intervalReq.DeclaredMax.HasValue && resultInterval.Max > intervalReq.DeclaredMax.Value)
            return false;

        return true;
    }

    private static bool TryIntervalContainmentProofNarrowed(
        ProofObligation obligation,
        SemanticIndex semantics,
        out NumericInterval? computedInterval)
    {
        computedInterval = null;
        if (obligation.Requirement is not IntervalContainmentProofRequirement intervalReq)
            return false;

        var narrowed = BuildNarrowedIntervals(obligation, semantics);
        var resultInterval = IntervalOfNarrowed(obligation.Site, semantics, narrowed);
        computedInterval = resultInterval;

        if (resultInterval.IsUnbounded) return false;

        if (intervalReq.DeclaredMin.HasValue && resultInterval.Min < intervalReq.DeclaredMin.Value)
            return false;
        if (intervalReq.DeclaredMax.HasValue && resultInterval.Max > intervalReq.DeclaredMax.Value)
            return false;

        return true;
    }

    private static ImmutableDictionary<string, NumericInterval>? BuildNarrowedIntervals(
        ProofObligation obligation,
        SemanticIndex semantics)
    {
        var guard = obligation.Context switch
        {
            TransitionRowContext t => t.Row.Guard,
            StateHookContext s => s.Hook.Guard,
            _ => null
        };
        if (guard is null) return null;

        var branches = ExtractGuardBranches(guard);
        if (branches.IsEmpty) return null;

        // Use the first branch for now (Slice 3 handles single-branch guards)
        var builder = ImmutableDictionary.CreateBuilder<string, NumericInterval>(StringComparer.Ordinal);

        foreach (var branch in branches)
        {
            var branchNarrowings = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);

            foreach (var gc in branch)
            {
                if (gc.IsPresenceCheck || gc.Value is null) continue;

                var baseInterval = ExtractFieldInterval(gc.Field, semantics);
                if (!branchNarrowings.TryGetValue(gc.Field, out var current))
                    current = baseInterval.IsUnbounded
                        ? new NumericInterval(decimal.MinValue, decimal.MaxValue)
                        : baseInterval;

                var value = gc.Value.Value;
                current = gc.Comparison switch
                {
                    OperatorKind.GreaterThanOrEqual => new NumericInterval(Math.Max(current.Min, value), current.Max),
                    OperatorKind.GreaterThan => new NumericInterval(Math.Max(current.Min, value), current.Max),
                    OperatorKind.LessThanOrEqual => new NumericInterval(current.Min, Math.Min(current.Max, value)),
                    OperatorKind.LessThan => new NumericInterval(current.Min, Math.Min(current.Max, value)),
                    _ => current
                };
                branchNarrowings[gc.Field] = current;
            }

            foreach (var (field, interval) in branchNarrowings)
            {
                if (builder.TryGetValue(field, out var existing))
                    builder[field] = existing.Union(interval);
                else
                    builder[field] = interval;
            }
        }

        return builder.Count > 0 ? builder.ToImmutable() : null;
    }
}
