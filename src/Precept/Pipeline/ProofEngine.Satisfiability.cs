using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;

// Proof-engine satisfiability scan.
//
// Detects guards and rules whose conjunction with the fields' declared bounds
// is empty (unsatisfiable / contradictory) — the dual of the per-obligation
// discharge that the rest of the proof engine performs. Lives as a lateral
// pass between `IncorporateForwardingFacts` and the discharge loop. See
// docs/compiler/proof-engine.md § Two-Pass Design (Pass 1.5).
//
// The existing strategy set in `ProofEngine.Strategies.cs` is obligation-
// discharge-shaped (each strategy takes a ProofObligation and answers "did
// this discharge?"). Satisfiability is whole-construct ("does ANY data
// configuration satisfy this guard?") with no obligation site — folding it
// into TryDischarge would deform the dispatch shape, so this scan emits
// diagnostics directly rather than flowing through the obligation channel.

public static partial class ProofEngine
{
    /// <summary>
    /// Scans the precept for provably-unsatisfiable guards and contradictory
    /// rule pairs, emitting `UnsatisfiableGuard` (PRE0082), `TautologicalGuard`
    /// (PRE0153), `VacuousRule` (PRE0154), and `ContradictoryRule` (PRE0155)
    /// directly into the diagnostic stream. Soundness-over-completeness
    /// (per `precept-language-spec.md § 0.6 Proof philosophy`): every emission
    /// corresponds to a verdict computed exactly under the existing
    /// `NumericInterval` algebra — no solver, no heuristic, no "unknown"
    /// verdict.
    /// </summary>
    private static void ScanSatisfiability(SemanticIndex semantics, List<Diagnostic> diagnostics, List<ProofForwardingFact> producedFacts)
    {
        ScanTransitionRowGuards(semantics, diagnostics, producedFacts);
        ScanRules(semantics, diagnostics);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TautologicalGuard / VacuousRule
    //
    //  Shared mechanic: a predicate is provably-true iff every leaf constraint
    //  is provably-true under the field's bounded interval. We sidestep
    //  expression negation (which would require synthesizing inverted
    //  TypedBinaryOp nodes) by checking each leaf directly against the field's
    //  interval: e.g. `field > V` is provably-true iff `min(field) > V`.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// True iff every leaf constraint of the predicate is provably-true under
    /// the field's declared bounds. Returns false for OR-disjunctive predicates
    /// (every branch must independently be tautological for the disjunction to
    /// be tautological — we conservatively decline on disjunctions).
    /// </summary>
    private static bool IsPredicateProvablyTrue(
        TypedExpression predicate,
        SemanticIndex semantics,
        Dictionary<string, NumericInterval>? extraNarrowing = null)
    {
        var branches = ExtractGuardBranches(predicate);
        if (branches.IsEmpty) return false;

        foreach (var branchConstraints in branches)
        {
            if (branchConstraints.IsEmpty) return false; // empty branch: nothing proven
            foreach (var gc in branchConstraints)
            {
                if (gc.IsPresenceCheck) return false; // out of scope for this check
                if (gc.Value is not { } value) return false;
                // An arg-sourced constraint must not be proven against a same-named field's
                // interval — the arg carries its own (here unbounded) bounds, so the
                // tautological scan cannot discharge it (soundness over completeness).
                if (gc.IsArg) return false;
                if (!IsConstraintProvablyTrue(gc.Field, gc.Comparison, value, semantics, extraNarrowing))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// True iff the constraint `field comparison value` holds for every value
    /// in the field's bounded interval (optionally further narrowed by
    /// <paramref name="extraNarrowing"/> — used for the rule's own `when`
    /// guard when checking VacuousRule).
    /// </summary>
    private static bool IsConstraintProvablyTrue(
        string fieldName,
        OperatorKind comparison,
        decimal value,
        SemanticIndex semantics,
        Dictionary<string, NumericInterval>? extraNarrowing)
    {
        var interval = ExtractFieldInterval(fieldName, semantics);
        if (extraNarrowing is not null && extraNarrowing.TryGetValue(fieldName, out var extra))
            interval = interval.IsUnbounded
                ? extra
                : interval.Intersect(extra);

        // An unbounded interval cannot guarantee any non-trivial constraint
        // (soundness over completeness — § 0.6 #1).
        if (interval.IsUnbounded) return false;
        if (interval.IsEmpty) return false;

        return comparison switch
        {
            OperatorKind.GreaterThan        => interval.Min >  value,
            OperatorKind.GreaterThanOrEqual => interval.Min >= value,
            OperatorKind.LessThan           => interval.Max <  value,
            OperatorKind.LessThanOrEqual    => interval.Max <= value,
            OperatorKind.Equals             => interval.Min == value && interval.Max == value,
            OperatorKind.NotEquals          => value < interval.Min || value > interval.Max,
            _ => false,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UnsatisfiableGuard (PRE0082)
    // ─────────────────────────────────────────────────────────────────────────

    private static void ScanTransitionRowGuards(SemanticIndex semantics, List<Diagnostic> diagnostics, List<ProofForwardingFact> producedFacts)
    {
        foreach (var row in semantics.TransitionRows)
        {
            if (row.Guard is null) continue;

            if (IsGuardUnsatisfiableUnderFieldBounds(row.Guard, semantics))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UnsatisfiableGuard,
                    row.Guard.Span,
                    FormatGuardText(row.Guard),
                    row.EventName,
                    string.Empty));

                // Produce a structured reachability fact for downstream
                // consumers (LS hover, MCP precept_proofs). Wildcard FromState
                // is represented as "*" for serialization stability.
                producedFacts.Add(new UnreachableRowFact(
                    FromState: row.FromState ?? "*",
                    EventName: row.EventName,
                    RowSpan: row.RowSpan));
                continue; // a guard can't be both unsatisfiable AND tautological
            }

            if (IsPredicateProvablyTrue(row.Guard, semantics))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.TautologicalGuard,
                    row.Guard.Span,
                    FormatGuardText(row.Guard)));
            }
        }
    }

