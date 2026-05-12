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

public static class ProofEngine
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Internal records for guard decomposition (Strategy 3 + 4)
    // ════════════════════════════════════════════════════════════════════════════

    private record GuardConstraint(
        string Field,
        OperatorKind Comparison,
        decimal? Value,
        bool IsPresenceCheck);

    private record FieldToFieldConstraint(
        string LeftField,
        OperatorKind Comparison,
        string RightField);

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
                continue;
            }

            var (disposition, strategy) = TryDischarge(obligation, semantics);
            obligations[i] = obligation with { Disposition = disposition, Strategy = strategy };

            if (disposition == ProofDisposition.Unresolved)
            {
                diagnostics.Add(CreateDiagnostic(obligation));
                faultSiteLinks.Add(CreateFaultSiteLink(obligation));
            }
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
            WalkActions(row.Actions, ctx, obligations);
        }

        // EventHandlers[].Actions[]
        foreach (var handler in semantics.EventHandlers)
        {
            var ctx = new EventHandlerContext(handler);
            WalkActions(handler.Actions, ctx, obligations);
        }

        // StateHooks[].Actions[]
        foreach (var hook in semantics.StateHooks)
        {
            var ctx = new StateHookContext(hook);
            WalkActions(hook.Actions, ctx, obligations);
        }

        // Rules[].Condition
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var ctx = new ConstraintContext(new RuleIdentity(i));
            WalkExpression(semantics.Rules[i].Condition, ctx, obligations);
        }

        // Ensures[].Condition
        for (int i = 0; i < semantics.Ensures.Length; i++)
        {
            var ensure = semantics.Ensures[i];
            var ctx = new ConstraintContext(new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, i));
            WalkExpression(ensure.Condition, ctx, obligations);
        }

        // Fields[].ComputedExpression
        foreach (var field in semantics.Fields)
        {
            if (field.ComputedExpression is not null)
            {
                var ctx = new FieldExpressionContext(field);
                WalkExpression(field.ComputedExpression, ctx, obligations);
            }
        }

        return obligations;
    }

    private static void WalkExpression(TypedExpression expr, ObligationContext ctx, List<ProofObligation> obligations)
    {
        switch (expr)
        {
            case TypedBinaryOp bin:
                foreach (var req in bin.ProofRequirements)
                    obligations.Add(new ProofObligation(req, bin, ctx, ProofDisposition.Unresolved, null, null));
                WalkExpression(bin.Left, ctx, obligations);
                WalkExpression(bin.Right, ctx, obligations);
                break;

            case TypedFunctionCall call:
                foreach (var req in call.ProofRequirements)
                    obligations.Add(new ProofObligation(req, call, ctx, ProofDisposition.Unresolved, null, null));
                foreach (var arg in call.Arguments)
                    WalkExpression(arg, ctx, obligations);
                break;

            case TypedMemberAccess ma:
                foreach (var req in ma.ProofRequirements)
                    obligations.Add(new ProofObligation(req, ma, ctx, ProofDisposition.Unresolved, null, null));
                WalkExpression(ma.Object, ctx, obligations);
                break;

            case TypedUnaryOp un:
                WalkExpression(un.Operand, ctx, obligations);
                break;

            case TypedConditional cond:
                WalkExpression(cond.Condition, ctx, obligations);
                WalkExpression(cond.ThenBranch, ctx, obligations);
                WalkExpression(cond.ElseBranch, ctx, obligations);
                break;

            case TypedQuantifier quant:
                WalkExpression(quant.Collection, ctx, obligations);
                WalkExpression(quant.Predicate, ctx, obligations);
                break;

            case TypedPostfixOp post:
                WalkExpression(post.Operand, ctx, obligations);
                break;

            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                {
                    if (seg is TypedHoleSegment hole)
                        WalkExpression(hole.Expression, ctx, obligations);
                }
                break;

            case TypedListLiteral list:
                foreach (var elem in list.Elements)
                    WalkExpression(elem, ctx, obligations);
                break;
        }
    }

    private static void WalkActions(ImmutableArray<TypedAction> actions, ObligationContext ctx, List<ProofObligation> obligations)
    {
        foreach (var action in actions)
        {
            foreach (var req in action.ProofRequirements)
                obligations.Add(new ProofObligation(req, CreateActionProofSite(action, req), ctx, ProofDisposition.Unresolved, null, null));

            if (action is TypedInputAction inputAction)
            {
                WalkExpression(inputAction.InputExpression, ctx, obligations);
                if (inputAction.SecondaryExpression is not null)
                    WalkExpression(inputAction.SecondaryExpression, ctx, obligations);
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

        return (ProofDisposition.Unresolved, null);
    }

    // ── Strategy 1: Literal Proof ─────────────────────────────────────────────

    private static bool TryLiteralProof(ProofObligation obligation)
    {
        if (obligation.Requirement is not NumericProofRequirement numeric)
            return false;

        var subject = ResolveSubject(numeric.Subject, obligation.Site);
        if (subject is not TypedLiteral literal)
            return false;

        var value = literal.Value switch
        {
            decimal d => (decimal?)d,
            int i => (decimal?)i,
            long l => (decimal?)l,
            _ => null
        };
        if (value is null) return false;

        return numeric.Comparison switch
        {
            OperatorKind.NotEquals => value != numeric.Threshold,
            OperatorKind.GreaterThan => value > numeric.Threshold,
            OperatorKind.GreaterThanOrEqual => value >= numeric.Threshold,
            OperatorKind.LessThan => value < numeric.Threshold,
            OperatorKind.LessThanOrEqual => value <= numeric.Threshold,
            _ => false
        };
    }

    // ── Strategy 2: Declaration Attribute Proof ───────────────────────────────

    private static bool TryDeclarationAttributeProof(ProofObligation obligation, SemanticIndex semantics)
    {
        // Dimension arm
        if (obligation.Requirement is DimensionProofRequirement dimReq)
        {
            var resolvedSubject = ResolveSubject(dimReq.Subject, obligation.Site);
            var dimension = ResolvePeriodDimension(resolvedSubject, semantics);
            if (dimension is null) return false;
            return dimension == PeriodDimension.Any || dimension == dimReq.RequiredDimension;
        }

        // Modifier arm
        if (obligation.Requirement is ModifierRequirement modReq)
        {
            var fieldName = GetFieldName(modReq.Subject, obligation.Site);
            if (fieldName is null) return false;
            if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return false;
            return field.Modifiers.Contains(modReq.Required);
        }

        // Numeric/Presence arm — walk effective modifiers
        ProofSubject? reqSubject = obligation.Requirement is NumericProofRequirement numericReq
            ? numericReq.Subject
            : obligation.Requirement is PresenceProofRequirement presenceReq
                ? presenceReq.Subject
                : null;
        if (reqSubject is null) return false;

        var subject = ResolveSubject(reqSubject, obligation.Site);
        if (subject is TypedFunctionCall functionCall &&
            FunctionReturnSatisfies(functionCall, obligation.Requirement))
        {
            return true;
        }

        var attributeFieldName = GetFieldName(subject);
        if (attributeFieldName is null) return false;
        if (!semantics.FieldsByName.TryGetValue(attributeFieldName, out var attributeField)) return false;

        // Accessor-level nonnegative guarantee: collection count can never be negative,
        // so discharge >= 0 trivially without requiring user-declared modifiers.
        if (reqSubject is SelfSubject { Accessor: FixedReturnAccessor { ReturnNonnegative: true } }
            && obligation.Requirement is NumericProofRequirement
            {
                Comparison: OperatorKind.GreaterThanOrEqual,
                Threshold: 0m,
            })
        {
            return true;
        }

        // Walk declared + implied modifiers
        foreach (var modifier in attributeField.Modifiers.Concat(attributeField.ImpliedModifiers))
        {
            var meta = Modifiers.GetMeta(modifier);
            if (meta is not ValueModifierMeta fmm) continue;

            foreach (var satisfaction in fmm.ProofSatisfactions)
            {
                if (SatisfactionCovers(satisfaction, obligation.Requirement))
                    return true;
            }
        }

        // Presence fallback
        if (obligation.Requirement is PresenceProofRequirement)
        {
            if (attributeField.Presence is DeclaredPresenceMeta.Guaranteed guaranteed)
            {
                return guaranteed.ProofSatisfactions
                    .Any(s => s.RequirementKind == ProofRequirementKind.Presence);
            }
        }

        return false;
    }

    private static bool FunctionReturnSatisfies(TypedFunctionCall call, ProofRequirement requirement)
    {
        if (requirement is not NumericProofRequirement
            {
                Comparison: OperatorKind.GreaterThanOrEqual,
                Threshold: 0m,
            })
        {
            return false;
        }

        var overload = ResolveFunctionOverload(call);
        return overload?.ReturnNonnegative == true;
    }

    private static FunctionOverload? ResolveFunctionOverload(TypedFunctionCall call)
    {
        var meta = Functions.GetMeta(call.ResolvedFunction);
        FunctionOverload? best = null;
        var bestScore = int.MaxValue;

        foreach (var overload in meta.Overloads)
        {
            if (overload.Parameters.Count != call.Arguments.Length || overload.ReturnType != call.ResultType)
                continue;

            var score = 0;
            var valid = true;
            for (var i = 0; i < call.Arguments.Length; i++)
            {
                var argType = call.Arguments[i].ResultType;
                var paramType = overload.Parameters[i].Kind;
                if (argType == paramType)
                    continue;

                if (IsAssignable(argType, paramType))
                {
                    score++;
                    continue;
                }

                valid = false;
                break;
            }

            if (!valid || score >= bestScore)
                continue;

            best = overload;
            bestScore = score;
            if (score == 0)
                break;
        }

        return best;
    }

    private static bool IsAssignable(TypeKind source, TypeKind target)
    {
        if (source == target || source == TypeKind.Error || target == TypeKind.Error)
            return true;

        return Types.GetMeta(source).WidensTo.Contains(target);
    }

    private static PeriodDimension? ResolvePeriodDimension(TypedExpression? subject, SemanticIndex semantics)
    {
        if (subject is TypedFieldRef fieldRef &&
            semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out var field))
        {
            foreach (var qual in field.DeclaredQualifiers)
            {
                if (qual is DeclaredQualifierMeta.TemporalDimension td)
                    return td.Value;
                if (qual is DeclaredQualifierMeta.TemporalUnit tu)
                    return tu.DerivedDimension;
            }
        }
        return null;
    }

    private static bool SatisfactionCovers(ProofSatisfaction satisfaction, ProofRequirement requirement)
    {
        if (requirement is NumericProofRequirement numeric && satisfaction is ProofSatisfaction.Numeric numSat)
        {
            if (numSat.RequirementKind != ProofRequirementKind.Numeric) return false;

            // Check projection match — SelfValue matches any single-value requirement,
            // Accessor must match the accessor on the subject
            if (numSat.Projection is SatisfactionProjection.Accessor accProj)
            {
                // The requirement's subject must be a SelfSubject with matching accessor
                if (numeric.Subject is SelfSubject self && self.Accessor is { } accessor)
                {
                    if (!string.Equals(accProj.Name, accessor.Name, StringComparison.Ordinal))
                        return false;
                }
                else
                {
                    return false;
                }
            }

            // Resolve the bound value
            decimal? boundValue = numSat.Bound switch
            {
                NumericBoundSource.Constant c => c.Value,
                NumericBoundSource.DeclarationValue => null, // conservative — cannot compare without runtime value
                _ => null
            };
            if (boundValue is null) return false;

            // Subsumption: check if the satisfaction's comparison at its bound covers the requirement
            return (numSat.Comparison, numeric.Comparison) switch
            {
                // positive (> 0) covers != 0 and >= 0
                (OperatorKind.GreaterThan, OperatorKind.NotEquals)
                    when boundValue == 0 && numeric.Threshold == 0 => true,
                (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual)
                    when boundValue >= numeric.Threshold => true,
                (OperatorKind.GreaterThan, OperatorKind.GreaterThan)
                    when boundValue >= numeric.Threshold => true,

                // nonnegative (>= 0) covers >= 0 but NOT != 0
                (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual)
                    when boundValue >= numeric.Threshold => true,

                // nonzero (!= 0) covers != 0
                (OperatorKind.NotEquals, OperatorKind.NotEquals)
                    when boundValue == numeric.Threshold => true,

                // LessThanOrEqual covers LessThanOrEqual
                (OperatorKind.LessThanOrEqual, OperatorKind.LessThanOrEqual)
                    when boundValue <= numeric.Threshold => true,

                // LessThan covers LessThan, NotEquals
                (OperatorKind.LessThan, OperatorKind.NotEquals)
                    when boundValue == 0 && numeric.Threshold == 0 => true,
                (OperatorKind.LessThan, OperatorKind.LessThan)
                    when boundValue <= numeric.Threshold => true,

                _ => false
            };
        }

        if (requirement is PresenceProofRequirement && satisfaction is ProofSatisfaction.Presence)
            return true;

        return false;
    }

    // ── Strategy 3: Guard-in-Path Proof ───────────────────────────────────────

    private static bool TryGuardInPathProof(ProofObligation obligation, SemanticIndex semantics)
    {
        var guard = obligation.Context switch
        {
            TransitionRowContext t => t.Row.Guard,
            StateHookContext s => s.Hook.Guard,
            _ => null
        };
        if (guard is null) return false;

        var guardConstraints = ExtractGuardConstraints(guard);

        foreach (var gc in guardConstraints)
        {
            if (obligation.Requirement is NumericProofRequirement numeric)
            {
                if (GuardSubsumes(gc, numeric, obligation.Site))
                    return true;
            }
            else if (obligation.Requirement is PresenceProofRequirement presence)
            {
                if (gc.Field == GetFieldName(presence.Subject, obligation.Site)
                    && gc.IsPresenceCheck)
                    return true;
            }
        }

        return false;
    }

    private static ImmutableArray<GuardConstraint> ExtractGuardConstraints(TypedExpression guard)
    {
        var builder = ImmutableArray.CreateBuilder<GuardConstraint>();
        ExtractGuardConstraintsCore(guard, builder);
        return builder.ToImmutable();
    }

    private static void ExtractGuardConstraintsCore(TypedExpression expr, ImmutableArray<GuardConstraint>.Builder builder)
    {
        switch (expr)
        {
            case TypedBinaryOp { ResolvedOp: var op } bin when Operations.GetMeta(op).Op == OperatorKind.And:
                ExtractGuardConstraintsCore(bin.Left, builder);
                ExtractGuardConstraintsCore(bin.Right, builder);
                break;

            case TypedBinaryOp bin when Operations.GetMeta(bin.ResolvedOp).Op == OperatorKind.Or:
                // OR: do NOT decompose — neither disjunct is guaranteed
                break;

            case TypedBinaryOp bin:
            {
                var compOp = Operations.GetMeta(bin.ResolvedOp).Op;

                // field op literal
                if (bin.Left is TypedFieldRef leftField && bin.Right is TypedLiteral rightLit)
                {
                    var litValue = ToDecimal(rightLit.Value);
                    if (litValue is not null)
                        builder.Add(new GuardConstraint(leftField.FieldName, compOp, litValue, false));
                }
                // literal op field → invert
                else if (bin.Left is TypedLiteral leftLit && bin.Right is TypedFieldRef rightField)
                {
                    var litValue = ToDecimal(leftLit.Value);
                    if (litValue is not null)
                        builder.Add(new GuardConstraint(rightField.FieldName, InvertOp(compOp), litValue, false));
                }
                // collection.count op literal (count is a member accessor, not a function)
                else if (bin.Left is TypedMemberAccess { Object: TypedFieldRef maField, ResolvedAccessor: var acc }
                    && acc.Name == "count"
                    && bin.Right is TypedLiteral maLit)
                {
                    var litValue = ToDecimal(maLit.Value);
                    if (litValue is not null)
                        builder.Add(new GuardConstraint(maField.FieldName, compOp, litValue, false));
                }
                break;
            }

            case TypedPostfixOp post when !post.IsNegated && post.Operand is TypedFieldRef postField:
                // field is set
                builder.Add(new GuardConstraint(postField.FieldName, OperatorKind.NotEquals, null, true));
                break;

            case TypedUnaryOp { ResolvedOp: var uop } un when Operations.GetMeta(uop).Op == OperatorKind.Not:
                // not (X op Y) — attempt negation of simple comparisons
                if (un.Operand is TypedBinaryOp innerBin)
                {
                    var innerOp = Operations.GetMeta(innerBin.ResolvedOp).Op;
                    var negated = NegateOp(innerOp);
                    if (negated is not null)
                    {
                        if (innerBin.Left is TypedFieldRef nf && innerBin.Right is TypedLiteral nl)
                        {
                            var v = ToDecimal(nl.Value);
                            if (v is not null)
                                builder.Add(new GuardConstraint(nf.FieldName, negated.Value, v, false));
                        }
                        else if (innerBin.Left is TypedLiteral nl2 && innerBin.Right is TypedFieldRef nf2)
                        {
                            var v = ToDecimal(nl2.Value);
                            if (v is not null)
                                builder.Add(new GuardConstraint(nf2.FieldName, InvertOp(negated.Value), v, false));
                        }
                    }
                }
                break;
        }
    }

    private static bool GuardSubsumes(GuardConstraint guard, NumericProofRequirement requirement, TypedExpression site)
    {
        if (guard.Field != GetFieldName(requirement.Subject, site)) return false;

        return (guard.Comparison, requirement.Comparison) switch
        {
            (OperatorKind.GreaterThan, OperatorKind.NotEquals)
                when guard.Value == 0 && requirement.Threshold == 0 => true,
            (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual)
                when guard.Value >= requirement.Threshold => true,
            (OperatorKind.GreaterThan, OperatorKind.GreaterThan)
                when guard.Value >= requirement.Threshold => true,

            (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual)
                when guard.Value >= requirement.Threshold => true,

            (OperatorKind.LessThan, OperatorKind.NotEquals)
                when guard.Value == 0 && requirement.Threshold == 0 => true,

            // Exact match fallback
            _ when guard.Comparison == requirement.Comparison
                && guard.Value == requirement.Threshold => true,

            _ => false
        };
    }

    private static OperatorKind InvertOp(OperatorKind op) => op switch
    {
        OperatorKind.GreaterThan => OperatorKind.LessThan,
        OperatorKind.LessThan => OperatorKind.GreaterThan,
        OperatorKind.GreaterThanOrEqual => OperatorKind.LessThanOrEqual,
        OperatorKind.LessThanOrEqual => OperatorKind.GreaterThanOrEqual,
        OperatorKind.Equals => OperatorKind.Equals,
        OperatorKind.NotEquals => OperatorKind.NotEquals,
        _ => op
    };

    private static OperatorKind? NegateOp(OperatorKind op) => op switch
    {
        OperatorKind.Equals => OperatorKind.NotEquals,
        OperatorKind.NotEquals => OperatorKind.Equals,
        OperatorKind.GreaterThan => OperatorKind.LessThanOrEqual,
        OperatorKind.LessThanOrEqual => OperatorKind.GreaterThan,
        OperatorKind.LessThan => OperatorKind.GreaterThanOrEqual,
        OperatorKind.GreaterThanOrEqual => OperatorKind.LessThan,
        _ => null
    };

    private static decimal? ToDecimal(object? value) => value switch
    {
        decimal d => d,
        int i => i,
        long l => l,
        _ => null
    };

    // ── Strategy 4: Flow Narrowing ────────────────────────────────────────────

    private static bool TryFlowNarrowingProof(ProofObligation obligation, SemanticIndex semantics)
    {
        var guard = obligation.Context switch
        {
            TransitionRowContext t => t.Row.Guard,
            StateHookContext s => s.Hook.Guard,
            _ => null
        };
        if (guard is null) return false;

        var relationalGuards = ExtractFieldToFieldConstraints(guard);
        if (relationalGuards.IsEmpty) return false;

        if (obligation.Site is not TypedBinaryOp binaryOp) return false;
        if (obligation.Requirement is not NumericProofRequirement numeric) return false;

        if (!IsSubtractionOp(binaryOp.ResolvedOp)) return false;

        var leftField = GetFieldName(binaryOp.Left);
        var rightField = GetFieldName(binaryOp.Right);
        if (leftField is null || rightField is null) return false;

        foreach (var rg in relationalGuards)
        {
            if (!((rg.LeftField == leftField && rg.RightField == rightField) ||
                  (rg.LeftField == rightField && rg.RightField == leftField)))
                continue;

            if (GuardRelationImpliesObligation(rg, binaryOp, leftField, rightField, numeric))
                return true;
        }

        return false;
    }

    private static ImmutableArray<FieldToFieldConstraint> ExtractFieldToFieldConstraints(TypedExpression guard)
    {
        var builder = ImmutableArray.CreateBuilder<FieldToFieldConstraint>();
        ExtractFieldToFieldCore(guard, builder);
        return builder.ToImmutable();
    }

    private static void ExtractFieldToFieldCore(TypedExpression expr, ImmutableArray<FieldToFieldConstraint>.Builder builder)
    {
        switch (expr)
        {
            case TypedBinaryOp bin when Operations.GetMeta(bin.ResolvedOp).Op == OperatorKind.And:
                ExtractFieldToFieldCore(bin.Left, builder);
                ExtractFieldToFieldCore(bin.Right, builder);
                break;

            case TypedBinaryOp bin when Operations.GetMeta(bin.ResolvedOp).Op == OperatorKind.Or:
                break;

            case TypedBinaryOp bin:
            {
                var compOp = Operations.GetMeta(bin.ResolvedOp).Op;
                if (bin.Left is TypedFieldRef leftF && bin.Right is TypedFieldRef rightF)
                    builder.Add(new FieldToFieldConstraint(leftF.FieldName, compOp, rightF.FieldName));
                break;
            }
        }
    }

    private static bool GuardRelationImpliesObligation(
        FieldToFieldConstraint guard,
        TypedBinaryOp expr,
        string exprLeftField,
        string exprRightField,
        NumericProofRequirement requirement)
    {
        bool sameOrder = exprLeftField == guard.LeftField && exprRightField == guard.RightField;
        bool reversed = exprLeftField == guard.RightField && exprRightField == guard.LeftField;
        if (!sameOrder && !reversed) return false;

        var effectiveOp = sameOrder ? guard.Comparison : InvertOp(guard.Comparison);

        return (effectiveOp, requirement.Comparison) switch
        {
            (OperatorKind.GreaterThan, OperatorKind.GreaterThan) when requirement.Threshold == 0 => true,
            (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual) when requirement.Threshold <= 0 => true,
            (OperatorKind.GreaterThan, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,
            (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual) when requirement.Threshold <= 0 => true,
            (OperatorKind.LessThan, OperatorKind.LessThan) when requirement.Threshold == 0 => true,
            (OperatorKind.LessThan, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,
            (OperatorKind.LessThanOrEqual, OperatorKind.LessThanOrEqual) when requirement.Threshold <= 0 => true,
            (OperatorKind.NotEquals, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,
            _ => false
        };
    }

    private static bool IsSubtractionOp(OperationKind op)
    {
        return Operations.GetMeta(op).Op == OperatorKind.Minus;
    }

    // ── Strategy 5: Qualifier Compatibility Proof ─────────────────────────────

    private static bool TryQualifierCompatibilityProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is QualifierCompatibilityProofRequirement qcReq)
        {
            if (obligation.Site is not TypedBinaryOp binOp)
                return false;

            var leftQualifier = ResolveQualifierFromExpression(binOp.Left, qcReq.Axis, semantics);
            var rightQualifier = ResolveQualifierFromExpression(binOp.Right, qcReq.Axis, semantics);

            return QualifiersAreCompatible(leftQualifier, rightQualifier, qcReq.Axis);
        }

        if (obligation.Requirement is QualifierChainProofRequirement chainReq)
        {
            var leftQualifier = ResolveQualifierOnAxis(chainReq.LeftSubject, chainReq.LeftAxis, obligation.Site, semantics);
            var rightQualifier = ResolveQualifierOnAxis(chainReq.RightSubject, chainReq.RightAxis, obligation.Site, semantics);

            if (leftQualifier is null || rightQualifier is null)
                return false;

            return ChainQualifiersMatch(leftQualifier, rightQualifier);
        }

        return false;
    }

    /// <summary>
    /// Compares two qualifier values across potentially different axes by extracting
    /// their comparable string values.
    /// </summary>
    private static bool ChainQualifiersMatch(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
    {
        var leftValue = ExtractComparableValue(left);
        var rightValue = ExtractComparableValue(right);
        if (leftValue is not null && rightValue is not null
            && string.Equals(leftValue, rightValue, StringComparison.Ordinal))
        {
            return true;
        }

        return QualifiersSymbolicallyEqual(left, right);
    }

    private static bool QualifiersAreCompatible(
        DeclaredQualifierMeta? leftQualifier,
        DeclaredQualifierMeta? rightQualifier,
        QualifierAxis axis)
    {
        if (leftQualifier is null || rightQualifier is null)
            return false;

        if (axis == QualifierAxis.TemporalDimension)
        {
            if (leftQualifier is DeclaredQualifierMeta.TemporalDimension { Value: PeriodDimension.Any }
                || rightQualifier is DeclaredQualifierMeta.TemporalDimension { Value: PeriodDimension.Any })
            {
                return false;
            }
        }

        if (leftQualifier == rightQualifier || QualifiersSymbolicallyEqual(leftQualifier, rightQualifier))
            return true;

        // Cross-type dimension compatibility: Unit(code, dim) and Dimension(dim) denote the same
        // dimension at different levels of specificity (e.g., a dimension-only price field vs a
        // unit-qualified typed constant). Consider them compatible if their dimension names agree.
        if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
        {
            string? leftDim = leftQualifier switch
            {
                DeclaredQualifierMeta.Unit { DimensionName: var d } => d,
                DeclaredQualifierMeta.Dimension { DimensionName: var d } => d,
                _ => null,
            };
            string? rightDim = rightQualifier switch
            {
                DeclaredQualifierMeta.Unit { DimensionName: var d } => d,
                DeclaredQualifierMeta.Dimension { DimensionName: var d } => d,
                _ => null,
            };
            if (leftDim is not null && rightDim is not null
                && !string.IsNullOrEmpty(leftDim)
                && string.Equals(leftDim, rightDim, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool QualifiersSymbolicallyEqual(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
    {
        // Primary: SourceFieldName populated at type-check time — cross-subtype capable
        if (left.SourceFieldName is { } lsf && right.SourceFieldName is { } rsf)
            return string.Equals(lsf, rsf, StringComparison.Ordinal);

        // Fallback: structural path extraction for legacy/non-interpolated qualifiers
        var leftSourcePath = ExtractQualifierSourcePath(left);
        var rightSourcePath = ExtractQualifierSourcePath(right);
        return leftSourcePath is not null
            && rightSourcePath is not null
            && string.Equals(leftSourcePath, rightSourcePath, StringComparison.Ordinal);
    }

    private static string? ExtractQualifierSourcePath(DeclaredQualifierMeta qualifier)
    {
        var raw = qualifier switch
        {
            DeclaredQualifierMeta.Currency { CurrencyCode: var value } => value,
            DeclaredQualifierMeta.Unit { UnitCode: var value } => value,
            DeclaredQualifierMeta.Dimension { DimensionName: var value } => value,
            DeclaredQualifierMeta.TemporalUnit { UnitName: var value } => value,
            _ => null,
        };

        if (string.IsNullOrEmpty(raw)
            || raw[0] != '{'
            || raw[^1] != '}')
        {
            return null;
        }

        var inner = raw[1..^1];
        var dotIndex = inner.IndexOf('.');
        return dotIndex >= 0 ? inner[..dotIndex] : inner;
    }

    /// <summary>
    /// Extracts a string value suitable for cross-axis comparison from a qualifier.
    /// </summary>
    private static string? ExtractComparableValue(DeclaredQualifierMeta qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.Currency c       => c.CurrencyCode,
        DeclaredQualifierMeta.FromCurrency fc  => fc.CurrencyCode,
        DeclaredQualifierMeta.ToCurrency tc    => tc.CurrencyCode,
        DeclaredQualifierMeta.Unit u           => u.UnitCode,
        DeclaredQualifierMeta.Dimension d      => d.DimensionName,
        DeclaredQualifierMeta.TemporalUnit tu  => tu.UnitName,
        DeclaredQualifierMeta.TemporalDimension td => td.Value switch
        {
            PeriodDimension.Date => "date",
            PeriodDimension.Time => "time",
            _                    => null,   // PeriodDimension.Any cannot satisfy chain comparisons
        },
        _                                      => null,
    };

    private static DeclaredQualifierMeta? ResolveQualifierOnAxis(
        ProofSubject subject, QualifierAxis axis, TypedExpression site, SemanticIndex semantics)
    {
        var resolved = ResolveSubject(subject, site);
        if (resolved is TypedArgRef { DeclaredQualifiers: { } argQualifiers })
        {
            foreach (var qual in argQualifiers)
            {
                if (qual.Axis == axis)
                    return qual;
            }

            if (axis == QualifierAxis.Unit)
            {
                foreach (var qual in argQualifiers)
                {
                    if (qual.Axis == QualifierAxis.Dimension)
                        return qual;
                }
            }

            if (axis == QualifierAxis.Dimension)
            {
                foreach (var qual in argQualifiers)
                {
                    if (qual.Axis == QualifierAxis.TemporalDimension)
                        return qual;
                }
            }
        }

        if (resolved is TypedTypedConstant { DeclaredQualifiers: { } tcQualifiers })
        {
            foreach (var qual in tcQualifiers)
            {
                if (qual.Axis == axis)
                    return qual;
            }

            if (axis == QualifierAxis.Unit)
            {
                foreach (var qual in tcQualifiers)
                {
                    if (qual.Axis == QualifierAxis.Dimension)
                        return qual;
                }
            }

            if (axis == QualifierAxis.Dimension)
            {
                foreach (var qual in tcQualifiers)
                {
                    if (qual.Axis == QualifierAxis.TemporalDimension)
                        return qual;
                }
            }
        }

        // Transitive qualifier resolution through binary operations:
        // When the resolved subject is a TypedBinaryOp (a subexpression), its
        // ResultQualifier tells us how to derive the result's qualifiers.
        if (resolved is TypedBinaryOp binOp && binOp.ResultQualifier is not null)
        {
            switch (binOp.ResultQualifier)
            {
                case SameQualifierRequired:
                    return ResolveQualifierFromExpression(binOp.Left, axis, semantics);

                case QualifiedOperandInherited:
                    var qualifiedOperand = binOp.Left.ResultType == binOp.ResultType
                        ? binOp.Left : binOp.Right;
                    return ResolveQualifierFromExpression(qualifiedOperand, axis, semantics);

                case CompoundUnitCancellationRequired:
                    if (axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency)
                    {
                        return ResolveQualifierFromExpression(binOp.Left, axis, semantics)
                            ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics);
                    }

                    return TryResolveCompoundCancellationUnit(binOp, axis, semantics);

                case CurrencyConversionRequired:
                    if (axis == QualifierAxis.Currency)
                    {
                        // Result currency = exchangerate operand's ToCurrency
                        return ResolveQualifierFromExpression(
                            binOp.Left.ResultType == TypeKind.ExchangeRate ? binOp.Left : binOp.Right,
                            QualifierAxis.ToCurrency, semantics);
                    }
                    return null;

                case CompoundDimensionElevationRequired:
                    if (axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency)
                    {
                        // Currency inherits from price (left operand)
                        return ResolveQualifierFromExpression(binOp.Left, axis, semantics);
                    }
                    // Unit/Dimension: elevated from compound-quantity numerator (right operand)
                    return TryResolveCompoundElevationDimension(binOp, axis, semantics);
            }
        }

        var fieldName = GetFieldName(resolved);
        if (fieldName is null) return null;
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return null;

        foreach (var qual in field.DeclaredQualifiers)
        {
            if (qual.Axis == axis)
                return qual;
        }

        // Axis fallback: Unit → Dimension (dimension-only fields satisfy unit-axis proofs)
        if (axis == QualifierAxis.Unit)
        {
            foreach (var qual in field.DeclaredQualifiers)
            {
                if (qual.Axis == QualifierAxis.Dimension)
                    return qual;
            }
        }

        // Axis fallback: Dimension → TemporalDimension (price of 'time'/'date' satisfies Dimension-axis proofs)
        if (axis == QualifierAxis.Dimension)
        {
            foreach (var qual in field.DeclaredQualifiers)
            {
                if (qual.Axis == QualifierAxis.TemporalDimension)
                    return qual;
            }
        }

        // Implied qualifiers: check type-level metadata after declared qualifiers are exhausted
        // (e.g., duration carries implied TemporalDimension(Time, Baseline))
        var typeMeta = Types.GetMeta(field.ResolvedType);
        foreach (var qual in typeMeta.ImpliedQualifiers)
        {
            if (qual.Axis == axis)
                return qual;
        }

        return null;
    }

    /// <summary>
    /// Resolve a qualifier on an axis from an arbitrary typed expression.
    /// Handles field refs, arg refs, and recursive binary ops.
    /// </summary>
    private static DeclaredQualifierMeta? ResolveQualifierFromExpression(
        TypedExpression expr, QualifierAxis axis, SemanticIndex semantics)
    {
        switch (expr)
        {
            case TypedArgRef { DeclaredQualifiers: { IsDefaultOrEmpty: false } argQuals }:
                foreach (var q in argQuals)
                    if (q.Axis == axis) return q;
                if (axis == QualifierAxis.Unit)
                    foreach (var q in argQuals)
                        if (q.Axis == QualifierAxis.Dimension) return q;
                if (axis == QualifierAxis.Dimension)
                    foreach (var q in argQuals)
                        if (q.Axis == QualifierAxis.TemporalDimension) return q;
                return null;

            case TypedTypedConstant { DeclaredQualifiers: { IsDefaultOrEmpty: false } tcQuals }:
                foreach (var q in tcQuals)
                    if (q.Axis == axis) return q;
                if (axis == QualifierAxis.Unit)
                    foreach (var q in tcQuals)
                        if (q.Axis == QualifierAxis.Dimension) return q;
                if (axis == QualifierAxis.Dimension)
                    foreach (var q in tcQuals)
                        if (q.Axis == QualifierAxis.TemporalDimension) return q;
                return null;

            case TypedFieldRef fieldRef:
                return ResolveFieldQualifier(fieldRef.FieldName, axis, semantics);

            case TypedMemberAccess { Object: TypedFieldRef fieldRef2 }:
                return ResolveFieldQualifier(fieldRef2.FieldName, axis, semantics);

            case TypedInterpolatedTypedConstant itc:
                return ResolveQualifierFromInterpolatedConstant(itc, axis);

            case TypedBinaryOp binOp when binOp.ResultQualifier is not null:
                return binOp.ResultQualifier switch
                {
                    SameQualifierRequired =>
                        ResolveQualifierFromExpression(binOp.Left, axis, semantics),
                    QualifiedOperandInherited =>
                        ResolveQualifierFromExpression(
                            binOp.Left.ResultType == binOp.ResultType ? binOp.Left : binOp.Right,
                            axis, semantics),
                    CompoundUnitCancellationRequired =>
                        axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency
                            ? ResolveQualifierFromExpression(binOp.Left, axis, semantics)
                                ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics)
                            : TryResolveCompoundCancellationUnit(binOp, axis, semantics),
                    CurrencyConversionRequired =>
                        axis == QualifierAxis.Currency
                            // Result currency = exchangerate operand's ToCurrency
                            ? ResolveQualifierFromExpression(
                                binOp.Left.ResultType == TypeKind.ExchangeRate ? binOp.Left : binOp.Right,
                                QualifierAxis.ToCurrency, semantics)
                            : null,
                    CompoundDimensionElevationRequired =>
                        axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency
                            // Currency inherits from price (left operand)
                            ? ResolveQualifierFromExpression(binOp.Left, axis, semantics)
                            // Unit/Dimension: elevated from compound-quantity numerator
                            : TryResolveCompoundElevationDimension(binOp, axis, semantics),
                    _ => null,
                };

            default:
                return null;
        }
    }

    private static DeclaredQualifierMeta? ResolveQualifierFromInterpolatedConstant(
        TypedInterpolatedTypedConstant itc, QualifierAxis axis)
    {
        InterpolationSlotKind? targetSlot = axis switch
        {
            QualifierAxis.Currency => InterpolationSlotKind.Currency,
            QualifierAxis.Unit => InterpolationSlotKind.Unit,
            QualifierAxis.Dimension => InterpolationSlotKind.Unit,
            QualifierAxis.FromCurrency => InterpolationSlotKind.FromCurrency,
            QualifierAxis.ToCurrency => InterpolationSlotKind.ToCurrency,
            _ => null,
        };

        if (targetSlot is null)
            return null;

        foreach (var slot in itc.Slots)
        {
            if (slot.SlotKind == targetSlot)
                return CreateQualifierFromSlotExpression(slot.Expression, axis);
        }

        if (axis == QualifierAxis.Currency)
        {
            foreach (var slot in itc.Slots)
            {
                if (slot.SlotKind == InterpolationSlotKind.NumeratorUnit)
                    return CreateQualifierFromSlotExpression(slot.Expression, axis);
            }
        }

        if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
        {
            foreach (var slot in itc.Slots)
            {
                if (slot.SlotKind == InterpolationSlotKind.DenominatorUnit)
                    return CreateQualifierFromSlotExpression(slot.Expression, axis);
            }
        }

        return null;
    }

    private static DeclaredQualifierMeta? CreateQualifierFromSlotExpression(
        TypedExpression expr, QualifierAxis axis)
    {
        var fieldName = expr switch
        {
            TypedFieldRef f => f.FieldName,
            TypedArgRef a => a.ArgName,
            _ => null,
        };

        if (fieldName is null)
            return null;

        return axis switch
        {
            QualifierAxis.Currency => new DeclaredQualifierMeta.Currency($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit($"{{{fieldName}}}", $"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.FromCurrency => new DeclaredQualifierMeta.FromCurrency($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.ToCurrency => new DeclaredQualifierMeta.ToCurrency($"{{{fieldName}}}", SourceFieldName: fieldName),
            _ => null,
        };
    }

    private static DeclaredQualifierMeta? TryResolveCompoundCancellationUnit(
        TypedBinaryOp binOp, QualifierAxis axis, SemanticIndex semantics)
    {
        var leftQualifier = axis == QualifierAxis.Dimension
            ? ResolveQualifierFromExpression(binOp.Left, QualifierAxis.Unit, semantics)
                ?? ResolveQualifierFromExpression(binOp.Left, axis, semantics)
            : ResolveQualifierFromExpression(binOp.Left, axis, semantics);

        var rightQualifier = axis == QualifierAxis.Dimension
            ? ResolveQualifierFromExpression(binOp.Right, QualifierAxis.Unit, semantics)
                ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics)
            : ResolveQualifierFromExpression(binOp.Right, axis, semantics);

        var compoundValue = ExtractCompoundValue(leftQualifier) ?? ExtractCompoundValue(rightQualifier);
        if (compoundValue is null)
            return null;

        var slashIndex = compoundValue.IndexOf('/');
        if (slashIndex < 0)
            return null;

        var numerator = compoundValue[..slashIndex].Trim();
        if (!TryDeriveCompoundNumeratorDimension(numerator, out var numeratorDimension))
            return null;

        return axis switch
        {
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(numerator, numeratorDimension, QualifierOrigin.Derived),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(numeratorDimension, QualifierOrigin.Derived),
            _ => null,
        };
    }

    private static DeclaredQualifierMeta? TryResolveCompoundElevationDimension(
        TypedBinaryOp binOp, QualifierAxis axis, SemanticIndex semantics)
    {
        // Right operand is compound-quantity[Y/X]; elevation produces the numerator Y.
        var rightQualifier = axis == QualifierAxis.Dimension
            ? ResolveQualifierFromExpression(binOp.Right, QualifierAxis.Unit, semantics)
                ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics)
            : ResolveQualifierFromExpression(binOp.Right, axis, semantics);

        var compoundValue = ExtractCompoundValue(rightQualifier);
        if (compoundValue is null)
            return null;

        var slashIndex = compoundValue.IndexOf('/');
        if (slashIndex < 0)
            return null;

        var numerator = compoundValue[..slashIndex].Trim();
        if (!TryDeriveCompoundNumeratorDimension(numerator, out var numeratorDimension))
            return null;

        return axis switch
        {
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(numerator, numeratorDimension, QualifierOrigin.Derived),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(numeratorDimension, QualifierOrigin.Derived),
            _ => null,
        };
    }

    private static string? ExtractCompoundValue(DeclaredQualifierMeta? qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.Unit { UnitCode: var code } when code.Contains('/') => code,
        DeclaredQualifierMeta.Dimension { DimensionName: var name } when name.Contains('/') => name,
        _ => null,
    };

    private static bool TryDeriveCompoundNumeratorDimension(string unitCode, out string dimensionName)
    {
        if (UnitDimensionHelper.CountQualifierUnitCodes.Contains(unitCode))
        {
            dimensionName = "count";
            return true;
        }

        if (unitCode.StartsWith("{", StringComparison.Ordinal)
            && unitCode.EndsWith("}", StringComparison.Ordinal)
            && unitCode.IndexOf('/') < 0)
        {
            dimensionName = $"{unitCode[..^1]}.dimension}}";
            return true;
        }

        var result = UcumParser.Parse(unitCode);
        if (result.IsValid)
        {
            dimensionName = UnitDimensionHelper.DeriveUnitDimensionName(result.Unit!);
            return !string.IsNullOrWhiteSpace(dimensionName);
        }

        dimensionName = string.Empty;
        return false;
    }

    /// <summary>Look up a field's qualifier on a specific axis (with standard fallbacks).</summary>
    private static DeclaredQualifierMeta? ResolveFieldQualifier(
        string fieldName, QualifierAxis axis, SemanticIndex semantics)
    {
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field))
            return null;

        foreach (var q in field.DeclaredQualifiers)
            if (q.Axis == axis) return q;

        if (axis == QualifierAxis.Unit)
            foreach (var q in field.DeclaredQualifiers)
                if (q.Axis == QualifierAxis.Dimension) return q;

        if (axis == QualifierAxis.Dimension)
            foreach (var q in field.DeclaredQualifiers)
                if (q.Axis == QualifierAxis.TemporalDimension) return q;

        var typeMeta = Types.GetMeta(field.ResolvedType);
        foreach (var q in typeMeta.ImpliedQualifiers)
            if (q.Axis == axis) return q;

        return null;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S6 — Strategy 6: Compositional Constraint Propagation
    // ════════════════════════════════════════════════════════════════════════════

    private static bool TryCompositionalConstraintProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is not NumericProofRequirement numericReq)
            return false;

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

    // ════════════════════════════════════════════════════════════════════════════
    //  S9 — Diagnostic Emission and FaultSiteLink Production
    // ════════════════════════════════════════════════════════════════════════════

    private static Diagnostic CreateDiagnostic(ProofObligation obligation)
    {
        var contextDesc = FormatContextDescription(obligation.Context);
        var usageSuffix = FormatUsageContextSuffix(obligation.Context);

        switch (obligation.Requirement)
        {
            case NumericProofRequirement numeric when TryCreateCollectionSafetyDiagnostic(obligation, out var collectionDiagnostic):
                return collectionDiagnostic;

            case NumericProofRequirement numeric:
                return Diagnostics.Create(GetNumericRequirementDiagnosticCode(obligation, numeric), obligation.Site.Span,
                    GetFieldName(numeric.Subject, obligation.Site) ?? "<unknown>",
                    contextDesc);

            case ModifierRequirement modReq:
                return Diagnostics.Create(DiagnosticCode.UnprovedModifierRequirement, obligation.Site.Span,
                    GetFieldName(modReq.Subject, obligation.Site) ?? "<unknown>",
                    modReq.Required.ToString(),
                    usageSuffix);

            case DimensionProofRequirement dimReq:
                return Diagnostics.Create(DiagnosticCode.UnprovedDimensionRequirement, obligation.Site.Span,
                    GetFieldName(dimReq.Subject, obligation.Site) ?? "<unknown>",
                    FormatPeriodDimension(dimReq.RequiredDimension),
                    usageSuffix);

            case QualifierCompatibilityProofRequirement qcReq:
                string leftName, rightName;
                if (obligation.Site is TypedBinaryOp qcBin)
                {
                    leftName = GetFieldName(qcBin.Left) ?? "<expression>";
                    rightName = GetFieldName(qcBin.Right) ?? "<expression>";
                }
                else
                {
                    leftName = GetFieldName(qcReq.LeftSubject, obligation.Site) ?? "<unknown>";
                    rightName = GetFieldName(qcReq.RightSubject, obligation.Site) ?? "<unknown>";
                }

                return Diagnostics.Create(DiagnosticCode.UnprovedQualifierCompatibility, obligation.Site.Span,
                    leftName,
                    rightName,
                    qcReq.Axis.ToString(),
                    usageSuffix);

            case QualifierChainProofRequirement chainReq:
                return Diagnostics.Create(DiagnosticCode.UnprovedQualifierCompatibility, obligation.Site.Span,
                    GetFieldName(chainReq.LeftSubject, obligation.Site) ?? "<unknown>",
                    GetFieldName(chainReq.RightSubject, obligation.Site) ?? "<unknown>",
                    $"{chainReq.LeftAxis}↔{chainReq.RightAxis}",
                    usageSuffix);

            case PresenceProofRequirement presence:
                return Diagnostics.Create(DiagnosticCode.UnprovedPresenceRequirement, obligation.Site.Span,
                    GetFieldName(presence.Subject, obligation.Site) ?? "<unknown>",
                    usageSuffix);
        }

        throw new InvalidOperationException($"Unexpected proof requirement type '{obligation.Requirement.GetType().FullName}'.");
    }

    private static bool TryCreateCollectionSafetyDiagnostic(ProofObligation obligation, out Diagnostic diagnostic)
    {
        diagnostic = default!;

        if (!IsCollectionCountRequirement(obligation.Requirement, out _))
            return false;

        switch (obligation.Site)
        {
            case TypedMemberAccess access:
                diagnostic = Diagnostics.Create(
                    DiagnosticCode.UnguardedCollectionAccess,
                    obligation.Site.Span,
                    GetFieldName(access.Object) ?? "<unknown>",
                    access.ResolvedAccessor.Name);
                return true;

            case TypedFieldRef fieldRef:
                diagnostic = Diagnostics.Create(
                    DiagnosticCode.UnguardedCollectionMutation,
                    obligation.Site.Span,
                    fieldRef.FieldName,
                    "this mutation action");
                return true;

            default:
                return false;
        }
    }

    private static bool IsCollectionCountRequirement(
        ProofRequirement requirement,
        out NumericProofRequirement? numericRequirement)
    {
        if (requirement is NumericProofRequirement numeric
            && numeric.Subject is SelfSubject { Accessor: { Name: "count" } }
            && numeric.Comparison == OperatorKind.GreaterThan
            && numeric.Threshold == 0m)
        {
            numericRequirement = numeric;
            return true;
        }

        numericRequirement = null;
        return false;
    }

    private static DiagnosticCode GetNumericRequirementDiagnosticCode(ProofObligation obligation, NumericProofRequirement requirement)
    {
        if (IsCollectionCountRequirement(requirement, out _))
        {
            return obligation.Site switch
            {
                TypedMemberAccess => DiagnosticCode.UnguardedCollectionAccess,
                TypedFieldRef => DiagnosticCode.UnguardedCollectionMutation,
                _ => DiagnosticCode.DivisionByZero,
            };
        }

        return requirement.Comparison == OperatorKind.GreaterThanOrEqual && requirement.Threshold == 0m
            ? DiagnosticCode.SqrtOfNegative
            : DiagnosticCode.DivisionByZero;
    }

    private static string FormatContextDescription(ObligationContext context) => context switch
    {
        TransitionRowContext trc => $"on event '{trc.Row.EventName}' from state '{trc.Row.FromState ?? "*"}'",
        EventHandlerContext ehc => $"event handler '{ehc.Handler.EventName}'",
        StateHookContext shc => $"state hook for '{shc.Hook.StateName}'",
        ConstraintContext cc => cc.Constraint switch
        {
            RuleIdentity ri => $"rule at index {ri.RuleIndex}",
            EnsureIdentity { AnchorName: { } anchorName } => $"ensure for '{anchorName}'",
            EnsureIdentity => "global ensure",
            _ => "constraint"
        },
        FieldExpressionContext fec => $"field '{fec.Field.Name}' computed expression",
        _ => "unknown context"
    };

    private static string FormatUsageContextSuffix(ObligationContext context)
    {
        var usageDescription = FormatUsageContextDescription(context);
        return usageDescription == "here"
            ? " (used here)"
            : $" (used {usageDescription})";
    }

    private static string FormatUsageContextDescription(ObligationContext context) => context switch
    {
        TransitionRowContext trc => $"on event '{trc.Row.EventName}' from state '{trc.Row.FromState ?? "*"}'",
        EventHandlerContext ehc => $"in event handler '{ehc.Handler.EventName}'",
        StateHookContext shc => $"in state hook for '{shc.Hook.StateName}'",
        ConstraintContext cc => cc.Constraint switch
        {
            RuleIdentity ri => $"while evaluating rule at index {ri.RuleIndex}",
            EnsureIdentity { AnchorName: { } anchorName } => $"while evaluating ensure for '{anchorName}'",
            EnsureIdentity => "while evaluating the global ensure",
            _ => "here"
        },
        FieldExpressionContext fec => $"in the computed expression for field '{fec.Field.Name}'",
        _ => "here"
    };

    private static string FormatPeriodDimension(PeriodDimension dimension) => dimension switch
    {
        PeriodDimension.Date => "date",
        PeriodDimension.Time => "time",
        _ => dimension.ToString().ToLowerInvariant(),
    };

    private static FaultSiteLink CreateFaultSiteLink(ProofObligation obligation)
    {
        switch (obligation.Requirement)
        {
            case NumericProofRequirement numeric:
                return CreateFaultSiteLink(obligation, GetNumericRequirementDiagnosticCode(obligation, numeric));
            case ModifierRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedModifierRequirement);
            case DimensionProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedDimensionRequirement);
            case QualifierCompatibilityProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedQualifierCompatibility);
            case QualifierChainProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedQualifierCompatibility);
            case PresenceProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedPresenceRequirement);
        }

        throw new InvalidOperationException($"Unexpected proof requirement type '{obligation.Requirement.GetType().FullName}'.");
    }

    private static FaultSiteLink CreateFaultSiteLink(ProofObligation obligation, DiagnosticCode diagnosticCode)
    {
        var faultCode = diagnosticCode switch
        {
            DiagnosticCode.DivisionByZero => FaultCode.DivisionByZero,
            DiagnosticCode.SqrtOfNegative => FaultCode.SqrtOfNegative,
            DiagnosticCode.UnguardedCollectionAccess => FaultCode.CollectionEmptyOnAccess,
            DiagnosticCode.UnguardedCollectionMutation => FaultCode.CollectionEmptyOnMutation,
            _ => FaultCode.DivisionByZero // Proof-only obligation families still share the existing conservative runtime backstop.
        };

        return new FaultSiteLink(obligation, faultCode, diagnosticCode, obligation.Site.Span);
    }

    // ════════════════════════════════════════════════════════════════════════════
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

            default:
                return UnknownSentinel;
        }
    }

    private static readonly object UnknownSentinel = new();

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
