using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

// CATALOG-DRIVEN IMPLEMENTATION GUIDE
//
// Proof obligations are declared in catalog metadata — never hardcoded per
// operator/function/accessor/action. Before writing obligation lists:
//
//   Binary operator obligations → BinaryOperationMeta.ProofRequirements
//   Function overload obligations → FunctionOverload.ProofRequirements
//   Type accessor obligations    → TypeAccessor.ProofRequirements
//   Action obligations           → ActionMeta.ProofRequirements
//   Obligation dispatch          → ProofRequirements.GetMeta(kind)
//
// Subject shape (what the obligation applies to) is encoded in the requirement
// instance (ParamSubject, SelfSubject, etc.) — do not hardcode per-requirement
// subject logic.
//
// See: docs/language/catalog-system.md § ProofEngine-catalog integration pattern

public static partial class ProofEngine
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Internal records for guard decomposition (Strategy 3 + 4)
    // ════════════════════════════════════════════════════════════════════════════

    private record GuardConstraint(
        string Field,
        OperatorKind Comparison,
        decimal? Value,
        bool IsPresenceCheck);

    private record ContainsGuardConstraint(
        string Field,
        bool Negated);

    private record FieldToFieldConstraint(
        string LeftField,
        OperatorKind Comparison,
        string RightField);

    [Flags]
    private enum NumericSignSet
    {
        None = 0,
        Negative = 1,
        Zero = 2,
        Positive = 4,
        Nonpositive = Negative | Zero,
        Nonnegative = Zero | Positive,
        Nonzero = Negative | Positive,
        Unknown = Negative | Zero | Positive,
    }

    private enum NumericSubjectKind
    {
        Field = 1,
        Arg = 2,
    }

    private readonly record struct NumericSubjectRef(
        NumericSubjectKind Kind,
        string Name,
        string? EventName = null);

    private readonly record struct ScopedNumericFact(
        NumericSubjectRef Subject,
        OperatorKind Comparison,
        decimal Value,
        string? AnchorState = null,
        string? AnchorEvent = null);

    // ════════════════════════════════════════════════════════════════════════════
    //  Main entry point
    // ════════════════════════════════════════════════════════════════════════════

    public static ProofLedger Prove(SemanticIndex semantics, StateGraph graph)
    {
        var obligations = CollectObligations(semantics);
        var faultSiteLinks = new List<FaultSiteLink>();
        var diagnostics = new List<Diagnostic>();

        // Incorporate forwarding facts before discharge
        IncorporateForwardingFacts(graph.ProofFacts, obligations, semantics);

        var suppressDiagnostics = new bool[obligations.Count];

        // Pass 2: Obligation Discharge
        for (int i = 0; i < obligations.Count; i++)
        {
            var obligation = obligations[i];

            // Skip obligations already proved by forwarding facts (unreachable/dead-end suppression)
            if (obligation.Disposition == ProofDisposition.Proved)
                continue;

            // PE-G13: Error-tainted obligation suppression
            if (ContainsErrorExpression(obligation.Site))
            {
                obligations[i] = obligation with { Disposition = ProofDisposition.Unresolved };
                suppressDiagnostics[i] = true;
                continue;
            }

            var (disposition, strategy) = TryDischarge(obligation, semantics);
            var enriched = obligation with { Disposition = disposition, Strategy = strategy };

            // Enrich ComputedInterval for interval containment obligations (proved or unresolved).
            if (enriched.Requirement is IntervalContainmentProofRequirement)
            {
                _ = TryIntervalContainmentProofNarrowed(obligation, semantics, out var computed);
                if (computed.HasValue)
                {
                    enriched = enriched with { ComputedInterval = computed };
                }
            }

            obligations[i] = enriched;
        }

        ApplyTrustedRuleFacts(obligations, suppressDiagnostics, semantics);

        for (int i = 0; i < obligations.Count; i++)
        {
            if (suppressDiagnostics[i])
                continue;

            var obligation = obligations[i];
            if (obligation.Disposition != ProofDisposition.Unresolved)
                continue;

            diagnostics.Add(CreateDiagnostic(obligation, semantics));
            faultSiteLinks.Add(CreateFaultSiteLink(obligation));
        }

        var initialStateResults = CheckInitialStateSatisfiability(semantics);
        foreach (var result in initialStateResults)
        {
            if (!result.IsSatisfiable)
            {
                foreach (var violation in result.Violations)
                {
                    diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnsatisfiableInitialState,
                        SourceSpan.Missing,
                        result.StateName,
                        violation.Reason));
                }
            }
        }

        return new ProofLedger(
            obligations.ToImmutableArray(),
            faultSiteLinks.ToImmutableArray(),
            ProjectConstraintInfluence(semantics),
            initialStateResults,
            diagnostics.ToImmutableArray());
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S1 — Pass 1: Obligation Collection
    // ════════════════════════════════════════════════════════════════════════════

    private static List<ProofObligation> CollectObligations(SemanticIndex semantics)
    {
        var obligations = new List<ProofObligation>();

        // TransitionRows[].Actions[] + nested expressions
        foreach (var row in semantics.TransitionRows)
        {
            var ctx = new TransitionRowContext(row);
            WalkActions(row.Actions, ctx, obligations, semantics);
        }

        // EventHandlers[].Actions[]
        foreach (var handler in semantics.EventHandlers)
        {
            var ctx = new EventHandlerContext(handler);
            WalkActions(handler.Actions, ctx, obligations, semantics);
        }

        // StateHooks[].Actions[]
        foreach (var hook in semantics.StateHooks)
        {
            var ctx = new StateHookContext(hook);
            WalkActions(hook.Actions, ctx, obligations, semantics);
        }

        // Rules[].Condition
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var ctx = new ConstraintContext(new RuleIdentity(i));
            WalkExpression(semantics.Rules[i].Condition, ctx, obligations, semantics);
        }

        // Ensures[].Condition
        for (int i = 0; i < semantics.Ensures.Length; i++)
        {
            var ensure = semantics.Ensures[i];
            var ctx = new ConstraintContext(new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, i));
            WalkExpression(ensure.Condition, ctx, obligations, semantics);
        }

        // Fields[].ComputedExpression
        foreach (var field in semantics.Fields)
        {
            if (field.ComputedExpression is not null)
            {
                var ctx = new FieldExpressionContext(field);
                WalkExpression(field.ComputedExpression, ctx, obligations, semantics);
            }
        }

        // Fields[].DefaultExpression (Slice 25: interval containment for interpolated defaults)
        CollectDefaultObligations(semantics, obligations);

        // Events[].Args[].DefaultExpression (Slice 26: interval containment for arg defaults)
        CollectArgDefaultObligations(semantics, obligations);

        return obligations;
    }

    // Slice 12: SemanticIndex is threaded through WalkExpression so that optional field
    // refs in value positions can generate PresenceProofRequirement obligations, including
    // interpolated typed-constant holes.
    private static void WalkExpression(
        TypedExpression expr,
        ObligationContext ctx,
        List<ProofObligation> obligations,
        SemanticIndex semantics,
        bool includeOptionalArgRefs = false)
    {
        switch (expr)
        {
            case TypedFieldRef fieldRef:
                // Slice 12 — Presence Obligation Generation:
                // Every reference to an optional field in a value position generates a
                // PresenceProofRequirement. Strategy 2 (Guaranteed presence) and Strategy 3
                // (when X is set guard-in-path) discharge these obligations; unresolved
                // obligations emit PRE0116 (UnprovedPresenceRequirement).
                if (semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out var referencedField)
                    && referencedField.Presence is DeclaredPresenceMeta.Optional)
                {
                    AddPresenceObligation("Field", fieldRef.FieldName, fieldRef, ctx, obligations);
                }
                break;

            case TypedArgRef argRef when includeOptionalArgRefs:
                if (TryGetArg(argRef, semantics, out var referencedArg)
                    && referencedArg.Presence is DeclaredPresenceMeta.Optional)
                {
                    AddPresenceObligation("Argument", argRef.ArgName, argRef, ctx, obligations);
                }
                break;

            case TypedBinaryOp bin:
                foreach (var req in bin.ProofRequirements)
                    obligations.Add(new ProofObligation(req, bin, ctx, ProofDisposition.Unresolved, null, null));
                WalkExpression(bin.Left, ctx, obligations, semantics, includeOptionalArgRefs);
                WalkExpression(bin.Right, ctx, obligations, semantics, includeOptionalArgRefs);
                break;

            case TypedFunctionCall call:
                foreach (var req in call.ProofRequirements)
                    obligations.Add(new ProofObligation(req, call, ctx, ProofDisposition.Unresolved, null, null));
                foreach (var arg in call.Arguments)
                    WalkExpression(arg, ctx, obligations, semantics, includeOptionalArgRefs);
                break;

            case TypedMemberAccess ma:
                foreach (var req in ma.ProofRequirements)
                    obligations.Add(new ProofObligation(req, ma, ctx, ProofDisposition.Unresolved, null, null));
                WalkExpression(ma.Object, ctx, obligations, semantics, includeOptionalArgRefs);
                break;

            case TypedUnaryOp un:
                WalkExpression(un.Operand, ctx, obligations, semantics, includeOptionalArgRefs);
                break;

            case TypedConditional cond:
                WalkExpression(cond.Condition, ctx, obligations, semantics, includeOptionalArgRefs);
                WalkExpression(cond.ThenBranch, ctx, obligations, semantics, includeOptionalArgRefs);
                WalkExpression(cond.ElseBranch, ctx, obligations, semantics, includeOptionalArgRefs);
                break;

            case TypedQuantifier quant:
                WalkExpression(quant.Collection, ctx, obligations, semantics, includeOptionalArgRefs);
                WalkExpression(quant.Predicate, ctx, obligations, semantics, includeOptionalArgRefs);
                break;

            case TypedPostfixOp:
                // Do NOT recurse into the operand of `X is set` / `X is not set`.
                // TypedPostfixOp is a presence check, not a value-position usage of its operand.
                // Recursing would generate a spurious PresenceProofRequirement on an optional X,
                // defeating the purpose of the presence guard.
                break;

            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                {
                    if (seg is TypedHoleSegment hole)
                        WalkExpression(hole.Expression, ctx, obligations, semantics, includeOptionalArgRefs);
                }
                break;

            case InterpolatedTypedConstant typedConstant:
                foreach (var slot in typedConstant.Slots)
                    WalkExpression(slot.Expression, ctx, obligations, semantics, includeOptionalArgRefs: true);
                break;

            case TypedListLiteral list:
                foreach (var elem in list.Elements)
                    WalkExpression(elem, ctx, obligations, semantics, includeOptionalArgRefs);
                break;
        }
    }

    private static void AddPresenceObligation(
        string subjectKind,
        string subjectName,
        TypedExpression site,
        ObligationContext ctx,
        List<ProofObligation> obligations)
    {
        var presenceReq = new PresenceProofRequirement(
            new SelfSubject(),
            $"{subjectKind} '{subjectName}' is optional and may be absent");
        obligations.Add(new ProofObligation(presenceReq, site, ctx, ProofDisposition.Unresolved, null, null));
    }

    private static bool TryGetArg(TypedArgRef argRef, SemanticIndex semantics, out TypedArg arg)
    {
        arg = null!;
        if (!semantics.EventsByName.TryGetValue(argRef.EventName, out var referencedEvent))
            return false;

        foreach (var candidate in referencedEvent.Args)
        {
            if (string.Equals(candidate.Name, argRef.ArgName, StringComparison.Ordinal))
            {
                arg = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks action declarations in an event/state handler and collects proof obligations.
    /// 
    /// Obligations are sourced from two places:
    /// 1. Static ProofRequirements declared in the Actions catalog (e.g., Dequeue and Pop require queue.count > 0)
    /// 2. Dynamic obligations generated by the DynamicObligationGenerator in action metadata.
    ///    Set actions on fields with catalog-declared interval constraints generate
    ///    IntervalContainmentProofRequirement to ensure the assigned expression's value interval fits
    ///    within the field's declared bounds [min..max].
    ///    This prevents compile-time-provable numeric overflow on assignment.
    /// </summary>
    private static void WalkActions(ImmutableArray<TypedAction> actions, ObligationContext ctx, List<ProofObligation> obligations, SemanticIndex semantics)
    {
        foreach (var action in actions)
        {
            // Static obligations from action metadata (Catalog-driven)
            foreach (var req in action.ProofRequirements)
                obligations.Add(new ProofObligation(req, CreateActionProofSite(action, req), ctx, ProofDisposition.Unresolved, null, null));

            // Dynamic obligations generated by action metadata (Catalog-driven).
            // For Set actions on fields with catalog-declared interval constraints: generates
            // interval containment obligations so assigned values stay within declared bounds.
            var actionMeta = Actions.GetMeta(action.Kind);
            if (actionMeta.DynamicObligationGenerator is not null)
            {
                var dynamicObligations = actionMeta.DynamicObligationGenerator(action, semantics);
                foreach (var dynamicObligation in dynamicObligations)
                {
                    // Update the obligation with the proper context (the obligation generator creates them with null context)
                    var updatedObligation = new ProofObligation(
                        dynamicObligation.Requirement,
                        dynamicObligation.Site,
                        ctx,
                        ProofDisposition.Unresolved,
                        null,
                        null);
                    obligations.Add(updatedObligation);
                }
            }

            if (action is TypedInputAction inputAction)
            {
                WalkExpression(inputAction.InputExpression, ctx, obligations, semantics);
                if (inputAction.SecondaryExpression is not null)
                    WalkExpression(inputAction.SecondaryExpression, ctx, obligations, semantics);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S2 — Subject Resolution Utilities
    // ════════════════════════════════════════════════════════════════════════════

    private static TypedExpression CreateActionProofSite(TypedAction action, ProofRequirement requirement)
    {
        if (requirement is NumericProofRequirement { Subject: SelfSubject }
            || requirement is ModifierRequirement { Subject: SelfSubject }
            || requirement is PresenceProofRequirement { Subject: SelfSubject })
        {
            return new TypedFieldRef(action.FieldType, action.FieldName, false, null, action.Span);
        }

        return action switch
        {
            TypedInputAction ia => ia.InputExpression,
            _ => new TypedLiteral(action.FieldType, null, action.Span)
        };
    }

    private static TypedExpression? ResolveSubject(ProofSubject subject, TypedExpression site)
    {
        return subject switch
        {
            ParamSubject param => site switch
            {
                TypedBinaryOp bin => ResolveParamInBinaryOp(param.Parameter, bin),
                TypedFunctionCall call => ResolveParamInFunctionCall(param.Parameter, call),
                TypedMemberAccess access => ResolveParamInMemberAccess(param.Parameter, access),
                _ => null
            },
            SelfSubject self => site switch
            {
                TypedMemberAccess access => access.Object,
                TypedFieldRef fieldRef => fieldRef,
                TypedArgRef argRef => argRef,
                _ => null
            },
            _ => null
        };
    }

    private static TypedExpression? ResolveParamInBinaryOp(ParameterMeta param, TypedBinaryOp bin)
    {
        var opMeta = Operations.GetMeta(bin.ResolvedOp);
        if (opMeta is BinaryOperationMeta bom)
        {
            // Check Rhs before Lhs: proof requirements (e.g., divisor ≠ 0) target the
            // right operand, and shared ParameterMeta instances make ReferenceEquals
            // match both sides — checking Rhs first resolves the correct operand.
            if (ReferenceEquals(param, bom.Rhs)) return bin.Right;
            if (ReferenceEquals(param, bom.Lhs)) return bin.Left;
        }
        return null;
    }

    private static TypedExpression? ResolveParamInFunctionCall(ParameterMeta param, TypedFunctionCall call)
    {
        var meta = Functions.GetMeta(call.ResolvedFunction);
        foreach (var overload in meta.Overloads)
        {
            for (int i = 0; i < overload.Parameters.Count; i++)
            {
                if (ReferenceEquals(param, overload.Parameters[i]))
                    return i < call.Arguments.Length ? call.Arguments[i] : null;
            }
        }
        return null;
    }

    private static TypedExpression? ResolveParamInMemberAccess(ParameterMeta param, TypedMemberAccess access)
    {
        // Member access proof requirements use SelfSubject, not ParamSubject
        return null;
    }

    private static string? GetFieldName(ProofSubject subject, TypedExpression site)
    {
        var resolved = ResolveSubject(subject, site);
        return GetFieldName(resolved);
    }

    private static string? GetFieldName(TypedExpression? resolved)
    {
        return resolved switch
        {
            TypedFieldRef fieldRef => fieldRef.FieldName,
            TypedArgRef argRef => argRef.ArgName,
            TypedMemberAccess { Object: TypedFieldRef fieldRef } => fieldRef.FieldName,
            _ => null
        };
    }

    private static string DescribeSubject(ProofSubject subject, TypedExpression site)
        => DescribeExpression(ResolveSubject(subject, site));

    private static (string Label, string QualifierValue) DescribeQualifiedSubject(
        ProofSubject subject,
        TypedExpression site,
        QualifierAxis axis,
        SemanticIndex semantics)
        => DescribeQualifiedExpression(ResolveSubject(subject, site), axis, semantics);

    private static (string Label, string QualifierValue) DescribeQualifiedExpression(
        TypedExpression? expr,
        QualifierAxis axis,
        SemanticIndex semantics)
    {
        var label = DescribeExpression(expr);
        var qualifier = expr is null ? null : ResolveQualifierFromExpression(expr, axis, semantics);
        return (label, FormatQualifierValue(qualifier));
    }

    private static string DescribeExpression(TypedExpression? expr) => expr switch
    {
        TypedFieldRef fieldRef => fieldRef.FieldName,
        TypedArgRef argRef => argRef.ArgName,
        TypedMemberAccess memberAccess => $"{DescribeExpression(memberAccess.Object)}.{memberAccess.ResolvedAccessor.Name}",
        TypedBinaryOp binaryOp => $"({DescribeExpression(binaryOp.Left)} {DescribeOperator(binaryOp.ResolvedOp)} {DescribeExpression(binaryOp.Right)})",
        TypedUnaryOp unaryOp => $"{DescribeOperator(unaryOp.ResolvedOp)}{DescribeExpression(unaryOp.Operand)}",
        TypedFunctionCall functionCall => $"{functionCall.ResolvedFunction}(...)",
        TypedLiteral { Value: null } => "<value>",
        TypedLiteral literal => $"'{literal.Value}'",
        TypedTypedConstant typedConstant => $"'{typedConstant.RawText}'",
        InterpolatedTypedConstant => "<typed constant>",
        TypedInterpolatedString => "<string>",
        TypedConditional => "<conditional>",
        TypedQuantifier quantifier => $"{quantifier.BindingName} in {DescribeExpression(quantifier.Collection)}",
        TypedListLiteral => "<list>",
        TypedPostfixOp postfixOp => $"{DescribeExpression(postfixOp.Operand)} is{(postfixOp.IsNegated ? " not" : string.Empty)} set",
        TypedErrorExpression => "<error>",
        null => "<unresolved>",
        _ => "<subexpression>"
    };

    private static string DescribeOperator(OperationKind operationKind)
    {
        var op = Operators.GetMeta(Operations.GetMeta(operationKind).Op);
        return op switch
        {
            SingleTokenOp single when !string.IsNullOrWhiteSpace(single.Token.Text) => single.Token.Text!,
            MultiTokenOp multi => string.Join(" ", multi.Tokens.Select(t => t.Text ?? t.Kind.ToString())),
            _ => op.Kind.ToString()
        };
    }

    private static string FormatQualifierValue(DeclaredQualifierMeta? qualifier)
    {
        if (qualifier is null)
            return "unresolved";

        var value = qualifier switch
        {
            DeclaredQualifierMeta.Currency currency => currency.CurrencyCode,
            DeclaredQualifierMeta.Unit unit => unit.UnitCode,
            DeclaredQualifierMeta.Dimension dimension => dimension.DimensionName,
            DeclaredQualifierMeta.FromCurrency fromCurrency => fromCurrency.CurrencyCode,
            DeclaredQualifierMeta.ToCurrency toCurrency => toCurrency.CurrencyCode,
            DeclaredQualifierMeta.Timezone timezone => timezone.TimezoneId,
            DeclaredQualifierMeta.TemporalUnit temporalUnit => temporalUnit.UnitName,
            DeclaredQualifierMeta.TemporalDimension temporalDimension => temporalDimension.Value switch
            {
                PeriodDimension.Date => "date",
                PeriodDimension.Time => "time",
                PeriodDimension.Any => "any",
                _ => temporalDimension.Value.ToString()
            },
            _ => null
        };

        return string.IsNullOrWhiteSpace(value)
            ? "unresolved"
            : $"'{value}'";
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S3–S7 — Discharge loop + five strategies
    // ════════════════════════════════════════════════════════════════════════════

    private static (ProofDisposition, ProofStrategy?) TryDischarge(ProofObligation obligation, SemanticIndex semantics)
    {
        if (TryLiteralProof(obligation))
            return (ProofDisposition.Proved, ProofStrategy.Literal);
        if (TryDeclarationAttributeProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.DeclarationAttribute);
        if (TryGuardInPathProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.GuardInPath);
        if (TryFlowNarrowingProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.FlowNarrowing);
        if (TryQualifierCompatibilityProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.QualifierCompatibility);
        if (TryCompositionalConstraintProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.CompositionalConstraint);
        if (TryIntervalContainmentProofNarrowed(obligation, semantics, out _))
            return (ProofDisposition.Proved, ProofStrategy.IntervalContainment);

        // Length containment: string literal assigned to a bounded string field
        if (obligation.Requirement is LengthContainmentProofRequirement lengthReq)
        {
            var result = TryLengthContainmentProof(lengthReq, obligation.Site);
            if (result == true)
                return (ProofDisposition.Proved, ProofStrategy.LengthContainment);
            // result == false means violation; leave Unresolved so Diagnostics emits the error
        }

        // Count containment: V1 always unresolved (set on collections rejected by type checker)
        if (obligation.Requirement is CountContainmentProofRequirement countReq)
        {
            var result = TryCountContainmentProof(countReq, obligation.Site);
            if (result == true)
                return (ProofDisposition.Proved, ProofStrategy.CountContainment);
        }

        return (ProofDisposition.Unresolved, null);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S12 — ProofForwardingFact Consumption
    // ════════════════════════════════════════════════════════════════════════════

    private static void IncorporateForwardingFacts(
        ImmutableArray<ProofForwardingFact> facts,
        List<ProofObligation> obligations,
        SemanticIndex semantics)
    {
        var unreachableStates = new HashSet<string>(StringComparer.Ordinal);
        var deadEndStates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var fact in facts)
        {
            switch (fact)
            {
                case ReachabilityFact { IsReachable: false } rf:
                    unreachableStates.Add(rf.StateName);
                    break;
                case DeadEndStateFact def:
                    foreach (var state in def.DeadEndStates)
                        deadEndStates.Add(state);
                    break;
            }
        }

        if (unreachableStates.Count == 0 && deadEndStates.Count == 0)
            return;

        for (int i = obligations.Count - 1; i >= 0; i--)
        {
            var obl = obligations[i];
            if (obl.Context is not TransitionRowContext trc)
                continue;

            var fromState = trc.Row.FromState;
            if (fromState is null) continue;

            // Suppress obligations on transitions FROM unreachable states
            if (unreachableStates.Contains(fromState))
            {
                obligations[i] = obl with
                {
                    Disposition = ProofDisposition.Proved,
                    Strategy = ProofStrategy.Literal // vacuously proved
                };
                continue;
            }

            // Suppress obligations on transitions FROM dead-end states TO other dead-end states
            if (deadEndStates.Contains(fromState) &&
                trc.Row.TargetState is not null &&
                deadEndStates.Contains(trc.Row.TargetState))
            {
                obligations[i] = obl with
                {
                    Disposition = ProofDisposition.Proved,
                    Strategy = ProofStrategy.Literal
                };
            }
        }
    }
}