    /// <summary>
    /// True iff every branch of the guard, narrowed against the field bounds,
    /// produces an empty interval on at least one field. Uses the existing
    /// `ExtractGuardBranches` infrastructure so AND/OR structure is honoured.
    /// </summary>
    private static bool IsGuardUnsatisfiableUnderFieldBounds(
        TypedExpression guard,
        SemanticIndex semantics)
    {
        var branches = ExtractGuardBranches(guard);
        if (branches.IsEmpty) return false;

        foreach (var branchConstraints in branches)
        {
            if (!BranchHasEmptyFieldInterval(branchConstraints, semantics))
                return false; // at least one branch is satisfiable
        }
        return true;
    }

    private static bool BranchHasEmptyFieldInterval(
        ImmutableArray<GuardConstraint> branchConstraints,
        SemanticIndex semantics)
    {
        var byField = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
        FoldConstraintsInto(byField, branchConstraints, field => ExtractFieldInterval(field, semantics), semantics);
        foreach (var (_, interval) in byField)
        {
            if (interval.IsEmpty) return true;
        }
        return false;
    }

    /// <summary>
    /// Apply a single constraint (comparison + literal value) to an interval,
    /// returning the tightened interval. Used by the satisfiability scan when
    /// composing guard-derived constraints with field-modifier bounds.
    ///
    /// Strict-inequality boundaries (`X &gt; V`, `X &lt; V`) dispatch on the
    /// field's domain: integer fields use the exact `V ± 1` half-step
    /// (no integer lies in `(V-1, V)`); decimal-backed fields (Decimal,
    /// Number, Money, Quantity, Price, etc.) close at `V` instead — a sound
    /// superset of truth that is over-wide by the single point `{V}`. Mirrors
    /// the same dispatch in <c>NegateConstraintToInterval</c>.
    ///
    /// The half-step is also sentinel-safe: when <paramref name="value"/>
    /// already sits at <see cref="decimal.MaxValue"/> / <see cref="decimal.MinValue"/>,
    /// `V + 1` / `V - 1` saturates at the sentinel rather than overflowing.
    /// </summary>
    private static NumericInterval NarrowByConstraint(
        NumericInterval current,
        OperatorKind comparison,
        decimal value,
        TypeKind fieldType)
    {
        if (current.IsUnbounded)
            current = new NumericInterval(decimal.MinValue, decimal.MaxValue);

        bool isIntegerDomain = fieldType == TypeKind.Integer;
        decimal strictUpper = isIntegerDomain && value < decimal.MaxValue ? value + 1m : value;
        decimal strictLower = isIntegerDomain && value > decimal.MinValue ? value - 1m : value;

        return comparison switch
        {
            OperatorKind.GreaterThan        => current.Intersect(new NumericInterval(strictUpper,    decimal.MaxValue)),
            OperatorKind.GreaterThanOrEqual => current.Intersect(new NumericInterval(value,          decimal.MaxValue)),
            OperatorKind.LessThan           => current.Intersect(new NumericInterval(decimal.MinValue, strictLower)),
            OperatorKind.LessThanOrEqual    => current.Intersect(new NumericInterval(decimal.MinValue, value)),
            OperatorKind.Equals             => current.Intersect(NumericInterval.Point(value)),
            OperatorKind.NotEquals          => current, // point-exclusion isn't representable in a closed interval — conservative
            _ => current,
        };
    }

