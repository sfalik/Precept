using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // ════════════════════════════════════════════════════════════════════════════
    //  S6 — Strategy 6: Compositional Constraint Propagation
    // ════════════════════════════════════════════════════════════════════════════

    private static bool TryCompositionalConstraintProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is not NumericProofRequirement numericReq)
            return false;

        var subject = ResolveSubject(numericReq.Subject, obligation.Site);
        if (subject is not null
            && SignSetSatisfiesRequirement(
                ResolveNumericSignSet(subject, obligation.Context, ImmutableArray<ScopedNumericFact>.Empty, semantics),
                numericReq))
        {
            return true;
        }

        // Resolve the target field from the obligation subject
        var fieldName = GetFieldName(numericReq.Subject, obligation.Site);
        if (fieldName is null) return false;

        // Find ALL assignments to this field across transition rows and event handlers
        var interpolatedAssignments = FindInterpolatedAssignments(fieldName, semantics);

        // No interpolated assignments → decline
        if (interpolatedAssignments.Length == 0) return false;

        // For each interpolated assignment, extract the relevant slot source and
        // verify its modifiers satisfy the numeric obligation
        foreach (var assignment in interpolatedAssignments)
        {
            var slotSource = GetMagnitudeSlotSource(assignment);
            if (slotSource is null) return false;

            // Resolve the source's modifiers (field or arg)
            var modifiers = ResolveSourceModifiers(slotSource, semantics);
            if (modifiers.IsDefault || modifiers.IsEmpty) return false;

            bool covered = false;
            foreach (var modifier in modifiers)
            {
                var meta = Modifiers.GetMeta(modifier);
                if (meta is not ValueModifierMeta vmm) continue;

                foreach (var satisfaction in vmm.ProofSatisfactions)
                {
                    if (SatisfactionCovers(satisfaction, numericReq))
                    {
                        covered = true;
                        break;
                    }
                }
                if (covered) break;
            }

            if (!covered) return false;
        }

        return true;
    }

    /// <summary>
    /// Finds all <see cref="TypedInterpolatedTypedConstant"/> nodes assigned to the
    /// named field across all transition rows and event handlers. If ANY assignment
    /// to this field is NOT an interpolated typed constant, returns empty (conservative).
    /// </summary>
    private static void ApplyTrustedRuleFacts(
        List<ProofObligation> obligations,
        bool[] suppressDiagnostics,
        SemanticIndex semantics)
    {
        var trustedFacts = CollectTrustedNumericFacts(obligations, suppressDiagnostics, semantics);
        if (trustedFacts.Count == 0)
            return;

        var trustedFactsArray = trustedFacts.ToImmutableArray();

        for (int i = 0; i < obligations.Count; i++)
        {
            if (suppressDiagnostics[i])
                continue;

            var obligation = obligations[i];
            if (obligation.Disposition != ProofDisposition.Unresolved)
                continue;
            if (obligation.Requirement is not NumericProofRequirement numeric)
                continue;

            var subject = ResolveSubject(numeric.Subject, obligation.Site);
            if (subject is null)
                continue;

            var signSet = ResolveNumericSignSet(subject, obligation.Context, trustedFactsArray, semantics);
            if (!SignSetSatisfiesRequirement(signSet, numeric))
                continue;

            obligations[i] = obligation with
            {
                Disposition = ProofDisposition.Proved,
                Strategy = ProofStrategy.CompositionalConstraint,
            };
        }
    }

    private static List<ScopedNumericFact> CollectTrustedNumericFacts(
        List<ProofObligation> obligations,
        bool[] suppressDiagnostics,
        SemanticIndex semantics)
    {
        var blockedRules = new HashSet<int>();
        var blockedEnsures = new HashSet<int>();

        for (int i = 0; i < obligations.Count; i++)
        {
            if (suppressDiagnostics[i])
                continue;

            if (obligations[i].Disposition != ProofDisposition.Unresolved
                || obligations[i].Context is not ConstraintContext constraintContext)
            {
                continue;
            }

            switch (constraintContext.Constraint)
            {
                case RuleIdentity ruleIdentity:
                    blockedRules.Add(ruleIdentity.RuleIndex);
                    break;
                case EnsureIdentity ensureIdentity:
                    blockedEnsures.Add(ensureIdentity.EnsureIndex);
                    break;
            }
        }

        var facts = new List<ScopedNumericFact>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            if (blockedRules.Contains(i))
                continue;
            if (TryGetNumericConstraintFact(semantics.Rules[i].Condition, null, null, out var fact))
                facts.Add(fact);
        }

        for (int i = 0; i < semantics.Ensures.Length; i++)
        {
            if (blockedEnsures.Contains(i))
                continue;
            if (TryGetNumericEnsureFact(semantics.Ensures[i], out var fact))
                facts.Add(fact);
        }

        return facts;
    }

    private static bool TryGetNumericEnsureFact(TypedEnsure ensure, out ScopedNumericFact fact)
    {
        fact = default;

        // Guarded ensures are conditional — they must NOT become unconditional numeric facts.
        if (ensure.Guard is not null)
            return false;

        return ensure.Kind switch
        {
            ConstraintKind.EventPrecondition => TryGetNumericConstraintFact(ensure.Condition, null, ensure.AnchorEvent, out fact),
            ConstraintKind.StateResident => TryGetNumericConstraintFact(ensure.Condition, ensure.AnchorState, null, out fact),
            _ => false,
        };
    }

    private static bool TryGetNumericConstraintFact(
        TypedExpression condition,
        string? anchorState,
        string? anchorEvent,
        out ScopedNumericFact fact)
    {
        fact = default;

        if (condition is not TypedBinaryOp comparison)
            return false;

        var comparisonOp = Operations.GetMeta(comparison.ResolvedOp).Op;

        if (TryGetNumericSubjectRef(comparison.Left, out var leftSubject)
            && TryGetStaticNumericValue(comparison.Right, out var rightValue))
        {
            fact = new ScopedNumericFact(leftSubject, comparisonOp, rightValue, anchorState, anchorEvent);
            return true;
        }

        if (TryGetNumericSubjectRef(comparison.Right, out var rightSubject)
            && TryGetStaticNumericValue(comparison.Left, out var leftValue))
        {
            fact = new ScopedNumericFact(rightSubject, InvertOp(comparisonOp), leftValue, anchorState, anchorEvent);
            return true;
        }

        return false;
    }

    private static bool TryGetStaticNumericValue(TypedExpression expression, out decimal value)
    {
        switch (expression)
        {
            case TypedLiteral literal when ToDecimal(literal.Value) is { } literalValue:
                value = literalValue;
                return true;

            case TypedTypedConstant typedConstant when TryGetTypedConstantMagnitude(typedConstant.ParsedValue, out var typedConstantValue):
                value = typedConstantValue;
                return true;

            case TypedInterpolatedTypedConstant { StaticMagnitude: { } magnitude }:
                value = magnitude;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private static bool TryGetTypedConstantMagnitude(object? parsedValue, out decimal value)
    {
        switch (parsedValue)
        {
            case decimal direct:
                value = direct;
                return true;
            case int integer:
                value = integer;
                return true;
            case long whole:
                value = whole;
                return true;
            case ValueTuple<decimal, object?> money:
                value = money.Item1;
                return true;
            case ValueTuple<decimal, UcumParsedUnit?> quantity:
                value = quantity.Item1;
                return true;
            case ValueTuple<decimal, object?, UcumParsedUnit?> price:
                value = price.Item1;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static ImmutableArray<TypedInterpolatedTypedConstant> FindInterpolatedAssignments(
        string fieldName, SemanticIndex semantics)
    {
        var builder = ImmutableArray.CreateBuilder<TypedInterpolatedTypedConstant>();
        bool hasNonInterpolated = false;

        void ScanActions(ImmutableArray<TypedAction> actions)
        {
            foreach (var action in actions)
            {
                if (action is TypedInputAction { FieldName: var name, InputExpression: var expr }
                    && string.Equals(name, fieldName, StringComparison.Ordinal))
                {
                    if (expr is TypedInterpolatedTypedConstant itc)
                        builder.Add(itc);
                    else
                        hasNonInterpolated = true;
                }
            }
        }

        foreach (var row in semantics.TransitionRows)
            ScanActions(row.Actions);
        foreach (var handler in semantics.EventHandlers)
            ScanActions(handler.Actions);

        return hasNonInterpolated ? ImmutableArray<TypedInterpolatedTypedConstant>.Empty : builder.ToImmutable();
    }

    /// <summary>
    /// Extracts the magnitude slot source expression from an interpolated typed constant.
    /// Falls back to whole-value slot if no magnitude slot exists.
    /// </summary>
    private static TypedExpression? GetMagnitudeSlotSource(TypedInterpolatedTypedConstant itc)
    {
        TypedExpression? wholeValue = null;

        foreach (var slot in itc.Slots)
        {
            if (slot.SlotKind == InterpolationSlotKind.Magnitude)
                return slot.Expression;
            if (slot.SlotKind == InterpolationSlotKind.WholeValue)
                wholeValue = slot.Expression;
        }

        return wholeValue;
    }

    /// <summary>
    /// Resolves the declared modifiers for a source expression that is either a
    /// <see cref="TypedFieldRef"/> (look up field modifiers) or a
    /// <see cref="TypedArgRef"/> (look up event arg modifiers).
    /// </summary>
    private static ImmutableArray<ModifierKind> ResolveSourceModifiers(
        TypedExpression source, SemanticIndex semantics)
    {
        if (source is TypedFieldRef fieldRef
            && semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out var field))
        {
            return field.Modifiers;
        }

        if (source is TypedArgRef argRef
            && semantics.EventsByName.TryGetValue(argRef.EventName, out var evt))
        {
            foreach (var arg in evt.Args)
            {
                if (string.Equals(arg.Name, argRef.ArgName, StringComparison.Ordinal))
                    return arg.Modifiers;
            }
        }

        return ImmutableArray<ModifierKind>.Empty;
    }

    private static NumericSignSet ResolveNumericSignSet(
        TypedExpression expression,
        ObligationContext context,
        ImmutableArray<ScopedNumericFact> trustedFacts,
        SemanticIndex semantics)
    {
        if (TryGetStaticNumericValue(expression, out var value))
            return GetExactSignSet(value);

        if (TryGetNumericSubjectRef(expression, out var subject))
            return ResolveNumericSubjectSignSet(subject, context, trustedFacts, semantics);

        return expression switch
        {
            TypedUnaryOp unaryOp => ResolveUnarySignSet(unaryOp, context, trustedFacts, semantics),
            TypedBinaryOp binaryOp => ResolveBinarySignSet(binaryOp, context, trustedFacts, semantics),
            TypedFunctionCall functionCall when ResolveFunctionOverload(functionCall)?.ReturnNonnegative == true => NumericSignSet.Nonnegative,
            TypedMemberAccess { ResolvedAccessor: FixedReturnAccessor { ReturnNonnegative: true } } => NumericSignSet.Nonnegative,
            TypedConditional conditional => ResolveNumericSignSet(conditional.ThenBranch, context, trustedFacts, semantics)
                                         | ResolveNumericSignSet(conditional.ElseBranch, context, trustedFacts, semantics),
            _ => NumericSignSet.Unknown,
        };
    }

    private static NumericSignSet ResolveUnarySignSet(
        TypedUnaryOp unaryOp,
        ObligationContext context,
        ImmutableArray<ScopedNumericFact> trustedFacts,
        SemanticIndex semantics)
    {
        var operandSigns = ResolveNumericSignSet(unaryOp.Operand, context, trustedFacts, semantics);
        return Operations.GetMeta(unaryOp.ResolvedOp).Op switch
        {
            OperatorKind.Minus => NegateSignSet(operandSigns),
            OperatorKind.Plus => operandSigns,
            _ => NumericSignSet.Unknown,
        };
    }

    private static NumericSignSet ResolveBinarySignSet(
        TypedBinaryOp binaryOp,
        ObligationContext context,
        ImmutableArray<ScopedNumericFact> trustedFacts,
        SemanticIndex semantics)
    {
        var leftSigns = ResolveNumericSignSet(binaryOp.Left, context, trustedFacts, semantics);
        var rightSigns = ResolveNumericSignSet(binaryOp.Right, context, trustedFacts, semantics);

        return Operations.GetMeta(binaryOp.ResolvedOp).Op switch
        {
            OperatorKind.Plus => AddSignSets(leftSigns, rightSigns),
            OperatorKind.Minus => AddSignSets(leftSigns, NegateSignSet(rightSigns)),
            OperatorKind.Times => MultiplySignSets(leftSigns, rightSigns),
            OperatorKind.Divide => DivideSignSets(leftSigns, rightSigns),
            _ => NumericSignSet.Unknown,
        };
    }

    private static NumericSignSet ResolveNumericSubjectSignSet(
        NumericSubjectRef subject,
        ObligationContext context,
        ImmutableArray<ScopedNumericFact> trustedFacts,
        SemanticIndex semantics)
    {
        var signSet = NumericSignSet.Unknown;
        var constrained = false;

        foreach (var modifier in ResolveNumericSubjectModifiers(subject, semantics))
        {
            if (!TryGetModifierSignSet(modifier, out var modifierSigns))
                continue;

            signSet &= modifierSigns;
            constrained = true;
        }

        foreach (var fact in trustedFacts)
        {
            if (!FactAppliesToContext(fact, context)
                || !SubjectsMatch(fact.Subject, subject)
                || !TryMapComparisonToSignSet(fact.Comparison, fact.Value, out var factSigns))
            {
                continue;
            }

            signSet &= factSigns;
            constrained = true;
        }

        return constrained && signSet != NumericSignSet.None
            ? signSet
            : NumericSignSet.Unknown;
    }

    private static ImmutableArray<ModifierKind> ResolveNumericSubjectModifiers(
        NumericSubjectRef subject,
        SemanticIndex semantics)
    {
        if (subject.Kind == NumericSubjectKind.Field
            && semantics.FieldsByName.TryGetValue(subject.Name, out var field))
        {
            return field.Modifiers.AddRange(field.ImpliedModifiers);
        }

        if (subject.Kind == NumericSubjectKind.Arg
            && subject.EventName is not null
            && semantics.EventsByName.TryGetValue(subject.EventName, out var evt))
        {
            foreach (var arg in evt.Args)
            {
                if (string.Equals(arg.Name, subject.Name, StringComparison.Ordinal))
                    return arg.Modifiers;
            }
        }

        return ImmutableArray<ModifierKind>.Empty;
    }

    private static bool TryGetModifierSignSet(ModifierKind modifier, out NumericSignSet signSet)
    {
        signSet = NumericSignSet.Unknown;

        var meta = Modifiers.GetMeta(modifier);
        if (meta is not ValueModifierMeta valueModifier)
            return false;

        foreach (var satisfaction in valueModifier.ProofSatisfactions)
        {
            if (satisfaction is not ProofSatisfaction.Numeric
                {
                    Projection: SatisfactionProjection.SelfValue,
                    Bound: NumericBoundSource.Constant constant,
                } numericSatisfaction)
            {
                continue;
            }

            if (!TryMapComparisonToSignSet(numericSatisfaction.Comparison, constant.Value, out signSet))
                continue;

            return true;
        }

        return false;
    }

    private static bool TryGetNumericSubjectRef(TypedExpression expression, out NumericSubjectRef subject)
    {
        switch (expression)
        {
            case TypedFieldRef fieldRef:
                subject = new NumericSubjectRef(NumericSubjectKind.Field, fieldRef.FieldName);
                return true;
            case TypedArgRef argRef:
                subject = new NumericSubjectRef(NumericSubjectKind.Arg, argRef.ArgName, argRef.EventName);
                return true;
            default:
                subject = default;
                return false;
        }
    }

    private static bool SubjectsMatch(NumericSubjectRef left, NumericSubjectRef right)
        => left.Kind == right.Kind
           && string.Equals(left.Name, right.Name, StringComparison.Ordinal)
           && string.Equals(left.EventName, right.EventName, StringComparison.Ordinal);

    private static bool FactAppliesToContext(ScopedNumericFact fact, ObligationContext context)
    {
        if (fact.AnchorEvent is not null)
        {
            return context switch
            {
                TransitionRowContext transitionRow => string.Equals(transitionRow.Row.EventName, fact.AnchorEvent, StringComparison.Ordinal),
                EventHandlerContext eventHandler => string.Equals(eventHandler.Handler.EventName, fact.AnchorEvent, StringComparison.Ordinal),
                _ => false,
            };
        }

        if (fact.AnchorState is not null)
        {
            return context switch
            {
                TransitionRowContext transitionRow => string.Equals(transitionRow.Row.FromState, fact.AnchorState, StringComparison.Ordinal),
                StateHookContext stateHook => string.Equals(stateHook.Hook.StateName, fact.AnchorState, StringComparison.Ordinal),
                _ => false,
            };
        }

        return true;
    }

    private static bool SignSetSatisfiesRequirement(NumericSignSet signSet, NumericProofRequirement requirement)
    {
        if (requirement.Threshold != 0m)
            return false;

        return requirement.Comparison switch
        {
            OperatorKind.NotEquals => !signSet.HasFlag(NumericSignSet.Zero),
            OperatorKind.GreaterThan => signSet == NumericSignSet.Positive,
            OperatorKind.GreaterThanOrEqual => !signSet.HasFlag(NumericSignSet.Negative),
            OperatorKind.LessThan => signSet == NumericSignSet.Negative,
            OperatorKind.LessThanOrEqual => !signSet.HasFlag(NumericSignSet.Positive),
            _ => false,
        };
    }

    private static bool TryMapComparisonToSignSet(
        OperatorKind comparison,
        decimal value,
        out NumericSignSet signSet)
    {
        signSet = comparison switch
        {
            OperatorKind.GreaterThan when value >= 0m => NumericSignSet.Positive,
            OperatorKind.GreaterThan when value < 0m => NumericSignSet.Unknown,
            OperatorKind.GreaterThanOrEqual when value > 0m => NumericSignSet.Positive,
            OperatorKind.GreaterThanOrEqual when value == 0m => NumericSignSet.Nonnegative,
            OperatorKind.GreaterThanOrEqual => NumericSignSet.Unknown,
            OperatorKind.LessThan when value <= 0m => NumericSignSet.Negative,
            OperatorKind.LessThan when value > 0m => NumericSignSet.Unknown,
            OperatorKind.LessThanOrEqual when value < 0m => NumericSignSet.Negative,
            OperatorKind.LessThanOrEqual when value == 0m => NumericSignSet.Nonpositive,
            OperatorKind.LessThanOrEqual => NumericSignSet.Unknown,
            OperatorKind.Equals => GetExactSignSet(value),
            OperatorKind.NotEquals when value == 0m => NumericSignSet.Nonzero,
            _ => NumericSignSet.Unknown,
        };

        return signSet != NumericSignSet.Unknown;
    }

    private static NumericSignSet GetExactSignSet(decimal value) => value switch
    {
        > 0m => NumericSignSet.Positive,
        < 0m => NumericSignSet.Negative,
        _ => NumericSignSet.Zero,
    };

    private static NumericSignSet NegateSignSet(NumericSignSet signSet)
    {
        var result = NumericSignSet.None;
        if (signSet.HasFlag(NumericSignSet.Negative)) result |= NumericSignSet.Positive;
        if (signSet.HasFlag(NumericSignSet.Zero)) result |= NumericSignSet.Zero;
        if (signSet.HasFlag(NumericSignSet.Positive)) result |= NumericSignSet.Negative;
        return result;
    }

    private static NumericSignSet AddSignSets(NumericSignSet left, NumericSignSet right)
    {
        var result = NumericSignSet.None;

        foreach (var leftSign in EnumerateSigns(left))
        {
            foreach (var rightSign in EnumerateSigns(right))
            {
                result |= (leftSign, rightSign) switch
                {
                    (NumericSignSet.Positive, NumericSignSet.Positive) => NumericSignSet.Positive,
                    (NumericSignSet.Positive, NumericSignSet.Zero) => NumericSignSet.Positive,
                    (NumericSignSet.Zero, NumericSignSet.Positive) => NumericSignSet.Positive,
                    (NumericSignSet.Zero, NumericSignSet.Zero) => NumericSignSet.Zero,
                    (NumericSignSet.Negative, NumericSignSet.Negative) => NumericSignSet.Negative,
                    (NumericSignSet.Negative, NumericSignSet.Zero) => NumericSignSet.Negative,
                    (NumericSignSet.Zero, NumericSignSet.Negative) => NumericSignSet.Negative,
                    _ => NumericSignSet.Unknown,
                };
            }
        }

        return result == NumericSignSet.None ? NumericSignSet.Unknown : result;
    }

    private static NumericSignSet MultiplySignSets(NumericSignSet left, NumericSignSet right)
    {
        var result = NumericSignSet.None;

        foreach (var leftSign in EnumerateSigns(left))
        {
            foreach (var rightSign in EnumerateSigns(right))
            {
                result |= (leftSign, rightSign) switch
                {
                    (NumericSignSet.Zero, _) or (_, NumericSignSet.Zero) => NumericSignSet.Zero,
                    (NumericSignSet.Positive, NumericSignSet.Positive) => NumericSignSet.Positive,
                    (NumericSignSet.Negative, NumericSignSet.Negative) => NumericSignSet.Positive,
                    (NumericSignSet.Positive, NumericSignSet.Negative) => NumericSignSet.Negative,
                    (NumericSignSet.Negative, NumericSignSet.Positive) => NumericSignSet.Negative,
                    _ => NumericSignSet.Unknown,
                };
            }
        }

        return result == NumericSignSet.None ? NumericSignSet.Unknown : result;
    }

    private static NumericSignSet DivideSignSets(NumericSignSet left, NumericSignSet right)
    {
        if (right.HasFlag(NumericSignSet.Zero))
            return NumericSignSet.Unknown;

        return MultiplySignSets(left, right);
    }

    private static IEnumerable<NumericSignSet> EnumerateSigns(NumericSignSet signSet)
    {
        if (signSet.HasFlag(NumericSignSet.Negative)) yield return NumericSignSet.Negative;
        if (signSet.HasFlag(NumericSignSet.Zero)) yield return NumericSignSet.Zero;
        if (signSet.HasFlag(NumericSignSet.Positive)) yield return NumericSignSet.Positive;
    }

    // ════════════════════════════════════════════════════════════════════════════

    private static bool ContainsErrorExpression(TypedExpression expr) => expr switch
    {
        TypedErrorExpression => true,
        TypedBinaryOp bin => ContainsErrorExpression(bin.Left) || ContainsErrorExpression(bin.Right),
        TypedUnaryOp un => ContainsErrorExpression(un.Operand),
        TypedFunctionCall call => call.Arguments.Any(ContainsErrorExpression),
        TypedMemberAccess ma => ContainsErrorExpression(ma.Object),
        TypedConditional cond => ContainsErrorExpression(cond.Condition)
                                 || ContainsErrorExpression(cond.ThenBranch)
                                 || ContainsErrorExpression(cond.ElseBranch),
        TypedQuantifier quant => ContainsErrorExpression(quant.Collection)
                                 || ContainsErrorExpression(quant.Predicate),
        TypedPostfixOp post => ContainsErrorExpression(post.Operand),
        _ => false
    };
}
