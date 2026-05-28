using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // Interval proof methods are in this file

    // ════════════════════════════════════════════════════════════════════════
    //  Interval Computation and Obligation Collection
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
                // Single-slot interpolations (quantity / money / price) where the slot
                // carries the full numeric value.
                if (interpolated.Slots.Length == 1)
                {
                    var slot = interpolated.Slots[0];
                    if (slot.SlotKind is InterpolationSlotKind.Magnitude or InterpolationSlotKind.WholeValue)
                    {
                        // Price magnitude intervals require a static denominator unit so that
                        // ApplyStaticUnitScaling can invert the UCUM factor and place the raw
                        // magnitude into the field's normalized (per-base-unit) price space.
                        // A missing or non-unit qualifier means the denominator is dynamic —
                        // we cannot normalize at compile time, so conservatively return Unbounded.
                        if (slot.SlotKind == InterpolationSlotKind.Magnitude
                            && interpolated.ResultType == TypeKind.Price
                            && interpolated.StaticQualifier is not StaticCurrencyAndUnitQualifier)
                            return NumericInterval.Unbounded;

                        // Money magnitude/WholeValue: currencies are not UCUM-convertible, so the
                        // raw magnitude interval IS the money interval. No scaling is applied here;
                        // ApplyStaticUnitScaling leaves money intervals untouched (no unit to scale).
                        // Quantity magnitude/WholeValue: interval is in slot units; ApplyStaticUnitScaling
                        // applies the UCUM factor from StaticUnitQualifier (or StaticCurrencyAndUnitQualifier
                        // for price) after this method returns.
                        return IntervalOfNarrowed(slot.Expression, semantics, narrowed);
                    }
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
            return TrimIntervalPrecision(interval.Scale(1m / scale));

        var scaled = offset.HasValue
            ? interval.Shift(offset.Value).Scale(scale)
            : interval.Scale(scale);
        return TrimIntervalPrecision(scaled);
    }

    private static NumericInterval TrimIntervalPrecision(NumericInterval interval)
    {
        if (interval.IsUnbounded)
            return interval;

        return new NumericInterval(
            decimal.Round(interval.Min, 24, MidpointRounding.ToEven),
            decimal.Round(interval.Max, 24, MidpointRounding.ToEven));
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
            EventHandlerContext h => h.Handler.Guard,
            _ => null
        };

        // Sibling reject-row composition: even without an explicit guard on
        // this row, an earlier reject-row on the same (state, event) pair
        // narrows the field values that can reach this row. Build the
        // exclusion set from those sibling reject-rows.
        var siblingExclusions = obligation.Context is TransitionRowContext trc
            ? BuildSiblingRejectExclusions(trc.Row, semantics)
            : null;

        if (guard is null && siblingExclusions is null) return null;

        var builder = ImmutableDictionary.CreateBuilder<string, NumericInterval>(StringComparer.Ordinal);

        if (guard is not null)
        {
            var branches = ExtractGuardBranches(guard);
            var perBranchNarrowings = new List<Dictionary<string, NumericInterval>>(branches.Length);
            var unionFields = new HashSet<string>(StringComparer.Ordinal);

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

                    // Defer to NarrowByConstraint for type-aware integer-vs-decimal
                    // dispatch on strict comparisons (sentinel-safe; see
                    // ProofEngine.Satisfiability.cs:NarrowByConstraint). Previously
                    // this site inlined a coarser switch that treated `>` like `>=`.
                    current = NarrowByConstraint(current, gc.Comparison, gc.Value.Value, GetFieldType(gc.Field, semantics));
                    branchNarrowings[gc.Field] = current;
                    unionFields.Add(gc.Field);
                }

                perBranchNarrowings.Add(branchNarrowings);
            }

            // Cross-branch union under OR: for each field constrained in ANY
            // branch, union the per-branch interval across ALL branches.
            // Branches that don't constrain a given field contribute the
            // field's base (declared) interval — an OR-arm that says nothing
            // about a field admits the field's full base range.
            foreach (var field in unionFields)
            {
                NumericInterval? unioned = null;
                foreach (var branchNarrowings in perBranchNarrowings)
                {
                    NumericInterval branchInterval;
                    if (branchNarrowings.TryGetValue(field, out var explicitInterval))
                    {
                        branchInterval = explicitInterval;
                    }
                    else
                    {
                        var baseInterval = ExtractFieldInterval(field, semantics);
                        branchInterval = baseInterval.IsUnbounded
                            ? NumericInterval.Unbounded
                            : baseInterval;
                    }

                    unioned = unioned is null ? branchInterval : unioned.Value.Union(branchInterval);
                }
                if (unioned is not null)
                    builder[field] = unioned.Value;
            }
        }

        // Apply sibling reject-row exclusions. The sibling reject's guard
        // NEGATION narrows the current row: this row reaches ONLY when the
        // sibling's guard fails, so the negation of each leaf constraint
        // applies to the current row's per-field intervals. Half-open
        // arithmetic at the constraint level (e.g., negating `X >= 10` to
        // `X < 10`, narrowing X to `[_, 9]` for integer fields) is more
        // precise than closed-interval set difference.
        if (siblingExclusions is not null)
        {
            foreach (var (field, negatedRange) in siblingExclusions)
            {
                var baseInterval = builder.TryGetValue(field, out var existing)
                    ? existing
                    : ExtractFieldInterval(field, semantics);
                if (baseInterval.IsUnbounded)
                    baseInterval = new NumericInterval(decimal.MinValue, decimal.MaxValue);
                builder[field] = baseInterval.Intersect(negatedRange);
            }
        }

        return builder.Count > 0 ? builder.ToImmutable() : null;
    }

    /// <summary>
    /// Collects per-field intervals that sibling reject-rows on the same
    /// (state, event) pair admit. The current row only fires when those
    /// guards failed, so the admitted intervals are excluded from this row's
    /// reachable field values.
    /// </summary>
    private static Dictionary<string, NumericInterval>? BuildSiblingRejectExclusions(
        TypedTransitionRow currentRow,
        SemanticIndex semantics)
    {
        // The current row is only a candidate for sibling-reject narrowing if
        // it's a TypedTransitionRowSuccess — reject rows themselves don't
        // benefit from this narrowing.
        if (currentRow is not TypedTransitionRowSuccess) return null;

        Dictionary<string, NumericInterval>? result = null;

        // Walk every reject row on the same (state, event). Reject rows that
        // would match-first under their guard narrow the field-value space
        // the current row implicitly inhabits.
        foreach (var sibling in semantics.TransitionRows)
        {
            if (ReferenceEquals(sibling, currentRow)) continue;
            if (sibling is not TypedTransitionRowReject) continue;
            if (sibling.Guard is null) continue; // unguarded reject already covered the event
            if (!string.Equals(sibling.EventName, currentRow.EventName, StringComparison.Ordinal)) continue;

            // First-match discipline: transition rows dispatch in declaration order
            // (precept-language-spec.md § 5.2). A reject row positioned BELOW the
            // current success row never intercepts at runtime — its negation can't
            // narrow the success row's reachable field-value space.
            if (sibling.RowSpan.Offset >= currentRow.RowSpan.Offset) continue;

            // FromState compatibility check. Four cases to consider:
            //   sibling=X, current=X        — same specific state; sibling intercepts.
            //   sibling=X, current=Y        — different specific states; sibling can't intercept.
            //   sibling=null, current=X     — sibling is wildcard; intercepts every state including X.
            //   sibling=null, current=null  — both wildcards; sibling intercepts everywhere current fires.
            //   sibling=X, current=null     — sibling specific, current wildcard.
            //                                  The wildcard fires on states the sibling DOES NOT cover,
            //                                  so we can't soundly apply the sibling's narrowing across
            //                                  the wildcard's full reach. Skip.
            if (sibling.FromState is not null && currentRow.FromState is null)
                continue;
            if (sibling.FromState is not null && currentRow.FromState is not null
                && !string.Equals(sibling.FromState, currentRow.FromState, StringComparison.Ordinal))
                continue;

            var siblingBranches = ExtractGuardBranches(sibling.Guard);
            if (siblingBranches.Length != 1) continue; // multi-branch reject guards skipped (sound under-approximation)
            var branch = siblingBranches[0];

            // A single-branch reject's guard is a conjunction A ∧ B ∧ ...;
            // its negation is the disjunction ¬A ∨ ¬B ∨ ..., which can't
            // soundly narrow any individual field (¬A admits the field's full
            // range whenever some OTHER conjunct is false). Only branches
            // with exactly one leaf (numeric and trackable) yield a sound
            // per-field narrowing; any companion leaf — numeric on a
            // different field, presence check, or non-numeric comparison —
            // forfeits the narrowing. Same-field multi-leaves
            // (e.g., `F >= V1 and F <= V2`) also forfeit under this rule:
            // sound but coarser than necessary — a same-field conjunction
            // could in principle compose by intersection before negation.
            GuardConstraint? singleLeaf = null;
            bool isMultiLeaf = false;
            foreach (var gc in branch)
            {
                if (singleLeaf is null && !gc.IsPresenceCheck && gc.Value is not null)
                {
                    singleLeaf = gc;
                }
                else
                {
                    isMultiLeaf = true;
                    break;
                }
            }
            if (singleLeaf is null || isMultiLeaf) continue;

            var fieldType = semantics.FieldsByName.TryGetValue(singleLeaf.Field, out var rejectField)
                ? rejectField.ResolvedType
                : TypeKind.Decimal;

            var negatedRange = NegateConstraintToInterval(singleLeaf.Comparison, singleLeaf.Value!.Value, fieldType);
            if (negatedRange is null) continue;

            // Multiple sibling-reject ROWS each contribute an independent
            // narrowing; they compose by intersection (the current row reaches
            // only when ALL sibling rejects' guards failed simultaneously).
            result ??= new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
            result[singleLeaf.Field] = result.TryGetValue(singleLeaf.Field, out var existing)
                ? existing.Intersect(negatedRange.Value)
                : negatedRange.Value;
        }

        return result;
    }

    /// <summary>
    /// Produce the negation of a constraint `field comparison value` as an
    /// interval. Used to derive the narrowing the current row inherits from
    /// a sibling reject-row above it.
    ///
    /// The half-open boundary differs by domain. For integer-typed fields,
    /// negating `X >= V` to `X &lt;= V - 1` is exact (no integer lies in
    /// (V-1, V)). For all other domains (Decimal/Number today; the dispatch
    /// is forward-compatible for any decimal-backed type whose literals
    /// reach the guard-constraint extractor), values exist strictly between
    /// V-1 and V, so subtracting 1 produces a SUBSET of the truth — unsound
    /// for the proof engine, which would over-claim discharges. For those
    /// domains we close the boundary at V instead, yielding a SUPERSET of
    /// the truth (over-wide by exactly the point {V}) that is sound at the
    /// cost of slightly missed precision near the bound.
    /// </summary>
    private static NumericInterval? NegateConstraintToInterval(
        OperatorKind comparison,
        decimal value,
        TypeKind fieldType)
    {
        bool isIntegerDomain = fieldType == TypeKind.Integer;

        return comparison switch
        {
            OperatorKind.GreaterThanOrEqual => isIntegerDomain
                ? new NumericInterval(decimal.MinValue, value - 1m)    // ¬(X ≥ V) ⇒ X ≤ V - 1
                : new NumericInterval(decimal.MinValue, value),        // ¬(X ≥ V) ⇒ closed at V (sound superset of X < V)
            OperatorKind.GreaterThan        => new NumericInterval(decimal.MinValue, value),    // ¬(X > V) ⇒ X ≤ V
            OperatorKind.LessThanOrEqual    => isIntegerDomain
                ? new NumericInterval(value + 1m, decimal.MaxValue)    // ¬(X ≤ V) ⇒ X ≥ V + 1
                : new NumericInterval(value,       decimal.MaxValue),  // ¬(X ≤ V) ⇒ closed at V (sound superset of X > V)
            OperatorKind.LessThan           => new NumericInterval(value, decimal.MaxValue),    // ¬(X < V) ⇒ X ≥ V
            OperatorKind.Equals             => null,                                            // X ≠ V isn't representable as a single closed interval
            OperatorKind.NotEquals          => NumericInterval.Point(value),                    // ¬(X ≠ V) ⇒ X = V
            _ => null,
        };
    }
}
