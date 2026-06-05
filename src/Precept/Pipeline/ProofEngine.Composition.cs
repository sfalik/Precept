using System.Collections.Immutable;
using System.Linq;
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
    /// Finds all <see cref="InterpolatedTypedConstant"/> nodes assigned to the
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

    /// <summary>
    /// The rule/ensure indices that must NOT contribute a trusted fact, for two reasons:
    /// (1) a rule or ensure whose own proof obligation is still Unresolved is itself unproven, so
    /// deriving a fact from its condition would be circular; and (2) a rule whose own predicate is
    /// self-unsatisfiable against its field's declared bounds (<c>⟦field⟧₀ ⊓ predicate-narrowing =
    /// ∅</c>) cannot hold for any value of the field, so folding it as a discharge fact would falsely
    /// "prove safe" a dependent fault-prone op. Blocking the self-unsatisfiable rule removes BOTH its
    /// magnitude <see cref="ScopedNumericFact"/> and its relational <see cref="FieldToFieldConstraint"/>
    /// at the shared collection point — the single mechanism that converges the magnitude and
    /// relational contradiction arms. The self-unsatisfiability judgment reuses the same
    /// predicate-vs-bounds emptiness computation the satisfiability self-unsat pre-pass uses
    /// (<c>ComposeRulePredicateWithFieldBounds</c>), so the block and that scan agree. Shared by the
    /// magnitude-fact and relational-fact collectors so both honor the same discipline.
    /// </summary>
    private static (HashSet<int> Rules, HashSet<int> Ensures) CollectBlockedConstraints(
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

        // A rule whose own predicate is self-unsatisfiable against its field's declared bounds
        // contributes no discharge fact, regardless of its own obligation disposition.
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            if (blockedRules.Contains(i))
                continue;
            if (RulePredicateSelfUnsatisfiable(semantics.Rules[i], semantics))
                blockedRules.Add(i);
        }

        return (blockedRules, blockedEnsures);
    }

    /// <summary>
    /// Whether a rule's own predicate (and its <c>when</c> guard, if any) is unsatisfiable against
    /// its field's declared bounds — any field whose composed interval is empty. Reuses the
    /// satisfiability pre-pass's predicate-vs-bounds composition so the discharge-side block and the
    /// PRE0159 scan agree on "self-unsatisfiable".
    /// </summary>
    private static bool RulePredicateSelfUnsatisfiable(TypedRule rule, SemanticIndex semantics)
    {
        foreach (var (_, interval) in ComposeRulePredicateWithFieldBounds(rule, semantics))
        {
            if (interval.IsEmpty)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Extracts the unconditional field-to-field relational facts declared by rules of shape
    /// <c>fieldRef op fieldRef</c> (<c>&gt;=</c>/<c>&gt;</c>/<c>&lt;=</c>/<c>&lt;</c>). A guarded
    /// rule holds only under its guard, so it contributes no global relational fact (it is dropped;
    /// its in-scope discharge stays on the guard-sourced flow-narrowing path). A self-relation
    /// (<c>X op X</c>) is vacuous and dropped. A rule whose own obligation is unresolved is blocked
    /// (same self-consistency discipline as the magnitude facts). The result feeds the discharge-time
    /// narrowed-interval builder and the sign-set fold — never the bare <see cref="ExtractFieldInterval"/>
    /// the satisfiability scans read.
    /// </summary>
    private static List<FieldToFieldConstraint> CollectRelationalFacts(
        List<ProofObligation> obligations,
        bool[] suppressDiagnostics,
        SemanticIndex semantics)
    {
        var (blockedRules, _) = CollectBlockedConstraints(obligations, suppressDiagnostics, semantics);

        var facts = new List<FieldToFieldConstraint>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            if (blockedRules.Contains(i))
                continue;
            if (semantics.Rules[i].Guard is not null)
                continue;
            if (TryGetRelationalFact(semantics.Rules[i].Condition, out var fact))
                facts.Add(fact);
        }

        return facts;
    }

    /// <summary>
    /// Recognizes a rule condition of shape <c>fieldRef op fieldRef</c> with
    /// <c>op ∈ {&gt;=, &gt;, &lt;=, &lt;}</c> over two DISTINCT fields, yielding the relation as a
    /// <see cref="FieldToFieldConstraint"/> — the same record the guard-sourced path produces. A
    /// self-relation (same field both sides) is rejected as vacuous.
    /// </summary>
    private static bool TryGetRelationalFact(TypedExpression condition, out FieldToFieldConstraint fact)
    {
        fact = default!;
        if (condition is not TypedBinaryOp comparison)
            return false;
        if (comparison.Left is not TypedFieldRef leftRef || comparison.Right is not TypedFieldRef rightRef)
            return false;
        if (string.Equals(leftRef.FieldName, rightRef.FieldName, StringComparison.Ordinal))
            return false;

        var op = Operations.GetMeta(comparison.ResolvedOp).Op;
        if (op is not (OperatorKind.GreaterThan or OperatorKind.GreaterThanOrEqual
            or OperatorKind.LessThan or OperatorKind.LessThanOrEqual))
            return false;

        fact = new FieldToFieldConstraint(leftRef.FieldName, op, rightRef.FieldName);
        return true;
    }

    private static List<ScopedNumericFact> CollectTrustedNumericFacts(
        List<ProofObligation> obligations,
        bool[] suppressDiagnostics,
        SemanticIndex semantics)
    {
        var (blockedRules, blockedEnsures) = CollectBlockedConstraints(obligations, suppressDiagnostics, semantics);

        var facts = new List<ScopedNumericFact>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            if (blockedRules.Contains(i))
                continue;
            // A guarded rule holds only under its guard — it is NOT an unconditional fact
            // about the field's global interval/sign. Folding it as a global magnitude fact
            // would discharge an obligation on a guard-false path (a false "safe"). Mirror the
            // ensure-path filter (TryGetNumericEnsureFact) so a guarded rule contributes no
            // global magnitude fact; its in-scope discharge stays on the guard-sourced path.
            if (semantics.Rules[i].Guard is not null)
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
        if (!TypedExpressionMagnitude.TryGetStaticMagnitude(expression, out value))
            return false;

        // For Quantity/Price typed-constants, normalize to base unit so
        // cross-precept subsumption compares unit-equivalent magnitudes.
        if (expression is TypedTypedConstant ttc)
        {
            value = ttc.ParsedValue switch
            {
                ValueTuple<decimal, UcumParsedUnit?> (var qm, var unit) when ttc.ResultType == TypeKind.Quantity =>
                    TypedConstantNormalizer.NormalizeQuantity(qm, unit),
                ValueTuple<decimal, object?, UcumParsedUnit?> (var pm, _, var denomUnit) when ttc.ResultType == TypeKind.Price =>
                    TypedConstantNormalizer.NormalizePrice(pm, denomUnit),
                _ => value,
            };
        }

        return true;
    }

    private static ImmutableArray<InterpolatedTypedConstant> FindInterpolatedAssignments(
        string fieldName, SemanticIndex semantics)
    {
        var builder = ImmutableArray.CreateBuilder<InterpolatedTypedConstant>();
        bool hasNonInterpolated = false;

        void ScanActions(ImmutableArray<TypedAction> actions)
        {
            foreach (var action in actions)
            {
                if (action is TypedInputAction { FieldName: var name, InputExpression: var expr }
                    && string.Equals(name, fieldName, StringComparison.Ordinal))
                {
                    if (expr is InterpolatedTypedConstant itc)
                        builder.Add(itc);
                    else
                        hasNonInterpolated = true;
                }
            }
        }

        foreach (var row in semantics.TransitionRows)
            if (row is TypedTransitionRowSuccess s1) ScanActions(s1.Actions);
        foreach (var handler in semantics.EventHandlers)
            if (handler is TypedEventRowSuccess s2) ScanActions(s2.Actions);

        return hasNonInterpolated ? ImmutableArray<InterpolatedTypedConstant>.Empty : builder.ToImmutable();
    }

    /// <summary>
    /// Extracts the magnitude slot source expression from an interpolated typed constant.
    /// Falls back to whole-value slot if no magnitude slot exists.
    /// </summary>
    private static TypedExpression? GetMagnitudeSlotSource(InterpolatedTypedConstant itc)
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

        // An unconditional field-to-field relation gives the subject a sign when the related
        // field's NON-relational interval has a decidable-sign bound: e.g. `X > Y` with ⟦Y⟧₀'s
        // lower bound ≥ 0 establishes X positive. The related field is read one hop, via the bare
        // ExtractFieldInterval — never the discharge-time narrowed dict — so this cannot chase a
        // third field transitively. Field-only relations apply to fields only.
        if (subject.Kind == NumericSubjectKind.Field)
        {
            foreach (var relation in CollectUnconditionalRelationalFacts(semantics))
            {
                if (TryRelationalSignForField(relation, subject.Name, semantics, out var relSigns))
                {
                    signSet &= relSigns;
                    constrained = true;
                }
            }
        }

        return constrained && signSet != NumericSignSet.None
            ? signSet
            : NumericSignSet.Unknown;
    }

    /// <summary>
    /// The unconditional field-to-field relations declared by rules, derived directly from
    /// <see cref="SemanticIndex.Rules"/> with the guard-null + distinct-fields filter. Used by the
    /// sign-set fold, which runs deep inside the discharge recursion where the per-obligation
    /// blocked-rule set is not threaded; circularity is structurally impossible here because a
    /// relation gives the subject a sign from the RELATED field's declared interval, never from the
    /// subject's own unproven obligation.
    /// </summary>
    private static IEnumerable<FieldToFieldConstraint> CollectUnconditionalRelationalFacts(SemanticIndex semantics)
    {
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            if (semantics.Rules[i].Guard is not null)
                continue;
            if (TryGetRelationalFact(semantics.Rules[i].Condition, out var fact))
                yield return fact;
        }
    }

    /// <summary>
    /// Whether a relation <c>subject op related</c> contradicts the subject's OWN declared bounds:
    /// the half-line the relation licenses for the subject, intersected with the subject's
    /// non-relational interval, is empty (⊥). An empty intersection is a proof the relation cannot
    /// hold for any value of the subject in its declared range — the octagon/DBM "empty zone =
    /// infeasible" signal — so the relation must contribute NO discharge fact (neither a narrowed
    /// interval nor a sign). The orientation is normalized so <paramref name="field"/> is the subject;
    /// the related field is read ONE HOP via the bare non-relational interval. Shared by the interval
    /// fold, the sign fold, and the reader backstop so they cannot diverge on what counts as a
    /// contradiction. An unbounded related operand makes the half-line ±∞ on the relevant side, so the
    /// intersection is the subject's own interval — never empty from the relation alone.
    /// </summary>
    private static bool RelationContradictsSubjectBounds(
        FieldToFieldConstraint relation,
        string field,
        SemanticIndex semantics)
    {
        string relatedField;
        OperatorKind op;
        if (string.Equals(relation.LeftField, field, StringComparison.Ordinal))
        {
            relatedField = relation.RightField;
            op = relation.Comparison;
        }
        else if (string.Equals(relation.RightField, field, StringComparison.Ordinal))
        {
            relatedField = relation.LeftField;
            op = InvertOp(relation.Comparison);
        }
        else
        {
            return false;
        }

        var subjectInterval = ExtractFieldInterval(field, semantics);
        if (subjectInterval.IsUnbounded)
            subjectInterval = new NumericInterval(decimal.MinValue, decimal.MaxValue);

        var halfLine = RelationalHalfLine(op, ExtractFieldInterval(relatedField, semantics), GetFieldType(field, semantics));
        if (halfLine is not { } hl)
            return false;

        return subjectInterval.Intersect(hl).IsEmpty;
    }

    /// <summary>
    /// Maps a relation that names <paramref name="field"/> on one side to the sign it implies for
    /// that field, sourced from the OTHER field's non-relational interval. Only a relation whose
    /// related-field bound has a decidable sign yields a sign; an unbounded related operand yields
    /// nothing (identity). Orientation is normalized so the subject sits on the left.
    /// </summary>
    private static bool TryRelationalSignForField(
        FieldToFieldConstraint relation,
        string field,
        SemanticIndex semantics,
        out NumericSignSet signSet)
    {
        signSet = NumericSignSet.Unknown;

        // A relation that contradicts the subject's own declared bounds (empty intersection) is a
        // proven infeasibility, not a sign carrier — withhold the sign so the dependent op falls
        // back to its real (failing) proof. Same emptiness computation as the interval fold.
        if (RelationContradictsSubjectBounds(relation, field, semantics))
            return false;

        string relatedField;
        OperatorKind op;
        if (string.Equals(relation.LeftField, field, StringComparison.Ordinal))
        {
            relatedField = relation.RightField;
            op = relation.Comparison;
        }
        else if (string.Equals(relation.RightField, field, StringComparison.Ordinal))
        {
            relatedField = relation.LeftField;
            op = InvertOp(relation.Comparison);
        }
        else
        {
            return false;
        }

        // Read the related field ONE HOP, via the bare non-relational interval.
        var relatedInterval = ExtractFieldInterval(relatedField, semantics);
        if (relatedInterval.IsUnbounded)
            return false;

        // X op Y where the relevant bound of ⟦Y⟧₀ decides X's sign:
        //   X >  Y, Y's lower bound ≥ 0  ⇒ X positive
        //   X >= Y, Y's lower bound > 0  ⇒ X positive;  Y's lower bound == 0 ⇒ X nonnegative
        //   X <  Y, Y's upper bound ≤ 0  ⇒ X negative
        //   X <= Y, Y's upper bound < 0  ⇒ X negative;  Y's upper bound == 0 ⇒ X nonpositive
        switch (op)
        {
            case OperatorKind.GreaterThan when relatedInterval.Min >= 0m:
                signSet = NumericSignSet.Positive;
                return true;
            case OperatorKind.GreaterThanOrEqual when relatedInterval.Min > 0m:
                signSet = NumericSignSet.Positive;
                return true;
            case OperatorKind.GreaterThanOrEqual when relatedInterval.Min == 0m:
                signSet = NumericSignSet.Nonnegative;
                return true;
            case OperatorKind.LessThan when relatedInterval.Max <= 0m:
                signSet = NumericSignSet.Negative;
                return true;
            case OperatorKind.LessThanOrEqual when relatedInterval.Max < 0m:
                signSet = NumericSignSet.Negative;
                return true;
            case OperatorKind.LessThanOrEqual when relatedInterval.Max == 0m:
                signSet = NumericSignSet.Nonpositive;
                return true;
            default:
                return false;
        }
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