    private static TypeKind GetFieldType(string fieldName, SemanticIndex semantics) =>
        semantics.FieldsByName.TryGetValue(fieldName, out var field)
            ? field.ResolvedType
            : TypeKind.Decimal;

    /// <summary>
    /// Fold a set of guard-leaf constraints into a per-field <see cref="NumericInterval"/>
    /// dictionary by intersecting each leaf via <see cref="NarrowByConstraint"/>. The
    /// <paramref name="seedFn"/> picks the starting interval for each field — different
    /// call sites use different seed policies (field-declared bounds for self-unsat
    /// detection vs. <c>[MinValue, MaxValue]</c> for pair-wise comparison). Skips
    /// presence-checks and non-numeric leaves. Mutates the supplied dictionary so
    /// callers can chain folds (e.g., guard then predicate) without re-seeding.
    /// </summary>
    private static void FoldConstraintsInto(
        Dictionary<string, NumericInterval> result,
        IEnumerable<GuardConstraint> constraints,
        Func<string, NumericInterval> seedFn,
        SemanticIndex semantics)
    {
        foreach (var gc in constraints)
        {
            if (gc.IsPresenceCheck) continue;
            if (gc.Value is not { } v) continue;

            if (!result.TryGetValue(gc.Field, out var current))
                // An arg-sourced constraint must not adopt a same-named field's declared
                // interval as its seed — seed it unbounded so only the constraint itself
                // narrows it.
                current = gc.IsArg ? new NumericInterval(decimal.MinValue, decimal.MaxValue) : seedFn(gc.Field);

            result[gc.Field] = NarrowByConstraint(current, gc.Comparison, v, GetFieldType(gc.Field, semantics));
        }
    }

    private static NumericInterval BoundedFieldInterval(string fieldName, SemanticIndex semantics)
    {
        var i = ExtractFieldInterval(fieldName, semantics);
        return i.IsUnbounded ? new NumericInterval(decimal.MinValue, decimal.MaxValue) : i;
    }

    private static NumericInterval MinMaxInterval(string _) =>
        new NumericInterval(decimal.MinValue, decimal.MaxValue);

    // ─────────────────────────────────────────────────────────────────────────
    //  ContradictoryRule (PRE0155)
    // ─────────────────────────────────────────────────────────────────────────

