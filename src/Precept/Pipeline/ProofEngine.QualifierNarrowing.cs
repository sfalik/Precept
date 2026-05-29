using System;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // ── Qualifier guard-narrowing ─────────────────────────────────────────────
    //
    // A guard leaf of shape `X.<accessor> == 'value'`, where the accessor returns a
    // qualifier axis (ReturnsQualifier ≠ None), pins an OPEN field's qualifier-axis
    // value within the guarded branch. A downstream qualifier-compatibility obligation
    // about that open field then discharges against the narrowed value — soundly:
    //   - value-exact (the narrowed value must equal the constrained operand's value),
    //   - all-branches (every OR branch must narrow the field to the SAME value),
    //   - literal-RHS + Equals only (a `!=` or field-to-field guard narrows nothing),
    //   - reassignment-aware (a `set`/`clear` of the field earlier in the chain
    //     invalidates the fact — consults the shared ReassignedBefore set).
    //
    // The fact is a dedicated record (not an extension of the decimal-only GuardConstraint):
    // qualifier identities are discrete strings with no numeric subsumption algebra, and the
    // parallel AND/OR walk inherits OR-union / AND-cross-product composition the same way the
    // numeric branch extractor does.

    private sealed record QualifierNarrowingConstraint(string Field, QualifierAxis Axis, string Value);

    /// <summary>
    /// Discharges an <see cref="AssignmentQualifierProofRequirement"/> — the open-field assignment
    /// case relocated from the type-checker-immediate PRE0141 to the proof stage. The obligation's
    /// Site is the assigned source expression; it discharges iff the source's qualifier on the axis
    /// (declared, or guard-narrowed via <see cref="NarrowedValueFromGuard"/>) value-matches the
    /// target field's required qualifier. Sound: identity-axis value-equality, all-branches,
    /// reassignment-aware (the narrowing path consults <c>ReassignedBefore</c>).
    /// </summary>
    private static bool TryAssignmentQualifierProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is not AssignmentQualifierProofRequirement aqReq)
            return false;

        var targetValue = ExtractComparableValue(aqReq.TargetQualifier);
        if (targetValue is null)
            return false;

        var source = obligation.Site;
        var declared = ResolveQualifierFromExpression(source, aqReq.Axis, semantics);
        var sourceValue = declared is not null
            ? ExtractComparableValue(declared)
            : NarrowedValueFromGuard(source, aqReq.Axis, obligation, semantics);

        return sourceValue is not null && string.Equals(sourceValue, targetValue, StringComparison.Ordinal);
    }

    /// <summary>Author-facing axis label for diagnostics (matches the type checker's `FormatQualifierAxisName`).</summary>
    private static string QualifierAxisLabel(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency => "currency",
        QualifierAxis.Unit => "unit",
        QualifierAxis.Dimension => "dimension",
        QualifierAxis.FromCurrency => "from currency",
        QualifierAxis.ToCurrency => "to currency",
        _ => axis.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// Discharges a <see cref="QualifierCompatibilityProofRequirement"/> when a guard narrows an
    /// open operand's qualifier axis to a value matching the constrained operand. Composes after
    /// the declaration-based compatibility strategy: it only fires when at least one operand has
    /// no declared qualifier on the axis (i.e. is open) and the guard supplies its value.
    /// </summary>
    private static bool TryQualifierGuardNarrowingProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is not QualifierCompatibilityProofRequirement qcReq)
            return false;
        if (obligation.Site is not TypedBinaryOp binOp)
            return false;

        var axis = qcReq.Axis;
        var leftDeclared = ResolveQualifierFromExpression(binOp.Left, axis, semantics);
        var rightDeclared = ResolveQualifierFromExpression(binOp.Right, axis, semantics);

        // An operand resolves to its declared value, or — when open (no declared qualifier on the
        // axis) — to its guard-narrowed value. Discharge requires both sides to resolve AND that
        // narrowing actually contributed (otherwise the declaration-only case belongs to
        // TryQualifierCompatibilityProof, which runs first).
        var leftValue = leftDeclared is not null
            ? ExtractComparableValue(leftDeclared)
            : NarrowedValueFromGuard(binOp.Left, axis, obligation, semantics);
        var rightValue = rightDeclared is not null
            ? ExtractComparableValue(rightDeclared)
            : NarrowedValueFromGuard(binOp.Right, axis, obligation, semantics);

        var narrowingContributed = leftDeclared is null || rightDeclared is null;
        if (!narrowingContributed)
            return false;

        return leftValue is not null
            && rightValue is not null
            && string.Equals(leftValue, rightValue, StringComparison.Ordinal);
    }

    /// <summary>
    /// The value an open operand's qualifier axis is narrowed to by the enclosing guard, or
    /// <c>null</c> if the guard does not provably pin it. Returns a value only when EVERY
    /// disjunctive branch of the guard narrows the field to the SAME value (all-branches-OR),
    /// and the field was not reassigned earlier in the action chain (sequential proof flow).
    /// </summary>
    private static string? NarrowedValueFromGuard(
        TypedExpression operand, QualifierAxis axis, ProofObligation obligation, SemanticIndex semantics)
    {
        var field = GetFieldName(operand);
        if (field is null)
            return null;

        // Sequential proof flow (spec § 0.6 item 7): a guard fact about a field reassigned earlier
        // in the same chain is stale and must not discharge. Rides the shared ReassignedBefore set.
        if (obligation.ReassignedBefore.Contains(field))
            return null;

        var guard = GuardOfContext(obligation.Context, semantics);
        if (guard is null)
            return null;

        var branches = ExtractQualifierNarrowingBranches(guard);
        if (branches.Length == 0)
            return null;

        string? agreed = null;
        foreach (var branch in branches)
        {
            string? inBranch = null;
            foreach (var c in branch)
                if (c.Field == field && c.Axis == axis) { inBranch = c.Value; break; }

            // A branch that fails to narrow the field collapses the discharge (OR-arm collapse):
            // `X.currency == 'USD' or X.currency == 'EUR'` does not pin X to a single value.
            if (inBranch is null)
                return null;
            if (agreed is null)
                agreed = inBranch;
            else if (!string.Equals(agreed, inBranch, StringComparison.Ordinal))
                return null;
        }
        return agreed;
    }

    /// <summary>The guard expression governing an obligation's context (row / hook / handler / rule / ensure).</summary>
    private static TypedExpression? GuardOfContext(ObligationContext context, SemanticIndex semantics) => context switch
    {
        TransitionRowContext t => t.Row.Guard,
        StateHookContext s => s.Hook.Guard,
        EventHandlerContext h => h.Handler.Guard,
        ConstraintContext c => c.Constraint switch
        {
            RuleIdentity ri => semantics.Rules[ri.RuleIndex].Guard,
            EnsureIdentity ei => semantics.Ensures[ei.EnsureIndex].Guard,
            _ => null,
        },
        _ => null,
    };

    private static ImmutableArray<ImmutableArray<QualifierNarrowingConstraint>> ExtractQualifierNarrowingBranches(TypedExpression guard)
    {
        var branches = ExtractQualifierNarrowingBranchesCore(guard);
        return branches.IsEmpty
            ? ImmutableArray.Create(ImmutableArray<QualifierNarrowingConstraint>.Empty)
            : branches;
    }

    private static ImmutableArray<ImmutableArray<QualifierNarrowingConstraint>> ExtractQualifierNarrowingBranchesCore(TypedExpression expr)
    {
        if (expr is TypedBinaryOp bin)
        {
            var op = Operations.GetMeta(bin.ResolvedOp).Op;

            if (op == OperatorKind.Or)
            {
                var left = ExtractQualifierNarrowingBranchesCore(bin.Left);
                var right = ExtractQualifierNarrowingBranchesCore(bin.Right);
                return left.AddRange(right);
            }

            if (op == OperatorKind.And)
            {
                var left = ExtractQualifierNarrowingBranchesCore(bin.Left);
                var right = ExtractQualifierNarrowingBranchesCore(bin.Right);
                if (left.IsEmpty) return right;
                if (right.IsEmpty) return left;
                var cross = ImmutableArray.CreateBuilder<ImmutableArray<QualifierNarrowingConstraint>>(left.Length * right.Length);
                foreach (var lb in left)
                    foreach (var rb in right)
                        cross.Add(lb.AddRange(rb));
                return cross.ToImmutable();
            }
        }

        var leaf = ImmutableArray.CreateBuilder<QualifierNarrowingConstraint>();
        ExtractQualifierNarrowingLeaf(expr, leaf);
        return ImmutableArray.Create(leaf.ToImmutable());
    }

    /// <summary>
    /// Extracts a qualifier-narrowing fact from an atomic guard node: <c>X.acc == 'literal'</c>
    /// (either operand order) where <c>acc</c> returns a qualifier axis. Equals-only and
    /// literal-RHS-only — a negated or field-to-field comparison narrows nothing (it does not
    /// pin a single value), so no fact is emitted.
    /// </summary>
    private static void ExtractQualifierNarrowingLeaf(TypedExpression expr, ImmutableArray<QualifierNarrowingConstraint>.Builder builder)
    {
        if (expr is not TypedBinaryOp bin)
            return;
        if (Operations.GetMeta(bin.ResolvedOp).Op != OperatorKind.Equals)
            return;

        if (TryAddQualifierEqLeaf(bin.Left, bin.Right, builder))
            return;
        TryAddQualifierEqLeaf(bin.Right, bin.Left, builder);
    }

    private static bool TryAddQualifierEqLeaf(
        TypedExpression maybeAccessor, TypedExpression maybeLiteral, ImmutableArray<QualifierNarrowingConstraint>.Builder builder)
    {
        if (maybeAccessor is TypedMemberAccess
            {
                Object: TypedFieldRef field,
                ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: var axis }
            }
            && axis != QualifierAxis.None
            && maybeLiteral is TypedTypedConstant tc
            && NarrowedLiteralValue(tc) is { } value)
        {
            builder.Add(new QualifierNarrowingConstraint(field.FieldName, axis, value));
            return true;
        }
        return false;
    }

    /// <summary>
    /// The comparable value a guard-literal pins an axis to, in the SAME form
    /// <see cref="ExtractComparableValue"/> yields for the target's declared qualifier — so the
    /// discharge's value-equality is apples-to-apples. Currency/dimension constants carry a plain
    /// string <c>ParsedValue</c>; a unit constant carries a <see cref="UcumParsedUnit"/> whose
    /// <c>CanonicalCode</c> matches the declared unit's code; otherwise fall back to the raw text.
    /// </summary>
    private static string? NarrowedLiteralValue(TypedTypedConstant tc) => tc.ParsedValue switch
    {
        string s => s,
        UcumParsedUnit u => u.CanonicalCode,
        _ => string.IsNullOrEmpty(tc.RawText) ? null : tc.RawText,
    };
}
