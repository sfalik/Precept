using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // Interval proof methods are in this file

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 2: Interval Computation and Obligation Collection
    // ════════════════════════════════════════════════════════════════════════

    private static NumericInterval IntervalOf(TypedExpression expr, SemanticIndex semantics)
    {
        var interval = IntervalOfNarrowed(expr, semantics, null);
        return ApplyStaticUnitScaling(expr, interval);
    }

    private static NumericInterval IntervalOfNarrowed(
        TypedExpression expr,
        SemanticIndex semantics,
        ImmutableDictionary<string, NumericInterval>? narrowed)
    {
        switch (expr)
        {
            case TypedLiteral literal when TryExtractNumericLiteralMagnitude(literal.Value, out var literalMagnitude):
                return NumericInterval.Point(literalMagnitude);

            case TypedTypedConstant typedConstant when TryExtractTypedConstantMagnitudeRaw(typedConstant.ParsedValue, out var typedConstantMagnitude):
                return NumericInterval.Point(typedConstantMagnitude);

            case InterpolatedTypedConstant interpolated:
            {
                if (interpolated.Slots.Length == 1)
                {
                    var slot = interpolated.Slots[0];
                    if (slot.SlotKind is InterpolationSlotKind.Magnitude or InterpolationSlotKind.WholeValue)
                        return IntervalOfNarrowed(slot.Expression, semantics, narrowed);
                }

                return NumericInterval.Unbounded;
            }

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

    private static bool TryExtractNumericLiteralMagnitude(object? value, out decimal magnitude)
    {
        switch (value)
        {
            case decimal d:
                magnitude = d;
                return true;
            case int i:
                magnitude = i;
                return true;
            case long l:
                magnitude = l;
                return true;
            case ITuple tuple when tuple.Length > 0:
            {
                switch (tuple)
                {
                    case ValueTuple<decimal, UcumParsedUnit?> quantity:
                        magnitude = TypedConstantNormalizer.NormalizeQuantity(quantity.Item1, quantity.Item2);
                        return true;
                    case ValueTuple<decimal, object?, UcumParsedUnit?> price:
                        magnitude = TypedConstantNormalizer.NormalizePrice(price.Item1, price.Item3);
                        return true;
                }

                return TryExtractNumericLiteralMagnitude(tuple[0], out magnitude);
            }
            default:
                magnitude = default;
                return false;
        }
    }

    private static bool TryExtractTypedConstantMagnitudeRaw(object? parsedValue, out decimal magnitude)
    {
        switch (parsedValue)
        {
            case decimal decimalValue:
                magnitude = decimalValue;
                return true;
            case int intValue:
                magnitude = intValue;
                return true;
            case long longValue:
                magnitude = longValue;
                return true;
            case ITuple tuple when tuple.Length > 0 && tuple[0] is decimal tupleMagnitude:
                magnitude = tupleMagnitude;
                return true;
            default:
                magnitude = default;
                return false;
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

            var min = arg.NormalizedDeclaredMin ?? arg.DeclaredMin;
            var max = arg.NormalizedDeclaredMax ?? arg.DeclaredMax;

            if (!min.HasValue && !max.HasValue)
                return NumericInterval.Unbounded;
            return new NumericInterval(min ?? decimal.MinValue, max ?? decimal.MaxValue);
        }
        return NumericInterval.Unbounded;
    }

    internal static (decimal? min, decimal? max) GetFieldBounds(TypedField field)
    {
        decimal? min = null;
        decimal? max = null;

        foreach (var modifierKind in field.Modifiers.Concat(field.ImpliedModifiers))
        {
            if (Modifiers.GetMeta(modifierKind) is not ValueModifierMeta modifierMeta)
                continue;
            if (!ModifierAppliesToField(modifierMeta, field))
                continue;

            foreach (var satisfaction in modifierMeta.ProofSatisfactions.OfType<ProofSatisfaction.Numeric>())
            {
                if (satisfaction.Projection is not SatisfactionProjection.SelfValue)
                    continue;
                if (satisfaction.Bound is not NumericBoundSource.DeclarationValue)
                    continue;
                if (!TryResolveNumericBoundValue(field, modifierKind, satisfaction.Bound, out var boundValue))
                    continue;

                switch (satisfaction.Comparison)
                {
                    case OperatorKind.GreaterThanOrEqual:
                        min = min.HasValue ? Math.Max(min.Value, boundValue) : boundValue;
                        break;
                    case OperatorKind.LessThanOrEqual:
                        max = max.HasValue ? Math.Min(max.Value, boundValue) : boundValue;
                        break;
                }
            }
        }

        return (min, max);
    }

    private static bool ModifierAppliesToField(ValueModifierMeta modifierMeta, TypedField field)
    {
        if (modifierMeta.ApplicableTo.Length == 0)
            return true;

        foreach (var target in modifierMeta.ApplicableTo)
        {
            if (target.Kind is not null && target.Kind != field.ResolvedType)
                continue;

            if (target is ModifiedTypeTarget modifiedTarget)
            {
                if (!modifiedTarget.RequiredModifiers.All(required =>
                        field.Modifiers.Contains(required) || field.ImpliedModifiers.Contains(required)))
                {
                    continue;
                }
            }

            return true;
        }

        return false;
    }

    private static bool TryResolveNumericBoundValue(
        TypedField field,
        ModifierKind modifierKind,
        NumericBoundSource source,
        out decimal value)
    {
        switch (source)
        {
            case NumericBoundSource.DeclarationValue:
            {
                var declarationBound = modifierKind switch
                {
                    ModifierKind.Min => field.NormalizedDeclaredMin ?? field.DeclaredMin,
                    ModifierKind.Max => field.NormalizedDeclaredMax ?? field.DeclaredMax,
                    _ => null
                };

                if (declarationBound.HasValue)
                {
                    value = declarationBound.Value;
                    return true;
                }
                break;
            }
        }

        value = default;
        return false;
    }

    private static NumericInterval ApplyStaticUnitScaling(TypedExpression expr, NumericInterval interval)
    {
        if (interval.IsUnbounded)
            return interval;

        UcumParsedUnit? staticUnit = expr switch
        {
            TypedTypedConstant { ParsedValue: ValueTuple<decimal, UcumParsedUnit?> (_, var unit) } => unit,
            TypedTypedConstant { ParsedValue: ValueTuple<decimal, object?, UcumParsedUnit?> (_, _, var unit) } => unit,
            InterpolatedTypedConstant { StaticQualifier: StaticUnitQualifier { Unit: var unit } } interpolated
                when HasSingleMagnitudeSlot(interpolated) => unit,
            InterpolatedTypedConstant { StaticQualifier: StaticCurrencyAndUnitQualifier { Unit: var unit } } interpolated
                when HasSingleMagnitudeSlot(interpolated) => unit,
            _ => null,
        };

        if (staticUnit is null)
            return interval;

        if (!TypedConstantNormalizer.TryGetStaticAffineParams(staticUnit, out var scale, out var offset))
            return interval;

        if (expr.ResultType == TypeKind.Price)
            return interval.Scale(1m / scale);

        return offset.HasValue
            ? interval.Shift(offset.Value).Scale(scale)
            : interval.Scale(scale);
    }

    private static bool HasSingleMagnitudeSlot(InterpolatedTypedConstant interpolated)
        => interpolated.Slots.Length == 1 && interpolated.Slots[0].SlotKind == InterpolationSlotKind.Magnitude;

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
        var resultInterval = ApplyStaticUnitScaling(
            obligation.Site,
            IntervalOfNarrowed(obligation.Site, semantics, narrowed));
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
