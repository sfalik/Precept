using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
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

    // ── Declared-value default bound (OutOfRange family) ──────────────────────
    //
    // A numeric field/arg default carries a stamped Numeric(SelfValue, ⊕, bound) obligation per
    // applicable modifier. Discharge by evaluating the default's static value against the bound:
    // a point magnitude (literal / typed constant, unit-normalized — covers duration/period the
    // interval domain does not) or, for an interpolated default whose magnitude resolves only to an
    // interval (e.g. '{n} kg' with n bounded), the relevant interval edge. An unresolvable magnitude
    // leaves the obligation Unresolved only when the engine cannot decide — but a value the engine
    // CAN place outside the bound fails here, surfacing OutOfRange.
    private static bool? TryNumericDefaultBoundProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is not NumericProofRequirement { BoundModifierLabel: not null } numeric)
            return null;
        if (obligation.Context is not (FieldDefaultContext or ArgDefaultContext))
            return null;

        // Point magnitude first (literal/typed-constant, unit-normalized) — this is the path that
        // covers duration/period defaults, which the interval domain returns Unbounded for.
        if (TypedExpressionMagnitude.TryGetStaticMagnitude(obligation.Site, out var pointMagnitude))
        {
            var comparable = NormalizeDefaultMagnitudeForComparison(pointMagnitude, obligation.Site);
            return ValueSatisfiesRequirement(comparable, numeric);
        }

        // Interval magnitude (interpolated default whose slot resolves to a bounded interval).
        var interval = IntervalOf(obligation.Site, semantics);
        if (interval.IsUnbounded)
            return null; // unresolvable magnitude — conservative no-decision (Decision 7)

        // A point interval [v,v] decides every comparison; a strict interval decides only the
        // monotone bound checks (≥/>/≤/<). != / == over a non-point interval is undecidable here.
        var isPoint = interval.Min == interval.Max;
        return numeric.Comparison switch
        {
            OperatorKind.GreaterThanOrEqual => interval.Min >= numeric.Threshold,
            OperatorKind.GreaterThan        => interval.Min >  numeric.Threshold,
            OperatorKind.LessThanOrEqual    => interval.Max <= numeric.Threshold,
            OperatorKind.LessThan           => interval.Max <  numeric.Threshold,
            OperatorKind.NotEquals when isPoint => interval.Min != numeric.Threshold,
            OperatorKind.Equals when isPoint    => interval.Min == numeric.Threshold,
            _ => null,
        };
    }

    // ── Strategy 2: Declaration Attribute Proof ───────────────────────────────

    private static bool TryDeclarationAttributeProof(ProofObligation obligation, SemanticIndex semantics)
    {
        // Dimension arm
        if (obligation.Requirement is DimensionProofRequirement dimReq)
        {
            var resolvedSubject = ResolveSubject(dimReq.Subject, obligation.Site);
            // Declared (or derived) dimension first; for an open period, fall back to a guard that
            // narrows its dimension axis (`when X.dimension == 'date'`) — riding the same
            // all-branches / reassignment-aware narrowing as the qualifier axes. A 'datetime'
            // narrowing yields PeriodDimension.Datetime, which equals no single-class (Date/Time)
            // requirement, so it stays compare-but-inert.
            var dimension = ResolvePeriodDimension(resolvedSubject, semantics)
                         ?? NarrowedPeriodDimensionFromGuard(resolvedSubject, obligation, semantics);
            if (dimension is null) return false;
            return dimension == PeriodDimension.Any || dimension == dimReq.RequiredDimension;
        }

        // Modifier arm
        if (obligation.Requirement is ModifierRequirement modReq)
        {
            // Typed-literal inference: when the subject resolves to a literal/typed-constant
            // operand of a binary op, lift the modifier from the contextual sibling operand.
            // A choice literal carries the modifiers of the choice type that gives it meaning;
            // the operator's same-set requirement already established the typing link.
            //
            // Scope-cut: only fires for binary-op sites. This is exhaustive for the current
            // catalog — `ModifierKind.Ordered` is emitted only by ChoiceLessThanChoice and
            // siblings in Operations.cs, all binary ops. A future op that emits an Ordered
            // requirement at a function-call or member-access site would silently miss this
            // inference; the assertion-style coverage at OperationOrderedRequirementShapeTests
            // catches that drift in CI.
            var resolved = ResolveSubject(modReq.Subject, obligation.Site);

            // Accessor / conditional result with inline choice metadata — discharge directly
            // from the propagated TypedChoiceElement slot. Covers cases where GetFieldName
            // can't reach a declaration (accessor chains, conditional receivers).
            if (modReq.Required == ModifierKind.Ordered
                && resolved is TypedExpression resolvedExpr
                && ChoiceMetadataOf(resolvedExpr) is { Ordered: true })
            {
                return true;
            }

            if (obligation.Site is TypedBinaryOp binSite
                && resolved is TypedExpression literalSubject
                && IsTypedLiteral(literalSubject))
            {
                var sibling = ReferenceEquals(literalSubject, binSite.Right) ? binSite.Left : binSite.Right;
                if (OperandSatisfiesModifier(sibling, modReq.Required, semantics))
                    return true;
            }

            var fieldName = GetFieldName(modReq.Subject, obligation.Site);
            if (fieldName is null) return false;
            if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return false;
            if (field.Modifiers.Contains(modReq.Required)) return true;

            // Field-level choice ordering: when the obligation is Ordered on a choice
            // (scalar field) or a choice returned from a collection accessor
            // (.first/.last/.at on `set of choice of T(...) ordered`), discharge from the
            // field's TypedChoiceElement — the single source of truth for choice ordering.
            if (modReq.Required == ModifierKind.Ordered
                && field.ElementType is TypedChoiceElement { Ordered: true })
            {
                return true;
            }

            return false;
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

        // mincount discharge: a statically-literal `mincount N` (N ≥ 1) guarantees the collection
        // is non-empty, so it discharges the `count > 0` access-safety obligation for
        // .peek/.first/.last/.min/.max. This is the discharge collection-`notempty` used to provide
        // before it became string-only; mincount's own `count >= DeclarationValue` satisfaction
        // resolves conservatively to null in SatisfactionCovers (no runtime value), so it is read
        // here from the field's resolved declared magnitude instead.
        if (reqSubject is SelfSubject { Accessor: { Name: "count" } }
            && obligation.Requirement is NumericProofRequirement
            {
                Comparison: OperatorKind.GreaterThan,
                Threshold: 0m,
            }
            && attributeField.DeclaredMinCount is { } minCount
            && minCount >= 1)
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

    /// <summary>
    /// Discharges an <see cref="IndexBoundsProofRequirement"/> by walking the
    /// row/handler/hook guard branches for both lower-bound (<c>N &gt;= 0</c>) and
    /// upper-bound (<c>N &lt; F.count</c> or <c>N &lt;= F.count</c> per
    /// <see cref="IndexBoundsMode"/>) facts. Each disjunctive branch must
    /// independently establish both bounds.
    /// </summary>
    private static bool TryIndexBoundsProof(
        IndexBoundsProofRequirement req,
        ProofObligation obligation,
        SemanticIndex semantics)
    {
        // Two site shapes:
        //  (a) Accessor case (.at(N)): Site = TypedMemberAccess; receiver field = Site.Object;
        //      index expression = ResolveSubject(ParamSubject(IndexParam), Site) = Site.Arguments[0].
        //  (b) Action case (Insert/RemoveAt): Site = TypedFieldRef (receiver field); the index
        //      expression is captured from the parent action via the obligation's Context.
        TypedExpression? indexExpr;
        string? receiverField;
        if (obligation.Site is TypedMemberAccess accessSite)
        {
            indexExpr = ResolveSubject(req.Subject, accessSite);
            receiverField = (accessSite.Object as TypedFieldRef)?.FieldName;
        }
        else if (obligation.Site is TypedFieldRef fieldSite)
        {
            receiverField = fieldSite.FieldName;
            indexExpr = FindActionIndexInContext(obligation.Context, fieldSite.FieldName);
        }
        else
        {
            return false;
        }
        if (indexExpr is null || receiverField is null) return false;

        // Sequential proof flow: the bound is established by a guard over the index subject
        // (`N >= 0`) and the collection's count (`N < F.count`). If either the index field or
        // the collection field was reassigned earlier in this action chain, the bound fact is
        // stale and must not discharge. (Event args / literals are never reassigned by a
        // full-replacement action, so only field subjects participate.)
        if (obligation.ReassignedBefore.Contains(receiverField)) return false;
        if (GetFieldName(indexExpr) is { } idxField
            && obligation.ReassignedBefore.Contains(idxField))
            return false;

        var guard = obligation.Context switch
        {
            TransitionRowContext t => t.Row.Guard,
            StateHookContext s => s.Hook.Guard,
            EventHandlerContext h => h.Handler.Guard,
            _ => null,
        };

        // Type-derived lower bound: if the index expression resolves to a field/arg
        // declared `nonnegative`, the lower bound discharges silently. Otherwise the
        // discharge requires an explicit `N >= 0` guard constraint.
        bool lowerBoundTypeDerived = IsTypeDerivedNonnegative(indexExpr, semantics);

        // No guard at all → cannot discharge. The upper bound has no type-derived
        // counterpart to `nonnegative` (a collection's max-index is dynamic), so a
        // guard is always required even when the lower bound is type-derived.
        if (guard is null)
            return false;

        var branches = ExtractGuardBranches(guard);
        if (branches.Length == 0) return false;

        foreach (var numericBranch in branches)
        {
            bool lowerBoundOk = lowerBoundTypeDerived
                || BranchEstablishesLowerBound(numericBranch, indexExpr);
            if (!lowerBoundOk) return false;
        }

        // Upper bound walk (separate from lower-bound branches because the
        // upper-bound constraint shape isn't representable as a GuardConstraint).
        var upperBounds = ExtractParamUpperBoundsByBranch(guard);
        if (upperBounds.Length != branches.Length)
        {
            // Shouldn't happen — both walkers use the same branching structure.
            // Conservative reject.
            return false;
        }

        for (int i = 0; i < upperBounds.Length; i++)
        {
            if (!BranchEstablishesUpperBound(upperBounds[i], indexExpr, receiverField, req.Mode, req.UpperBoundAccessor.Name))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Walks the obligation's parent context (a TransitionRow / EventHandler / StateHook)
    /// looking for an action targeting <paramref name="fieldName"/> that carries an
    /// <see cref="ActionSlotRole.Index"/> slot; returns the action's index TypedExpression.
    /// Resolution is catalog-driven: the action's <see cref="TypedInputAction.SecondaryRole"/>
    /// (for actions where the index lives on the secondary expression) and the catalog's
    /// <see cref="ActionMeta.InputSlotRole"/> (for actions where the input expression IS
    /// the index) both surface as <see cref="ActionSlotRole.Index"/>. No switching on
    /// <see cref="ActionKind"/> — adding a new parameterized-index action just sets the
    /// appropriate role in its catalog entry.
    /// </summary>
    private static TypedExpression? FindActionIndexInContext(ObligationContext context, string fieldName)
    {
        var actions = context switch
        {
            TransitionRowContext t => (t.Row is TypedTransitionRowSuccess s) ? s.Actions : default,
            StateHookContext s => s.Hook.Actions,
            EventHandlerContext h => (h.Handler is TypedEventRowSuccess es) ? es.Actions : default,
            _ => default,
        };
        if (actions.IsDefaultOrEmpty) return null;

        foreach (var action in actions)
        {
            if (action.FieldName != fieldName) continue;
            if (action is not TypedInputAction input) continue;
            // The index expression lives in whichever slot the catalog declares for the
            // Index role. SecondaryRole is per-action-instance metadata; InputSlotRole
            // is per-action-kind catalog metadata. Either path resolves to "this slot
            // holds the index" without naming the ActionKind.
            if (input.SecondaryRole == ActionSecondaryRole.Index)
                return input.SecondaryExpression;
            if (Actions.GetMeta(action.Kind).InputSlotRole == ActionSlotRole.Index)
                return input.InputExpression;
        }
        return null;
    }

    private static bool IsTypeDerivedNonnegative(TypedExpression expr, SemanticIndex semantics)
    {
        if (expr is TypedFieldRef fr
            && semantics.FieldsByName.TryGetValue(fr.FieldName, out var field))
        {
            return field.Modifiers.Contains(ModifierKind.Nonnegative)
                || field.ImpliedModifiers.Contains(ModifierKind.Nonnegative);
        }

        if (expr is TypedArgRef ar
            && semantics.EventsByName.TryGetValue(ar.EventName, out var evt))
        {
            var arg = evt.Args.FirstOrDefault(a => a.Name == ar.ArgName);
            if (arg is not null)
                return arg.Modifiers.Contains(ModifierKind.Nonnegative);
        }

        return false;
    }

    private static bool BranchEstablishesLowerBound(
        ImmutableArray<GuardConstraint> branch, TypedExpression indexExpr)
    {
        var indexName = indexExpr switch
        {
            TypedFieldRef fr => fr.FieldName,
            TypedArgRef ar => ar.ArgName,
            TypedMemberAccess { Object: TypedFieldRef ofr } => ofr.FieldName,
            TypedMemberAccess { Object: TypedArgRef oar } => oar.ArgName,
            _ => null,
        };
        if (indexName is null) return false;

        foreach (var c in branch)
        {
            if (c.Field == indexName && c.Value is { } v && v >= 0
                && c.Comparison is OperatorKind.GreaterThanOrEqual or OperatorKind.GreaterThan)
            {
                return true;
            }
        }
        return false;
    }

    private static ImmutableArray<ImmutableArray<ParamUpperBoundConstraint>> ExtractParamUpperBoundsByBranch(
        TypedExpression guard)
    {
        var branches = ExtractParamUpperBoundsByBranchCore(guard);
        return branches.IsEmpty
            ? ImmutableArray.Create(ImmutableArray<ParamUpperBoundConstraint>.Empty)
            : branches;
    }

    private static ImmutableArray<ImmutableArray<ParamUpperBoundConstraint>> ExtractParamUpperBoundsByBranchCore(
        TypedExpression expr)
    {
        if (expr is TypedBinaryOp bin)
        {
            var op = Operations.GetMeta(bin.ResolvedOp).Op;

            if (op == OperatorKind.Or)
            {
                var left = ExtractParamUpperBoundsByBranchCore(bin.Left);
                var right = ExtractParamUpperBoundsByBranchCore(bin.Right);
                return left.AddRange(right);
            }

            if (op == OperatorKind.And)
            {
                var left = ExtractParamUpperBoundsByBranchCore(bin.Left);
                var right = ExtractParamUpperBoundsByBranchCore(bin.Right);
                if (left.IsEmpty) return right;
                if (right.IsEmpty) return left;
                var cross = ImmutableArray.CreateBuilder<ImmutableArray<ParamUpperBoundConstraint>>(left.Length * right.Length);
                foreach (var lb in left)
                    foreach (var rb in right)
                        cross.Add(lb.AddRange(rb));
                return cross.ToImmutable();
            }
        }

        var leaf = ImmutableArray.CreateBuilder<ParamUpperBoundConstraint>();
        ExtractParamUpperBoundLeaf(expr, leaf);
        return ImmutableArray.Create(leaf.ToImmutable());
    }

    private static void ExtractParamUpperBoundLeaf(
        TypedExpression expr, ImmutableArray<ParamUpperBoundConstraint>.Builder builder)
    {
        if (expr is not TypedBinaryOp bin) return;
        var op = Operations.GetMeta(bin.ResolvedOp).Op;
        if (op is not (OperatorKind.LessThan or OperatorKind.LessThanOrEqual
            or OperatorKind.GreaterThan or OperatorKind.GreaterThanOrEqual))
            return;

        // <index_expr> <op> <field>.<accessor>
        if (bin.Right is TypedMemberAccess { Object: TypedFieldRef rf, ResolvedAccessor: var ra })
        {
            builder.Add(new ParamUpperBoundConstraint(bin.Left, op, rf.FieldName, ra.Name));
            return;
        }
        // <field>.<accessor> <op> <index_expr>  →  invert
        if (bin.Left is TypedMemberAccess { Object: TypedFieldRef lf, ResolvedAccessor: var la })
        {
            builder.Add(new ParamUpperBoundConstraint(bin.Right, InvertOp(op), lf.FieldName, la.Name));
        }
    }

    private static bool BranchEstablishesUpperBound(
        ImmutableArray<ParamUpperBoundConstraint> branch,
        TypedExpression indexExpr,
        string receiverField,
        IndexBoundsMode mode,
        string upperBoundAccessor)
    {
        foreach (var c in branch)
        {
            if (c.CollectionField != receiverField) continue;
            if (c.AccessorName != upperBoundAccessor) continue;
            if (!TypedExpressionShapeEqual(c.IndexExpression, indexExpr)) continue;

            var ok = mode switch
            {
                IndexBoundsMode.StrictlyBefore => c.Comparison == OperatorKind.LessThan,
                IndexBoundsMode.AtOrBefore => c.Comparison is OperatorKind.LessThan or OperatorKind.LessThanOrEqual,
                _ => false,
            };
            if (ok) return true;
        }
        return false;
    }

    /// <summary>
    /// Structural equality on TypedExpression shapes — ignores Span (which differs
    /// between the guard occurrence and the obligation-site occurrence even when
    /// the expressions are semantically the same identifier reference).
    /// </summary>
    private static bool TypedExpressionShapeEqual(TypedExpression a, TypedExpression b) => (a, b) switch
    {
        (TypedFieldRef af, TypedFieldRef bf) => af.FieldName == bf.FieldName,
        (TypedArgRef ar, TypedArgRef br) => ar.EventName == br.EventName && ar.ArgName == br.ArgName,
        (TypedMemberAccess ama, TypedMemberAccess bma) =>
            ama.ResolvedAccessor.Name == bma.ResolvedAccessor.Name
            && TypedExpressionShapeEqual(ama.Object, bma.Object),
        (TypedLiteral al, TypedLiteral bl) => Equals(al.Value, bl.Value) && al.ResultType == bl.ResultType,
        _ => false,
    };

    /// <summary>
    /// Discharges a <see cref="KeyPresenceProofRequirement"/> by matching the row/handler
    /// guard against a <c>F contains X</c> (or <c>not (F contains X)</c> when
    /// <c>RequireAbsence</c>) check. The field name is recovered from the obligation
    /// subject; the contains check must reference the same field.
    /// </summary>
    private static bool TryKeyPresenceProof(
        KeyPresenceProofRequirement req,
        ProofObligation obligation,
        SemanticIndex semantics)
    {
        var fieldName = GetFieldName(req.Subject, obligation.Site);
        if (fieldName is null) return false;

        // Sequential proof flow: a `contains` guard fact over a collection reassigned earlier
        // in this chain is stale — the membership the guard established is about the old value.
        if (obligation.ReassignedBefore.Contains(fieldName)) return false;

        var guard = obligation.Context switch
        {
            TransitionRowContext t => t.Row.Guard,
            StateHookContext s => s.Hook.Guard,
            EventHandlerContext h => h.Handler.Guard,
            _ => null,
        };
        if (guard is null) return false;

        var branches = ExtractGuardBranches(guard);
        if (branches.Length == 0) return false;

        foreach (var branchConstraints in branches)
        {
            if (!GuardHasContainsCheck(guard, fieldName, requireNegated: req.RequireAbsence))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Discharges a collection non-empty obligation (<c>F.count &gt; 0</c>) when a prior grow action
    /// in the same chain established <c>F</c> as non-empty (spec § 0.6 item 7, forward-propagation).
    /// A grow adds at least one element, so <c>count &gt;= 1</c> holds regardless of prior contents
    /// and regardless of any guard — this is an independent positive proof source, not guard reuse.
    /// </summary>
    private static bool TryCollectionGrowthProof(ProofObligation obligation)
    {
        if (!IsCollectionCountRequirement(obligation.Requirement, out var countReq)) return false;
        return GetFieldName(countReq!.Subject, obligation.Site) is { } field
            && obligation.CountEstablishedBefore.Contains(field);
    }

    private static bool TryGuardInPathProof(ProofObligation obligation, SemanticIndex semantics)
    {
        // Sequential proof flow (spec § 0.6 item 7): a non-empty (`count > 0`) guard fact is stale
        // once the collection was shrunk earlier in the chain — the shrink may have reduced count
        // to 0, so the pre-shrink guard must not discharge a `count > 0` obligation after it.
        if (IsCollectionCountRequirement(obligation.Requirement, out var staleCountReq)
            && GetFieldName(staleCountReq!.Subject, obligation.Site) is { } shrunkField
            && obligation.CountInvalidatedBefore.Contains(shrunkField))
            return false;

        var guard = obligation.Context switch
        {
            TransitionRowContext t => t.Row.Guard,
            StateHookContext s => s.Hook.Guard,
            EventHandlerContext h => h.Handler.Guard,
            ConstraintContext c => c.Constraint switch
            {
                RuleIdentity ri => semantics.Rules[ri.RuleIndex].Guard,
                EnsureIdentity ei => semantics.Ensures[ei.EnsureIndex].Guard,
                _ => null
            },
            _ => null
        };

        // Event-ensure narrowing: when the obligation site is inside a transition row
        // body, every event ensure anchored to the row's event provably holds before
        // the row body runs — the event-ensure layer rejects the event when its
        // condition is false. Contribute those ensure conditions as additional
        // narrowing facts, AND-combined with the row's explicit guard.
        var branches = guard is null
            ? ImmutableArray.Create(ImmutableArray<GuardConstraint>.Empty)
            : ExtractGuardBranches(guard);

        if (obligation.Context is TransitionRowContext trc)
        {
            foreach (var ensure in semantics.Ensures)
            {
                if (ensure.AnchorEvent != trc.Row.EventName) continue;
                // The ensure's Condition is the body assertion (e.g., `Amount is set`).
                // Ensure's own Guard, if present, is handled separately when the
                // ensure itself is being proven; it does NOT gate downstream narrowing
                // because guarded ensures are conditional facts.
                if (ensure.Guard is not null) continue;
                var ensureBranches = ExtractGuardBranches(ensure.Condition);
                branches = CombineAndBranches(branches, ensureBranches);
            }
        }

        if (guard is null && branches.Length == 1 && branches[0].IsEmpty) return false;

        // Every OR branch must independently prove the obligation for it to discharge.
        foreach (var branchConstraints in branches)
        {
            var thisBranchProved = false;
            foreach (var gc in branchConstraints)
            {
                // Sequential proof flow (spec § 0.6 item 7): a guard fact about a field
                // reassigned earlier in this action chain is stale — it must not discharge.
                if (obligation.ReassignedBefore.Contains(gc.Field)) continue;

                if (obligation.Requirement is NumericProofRequirement numeric)
                {
                    if (GuardSubsumes(gc, numeric, obligation.Site)) { thisBranchProved = true; break; }
                }
                else if (obligation.Requirement is PresenceProofRequirement presence)
                {
                    if (gc.Field == GetFieldName(presence.Subject, obligation.Site) && gc.IsPresenceCheck)
                    { thisBranchProved = true; break; }
                }
            }
            if (!thisBranchProved) return false;
        }

        return branches.Length > 0;
    }

    /// <summary>
    /// Returns the disjunctive branches of a guard expression as sets of <see cref="GuardConstraint"/>.
    /// AND nodes cross-product their children's branch sets; OR nodes union them.
    /// Each branch set contains all constraints that hold simultaneously in that branch.
    /// </summary>
    private static ImmutableArray<ImmutableArray<GuardConstraint>> ExtractGuardBranches(TypedExpression guard)
    {
        var branches = ExtractGuardBranchesCore(guard);
        return branches.IsEmpty
            ? ImmutableArray.Create(ImmutableArray<GuardConstraint>.Empty)
            : branches;
    }

    /// <summary>
    /// AND-combines two branch sets via cross-product, matching the AND-node logic
    /// inside <see cref="ExtractGuardBranchesCore"/>. Used to fold additional narrowing
    /// sources (e.g., event-ensure conditions) into a row's existing guard branches.
    /// </summary>
    private static ImmutableArray<ImmutableArray<GuardConstraint>> CombineAndBranches(
        ImmutableArray<ImmutableArray<GuardConstraint>> left,
        ImmutableArray<ImmutableArray<GuardConstraint>> right)
    {
        if (left.IsEmpty) return right;
        if (right.IsEmpty) return left;
        // If either side is the single "no constraints" branch, the other side passes through.
        if (left.Length == 1 && left[0].IsEmpty) return right;
        if (right.Length == 1 && right[0].IsEmpty) return left;
        var cross = ImmutableArray.CreateBuilder<ImmutableArray<GuardConstraint>>(left.Length * right.Length);
        foreach (var lb in left)
            foreach (var rb in right)
                cross.Add(lb.AddRange(rb));
        return cross.ToImmutable();
    }

    private static ImmutableArray<ImmutableArray<GuardConstraint>> ExtractGuardBranchesCore(TypedExpression expr)
    {
        if (expr is TypedBinaryOp bin)
        {
            var op = Operations.GetMeta(bin.ResolvedOp).Op;

            if (op == OperatorKind.Or)
            {
                var leftBranches = ExtractGuardBranchesCore(bin.Left);
                var rightBranches = ExtractGuardBranchesCore(bin.Right);
                return leftBranches.AddRange(rightBranches);
            }

            if (op == OperatorKind.And)
            {
                var leftBranches = ExtractGuardBranchesCore(bin.Left);
                var rightBranches = ExtractGuardBranchesCore(bin.Right);
                if (leftBranches.IsEmpty) return rightBranches;
                if (rightBranches.IsEmpty) return leftBranches;
                // Cross-product: each left-branch paired with each right-branch
                var cross = ImmutableArray.CreateBuilder<ImmutableArray<GuardConstraint>>(leftBranches.Length * rightBranches.Length);
                foreach (var lb in leftBranches)
                    foreach (var rb in rightBranches)
                        cross.Add(lb.AddRange(rb));
                return cross.ToImmutable();
            }
        }

        // Atomic node: extract leaf constraints via the existing non-AND/OR handler
        var leafBuilder = ImmutableArray.CreateBuilder<GuardConstraint>();
        ExtractGuardLeafConstraints(expr, leafBuilder);
        return ImmutableArray.Create(leafBuilder.ToImmutable());
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

            default:
                ExtractGuardLeafConstraints(expr, builder);
                break;
        }
    }

    /// <summary>
    /// Extracts a single guard constraint from an atomic (non-AND, non-OR) expression.
    /// </summary>
    private static void ExtractGuardLeafConstraints(TypedExpression expr, ImmutableArray<GuardConstraint>.Builder builder)
    {
        switch (expr)
        {
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
                // arg op literal — events expose args as TypedArgRef. Used by IndexBounds
                // lower-bound discharge (`when Pick.Index >= 0`).
                else if (bin.Left is TypedArgRef leftArg && bin.Right is TypedLiteral rightArgLit)
                {
                    var litValue = ToDecimal(rightArgLit.Value);
                    if (litValue is not null)
                        builder.Add(new GuardConstraint(leftArg.ArgName, compOp, litValue, false, IsArg: true));
                }
                // literal op arg → invert
                else if (bin.Left is TypedLiteral leftArgLit && bin.Right is TypedArgRef rightArg)
                {
                    var litValue = ToDecimal(leftArgLit.Value);
                    if (litValue is not null)
                        builder.Add(new GuardConstraint(rightArg.ArgName, InvertOp(compOp), litValue, false, IsArg: true));
                }
                break;
            }

            case TypedPostfixOp post when !post.IsNegated && post.Operand is TypedFieldRef postField:
                // field is set
                builder.Add(new GuardConstraint(postField.FieldName, OperatorKind.NotEquals, null, true));
                break;

            case TypedPostfixOp post when !post.IsNegated && post.Operand is TypedArgRef postArg:
                // event arg is set
                builder.Add(new GuardConstraint(postArg.ArgName, OperatorKind.NotEquals, null, true, IsArg: true));
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
        return guard.Value is { } value && NumericConstraintSubsumes(guard.Comparison, value, requirement);
    }

    private static bool NumericConstraintSubsumes(
        OperatorKind comparison,
        decimal value,
        NumericProofRequirement requirement)
    {
        // Discrete equality narrowing: when the guard pins the subject to a
        // singleton value (`F == V_lit`), check whether V_lit satisfies the
        // requirement's (comparison, threshold) pair directly. Closes the
        // false-positive class where `when Severity == 1 ⇒ Severity > 0`
        // was previously undischarged. Minimal sound surface: direct-equality
        // only; disjunctive/range narrowing is a separate extension.
        if (comparison == OperatorKind.Equals && ValueSatisfiesRequirement(value, requirement))
            return true;

        return (comparison, requirement.Comparison) switch
        {
            (OperatorKind.GreaterThan, OperatorKind.NotEquals)
                when value == 0 && requirement.Threshold == 0 => true,
            (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual)
                when value >= requirement.Threshold => true,
            (OperatorKind.GreaterThan, OperatorKind.GreaterThan)
                when value >= requirement.Threshold => true,
            (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual)
                when value >= requirement.Threshold => true,
            (OperatorKind.LessThan, OperatorKind.NotEquals)
                when value == 0 && requirement.Threshold == 0 => true,
            _ when comparison == requirement.Comparison && value == requirement.Threshold => true,
            _ => false,
        };
    }

    /// <summary>
    /// Evaluates whether a singleton value satisfies a numeric requirement's
    /// (comparison, threshold) pair. Used by the discrete-equality narrowing
    /// path in <see cref="NumericConstraintSubsumes"/>.
    /// </summary>
    private static bool ValueSatisfiesRequirement(decimal value, NumericProofRequirement requirement) =>
        requirement.Comparison switch
        {
            OperatorKind.GreaterThan        => value >  requirement.Threshold,
            OperatorKind.GreaterThanOrEqual => value >= requirement.Threshold,
            OperatorKind.LessThan           => value <  requirement.Threshold,
            OperatorKind.LessThanOrEqual    => value <= requirement.Threshold,
            OperatorKind.Equals             => value == requirement.Threshold,
            OperatorKind.NotEquals          => value != requirement.Threshold,
            _ => false,
        };

    /// <summary>
    /// Unit-normalizes a default's raw point magnitude for comparison against its declared
    /// (normalized) bound: quantity → UCUM base unit, price → per-base-unit. Other types compare
    /// as-authored. This is the discharge-side counterpart of the bound normalization the type
    /// checker applies when it records NormalizedDeclaredMin/Max.
    /// </summary>
    private static decimal NormalizeDefaultMagnitudeForComparison(decimal rawMagnitude, TypedExpression site)
    {
        if (site is not TypedTypedConstant ttc)
            return rawMagnitude;

        return ttc.ParsedValue switch
        {
            ValueTuple<decimal, UcumParsedUnit?> (_, var unit) when site.ResultType == TypeKind.Quantity =>
                TypedConstantNormalizer.NormalizeQuantity(rawMagnitude, unit),
            ValueTuple<decimal, object?, UcumParsedUnit?> (_, _, var denominatorUnit) when site.ResultType == TypeKind.Price =>
                TypedConstantNormalizer.NormalizePrice(rawMagnitude, denominatorUnit),
            _ => rawMagnitude,
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
            EventHandlerContext h => h.Handler.Guard,
            ConstraintContext c => c.Constraint switch
            {
                RuleIdentity ri => semantics.Rules[ri.RuleIndex].Guard,
                EnsureIdentity ei => semantics.Ensures[ei.EnsureIndex].Guard,
                _ => null
            },
            _ => null
        };
        if (guard is null) return false;

        if (obligation.Site is not TypedBinaryOp binaryOp) return false;
        if (obligation.Requirement is not NumericProofRequirement numeric) return false;

        if (!IsSubtractionOp(binaryOp.ResolvedOp)) return false;

        var leftField = GetFieldName(binaryOp.Left);
        var rightField = GetFieldName(binaryOp.Right);
        if (leftField is null || rightField is null) return false;

        // Sequential proof flow: the narrowing relies on a guard relating the two operands
        // (e.g. `A >= B` discharges `A - B >= 0`). If either operand was reassigned earlier in
        // this chain, that relation is stale and must not discharge.
        if (obligation.ReassignedBefore.Contains(leftField)
            || obligation.ReassignedBefore.Contains(rightField))
            return false;

        var branches = ExtractFieldToFieldBranches(guard);
        if (branches.IsEmpty) return false;

        // Every OR branch must independently prove the flow-narrowing obligation.
        foreach (var branchConstraints in branches)
        {
            var thisBranchProved = false;
            foreach (var rg in branchConstraints)
            {
                if (!((rg.LeftField == leftField && rg.RightField == rightField) ||
                      (rg.LeftField == rightField && rg.RightField == leftField)))
                    continue;

                if (GuardRelationImpliesObligation(rg, binaryOp, leftField, rightField, numeric))
                { thisBranchProved = true; break; }
            }
            if (!thisBranchProved) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the disjunctive branches of a guard expression as sets of <see cref="FieldToFieldConstraint"/>.
    /// AND nodes cross-product their children's branch sets; OR nodes union them.
    /// </summary>
    private static ImmutableArray<ImmutableArray<FieldToFieldConstraint>> ExtractFieldToFieldBranches(TypedExpression guard)
    {
        var branches = ExtractFieldToFieldBranchesCore(guard);
        return branches.IsEmpty
            ? ImmutableArray.Create(ImmutableArray<FieldToFieldConstraint>.Empty)
            : branches;
    }

    private static ImmutableArray<ImmutableArray<FieldToFieldConstraint>> ExtractFieldToFieldBranchesCore(TypedExpression expr)
    {
        if (expr is TypedBinaryOp bin)
        {
            var op = Operations.GetMeta(bin.ResolvedOp).Op;

            if (op == OperatorKind.Or)
            {
                var leftBranches = ExtractFieldToFieldBranchesCore(bin.Left);
                var rightBranches = ExtractFieldToFieldBranchesCore(bin.Right);
                return leftBranches.AddRange(rightBranches);
            }

            if (op == OperatorKind.And)
            {
                var leftBranches = ExtractFieldToFieldBranchesCore(bin.Left);
                var rightBranches = ExtractFieldToFieldBranchesCore(bin.Right);
                if (leftBranches.IsEmpty) return rightBranches;
                if (rightBranches.IsEmpty) return leftBranches;
                var cross = ImmutableArray.CreateBuilder<ImmutableArray<FieldToFieldConstraint>>(leftBranches.Length * rightBranches.Length);
                foreach (var lb in leftBranches)
                    foreach (var rb in rightBranches)
                        cross.Add(lb.AddRange(rb));
                return cross.ToImmutable();
            }
        }

        // Atomic: extract field-to-field leaf constraint
        var leafBuilder = ImmutableArray.CreateBuilder<FieldToFieldConstraint>();
        ExtractFieldToFieldLeaf(expr, leafBuilder);
        return ImmutableArray.Create(leafBuilder.ToImmutable());
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

            default:
                ExtractFieldToFieldLeaf(expr, builder);
                break;
        }
    }

    private static void ExtractFieldToFieldLeaf(TypedExpression expr, ImmutableArray<FieldToFieldConstraint>.Builder builder)
    {
        if (expr is TypedBinaryOp bin)
        {
            var compOp = Operations.GetMeta(bin.ResolvedOp).Op;
            if (bin.Left is TypedFieldRef leftF && bin.Right is TypedFieldRef rightF)
                builder.Add(new FieldToFieldConstraint(leftF.FieldName, compOp, rightF.FieldName));
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

    // ── Contains Guard Matching (PRE0099, PRE0101) ────────────────────────────

    /// <summary>
    /// Checks whether the guard expression contains a 'Field contains X' check
    /// (or 'not (Field contains X)' if <paramref name="requireNegated"/> is true).
    /// Used to satisfy <see cref="KeyPresenceProofRequirement"/> obligations.
    /// </summary>
    private static bool GuardHasContainsCheck(TypedExpression guard, string? fieldName, bool requireNegated)
    {
        if (fieldName is null) return false;
        return WalkForContains(guard, fieldName, requireNegated);
    }

    private static bool WalkForContains(TypedExpression expr, string fieldName, bool requireNegated)
    {
        switch (expr)
        {
            case TypedBinaryOp bin:
            {
                var op = Operations.GetMeta(bin.ResolvedOp).Op;

                if (op == OperatorKind.And)
                    return WalkForContains(bin.Left, fieldName, requireNegated)
                        || WalkForContains(bin.Right, fieldName, requireNegated);

                if (op == OperatorKind.Contains && !requireNegated)
                {
                    // Field contains X — positive contains guard
                    if (bin.Left is TypedFieldRef fr && fr.FieldName == fieldName)
                        return true;
                }
                break;
            }

            case TypedUnaryOp { ResolvedOp: var uop } un when Operations.GetMeta(uop).Op == OperatorKind.Not:
            {
                // not (Field contains X) — negative contains guard
                if (requireNegated && un.Operand is TypedBinaryOp innerBin)
                {
                    var innerOp = Operations.GetMeta(innerBin.ResolvedOp).Op;
                    if (innerOp == OperatorKind.Contains
                        && innerBin.Left is TypedFieldRef fr
                        && fr.FieldName == fieldName)
                        return true;
                }
                break;
            }
        }

        return false;
    }

    private static bool IsTypedLiteral(TypedExpression expr) => expr switch
    {
        TypedLiteral => true,
        TypedTypedConstant => true,
        InterpolatedTypedConstant => true,
        _ => false,
    };

    /// <summary>
    /// True when an operand of a binary op satisfies <paramref name="required"/> in its own right.
    /// Mirrors the per-operand discharge a separate obligation would compute, without re-emitting
    /// one. Used to lift a literal-side modifier from the sibling field/accessor in
    /// <see cref="TryDeclarationAttributeProof"/>.
    /// </summary>
    private static bool OperandSatisfiesModifier(
        TypedExpression operand, ModifierKind required, SemanticIndex semantics)
    {
        // Accessor / conditional results propagate choice-element metadata directly on the
        // typed expression — read the slot before walking back to a field declaration.
        if (required == ModifierKind.Ordered
            && ChoiceMetadataOf(operand) is { Ordered: true })
        {
            return true;
        }

        var fieldName = GetFieldName(operand);
        if (fieldName is null) return false;
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return false;
        if (field.Modifiers.Contains(required)) return true;

        if (required == ModifierKind.Ordered
            && field.ElementType is TypedChoiceElement { Ordered: true })
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads choice-element metadata propagated onto a typed expression. Returns the slot
    /// on <see cref="TypedMemberAccess"/> and <see cref="TypedConditional"/>; <c>null</c>
    /// for expression shapes without an inline metadata carrier (the caller can fall back
    /// to walking to a field declaration).
    /// </summary>
    private static TypedChoiceElement? ChoiceMetadataOf(TypedExpression expr) => expr switch
    {
        TypedMemberAccess ma         => ma.ChoiceMetadata,
        TypedConditional cond        => cond.ChoiceMetadata,
        _                            => null,
    };
}
