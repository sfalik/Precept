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
        bool IsPresenceCheck,
        // True when Field names an event arg rather than a declared field. Field and
        // arg names share a flat namespace in this representation, so a constraint over
        // an arg must not be resolved against a same-named field's declared interval.
        bool IsArg = false);

    private record ContainsGuardConstraint(
        string Field,
        bool Negated);

    private record FieldToFieldConstraint(
        string LeftField,
        OperatorKind Comparison,
        string RightField);

    /// <summary>
    /// Guard constraint of shape <c>&lt;paramExpr&gt; &lt;op&gt; &lt;collectionField&gt;.&lt;accessor&gt;</c>
    /// — used by <see cref="IndexBoundsProofRequirement"/> discharge to match
    /// author-written `when N &lt; F.count` guards. <see cref="IndexExpression"/>
    /// is the TypedExpression that resolves to the parameter (arg-ref, field-ref,
    /// member-access, or literal). The discharge strategy walks for a matching
    /// shape relative to the obligation's resolved index expression.
    /// </summary>
    private record ParamUpperBoundConstraint(
        TypedExpression IndexExpression,
        OperatorKind Comparison,
        string CollectionField,
        string AccessorName);

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

        // Satisfiability scan: flag unsatisfiable guards and contradictory
        // rule pairs before the per-obligation discharge loop runs. Lateral
        // pass that produces diagnostics directly (not via the obligation
        // channel) — satisfiability is whole-construct, not obligation-shaped,
        // so the existing strategy dispatch (which is obligation-discharge-
        // shaped) would be the wrong fit. See docs/compiler/proof-engine.md
        // § Two-Pass Design (Pass 1.5).
        var producedFacts = new List<ProofForwardingFact>();
        ScanSatisfiability(semantics, diagnostics, producedFacts);

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

        // A default holds one value; the declared-bound check reports the first violated
        // numeric bound (in declared-then-implied order) and stops — so a default that
        // violates several bounds yields one diagnostic, not one per bound. The per-modifier
        // obligations are stamped in that order, so keeping the first failed OutOfRange per
        // default site reproduces it.
        var emittedDefaultBoundSites = new HashSet<ObligationContext>();
        for (int i = 0; i < obligations.Count; i++)
        {
            if (suppressDiagnostics[i])
                continue;

            var obligation = obligations[i];
            if (obligation.Disposition != ProofDisposition.Unresolved)
                continue;

            if (obligation.Requirement is NumericProofRequirement { BoundModifierLabel: not null }
                && obligation.Context is FieldDefaultContext or ArgDefaultContext
                && !emittedDefaultBoundSites.Add(obligation.Context))
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
            diagnostics.ToImmutableArray(),
            producedFacts.ToImmutableArray());
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
            if (row is TypedTransitionRowSuccess success)
                WalkActions(success.Actions, ctx, obligations, semantics);
        }

        // EventHandlers[].Actions[]
        foreach (var handler in semantics.EventHandlers)
        {
            var ctx = new EventHandlerContext(handler);
            if (handler is TypedEventRowSuccess success)
                WalkActions(success.Actions, ctx, obligations, semantics);
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

        // Fields[].ComputedExpression — result-interval vs the field's own declared bounds
        CollectComputedFieldBoundObligations(semantics, obligations);

        // Fields[].DefaultExpression — numeric/length declared-bound + qualifier-residual obligations
        CollectDefaultObligations(semantics, obligations);

        // Events[].Args[].DefaultExpression — numeric/length declared-bound + qualifier-residual obligations
        CollectArgDefaultObligations(semantics, obligations);

        return obligations;
    }

    // SemanticIndex is threaded through WalkExpression so that optional field
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
                // Presence Obligation Generation:
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
        // Sequential proof flow (spec § 0.6 item 7): a guard fact about a field is invalidated
        // for any obligation whose site comes AFTER that field is reassigned in the action chain.
        // We track the prefix-write set and stamp each action's obligations with the fields
        // written by PRIOR actions; the guard-discharge strategies then drop stale facts.
        //   - writtenSoFar:     fields fully replaced (set/clear) → ALL prior facts stale.
        //   - countEstablished: collections grown → non-empty (count > 0) guaranteed afterward.
        //   - countInvalidated: collections shrunk → non-empty fact stale (may be empty).
        // The two count sets are kept mutually exclusive per field (last collection effect wins).
        var writtenSoFar = ImmutableArray<string>.Empty;
        var countEstablished = ImmutableArray<string>.Empty;
        var countInvalidated = ImmutableArray<string>.Empty;

        foreach (var action in actions)
        {
            var reassignedBefore = writtenSoFar;
            var establishedBefore = countEstablished;
            var invalidatedBefore = countInvalidated;
            var start = obligations.Count;

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

            // Stamp every obligation generated for THIS action (static + dynamic + RHS
            // expression walk, all appended to [start..Count)) with the prefix-state from PRIOR
            // actions. The action's OWN effect is recorded afterward, so an obligation that reads a
            // field this action also writes (e.g. `set X = X + 1`) still sees the pre-write guard
            // fact — only strictly-prior effects apply. This is also why a shrink's own `count > 0`
            // obligation (e.g. the non-empty requirement on `dequeue`) is evaluated against the
            // pre-shrink state: the obligation must hold BEFORE the shrink executes.
            if (!reassignedBefore.IsEmpty || !establishedBefore.IsEmpty || !invalidatedBefore.IsEmpty)
                for (var i = start; i < obligations.Count; i++)
                    obligations[i] = obligations[i] with
                    {
                        ReassignedBefore = reassignedBefore,
                        CountEstablishedBefore = establishedBefore,
                        CountInvalidatedBefore = invalidatedBefore,
                    };

            // Record this action's effect on the field, classified by the Actions catalog
            // (ActionEffectClass) rather than by switching on ActionKind identity:
            //   ReplacesValue/Empties (set/clear) → full replacement: ALL prior facts stale.
            //   Grows  → non-empty established (count > 0 guaranteed after adding ≥1 element).
            //   Shrinks → non-empty invalidated (count may have dropped to 0).
            // The count sets stay mutually exclusive (last effect wins); a full replacement
            // subsumes both (writtenSoFar already blocks the count guard via ReassignedBefore).
            var field = action.FieldName;
            if (!string.IsNullOrEmpty(field))
                switch (actionMeta.Effect)
                {
                    case ActionEffectClass.ReplacesValue or ActionEffectClass.Empties:
                        if (!writtenSoFar.Contains(field)) writtenSoFar = writtenSoFar.Add(field);
                        countEstablished = countEstablished.Remove(field);
                        countInvalidated = countInvalidated.Remove(field);
                        break;
                    case ActionEffectClass.Grows:
                        if (!countEstablished.Contains(field)) countEstablished = countEstablished.Add(field);
                        countInvalidated = countInvalidated.Remove(field);
                        break;
                    case ActionEffectClass.Shrinks:
                        if (!countInvalidated.Contains(field)) countInvalidated = countInvalidated.Add(field);
                        countEstablished = countEstablished.Remove(field);
                        break;
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
            || requirement is PresenceProofRequirement { Subject: SelfSubject }
            || requirement is KeyPresenceProofRequirement { Subject: SelfSubject }
            || requirement is IndexBoundsProofRequirement)
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
                // Action-site obligations (Insert/RemoveAt index bounds) do NOT resolve via
                // this Subject path — the Site is a TypedFieldRef (the receiver), and the
                // index expression is recovered through the obligation's parent context
                // by FindActionIndexInContext in TryIndexBoundsProof.
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
        // Match the ParameterMeta against the accessor's declared parameter list.
        // Used by IndexBoundsProofRequirement on `.at(N)` and similar parameterized
        // accessors. Bare (zero-parameter) accessors leave Parameters empty.
        var parameters = access.ResolvedAccessor.Parameters;
        if (parameters.Length == 0 || access.Arguments.IsDefaultOrEmpty) return null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (ReferenceEquals(param, parameters[i]))
                return i < access.Arguments.Length ? access.Arguments[i] : null;
        }
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
        SemanticIndex semantics,
        ProofObligation? obligation = null)
        => DescribeQualifiedExpression(ResolveSubject(subject, site), axis, semantics, obligation);

    private static (string Label, string QualifierValue) DescribeQualifiedExpression(
        TypedExpression? expr,
        QualifierAxis axis,
        SemanticIndex semantics,
        ProofObligation? obligation = null)
    {
        var label = DescribeExpression(expr);
        var qualifier = expr is null ? null : ResolveQualifierFromExpression(expr, axis, semantics);
        if (qualifier is not null)
            return (label, FormatQualifierValue(qualifier));

        // Open operand: surface the guard-narrowed value so the diagnostic names what the guard
        // pinned it to (e.g. "EUR (from guard)") instead of an opaque "unresolved" — this is the
        // narrowed-vs-required signal. Null when no guard provably narrows it on this axis.
        var narrowed = expr is not null && obligation is not null
            ? NarrowedValueFromGuard(expr, axis, obligation, semantics)
            : null;
        return (label, narrowed is not null ? $"{narrowed} (from guard)" : FormatQualifierValue(null));
    }

    /// <summary>
    /// The display value for an operand's qualifier on an axis: its declared value, or — when
    /// open — the guard-narrowed value (suffixed "(from guard)"), or "unresolved". Mirrors
    /// <see cref="DescribeQualifiedExpression"/>'s value half for diagnostics that format the
    /// qualifier value directly (the qualifier-chain arm) rather than via a (label, value) pair.
    /// </summary>
    private static string FormatQualifierOrNarrowed(
        TypedExpression? expr, QualifierAxis axis, ProofObligation obligation, SemanticIndex semantics)
    {
        var declared = expr is null ? null : ResolveQualifierFromExpression(expr, axis, semantics);
        if (declared is not null)
            return FormatQualifierValue(declared);
        var narrowed = expr is not null ? NarrowedValueFromGuard(expr, axis, obligation, semantics) : null;
        return narrowed is not null ? $"{narrowed} (from guard)" : FormatQualifierValue(null);
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
                PeriodDimension.Datetime => "datetime",
                PeriodDimension.Any => "any",
                _ => temporalDimension.Value.ToString()
            },
            DeclaredQualifierMeta.CompoundPrice compound => $"{compound.CurrencyCode}/{compound.UnitCode}",
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
        // Declared-value default bound (OutOfRange family): a stamped numeric default obligation is
        // discharged by evaluating the default's static value against the bound. true → proved;
        // false → violated, leave Unresolved so OutOfRange surfaces; null → undecidable magnitude,
        // conservative no-emit (proved vacuously per the unresolvable-magnitude limitation).
        if (obligation.Requirement is NumericProofRequirement { BoundModifierLabel: not null }
            && obligation.Context is FieldDefaultContext or ArgDefaultContext)
        {
            return TryNumericDefaultBoundProof(obligation, semantics) == false
                ? (ProofDisposition.Unresolved, null)
                : (ProofDisposition.Proved, ProofStrategy.Literal);
        }

        if (TryLiteralProof(obligation))
            return (ProofDisposition.Proved, ProofStrategy.Literal);
        if (TryDeclarationAttributeProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.DeclarationAttribute);
        if (TryCollectionGrowthProof(obligation))
            return (ProofDisposition.Proved, ProofStrategy.CollectionGrowth);
        if (TryGuardInPathProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.GuardInPath);
        if (TryFlowNarrowingProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.FlowNarrowing);
        if (TryQualifierCompatibilityProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.QualifierCompatibility);
        if (TryQualifierGuardNarrowingProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.QualifierCompatibility);
        if (TryAssignmentQualifierProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.QualifierCompatibility);
        if (TryDimensionalProductProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.DimensionalProduct);
        if (TryCompositionalConstraintProof(obligation, semantics))
            return (ProofDisposition.Proved, ProofStrategy.CompositionalConstraint);
        if (TryIntervalContainmentProofNarrowed(obligation, semantics, out _))
            return (ProofDisposition.Proved, ProofStrategy.IntervalContainment);

        // Length containment: a string RHS (literal, reference, concat, conditional, or length-stable
        // function) assigned to a bounded string field, discharged against its static length interval.
        if (obligation.Requirement is LengthContainmentProofRequirement lengthReq)
        {
            var result = TryLengthContainmentProof(lengthReq, obligation.Site, semantics);
            if (result == true)
                return (ProofDisposition.Proved, ProofStrategy.LengthContainment);
            // result == false (provable violation) or null (not provable) both leave the obligation
            // Unresolved so Diagnostics emits the error (§0.7 prove-or-reject — unbounded ⇒ emit).
        }

        // Count containment: V1 always unresolved (set on collections rejected by type checker)
        if (obligation.Requirement is CountContainmentProofRequirement countReq)
        {
            var result = TryCountContainmentProof(countReq, obligation.Site);
            if (result == true)
                return (ProofDisposition.Proved, ProofStrategy.CountContainment);
        }

        // Key presence: collection contains (or doesn't contain) a specific element/key.
        // Discharged by a guard expression that pattern-matches the contains check —
        // e.g. `when not (F contains P)` for AppendBy uniqueness on `log of T by P`.
        if (obligation.Requirement is KeyPresenceProofRequirement keyReq)
        {
            if (TryKeyPresenceProof(keyReq, obligation, semantics))
                return (ProofDisposition.Proved, ProofStrategy.GuardInPath);
        }

        // Index bounds: parameterised-index access/mutation (.at(N), insert at N,
        // remove at N) requires `0 <= N < F.count` (or `<= F.count` for inserts).
        // Discharged via guard branches that establish both lower and upper bounds.
        if (obligation.Requirement is IndexBoundsProofRequirement indexReq)
        {
            if (TryIndexBoundsProof(indexReq, obligation, semantics))
                return (ProofDisposition.Proved, ProofStrategy.GuardInPath);
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
                trc.Row is TypedTransitionRowSuccess { TargetState: not null } successRow &&
                deadEndStates.Contains(successRow.TargetState))
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