    private static void ScanRules(SemanticIndex semantics, List<Diagnostic> diagnostics)
    {
        // PRE0159 UnsatisfiableRule pre-pass: classify each rule against its
        // field-declared bounds (and the rule's own `when` guard, if any).
        // Self-unsatisfiable rules emit PRE0159 and are excluded from the
        // pair-wise sweep below, so they cannot mis-attribute ContradictoryRule
        // to an innocent partner rule.
        var selfUnsatisfiableRules = new HashSet<int>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var rule = semantics.Rules[i];
            var composed = ComposeRulePredicateWithFieldBounds(rule, semantics);
            foreach (var (field, interval) in composed)
            {
                if (!interval.IsEmpty) continue;
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UnsatisfiableRule,
                    rule.Condition.Span,
                    FormatGuardText(rule.Condition),
                    field));
                selfUnsatisfiableRules.Add(i);
                break; // one diagnostic per rule
            }
        }

        var ruleConstraints = new List<RuleConstraintSummary>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            if (selfUnsatisfiableRules.Contains(i)) continue;

            var rule = semantics.Rules[i];

            // VacuousRule: rule predicate is provably-true under the fields'
            // bounds (and the rule's own `when` guard, if any).
            Dictionary<string, NumericInterval>? extraNarrowing = null;
            if (rule.Guard is not null)
            {
                extraNarrowing = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
                FoldConstraintsInto(extraNarrowing, ExtractGuardConstraints(rule.Guard), MinMaxInterval, semantics);
            }

            if (IsPredicateProvablyTrue(rule.Condition, semantics, extraNarrowing))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.VacuousRule,
                    rule.Condition.Span,
                    FormatGuardText(rule.Condition)));
            }

            var perField = SummariseRuleConstraints(rule.Condition, semantics);
            ruleConstraints.Add(new RuleConstraintSummary(i, rule, perField));
        }

        for (int j = 1; j < ruleConstraints.Count; j++)
        {
            var current = ruleConstraints[j];
            for (int k = 0; k < j; k++)
            {
                var prior = ruleConstraints[k];

                // Guard mutual-exclusion pre-check: if both rules have `when`
                // guards whose numeric constraints carve out disjoint
                // configuration spaces on some shared field, the rules apply
                // to disjoint sets of configurations — no contradiction is
                // possible because no concrete configuration triggers both.
                // (String-equality and presence-check guards are not folded;
                // the GuardConstraint representation is numeric-only. Pairs
                // disambiguated only by string/presence guards remain a
                // documented over-approximation — known limitation.)
                if (AreGuardsMutuallyExclusive(current.Rule, prior.Rule, semantics))
                    continue;

                foreach (var (field, currentInterval) in current.PerField)
                {
                    if (!prior.PerField.TryGetValue(field, out var priorInterval)) continue;

                    var combined = currentInterval.Intersect(priorInterval);
                    if (!combined.IsEmpty) continue;

                    // Soundness over completeness: skip if either operand is
                    // Unbounded — verdict is "cannot decide," not "contradicts."
                    if (currentInterval.IsUnbounded || priorInterval.IsUnbounded) continue;

                    diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.ContradictoryRule,
                        current.Rule.Condition.Span,
                        FormatGuardText(current.Rule.Condition),
                        field));
                    goto NextRule; // one contradiction per rule is enough
                }
            }
            NextRule: ;
        }
    }

    /// <summary>
    /// Two rules' `when` guards are mutually exclusive iff both have non-null
    /// guards AND the conjunction of their guards is unsatisfiable on at least
    /// one shared field. Numeric constraints only — string-equality and
    /// presence checks are not folded (GuardConstraint.Value is decimal?).
    /// </summary>
    private static bool AreGuardsMutuallyExclusive(TypedRule current, TypedRule prior, SemanticIndex semantics)
    {
        if (current.Guard is null || prior.Guard is null) return false;

        var currentGuard = SummariseGuardConstraints(current.Guard, semantics);
        var priorGuard = SummariseGuardConstraints(prior.Guard, semantics);
        if (currentGuard.Count == 0 || priorGuard.Count == 0) return false;

        foreach (var (field, currentInterval) in currentGuard)
        {
            if (!priorGuard.TryGetValue(field, out var priorInterval)) continue;
            if (currentInterval.Intersect(priorInterval).IsEmpty)
                return true;
        }
        return false;
    }

    private static Dictionary<string, NumericInterval> SummariseGuardConstraints(
        TypedExpression guard,
        SemanticIndex semantics)
    {
        var result = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
        FoldConstraintsInto(result, ExtractGuardConstraints(guard), MinMaxInterval, semantics);
        return result;
    }

    /// <summary>
    /// Compose a rule's predicate with its field-declared bounds AND its own
    /// `when` guard (if any) to detect self-unsatisfiability. Differs from
    /// <see cref="SummariseRuleConstraints"/> which seeds from a fresh
    /// MinValue/MaxValue interval (correct for pair-wise comparison but blind to
    /// field bounds). Per-field intervals here are the intersection of:
    ///   * the field's declared [min, max] (via ExtractFieldInterval)
    ///   * the rule's `when` guard leaf constraints, if present
    ///   * the rule's predicate leaf constraints
    /// A field whose per-field interval is empty indicates the rule is impossible
    /// on its own (PRE0159 UnsatisfiableRule, distinct from PRE0155
    /// ContradictoryRule which requires a pair of rules).
    /// </summary>
    private static Dictionary<string, NumericInterval> ComposeRulePredicateWithFieldBounds(
        TypedRule rule,
        SemanticIndex semantics)
    {
        // Seed each newly-seen field from the field's declared interval — that's
        // the distinguishing seed for self-unsat detection (vs. pair-wise
        // comparison which seeds from MinValue/MaxValue, see BoundedFieldInterval
        // / MinMaxInterval helpers and FoldConstraintsInto).
        var result = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
        if (rule.Guard is not null)
            FoldConstraintsInto(result, ExtractGuardConstraints(rule.Guard), field => BoundedFieldInterval(field, semantics), semantics);
        FoldConstraintsInto(result, ExtractGuardConstraints(rule.Condition), field => BoundedFieldInterval(field, semantics), semantics);
        return result;
    }

    /// <summary>
    /// Maps a rule's predicate expression to a per-field interval the rule
    /// imposes. Seeds from a fresh MinValue/MaxValue interval — correct for
    /// pair-wise comparison (used by the PRE0155 ContradictoryRule sweep),
    /// but blind to field bounds. For self-unsatisfiability detection use
    /// <see cref="ComposeRulePredicateWithFieldBounds"/> instead.
    /// </summary>
    private static Dictionary<string, NumericInterval> SummariseRuleConstraints(
        TypedExpression predicate,
        SemanticIndex semantics)
    {
        var result = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
        FoldConstraintsInto(result, ExtractGuardConstraints(predicate), MinMaxInterval, semantics);
        return result;
    }

    private sealed record RuleConstraintSummary(
        int RuleIndex,
        TypedRule Rule,
        Dictionary<string, NumericInterval> PerField);

    private static string FormatGuardText(TypedExpression expr) => expr.ToString() ?? "<guard>";
}
