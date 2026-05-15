using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    //  S10 — Constraint Influence Analysis
    // ════════════════════════════════════════════════════════════════════════════

    private static ImmutableArray<ConstraintInfluenceEntry> ProjectConstraintInfluence(SemanticIndex semantics)
    {
        var entries = new List<ConstraintInfluenceEntry>();

        foreach (var cfr in semantics.ConstraintRefs)
        {
            var qualifiedArgs = cfr.ReferencedArgs
                .Select(argName => ResolveArgToEvent(argName, semantics))
                .ToImmutableArray();

            entries.Add(new ConstraintInfluenceEntry(
                cfr.ConstraintIdentity,
                cfr.ReferencedFields,
                qualifiedArgs));
        }

        return entries.ToImmutableArray();
    }

    private static EventArgReference ResolveArgToEvent(string argName, SemanticIndex semantics)
    {
        foreach (var evt in semantics.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (string.Equals(arg.Name, argName, StringComparison.Ordinal))
                    return new EventArgReference(evt.Name, argName);
            }
        }
        return new EventArgReference("<unknown>", argName);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S11 — Initial-State Satisfiability
    // ════════════════════════════════════════════════════════════════════════════

    private static ImmutableArray<InitialStateSatisfiabilityResult> CheckInitialStateSatisfiability(
        SemanticIndex semantics)
    {
        var initialState = semantics.States
            .FirstOrDefault(s => s.Modifiers.Contains(ModifierKind.InitialState));
        if (initialState is null)
            return ImmutableArray<InitialStateSatisfiabilityResult>.Empty;

        // Build default value environment
        var defaults = new Dictionary<string, object?>(StringComparer.Ordinal);
        var unfoldable = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in semantics.Fields)
        {
            if (field.DefaultExpression is TypedLiteral lit)
                defaults[field.Name] = lit.Value;
            else if (field.DefaultExpression is InterpolatedTypedConstant)
            {
                // Part A: Try to fold interpolated defaults using already-accumulated defaults.
                // This enables ensures evaluation to reason about fields with foldable interpolated defaults.
                //
                // NOTE — ordering sensitivity: field declaration order affects foldability here.
                // If a field referenced in a slot (e.g., '{n} kg') has not yet been accumulated into
                // defaults at this point (because n is declared after the current field), FoldValue
                // returns UnknownSentinel and the field is marked unfoldable. This is graceful
                // degradation — no error, just a conservative skip of ensures obligations that
                // depend on that field's default. CollectDefaultObligations is unaffected because
                // it derives bounds from declared field limits (IntervalOf), not accumulated defaults.
                var folded = FoldValue(field.DefaultExpression, defaults, unfoldable);
                if (!ReferenceEquals(folded, UnknownSentinel))
                    defaults[field.Name] = folded;
                else
                    unfoldable.Add(field.Name);
            }
            else if (field.DefaultExpression is not null || field.IsComputed)
                unfoldable.Add(field.Name);
            else if (field.IsOptional)
                defaults[field.Name] = null;
            else
                defaults[field.Name] = GetTypeDefault(field.ResolvedType, unfoldable, field.Name);
        }

        // Collect StateResident ensures for initial state
        var initialEnsures = semantics.EnsuresByState.TryGetValue(initialState.Name, out var ensures)
            ? ensures.Where(e => e.Kind == ConstraintKind.StateResident).ToList()
            : new List<TypedEnsure>();

        var violations = new List<UnsatisfiedConstraint>();

        for (int i = 0; i < initialEnsures.Count; i++)
        {
            var ensure = initialEnsures[i];
            if (ensure.Guard is not null)
                continue; // guarded ensures skipped

            var foldResult = ConstantFold(ensure.Condition, defaults, unfoldable);

            if (foldResult is false)
            {
                violations.Add(new UnsatisfiedConstraint(
                    new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, i),
                    FormatViolationReason(ensure, defaults)));
            }
        }

        return
        [
            new InitialStateSatisfiabilityResult(
                initialState.Name,
                violations.Count == 0,
                violations.ToImmutableArray())
        ];
    }

    private static object? GetTypeDefault(TypeKind type, HashSet<string> unfoldable, string fieldName)
    {
        return type switch
        {
            TypeKind.Integer => 0m,
            TypeKind.Decimal => 0m,
            TypeKind.Number => 0m,
            TypeKind.String => "",
            TypeKind.Boolean => false,
            TypeKind.Set or TypeKind.Queue or TypeKind.Stack or TypeKind.Log or
            TypeKind.LogBy or TypeKind.Bag or TypeKind.List or TypeKind.QueueBy or
            TypeKind.Lookup => 0m, // collection count = 0
            _ => MarkUnfoldable(unfoldable, fieldName)
        };
    }

    private static object? MarkUnfoldable(HashSet<string> unfoldable, string fieldName)
    {
        unfoldable.Add(fieldName);
        return null;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Test entry points (InternalsVisibleTo — Precept.Tests)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Exposes <see cref="ExtractComparableValue"/> for unit testing.</summary>
    internal static string? ExtractComparableValueForTest(DeclaredQualifierMeta qualifier) =>
        ExtractComparableValue(qualifier);

    /// <summary>Exposes qualifier compatibility comparisons for unit testing.</summary>
    internal static bool QualifiersAreCompatibleForTest(
        DeclaredQualifierMeta? leftQualifier,
        DeclaredQualifierMeta? rightQualifier,
        QualifierAxis axis) =>
        QualifiersAreCompatible(leftQualifier, rightQualifier, axis);

    /// <summary>Exposes <see cref="QualifiersSymbolicallyEqual"/> for unit testing.</summary>
    internal static bool QualifiersSymbolicallyEqualForTest(
        DeclaredQualifierMeta left,
        DeclaredQualifierMeta right) =>
        QualifiersSymbolicallyEqual(left, right);

    /// <summary>Exposes <see cref="TryProjectCompoundPrice"/> for unit testing.</summary>
    internal static DeclaredQualifierMeta? TryProjectCompoundPriceForTest(
        DeclaredQualifierMeta qualifier,
        QualifierAxis axis) =>
        TryProjectCompoundPrice(qualifier, axis);

    /// <summary>
    /// Returns the first implied qualifier matching <paramref name="axis"/> for <paramref name="type"/>.
    /// Tests Slice 11B's Duration implied-qualifier path without requiring proof pipeline setup.
    /// </summary>
    internal static DeclaredQualifierMeta? GetImpliedQualifierOnAxis(TypeKind type, QualifierAxis axis)
    {
        var typeMeta = Types.GetMeta(type);
        foreach (var qual in typeMeta.ImpliedQualifiers)
        {
            if (qual.Axis == axis)
                return qual;
        }
        return null;
    }

    private static bool? ConstantFold(TypedExpression expr, Dictionary<string, object?> defaults, HashSet<string> unfoldable)
    {
        var result = FoldValue(expr, defaults, unfoldable);
        return result switch
        {
            bool b => b,
            _ => null // Unknown
        };
    }

    private static object? FoldValue(TypedExpression expr, Dictionary<string, object?> defaults, HashSet<string> unfoldable)
    {
        switch (expr)
        {
            case TypedLiteral lit:
                return lit.Value;

            case TypedFieldRef fieldRef:
                if (unfoldable.Contains(fieldRef.FieldName))
                    return UnknownSentinel;
                return defaults.TryGetValue(fieldRef.FieldName, out var val) ? val : UnknownSentinel;

            case TypedBinaryOp bin:
            {
                var left = FoldValue(bin.Left, defaults, unfoldable);
                var right = FoldValue(bin.Right, defaults, unfoldable);
                if (ReferenceEquals(left, UnknownSentinel) || ReferenceEquals(right, UnknownSentinel))
                    return UnknownSentinel;

                var opKind = Operations.GetMeta(bin.ResolvedOp).Op;
                return EvaluateBinaryOp(opKind, left, right);
            }

            case TypedUnaryOp un:
            {
                var operand = FoldValue(un.Operand, defaults, unfoldable);
                if (ReferenceEquals(operand, UnknownSentinel))
                    return UnknownSentinel;

                var opKind = Operations.GetMeta(un.ResolvedOp).Op;
                if (opKind == OperatorKind.Not && operand is bool b)
                    return !b;
                if (opKind == OperatorKind.Negate && operand is decimal d)
                    return -d;
                return UnknownSentinel;
            }

            case TypedConditional cond:
            {
                var condResult = FoldValue(cond.Condition, defaults, unfoldable);
                if (condResult is bool condBool)
                    return condBool
                        ? FoldValue(cond.ThenBranch, defaults, unfoldable)
                        : FoldValue(cond.ElseBranch, defaults, unfoldable);
                return UnknownSentinel;
            }

            case TypedPostfixOp post:
            {
                var operand = FoldValue(post.Operand, defaults, unfoldable);
                if (ReferenceEquals(operand, UnknownSentinel))
                    return UnknownSentinel;
                bool isSet = operand is not null;
                return post.IsNegated ? !isSet : isSet;
            }

            case InterpolatedTypedConstant interpolated:
            {
                // Part A — fully-static: StaticMagnitude extracted by the TypeChecker from a literal in the magnitude slot.
                if (interpolated.StaticMagnitude.HasValue)
                {
                    var mag = interpolated.StaticMagnitude.Value;
                    switch (interpolated.StaticQualifier)
                    {
                        case StaticUnitQualifier { Unit: var unit }:
                            if (TypedConstantNormalizer.TryGetStaticAffineParams(unit, out var scale, out var offset))
                                return offset.HasValue ? (mag + offset.Value) * scale : mag * scale;
                            break;
                        case StaticCurrencyQualifier:
                        case null:
                            return mag; // currency or dimensionless — magnitude as-is
                    }
                }

                // Single-slot magnitude from a foldable source (e.g., field ref with an explicit literal default).
                if (interpolated.Slots.Length == 1
                    && interpolated.Slots[0].SlotKind is InterpolationSlotKind.Magnitude or InterpolationSlotKind.WholeValue)
                {
                    var slotFolded = FoldValue(interpolated.Slots[0].Expression, defaults, unfoldable);
                    if (!ReferenceEquals(slotFolded, UnknownSentinel) && slotFolded is decimal slotMagnitude)
                    {
                        if (interpolated.StaticQualifier is StaticUnitQualifier { Unit: var slotUnit })
                        {
                            if (TypedConstantNormalizer.TryGetStaticAffineParams(slotUnit, out var s, out var o))
                                return o.HasValue ? (slotMagnitude + o.Value) * s : slotMagnitude * s;
                        }
                        return slotMagnitude;
                    }
                }

                return UnknownSentinel;
            }

            default:
                return UnknownSentinel;
        }
    }

    private static readonly object UnknownSentinel = new();

    private readonly record struct NumericRuleFact(string FieldName, OperatorKind Comparison, decimal Value);

    private static object? EvaluateBinaryOp(OperatorKind op, object? left, object? right)
    {
        // Boolean operations
        if (op == OperatorKind.And && left is bool lb1 && right is bool rb1)
            return lb1 && rb1;
        if (op == OperatorKind.Or && left is bool lb2 && right is bool rb2)
            return lb2 || rb2;

        // Numeric comparisons
        var dl = left switch { decimal d => (decimal?)d, int i => (decimal?)i, long l => (decimal?)l, _ => null };
        var dr = right switch { decimal d => (decimal?)d, int i => (decimal?)i, long l => (decimal?)l, _ => null };

        if (dl is not null && dr is not null)
        {
            return op switch
            {
                OperatorKind.Plus => dl.Value + dr.Value,
                OperatorKind.Minus => dl.Value - dr.Value,
                OperatorKind.Times => dl.Value * dr.Value,
                OperatorKind.Divide when dr.Value != 0 => dl.Value / dr.Value,
                OperatorKind.Modulo when dr.Value != 0 => dl.Value % dr.Value,
                OperatorKind.Equals => dl.Value == dr.Value,
                OperatorKind.NotEquals => dl.Value != dr.Value,
                OperatorKind.GreaterThan => dl.Value > dr.Value,
                OperatorKind.GreaterThanOrEqual => dl.Value >= dr.Value,
                OperatorKind.LessThan => dl.Value < dr.Value,
                OperatorKind.LessThanOrEqual => dl.Value <= dr.Value,
                _ => UnknownSentinel
            };
        }

        // String equality
        if (left is string sl && right is string sr)
        {
            return op switch
            {
                OperatorKind.Equals => string.Equals(sl, sr, StringComparison.Ordinal),
                OperatorKind.NotEquals => !string.Equals(sl, sr, StringComparison.Ordinal),
                _ => UnknownSentinel
            };
        }

        // Boolean equality
        if (left is bool bl && right is bool br)
        {
            return op switch
            {
                OperatorKind.Equals => bl == br,
                OperatorKind.NotEquals => bl != br,
                _ => UnknownSentinel
            };
        }

        return UnknownSentinel;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Part B — Default obligation collector (Slice 25)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates interval-containment proof obligations for fields whose
    /// <see cref="TypedField.DefaultExpression"/> is an <see cref="InterpolatedTypedConstant"/>
    /// and whose declared bounds are non-empty. Only emits obligations where
    /// <see cref="IntervalOf"/> returns a bounded (non-<see cref="NumericInterval.Unbounded"/>) result,
    /// so Unit-slot and multi-slot interpolated defaults are safely skipped.
    /// </summary>
    internal static void CollectDefaultObligations(SemanticIndex semantics, List<ProofObligation> obligations)
    {
        foreach (var field in semantics.Fields)
        {
            if (field.DefaultExpression is not InterpolatedTypedConstant || field.IsComputed)
                continue;

            var (min, max) = GetFieldBounds(field);
            if (!min.HasValue && !max.HasValue)
                continue;

            // Use the full IntervalOf path (includes ApplyStaticUnitScaling) so unit conversion is applied.
            var interval = IntervalOf(field.DefaultExpression, semantics);
            if (interval.IsUnbounded)
                continue;

            var authoredMin = field.DeclaredMin;
            var authoredMax = field.DeclaredMax;
            var minStr = (authoredMin ?? min)?.ToString() ?? "−∞";
            var maxStr = (authoredMax ?? max)?.ToString() ?? "+∞";
            var intervalReq = new IntervalContainmentProofRequirement(
                new SelfSubject(),
                field.Name,
                min, max,
                authoredMin, authoredMax,
                $"Interval containment: default of '{field.Name}' must be within declared bounds [{minStr} .. {maxStr}]");

            obligations.Add(new ProofObligation(
                intervalReq,
                field.DefaultExpression,
                new FieldDefaultContext(field),
                ProofDisposition.Unresolved,
                null,
                null));
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Part C — Arg default obligation collector (Slice 26)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates interval-containment proof obligations for event args whose
    /// <see cref="TypedArg.DefaultExpression"/> is non-null and whose declared
    /// bounds are non-empty. Handles both <see cref="TypedTypedConstant"/> and
    /// <see cref="InterpolatedTypedConstant"/> defaults. Only emits obligations where
    /// <see cref="IntervalOf"/> returns a bounded (non-<see cref="NumericInterval.Unbounded"/>)
    /// result.
    /// </summary>
    internal static void CollectArgDefaultObligations(SemanticIndex semantics, List<ProofObligation> obligations)
    {
        foreach (var evt in semantics.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.DefaultExpression is null)
                    continue;

                var min = arg.NormalizedDeclaredMin ?? arg.DeclaredMin;
                var max = arg.NormalizedDeclaredMax ?? arg.DeclaredMax;
                if (!min.HasValue && !max.HasValue)
                    continue;

                // Use the full IntervalOf path (includes ApplyStaticUnitScaling) so unit conversion is applied.
                var interval = IntervalOf(arg.DefaultExpression, semantics);
                if (interval.IsUnbounded)
                    continue;

                var authoredMin = arg.DeclaredMin;
                var authoredMax = arg.DeclaredMax;
                var minStr = (authoredMin ?? min)?.ToString() ?? "−∞";
                var maxStr = (authoredMax ?? max)?.ToString() ?? "+∞";
                var targetLabel = $"{evt.Name}.{arg.Name}";
                var intervalReq = new IntervalContainmentProofRequirement(
                    new SelfSubject(),
                    targetLabel,
                    min, max,
                    authoredMin, authoredMax,
                    $"Interval containment: default of '{targetLabel}' must be within declared bounds [{minStr} .. {maxStr}]");

                obligations.Add(new ProofObligation(
                    intervalReq,
                    arg.DefaultExpression,
                    new ArgDefaultContext(arg),
                    ProofDisposition.Unresolved,
                    null,
                    null));
            }
        }
    }

    private static string FormatViolationReason(TypedEnsure ensure, Dictionary<string, object?> defaults)
    {
        var fields = new List<string>();
        CollectFieldRefs(ensure.Condition, fields);
        if (fields.Count == 0)
            return "constraint fails with default values";

        var details = fields.Select(f =>
            defaults.TryGetValue(f, out var v) ? $"{f}={v ?? "null"}" : f);
        return $"constraint fails when {string.Join(", ", details)}";
    }

    private static void CollectFieldRefs(TypedExpression expr, List<string> fields)
    {
        switch (expr)
        {
            case TypedFieldRef fr:
                if (!fields.Contains(fr.FieldName)) fields.Add(fr.FieldName);
                break;
            case TypedBinaryOp bin:
                CollectFieldRefs(bin.Left, fields);
                CollectFieldRefs(bin.Right, fields);
                break;
            case TypedUnaryOp un:
                CollectFieldRefs(un.Operand, fields);
                break;
            case TypedConditional cond:
                CollectFieldRefs(cond.Condition, fields);
                CollectFieldRefs(cond.ThenBranch, fields);
                CollectFieldRefs(cond.ElseBranch, fields);
                break;
            case TypedFunctionCall call:
                foreach (var arg in call.Arguments) CollectFieldRefs(arg, fields);
                break;
            case TypedMemberAccess ma:
                CollectFieldRefs(ma.Object, fields);
                break;
        }
    }
}
