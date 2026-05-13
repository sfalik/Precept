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
        return guard.Value is { } value && NumericConstraintSubsumes(guard.Comparison, value, requirement);
    }

    private static bool NumericConstraintSubsumes(
        OperatorKind comparison,
        decimal value,
        NumericProofRequirement requirement)
    {
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

}
