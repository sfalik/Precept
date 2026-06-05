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
                // A quantifier binding identifier (`x` in `no x in S (x > 100)`) surfaces as a
                // TypedFieldRef carrying the collection element type's bounds; the binding ranges
                // over governed elements, so it carries band(m) — sound because ingress governs
                // every element into the bound (design § Semantic Rule 4 / 5, matched pair).
                if (fieldRef.ElementBounds is { HasNumericBound: true } bindingBounds)
                    return ElementNumericInterval(bindingBounds);
                return ExtractFieldInterval(fieldRef.FieldName, semantics);

            // A collection element read (.min/.max/.peek/.first/.last/.at) whose receiver field's
            // inner type declares a numeric bound carries that bound as its value interval — the
            // numeric half of the read-site reach (design § Semantic Rule 4). Feeds the existing
            // interval/OutOfRange assignment-range path unchanged. A receiver with no numeric
            // element bound falls through to Unbounded, preserving prior behavior exactly.
            //
            // Element-returning gate: the band is the ELEMENT's value band, so it may only be
            // carried by an accessor that returns an element. Element accessors are plain
            // TypeAccessor (return type = the element type); a FixedReturnAccessor returns a
            // fixed type independent of the element (collection `.count` returns the cardinality,
            // an Integer in [0, count] — NOT an element, so it must not inherit the element band).
            // The positive allowlist (NOT FixedReturnAccessor) also keeps any future numeric
            // collection accessor (`.sum`/`.average`, also FixedReturnAccessor) from silently
            // leaking the band — a `count`-name blocklist would not.
            case TypedMemberAccess access
                when access.ResolvedAccessor is not FixedReturnAccessor
                    && access.Object is TypedFieldRef receiver
                    && semantics.FieldsByName.TryGetValue(receiver.FieldName, out var receiverField)
                    && receiverField.ElementType?.ValueBounds is { HasNumericBound: true } elementBounds:
                return ElementNumericInterval(elementBounds);

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

        // Tighten the lower bound from flag-modifier constant lower bounds (nonnegative ⇒ ≥ 0;
        // positive ⇒ ≥ 0 as a sound over-approximation of > 0). GetFieldBounds deliberately reads
        // only DeclarationValue-sourced bounds, so the Constant-sourced flag bounds are folded here —
        // in the operand-interval READ path only — never into GetFieldBounds itself. A field's value
        // genuinely lies within these bounds, so this can only tighten the operand interval and help
        // discharge; it never creates a rejection.
        var flagMin = FlagLowerBound(field);
        if (flagMin.HasValue)
            min = min.HasValue ? Math.Max(min.Value, flagMin.Value) : flagMin.Value;

        if (!min.HasValue && !max.HasValue) return NumericInterval.Unbounded;
        return new NumericInterval(min ?? decimal.MinValue, max ?? decimal.MaxValue);
    }

    /// <summary>
    /// The tightest constant lower bound implied by the field's value-bounding flag modifiers, derived
    /// from catalog metadata — any SelfValue numeric satisfaction with a ≥ / &gt; comparison against a
    /// Constant bound. nonzero (a NotEquals hole, not a lower bound) contributes nothing. Returns null
    /// when no flag implies a constant lower bound.
    /// </summary>
    private static decimal? FlagLowerBound(TypedField field)
    {
        decimal? lower = null;

        foreach (var modifierKind in field.Modifiers.Concat(field.ImpliedModifiers))
        {
            if (Modifiers.GetMeta(modifierKind) is not ValueModifierMeta modifierMeta)
                continue;
            if (!ModifierAppliesToField(modifierMeta, field))
                continue;

            var fromMeta = FlagLowerBoundFromMeta(modifierMeta);
            if (fromMeta.HasValue)
                lower = lower.HasValue ? Math.Max(lower.Value, fromMeta.Value) : fromMeta.Value;
        }

        return lower;
    }

    /// <summary>
    /// The constant lower bound implied by a single value-modifier's catalog metadata: the tightest
    /// SelfValue ≥ / &gt; comparison against a Constant bound (e.g. <c>nonnegative</c>/<c>positive</c> ⇒ 0).
    /// Shared by the field flag-lower-bound path and the collection element band path so the
    /// flag→bound mapping lives in one place, catalog-driven.
    /// </summary>
    private static decimal? FlagLowerBoundFromMeta(ValueModifierMeta modifierMeta)
    {
        decimal? lower = null;
        foreach (var satisfaction in modifierMeta.ProofSatisfactions.OfType<ProofSatisfaction.Numeric>())
        {
            if (satisfaction.Projection is not SatisfactionProjection.SelfValue)
                continue;
            if (satisfaction.Comparison is not (OperatorKind.GreaterThanOrEqual or OperatorKind.GreaterThan))
                continue;
            if (satisfaction.Bound is not NumericBoundSource.Constant constant)
                continue;
            lower = lower.HasValue ? Math.Max(lower.Value, constant.Value) : constant.Value;
        }
        return lower;
    }

    /// <summary>
    /// The numeric band <c>(min, max)</c> declared on a collection inner type
    /// (<c>set of integer min 0 max 100</c>, <c>set of money in 'USD' nonnegative</c>).
    /// Mirrors <see cref="ExtractFieldInterval"/>'s composition: declared <c>min</c>/<c>max</c>
    /// (normalized to UCUM base units for qualified elements, matching the scalar field path),
    /// then the flag-implied constant lower bound (<c>nonnegative</c>/<c>positive</c> ⇒ ≥ 0) folded
    /// in via the same catalog metadata. The element typing already validated applicability, so no
    /// per-element applicability re-check is needed. Returns <c>(null, null)</c> when the element
    /// declares no numeric bound.
    /// </summary>
    internal static (decimal? min, decimal? max) GetElementNumericBounds(DeclaredValueBounds bounds)
    {
        decimal? min = bounds.NormalizedDeclaredMin ?? bounds.DeclaredMin;
        decimal? max = bounds.NormalizedDeclaredMax ?? bounds.DeclaredMax;

        if (!bounds.NumericFlags.IsDefaultOrEmpty)
        {
            foreach (var flag in bounds.NumericFlags)
            {
                if (Modifiers.GetMeta(flag) is not ValueModifierMeta meta)
                    continue;
                var flagMin = FlagLowerBoundFromMeta(meta);
                if (flagMin.HasValue)
                    min = min.HasValue ? Math.Max(min.Value, flagMin.Value) : flagMin.Value;
            }
        }

        return (min, max);
    }

    /// <summary>
    /// The numeric interval of a collection element read (<c>.min</c>/<c>.max</c>/<c>.peek</c>/…) whose
    /// inner type declares a numeric bound, as a closed <see cref="NumericInterval"/>. Returns
    /// <see cref="NumericInterval.Unbounded"/> when no numeric bound is declared (prior behavior).
    /// </summary>
    private static NumericInterval ElementNumericInterval(DeclaredValueBounds bounds)
    {
        var (min, max) = GetElementNumericBounds(bounds);
        if (!min.HasValue && !max.HasValue)
            return NumericInterval.Unbounded;
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

        // An empty result interval is infeasible (⊥), not a vacuously-contained value — guarding
        // here backstops the Contains(⊥) => true over-prove vector even if a narrowing path admits
        // an empty interval. An empty interval is unprovable, never a passing containment.
        if (resultInterval.IsEmpty) return false;

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

        // Unconditional field-to-field relations contribute a half-line bound even when there is
        // no guard and no sibling-reject narrowing — so a relation alone can populate the dict.
        var relationalFacts = CollectUnconditionalRelationalFacts(semantics).ToList();

        if (guard is null && siblingExclusions is null && relationalFacts.Count == 0) return null;

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

        // Unconditional field-to-field relations: for each `rule X op Y`, intersect X's narrowed
        // interval with the half-line the relation licenses, reading Y's bound ONE HOP via the bare
        // non-relational ExtractFieldInterval (never the dict being built). An unbounded Y edge makes
        // the half-line ±∞ on the relevant side, so Intersect is the identity — no false bound.
        foreach (var relation in relationalFacts)
        {
            var relatedInterval = ExtractFieldInterval(relation.RightField, semantics);
            var halfLine = RelationalHalfLine(relation.Comparison, relatedInterval, GetFieldType(relation.LeftField, semantics));
            if (halfLine is not { } hl)
                continue;

            var seed = builder.TryGetValue(relation.LeftField, out var existing)
                ? existing
                : ExtractFieldInterval(relation.LeftField, semantics);
            if (seed.IsUnbounded)
                seed = new NumericInterval(decimal.MinValue, decimal.MaxValue);

            // An empty intersection is a proven contradiction (the relation cannot hold given the
            // subject's declared bounds), NOT a vacuously-safe narrowing — writing ⊥ here would let
            // the containment reader discharge every obligation via Contains(⊥) => true. Suppress the
            // contribution so the dependent fault-prone op falls back to its real proof state.
            var narrowedInterval = seed.Intersect(hl);
            if (narrowedInterval.IsEmpty)
                continue;
            builder[relation.LeftField] = narrowedInterval;
        }

        return builder.Count > 0 ? builder.ToImmutable() : null;
    }

    /// <summary>
    /// The half-line a relation <c>X op Y</c> contributes to <c>X</c>'s interval, sourced from
    /// <c>Y</c>'s non-relational interval <paramref name="relatedInterval"/>:
    /// <c>&gt;=</c>/<c>&gt;</c> ⇒ <c>[ylo, +∞)</c>; <c>&lt;=</c>/<c>&lt;</c> ⇒ <c>(−∞, yhi]</c>. The
    /// closed <c>ylo</c>/<c>yhi</c> floor is a sound over-approximation of the strict relation on
    /// decimals (it can only fail to discharge, never over-discharge); for an INTEGER subject the
    /// strict <c>&gt;</c>/<c>&lt;</c> floors to <c>ylo + 1</c> / <c>yhi − 1</c>. When the relevant
    /// edge of <paramref name="relatedInterval"/> is the ±∞ sentinel, the half-line bound is that
    /// sentinel and the subsequent Intersect is the identity. Returns <c>null</c> for an operator
    /// that contributes no half-line.
    /// </summary>
    private static NumericInterval? RelationalHalfLine(
        OperatorKind comparison,
        NumericInterval relatedInterval,
        TypeKind subjectType)
    {
        if (relatedInterval.IsUnbounded)
            relatedInterval = new NumericInterval(decimal.MinValue, decimal.MaxValue);

        bool isIntegerSubject = subjectType == TypeKind.Integer;

        switch (comparison)
        {
            case OperatorKind.GreaterThanOrEqual:
                return new NumericInterval(relatedInterval.Min, decimal.MaxValue);
            case OperatorKind.GreaterThan:
            {
                decimal lo = isIntegerSubject && relatedInterval.Min < decimal.MaxValue
                    ? relatedInterval.Min + 1m
                    : relatedInterval.Min;
                return new NumericInterval(lo, decimal.MaxValue);
            }
            case OperatorKind.LessThanOrEqual:
                return new NumericInterval(decimal.MinValue, relatedInterval.Max);
            case OperatorKind.LessThan:
            {
                decimal hi = isIntegerSubject && relatedInterval.Max > decimal.MinValue
                    ? relatedInterval.Max - 1m
                    : relatedInterval.Max;
                return new NumericInterval(decimal.MinValue, hi);
            }
            default:
                return null;
        }
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
