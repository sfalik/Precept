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

        // Collect StateResident ensures for initial state
        var initialEnsures = semantics.EnsuresByState.TryGetValue(initialState.Name, out var ensures)
            ? ensures.Where(e => e.Kind == ConstraintKind.StateResident).ToList()
            : new List<TypedEnsure>();

        if (HasConstructionHandler(semantics))
        {
            return
            [
                new InitialStateSatisfiabilityResult(
                    initialState.Name,
                    true,
                    ImmutableArray<UnsatisfiedConstraint>.Empty)
            ];
        }

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

    private static bool HasConstructionHandler(SemanticIndex semantics)
    {
        foreach (var handler in semantics.EventHandlers)
        {
            if (handler.IsConstruction)
                return true;

            if (!string.IsNullOrWhiteSpace(handler.EventName)
                && semantics.EventsByName.TryGetValue(handler.EventName, out var resolvedEvent)
                && resolvedEvent.IsInitial)
            {
                return true;
            }
        }

        return false;
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

    /// <summary>Exposes <see cref="FormatQualifierValue"/> for unit testing.</summary>
    internal static string FormatQualifierValueForTest(DeclaredQualifierMeta? qualifier) =>
        FormatQualifierValue(qualifier);

    /// <summary>Exposes <see cref="ExtractQualifierSourcePath"/> for unit testing.</summary>
    internal static string? ExtractQualifierSourcePathForTest(DeclaredQualifierMeta qualifier) =>
        ExtractQualifierSourcePath(qualifier);

    /// <summary>
    /// Returns the first implied qualifier matching <paramref name="axis"/> for <paramref name="type"/>.
    /// Tests the Duration implied-qualifier path without requiring proof pipeline setup.
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
    //  Part B — Default obligation collector
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates declared-bound proof obligations for fields with a <see cref="TypedField.DefaultExpression"/>:
    /// a numeric <see cref="NumericProofRequirement"/> per applicable value-bounding modifier (the
    /// OutOfRange family — replaces the prior interval-containment-for-defaults usage), a
    /// <see cref="LengthContainmentProofRequirement"/> for a string default with declared length bounds
    /// (or <c>notempty</c>), and the stamped open-axis assignment-qualifier residual the type checker
    /// recorded. Computed-field result-vs-bound obligations are stamped separately
    /// (<see cref="CollectComputedFieldBoundObligations"/>).
    /// </summary>
    internal static void CollectDefaultObligations(SemanticIndex semantics, List<ProofObligation> obligations)
    {
        foreach (var field in semantics.Fields)
        {
            if (field.IsComputed)
                continue;

            // Assignment-qualifier residual stamped by the type checker (sited on the default expr).
            EmitCarriedQualifierObligations(field.DefaultQualifierObligations, field.DefaultExpression,
                new FieldDefaultContext(field), obligations);

            if (field.DefaultExpression is null)
                continue;

            var context = new FieldDefaultContext(field);

            CollectNumericDefaultObligations(
                field.Modifiers, field.ImpliedModifiers,
                field.NormalizedDeclaredMin, field.NormalizedDeclaredMax,
                field.ResolvedType, field.DefaultExpression, context, obligations);

            CollectLengthDefaultObligation(
                field.ResolvedType, field.Modifiers, field.DeclaredMinLength, field.DeclaredMaxLength,
                field.Name, field.DefaultExpression, context, obligations);

            CollectCountDefaultObligation(field, context, obligations);

            CollectElementLengthDefaultObligations(field, context, obligations);

            CollectElementIntervalDefaultObligations(field, context, obligations);
        }
    }

    /// <summary>
    /// A <c>default [...]</c> literal on a collection whose string inner type declares a length
    /// bound is an element-entry path: each element must satisfy the element bound, exactly as a
    /// write-site element source must (<see cref="Actions.GenerateElementLengthContainmentObligation"/>).
    /// Without this, the read-site reach would carry a bound a default element could violate
    /// (matched-pair soundness, design § Semantic Rule 5). Reuses the shared
    /// <see cref="LengthContainmentProofRequirement"/> + <c>TryLengthContainmentProof</c> prover;
    /// the default elements are literals, so the obligation discharges statically.
    /// </summary>
    private static void CollectElementLengthDefaultObligations(
        TypedField field, ObligationContext context, List<ProofObligation> obligations)
    {
        if (field.ElementType?.ValueBounds is not { IsEmpty: false } bounds)
            return;
        if (field.DefaultExpression is not TypedListLiteral list)
            return;

        var minLength = bounds.DeclaredMinLength;
        if (bounds.NotEmpty)
            minLength = Math.Max(minLength ?? 0, 1); // notempty ⇒ length ≥ 1
        if (!minLength.HasValue && !bounds.DeclaredMaxLength.HasValue)
            return;

        foreach (var element in list.Elements)
        {
            if (element.ResultType != TypeKind.String)
                continue;
            obligations.Add(new ProofObligation(
                new LengthContainmentProofRequirement(
                    new SelfSubject(),
                    field.Name,
                    minLength,
                    bounds.DeclaredMaxLength,
                    $"Length containment: each default element of '{field.Name}' must have length in [{minLength?.ToString() ?? "0"} .. {bounds.DeclaredMaxLength?.ToString() ?? "∞"}]"),
                element,
                context,
                ProofDisposition.Unresolved,
                null,
                null));
        }
    }

    /// <summary>
    /// The numeric sibling of <see cref="CollectElementLengthDefaultObligations"/>: a <c>default [...]</c>
    /// literal on a collection whose numeric inner type declares a bound is an element-entry path, so each
    /// numeric default element must be proven within the element band — the matched pair to the read-site
    /// reach (design § Semantic Rule 5). Reuses the same <see cref="IntervalContainmentProofRequirement"/>
    /// + <c>TryIntervalContainmentProofNarrowed</c> prover the write-site numeric path uses; the default
    /// elements are literals, so the obligation discharges statically.
    /// </summary>
    private static void CollectElementIntervalDefaultObligations(
        TypedField field, ObligationContext context, List<ProofObligation> obligations)
    {
        if (field.ElementType?.ValueBounds is not { HasNumericBound: true } bounds)
            return;
        if (field.DefaultExpression is not TypedListLiteral list)
            return;

        var (min, max) = GetElementNumericBounds(bounds);
        if (!min.HasValue && !max.HasValue)
            return;

        foreach (var element in list.Elements)
        {
            obligations.Add(new ProofObligation(
                new IntervalContainmentProofRequirement(
                    new SelfSubject(),
                    field.Name,
                    min, max,
                    bounds.DeclaredMin, bounds.DeclaredMax,
                    $"Interval containment: each default element of '{field.Name}' must stay within declared bounds [{(bounds.DeclaredMin ?? min)?.ToString() ?? "−∞"} .. {(bounds.DeclaredMax ?? max)?.ToString() ?? "+∞"}]"),
                element,
                context,
                ProofDisposition.Unresolved,
                null,
                null));
        }
    }

    /// <summary>
    /// Stamps an <see cref="IntervalContainmentProofRequirement"/> on a computed numeric field's
    /// result expression against its own declared bounds → NumericOverflow when the result interval
    /// exceeds them. Closes the computed-field result-vs-bound gap (computed fields are excluded from
    /// the default collector). Discharged by the existing narrowed interval-containment strategy.
    /// </summary>
    internal static void CollectComputedFieldBoundObligations(SemanticIndex semantics, List<ProofObligation> obligations)
    {
        foreach (var field in semantics.Fields)
        {
            if (!field.IsComputed || field.ComputedExpression is null)
                continue;

            // Open-axis assignment-qualifier residual stamped by the type checker on the computed
            // expression (sited on the computed expr so guard-narrowing discharge can reach it).
            EmitCarriedQualifierObligations(field.DefaultQualifierObligations, field.ComputedExpression,
                new FieldExpressionContext(field), obligations);

            var (min, max) = GetFieldBounds(field);
            if (!min.HasValue && !max.HasValue)
                continue;

            // The obligation is created whenever the computed field carries declared
            // bounds — even when the operand interval is unbounded. An unbounded result
            // stays Unresolved at discharge → NumericOverflow, rather than silently
            // passing as if the bound were vacuously satisfied.
            var authoredMin = field.DeclaredMin;
            var authoredMax = field.DeclaredMax;
            var minStr = (authoredMin ?? min)?.ToString() ?? "−∞";
            var maxStr = (authoredMax ?? max)?.ToString() ?? "+∞";
            var intervalReq = new IntervalContainmentProofRequirement(
                new SelfSubject(),
                field.Name,
                min, max,
                authoredMin, authoredMax,
                $"Interval containment: computed value of '{field.Name}' must be within declared bounds [{minStr} .. {maxStr}]");

            obligations.Add(new ProofObligation(
                intervalReq,
                field.ComputedExpression,
                new FieldExpressionContext(field),
                ProofDisposition.Unresolved,
                null,
                null));
        }
    }

    /// <summary>
    /// Stamps one <see cref="NumericProofRequirement"/> per applicable value-bounding modifier
    /// (declared + implied) carried as a SelfValue numeric satisfaction. The obligation carries the
    /// violated-modifier label and the authored display value so the proof stage reconstructs the
    /// OutOfRange diagnostic verbatim; the discharge evaluates the default's static value against the
    /// (normalized) bound.
    /// </summary>
    private static void CollectNumericDefaultObligations(
        ImmutableArray<ModifierKind> modifiers,
        ImmutableArray<ModifierKind> impliedModifiers,
        decimal? normalizedDeclaredMin,
        decimal? normalizedDeclaredMax,
        TypeKind targetType,
        TypedExpression defaultExpr,
        ObligationContext context,
        List<ProofObligation> obligations)
    {
        var displayValue = DefaultDisplayValue(defaultExpr);

        var allModifiers = impliedModifiers.IsDefaultOrEmpty
            ? modifiers
            : modifiers.Concat(impliedModifiers);

        foreach (var modKind in allModifiers)
        {
            if (Modifiers.GetMeta(modKind) is not ValueModifierMeta meta || meta.ProofSatisfactions is null)
                continue;

            foreach (var satisfaction in meta.ProofSatisfactions)
            {
                if (satisfaction is not ProofSatisfaction.Numeric numeric) continue;
                if (numeric.Projection is not SatisfactionProjection.SelfValue) continue;

                decimal? bound = numeric.Bound switch
                {
                    NumericBoundSource.Constant c => c.Value,
                    NumericBoundSource.DeclarationValue => modKind switch
                    {
                        ModifierKind.Min => normalizedDeclaredMin,
                        ModifierKind.Max => normalizedDeclaredMax,
                        _ => null,
                    },
                    _ => null,
                };
                if (bound is null) continue;

                var label = meta.Token.Text ?? modKind.ToString();
                obligations.Add(new ProofObligation(
                    new NumericProofRequirement(
                        new SelfSubject(),
                        numeric.Comparison,
                        bound.Value,
                        $"Default value of '{DescribeContextTarget(context)}' must satisfy '{label}'",
                        BoundModifierLabel: label,
                        DisplayValue: displayValue),
                    defaultExpr,
                    context,
                    ProofDisposition.Unresolved,
                    null,
                    null));
            }
        }
    }

    /// <summary>
    /// Stamps a <see cref="LengthContainmentProofRequirement"/> for a bounded string default (literal
    /// string), discharged by <see cref="TryLengthContainmentProof"/> against the value's length
    /// interval. <c>notempty</c> folds to a <c>minlength 1</c> lower bound (it sets no DeclaredMinLength).
    /// </summary>
    private static void CollectLengthDefaultObligation(
        TypeKind targetType,
        ImmutableArray<ModifierKind> modifiers,
        int? declaredMinLength,
        int? declaredMaxLength,
        string targetName,
        TypedExpression defaultExpr,
        ObligationContext context,
        List<ProofObligation> obligations)
    {
        if (targetType != TypeKind.String)
            return;
        if (defaultExpr is not TypedLiteral { ResultType: TypeKind.String })
            return;

        var minLength = declaredMinLength;
        if (!modifiers.IsDefaultOrEmpty && modifiers.Contains(ModifierKind.Notempty))
            minLength = Math.Max(minLength ?? 0, 1); // notempty ⇒ length ≥ 1

        if (!minLength.HasValue && !declaredMaxLength.HasValue)
            return;

        obligations.Add(new ProofObligation(
            new LengthContainmentProofRequirement(
                new SelfSubject(),
                targetName,
                minLength,
                declaredMaxLength,
                $"Length containment: default of '{targetName}' must have length in [{minLength?.ToString() ?? "0"} .. {declaredMaxLength?.ToString() ?? "∞"}]"),
            defaultExpr,
            context,
            ProofDisposition.Unresolved,
            null,
            null));
    }

    /// <summary>
    /// Stamps a <see cref="CountContainmentProofRequirement"/> for a collection field whose
    /// <c>default [...]</c> literal has a statically-known element count, against the field's declared
    /// mincount/maxcount. The default count is exact (the literal's element count), so the post-mutation
    /// interval is the point <c>[n, n]</c> — prove-or-reject discharges clean iff that point is in-band,
    /// otherwise <see cref="DiagnosticCode.CountBoundViolation"/> emits (Obligation Generation Contract:
    /// every count-bounded field's default participates). Reuses the shared
    /// <see cref="CountContainmentProofRequirement"/> + <c>TryCountContainmentProof</c> prover.
    /// </summary>
    private static void CollectCountDefaultObligation(
        TypedField field, ObligationContext context, List<ProofObligation> obligations)
    {
        if (!field.DeclaredMinCount.HasValue && !field.DeclaredMaxCount.HasValue)
            return;
        if (field.DefaultExpression is not TypedListLiteral list)
            return;

        var count = list.Elements.Length;
        obligations.Add(new ProofObligation(
            new CountContainmentProofRequirement(
                new SelfSubject(),
                field.Name,
                field.DeclaredMinCount,
                field.DeclaredMaxCount,
                CountLower: count,
                CountUpper: count,
                $"Count containment: default of '{field.Name}' has {count} element(s), must be in [{field.DeclaredMinCount?.ToString() ?? "0"} .. {field.DeclaredMaxCount?.ToString() ?? "∞"}]"),
            field.DefaultExpression,
            context,
            ProofDisposition.Unresolved,
            null,
            null));
    }

    private static void EmitCarriedQualifierObligations(
        ImmutableArray<ProofRequirement> carried,
        TypedExpression? site,
        ObligationContext context,
        List<ProofObligation> obligations)
    {
        if (carried.IsDefaultOrEmpty || site is null)
            return;
        foreach (var requirement in carried)
            obligations.Add(new ProofObligation(requirement, site, context, ProofDisposition.Unresolved, null, null));
    }

    private static string DescribeContextTarget(ObligationContext context) => context switch
    {
        FieldDefaultContext fdc => fdc.Field.Name,
        ArgDefaultContext adc => adc.Arg.Name,
        _ => "value",
    };

    /// <summary>
    /// The authored display value for the OutOfRange message: a typed constant's raw text (quoted),
    /// a bare literal's value, or the static magnitude — matching the prior type-stage formatting so
    /// the relocated diagnostic is byte-identical for constant defaults.
    /// </summary>
    private static string DefaultDisplayValue(TypedExpression defaultExpr) => defaultExpr switch
    {
        TypedTypedConstant ttc => "'" + ttc.RawText + "'",
        // Numeric literals route through the same magnitude→InvariantCulture path the prior
        // type-stage helper used, so the display value is culture-independent and byte-identical
        // (e.g. a decimal default renders "1.50", never "1,50").
        _ => TypedExpressionMagnitude.TryGetStaticMagnitude(defaultExpr, out var m)
            ? m.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : DescribeExpression(defaultExpr),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  Part C — Arg default obligation collector
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates declared-bound proof obligations for event-arg defaults — the same numeric
    /// (OutOfRange), length, and assignment-qualifier-residual treatment as field defaults. Event
    /// args carry no implied modifiers. Normalized bounds drive the numeric obligation's threshold.
    /// </summary>
    internal static void CollectArgDefaultObligations(SemanticIndex semantics, List<ProofObligation> obligations)
    {
        foreach (var evt in semantics.Events)
        {
            foreach (var arg in evt.Args)
            {
                EmitCarriedQualifierObligations(arg.DefaultQualifierObligations, arg.DefaultExpression,
                    new ArgDefaultContext(arg), obligations);

                if (arg.DefaultExpression is null)
                    continue;

                var context = new ArgDefaultContext(arg);

                CollectNumericDefaultObligations(
                    arg.Modifiers, ImmutableArray<ModifierKind>.Empty,
                    arg.NormalizedDeclaredMin ?? arg.DeclaredMin,
                    arg.NormalizedDeclaredMax ?? arg.DeclaredMax,
                    arg.ResolvedType, arg.DefaultExpression, context, obligations);

                CollectLengthDefaultObligation(
                    arg.ResolvedType, arg.Modifiers, arg.DeclaredMinLength, arg.DeclaredMaxLength,
                    arg.Name, arg.DefaultExpression, context, obligations);
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
