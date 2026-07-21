using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  CorpusMeasurement — classifies every rule × write-site obligation in a
//  .precept corpus against the discharge derivations the WP calculator
//  licenses today.
//
//  Scope of "licensed": the single-field numeric family over primitive numeric
//  fields, single-write handler plans. Everything outside that scope — other
//  rule shapes, other write-site categories, multi-write plans, enumerator
//  skips, and normal-form rules that are ruled but not yet implemented — is
//  classified out-of-scope with a named reason. No obligation is ever dropped
//  silently, and no reason is ever a generic "not supported".
//
//  Classification is deliberately conservative: a derivation is applied only
//  where its stated decision procedure applies. When in doubt the harness
//  refuses to license — an over-count of the respell-needed band is safe;
//  a wrong "licensed" verdict is not.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Verdict for one rule × write-site obligation.</summary>
public enum CorpusClassification
{
    /// <summary>A licensed derivation closes the obligation as written.</summary>
    LicensedAsWritten,
    /// <summary>No licensed derivation closes it; the suggestion schema's respelling is recorded.</summary>
    RespellNeeded,
    /// <summary>The WP is ground-false — no premise respelling can license it.</summary>
    NoLicensedRespelling,
    /// <summary>Outside the measured scope, with a named reason.</summary>
    OutOfScope,
}

/// <summary>The licensed derivation names the harness can report.</summary>
public static class CorpusDerivations
{
    public const string EstablishmentLiteralFold = "literal fold for establishment";
    public const string PreservationLiteralFold = "literal fold (WP folds to true)";
    public const string WholeGuardMatch = "whole-guard normal-form match";
    public const string GuardConjunctCoverage = "guard-fact conjunct coverage";
    public const string ArgBoundIntervalSum = "arg-bound interval sum";
    public const string GuardBoundConjunctInterval = "guard bound-conjunct interval coverage";
    public const string HypothesisDecrease = "pre-state hypothesis with nonnegative decrease";
}

/// <summary>The named out-of-scope reasons. Every out-of-scope row carries exactly one.</summary>
public static class CorpusReasons
{
    public const string ConditionalRule =
        "activation-conditioned rule — vacuity discharge is a separate contract family, not the single-field numeric family measured here";
    public const string RelationalRule =
        "relational rule (mentions two or more fields) — guard-fact substitution is a separate contract family, not measured here";
    public const string QuantifiedRule =
        "quantified rule — outside the single-field numeric family; its proof-surface status is an open item";
    public const string ComputedMention =
        "rule mentions a computed field — computed-field write transitivity is an open minting item";
    public const string NonPrimitiveNumeric =
        "governed field is not primitive numeric — outside the family's primitive-numeric coordinate";
    public const string NumberLane =
        "value sits on the `number` (IEEE double) lane — the interval and fold derivations are stated for the exact "
        + "lanes (integer, decimal) only; extending them to `number` needs an outward-rounding side condition that "
        + "is not written, so no stated derivation applies here";
    public const string LowerBoundIntervalUnstated =
        "closes only through a lower-bound interval mirror — the written class-(b) procedure is an exact-decimal "
        + "upper-bound sum over declared max bounds, and no lower-bound rule is stated, so the closure rests on a "
        + "derivation the definition does not license";
    public const string RuleShapeOutsideFamily =
        "rule shape outside the single-field numeric comparison scope";
    public const string MultiWritePlan =
        "multi-write plan — pending the queued write-plan decomposition ruling";
    public const string NonSetAction =
        "non-set action on a mentioned field — outside the scalar handler-set scope";
    public const string StateHookSite =
        "state entry/exit hook write site — outside the handler-set write-site coordinate";
    public const string EditableSite =
        "editable-field edit site — ingress-evaluation discharge (premise class (e)) is an open owner item";
    public const string EditDeclarationSite =
        "stateless edit declaration — ingress-evaluation discharge is an open owner item";
    public const string EstablishmentNotGround =
        "establishment WP is not ground — establishment discharge is stated only as the literal fold today";
    public const string LiteralDivisionUnfolded =
        "literal division present but unfolded — division folding is owner-ruled but not yet implemented in the calculator";
    public const string NegatedOrderingUnflipped =
        "negated ordering comparison present — the ruled comparison flip for exact families is not yet implemented in the calculator";
    public const string EnumerationSkip =
        "obligation enumerator skip — the named per-record reason applies";
    public const string EnsureNotEnumerated =
        "state/event-scoped ensure — not enumerated by the obligation enumerator today";
    public const string CanonicalSurface =
        "expression outside the calculator's canonical surface";
    public const string ErrorResidue =
        "expression carries a compile-error residue — not classified";
}

/// <summary>One classified rule × write-site obligation.</summary>
public sealed record CorpusObligationRow(
    string Obligation,
    string Rule,
    string Site,
    CorpusClassification Classification,
    string? Derivation,
    string? Reason,
    ImmutableArray<string> Respellings,
    string Detail,
    string? Wp);

/// <summary>Measurement of one .precept file.</summary>
public sealed record CorpusFileReport(
    string File,
    int ErrorDiagnostics,
    bool RuleBearing,
    int ExplicitRules,
    int Ensures,
    int ModifierObligations,
    int FramePreservedPairs,
    ImmutableArray<CorpusObligationRow> Rows);

/// <summary>Aggregated corpus measurement.</summary>
public sealed record CorpusReport(
    string CorpusRoot,
    string GeneratedAtUtc,
    int FileCount,
    int RuleBearingFileCount,
    int FilesWithExplicitRules,
    int FilesWithErrorDiagnostics,
    int FramePreservedPairs,
    SortedDictionary<string, int> ClassificationTotals,
    SortedDictionary<string, int> DerivationTotals,
    SortedDictionary<string, int> OutOfScopeReasonTotals,
    ImmutableArray<CorpusFileReport> Files);

public static class CorpusMeasurement
{
    // ── Entry points ─────────────────────────────────────────────────────────

    public static CorpusReport MeasureDirectory(string root)
    {
        var files = Directory.EnumerateFiles(root, "*.precept", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => MeasureFile(Path.GetFileName(p), File.ReadAllText(p)))
            .ToImmutableArray();
        return BuildReport(root, files);
    }

    public static CorpusReport BuildReport(string root, ImmutableArray<CorpusFileReport> files)
    {
        var classifications = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var derivations = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var reasons = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in files.SelectMany(f => f.Rows))
        {
            Bump(classifications, DisplayName(row.Classification));
            if (row.Derivation is not null)
                Bump(derivations, row.Derivation);
            if (row.Reason is not null)
                Bump(reasons, row.Reason);
        }

        return new CorpusReport(
            CorpusRoot: root,
            GeneratedAtUtc: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            FileCount: files.Length,
            RuleBearingFileCount: files.Count(f => f.RuleBearing),
            FilesWithExplicitRules: files.Count(f => f.ExplicitRules > 0),
            FilesWithErrorDiagnostics: files.Count(f => f.ErrorDiagnostics > 0),
            FramePreservedPairs: files.Sum(f => f.FramePreservedPairs),
            ClassificationTotals: classifications,
            DerivationTotals: derivations,
            OutOfScopeReasonTotals: reasons,
            Files: files);
    }

    public static CorpusFileReport MeasureFile(string file, string source)
    {
        var compilation = Precept.Compiler.Compile(source);
        var semantics = compilation.Semantics;
        int errors = compilation.Diagnostics.Count(d => d.Severity == Severity.Error);

        var rows = ImmutableArray.CreateBuilder<CorpusObligationRow>();
        int framePairs = 0;

        var entries = ObligationEnumerator.Enumerate(semantics);

        foreach (var skipped in entries.OfType<ObligationSkipped>())
            rows.Add(OutOfScope(skipped.Label, "", "(enumeration)", CorpusReasons.EnumerationSkip, skipped.Reason));

        for (int i = 0; i < semantics.Ensures.Length; i++)
        {
            var anchor = semantics.Ensures[i].AnchorState ?? semantics.Ensures[i].AnchorEvent ?? "(unanchored)";
            rows.Add(OutOfScope($"ensure[{i}]", "", "(enumeration)", CorpusReasons.EnsureNotEnumerated,
                $"anchored to {anchor}"));
        }

        var constructionRows = semantics.EventHandlers.OfType<TypedEventRowSuccess>()
            .Where(r => r.IsConstruction).ToArray();
        List<(string Site, IReadOnlyList<TypedAction> Actions, TypedExpression? Guard)> establishmentSites =
            constructionRows.Length == 0
            ? [("establishment: defaults", ImmutableArray<TypedAction>.Empty, null)]
            : DisambiguateSites(constructionRows
                .Select(r => ($"establishment: on {r.EventName}", (IReadOnlyList<TypedAction>)r.Actions, (TypedExpression?)r.Guard))
                .ToList());
        var preservationSites = PreservationSites(semantics);
        var computedInputs = semantics.ComputedDeps.ToDictionary(
            d => d.FieldName, d => d.DependsOn, StringComparer.Ordinal);

        foreach (var spec in entries.OfType<ObligationSpec>())
        {
            CanonExpr condition;
            CanonExpr? activation;
            try
            {
                condition = WpCalculator.Canonicalize(spec.Condition);
                activation = spec.ActivationCondition is null ? null : WpCalculator.Canonicalize(spec.ActivationCondition);
            }
            catch (NotSupportedException ex)
            {
                rows.Add(OutOfScope(spec.Label, "", "(all sites)", CorpusReasons.CanonicalSurface, ex.Message));
                continue;
            }

            var ruleDisplay = condition.ToString();
            var mention = new HashSet<string>(StringComparer.Ordinal);
            mention.UnionWith(FieldNames(condition));
            if (activation is not null)
                mention.UnionWith(FieldNames(activation));

            string? scope = ScopeReason(condition, activation, mention, semantics);

            // Establishment sites: through each construction row, else over defaults.
            foreach (var (site, actions, _) in establishmentSites)
                rows.Add(ClassifyEstablishment(spec, scope, ruleDisplay, site, actions, semantics));

            // Preservation sites: handler rows whose plan writes a mentioned field.
            foreach (var (site, actions, guard) in preservationSites)
            {
                if (actions.Count == 0)
                    continue; // no writes: not a write site for any rule

                var written = actions.Select(a => a.FieldName).ToHashSet(StringComparer.Ordinal);
                if (!written.Overlaps(mention))
                {
                    var inputHit = mention
                        .Where(m => computedInputs.TryGetValue(m, out var inputs) && inputs.Any(written.Contains))
                        .ToArray();
                    if (inputHit.Length > 0)
                        rows.Add(OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.ComputedMention,
                            $"plan writes an input of computed field {inputHit[0]}"));
                    else
                        framePairs++;
                    continue;
                }

                rows.Add(ClassifyPreservation(spec, scope, ruleDisplay, mention, site, actions, guard, semantics));
            }

            // Other write-site categories: named out-of-scope (or the rule's own
            // scope reason, which is more specific about why nothing applies).
            foreach (var hook in semantics.StateHooks)
            {
                if (hook.Actions.Any(a => mention.Contains(a.FieldName)))
                    rows.Add(OutOfScope(spec.Label, ruleDisplay,
                        $"hook: {ScopeWord(hook.Scope)} {hook.StateName}",
                        scope ?? CorpusReasons.StateHookSite, ""));
            }

            foreach (var mode in semantics.AccessModes)
            {
                if (mode.Mode == ModifierKind.Write && mention.Contains(mode.FieldName))
                    rows.Add(OutOfScope(spec.Label, ruleDisplay,
                        $"editable: {mode.FieldName} in {mode.StateName}",
                        scope ?? CorpusReasons.EditableSite, ""));
            }

            foreach (var edit in semantics.EditDeclarations)
            {
                if (edit.IsEditAll || edit.EditableFields.Any(mention.Contains))
                    rows.Add(OutOfScope(spec.Label, ruleDisplay, "edit declaration",
                        scope ?? CorpusReasons.EditDeclarationSite, ""));
            }
        }

        int explicitRules = semantics.Rules.Length;
        int modifierObligations = entries.Length - explicitRules;
        return new CorpusFileReport(
            File: file,
            ErrorDiagnostics: errors,
            RuleBearing: entries.Length > 0 || semantics.Ensures.Length > 0,
            ExplicitRules: explicitRules,
            Ensures: semantics.Ensures.Length,
            ModifierObligations: modifierObligations,
            FramePreservedPairs: framePairs,
            Rows: rows.ToImmutable());
    }

    // ── Scope gate: which obligations the measured family covers ─────────────

    private static string? ScopeReason(
        CanonExpr condition, CanonExpr? activation, HashSet<string> mention, SemanticIndex semantics)
    {
        if (Nodes(condition).Any(n => n is CanonError)
            || (activation is not null && Nodes(activation).Any(n => n is CanonError)))
            return CorpusReasons.ErrorResidue;

        if (activation is not null)
            return CorpusReasons.ConditionalRule;

        if (Nodes(condition).Any(n => n is CanonQuantifier))
            return CorpusReasons.QuantifiedRule;

        if (mention.Count >= 2)
            return CorpusReasons.RelationalRule;

        if (mention.Count == 1)
        {
            if (!semantics.FieldsByName.TryGetValue(mention.First(), out var field))
                return CorpusReasons.CanonicalSurface;
            if (field.IsComputed)
                return CorpusReasons.ComputedMention;
            // The `number` lane is excluded ahead of the family's numeric coordinate:
            // the stated interval and fold derivations argue truth-preservation from
            // exact arithmetic, which IEEE doubles do not provide.
            if (field.ResolvedType is TypeKind.Number)
                return CorpusReasons.NumberLane;
            if (field.ResolvedType is not (TypeKind.Integer or TypeKind.Decimal))
                return CorpusReasons.NonPrimitiveNumeric;
        }

        foreach (var node in Nodes(condition))
        {
            bool outside = node switch
            {
                CanonIsSet or CanonMember or CanonConditional or CanonList or CanonInterp => true,
                CanonString or CanonTypedConst or CanonUnset => true,
                CanonBinary { Op: CanonBinaryOp.Contains or CanonBinaryOp.LookupAccess } => true,
                CanonCall call => !call.Function.StartsWith("op:", StringComparison.Ordinal),
                _ => false,
            };
            if (outside)
                return CorpusReasons.RuleShapeOutsideFamily;
        }

        return null;
    }

    // ── Establishment classification ─────────────────────────────────────────

    private static CorpusObligationRow ClassifyEstablishment(
        ObligationSpec spec, string? scope, string ruleDisplay, string site,
        IReadOnlyList<TypedAction> plan, SemanticIndex semantics)
    {
        if (scope is not null)
            return OutOfScope(spec.Label, ruleDisplay, site, scope, "");

        if (PlanGate(plan) is { } planReason)
            return OutOfScope(spec.Label, ruleDisplay, site, planReason.Reason, planReason.Detail);

        WpResult result;
        try
        {
            result = WpCalculator.ComputeEstablishmentWp(semantics, plan, spec);
        }
        catch (NotSupportedException ex)
        {
            // A canonical-surface refusal is a named record, never an aborted run.
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.CanonicalSurface, ex.Message);
        }

        if (result is WpNotSupported notSupported)
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.CanonicalSurface, notSupported.Reason);

        var wp = ((WpComputed)result).Wp;
        if (Nodes(wp).Any(n => n is CanonError))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.ErrorResidue, "", wp.ToString());

        if (MentionsNumberLaneArg(wp, semantics))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.NumberLane, "", wp.ToString());

        // The stated establishment discharge is the literal fold over the
        // default (or initial-write) configuration — nothing else.
        if (wp is CanonBool { Value: true })
            return Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.EstablishmentLiteralFold,
                "rule folds to true over the establishment configuration", wp.ToString());
        if (wp is CanonBool { Value: false })
            return new CorpusObligationRow(spec.Label, ruleDisplay, site,
                CorpusClassification.NoLicensedRespelling, Derivation: null, Reason: null,
                Respellings: [],
                Detail: "establishment WP folds to false — the establishment configuration violates the rule; "
                    + "the fix is a data change, not a premise",
                Wp: wp.ToString());

        if (Nodes(wp).Any(IsUnfoldedLiteralDivision))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.LiteralDivisionUnfolded, "", wp.ToString());

        return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.EstablishmentNotGround, "", wp.ToString());
    }

    // ── Preservation classification ──────────────────────────────────────────

    private static CorpusObligationRow ClassifyPreservation(
        ObligationSpec spec, string? scope, string ruleDisplay, HashSet<string> mention,
        string site, IReadOnlyList<TypedAction> plan, TypedExpression? guard, SemanticIndex semantics)
    {
        if (scope is not null)
            return OutOfScope(spec.Label, ruleDisplay, site, scope, "");

        if (PlanGate(plan) is { } planReason)
            return OutOfScope(spec.Label, ruleDisplay, site, planReason.Reason, planReason.Detail);

        var write = (TypedInputAction)plan[0];
        WpResult result;
        try
        {
            result = WpCalculator.ComputePreservationWp(plan, spec);
        }
        catch (NotSupportedException ex)
        {
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.CanonicalSurface, ex.Message);
        }

        if (result is WpNotSupported notSupported)
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.CanonicalSurface, notSupported.Reason);

        var wp = ((WpComputed)result).Wp;
        CanonExpr? canonGuard;
        try
        {
            canonGuard = guard is null ? null : WpCalculator.Canonicalize(guard);
        }
        catch (NotSupportedException ex)
        {
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.CanonicalSurface, ex.Message);
        }

        if (Nodes(wp).Any(n => n is CanonError)
            || (canonGuard is not null && Nodes(canonGuard).Any(n => n is CanonError)))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.ErrorResidue, "", wp.ToString());

        if (MentionsNumberLaneArg(wp, semantics)
            || (canonGuard is not null && MentionsNumberLaneArg(canonGuard, semantics)))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.NumberLane, "", wp.ToString());

        if (wp is CanonBool { Value: true })
            return Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.PreservationLiteralFold,
                "the substituted rule folds to true", wp.ToString());
        if (wp is CanonBool { Value: false })
            return new CorpusObligationRow(spec.Label, ruleDisplay, site,
                CorpusClassification.NoLicensedRespelling, Derivation: null, Reason: null,
                Respellings: [],
                Detail: "preservation WP folds to false — the write always violates the rule; "
                    + "no premise respelling licenses it",
                Wp: wp.ToString());

        if (guard is not null)
        {
            if (WpCalculator.GuardMatchesWp(guard, wp))
                return Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.WholeGuardMatch,
                    "the guard, as a whole condition, is normal-form-equal to the WP", wp.ToString());
            if (WpCalculator.GuardFactsCoverWp(guard, wp))
                return Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.GuardConjunctCoverage,
                    "every WP conjunct appears among the guard's facts", wp.ToString());
        }

        // Interval closure that rests on a lower-bound mirror is not licensed by the
        // written contract (upper-bound sum only). It is remembered rather than
        // returned here, because another stated derivation may still close the row.
        bool unstatedLowerBound = false;
        if (TryIntervalClosure(wp, guard, canonGuard, semantics, out bool usedGuardBounds, out bool usedLowerBound))
        {
            if (!usedLowerBound)
                return usedGuardBounds
                    ? Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.GuardBoundConjunctInterval,
                        "per-term bound conjuncts in the guard close the WP by exact interval summation", wp.ToString())
                    : Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.ArgBoundIntervalSum,
                        "the args' declared bounds close the WP by exact interval summation", wp.ToString());

            unstatedLowerBound = true;
        }

        CanonExpr canonRule;
        try
        {
            canonRule = WpCalculator.Canonicalize(spec.Condition);
        }
        catch (NotSupportedException ex)
        {
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.CanonicalSurface, ex.Message);
        }

        if (TryHypothesisDecrease(canonRule, write.FieldName, wp, semantics))
            return Licensed(spec.Label, ruleDisplay, site, CorpusDerivations.HypothesisDecrease,
                "written-field decrease by a declared-nonnegative term; the pre-state hypothesis carries the bound",
                wp.ToString());

        // Ruled-but-unimplemented normal-form rules: a licensing verdict here
        // could flip once they land, so the row is a named skip, not a guess.
        if (Nodes(wp).Any(IsUnfoldedLiteralDivision)
            || (canonGuard is not null && Nodes(canonGuard).Any(IsUnfoldedLiteralDivision)))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.LiteralDivisionUnfolded, "", wp.ToString());
        if (Nodes(wp).Any(IsNegatedOrderingComparison)
            || (canonGuard is not null && Nodes(canonGuard).Any(IsNegatedOrderingComparison)))
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.NegatedOrderingUnflipped, "", wp.ToString());

        if (unstatedLowerBound)
            return OutOfScope(spec.Label, ruleDisplay, site, CorpusReasons.LowerBoundIntervalUnstated,
                "the interval sum closes, but only by taking a lower bound of a term", wp.ToString());

        var respellings = ImmutableArray.CreateBuilder<string>();
        respellings.Add($"add a guard normal-form-equal to: {wp}");
        var argNames = Nodes(wp).OfType<CanonArg>().Select(a => $"{a.EventName}.{a.ArgName}").Distinct().ToArray();
        if (argNames.Length > 0)
            respellings.Add(
                $"bound the args ({string.Join(", ", argNames)}) so the declared-bound interval combination satisfies: {wp}");

        return new CorpusObligationRow(spec.Label, ruleDisplay, site,
            CorpusClassification.RespellNeeded, Derivation: null, Reason: null,
            Respellings: respellings.ToImmutable(),
            Detail: "no licensed derivation closes the WP as written",
            Wp: wp.ToString());
    }

    // ── Plan gate: single scalar set writes only ─────────────────────────────

    private static (string Reason, string Detail)? PlanGate(IReadOnlyList<TypedAction> plan)
    {
        if (plan.Count > 1)
            return (CorpusReasons.MultiWritePlan, $"{plan.Count} writes in the plan");
        if (plan.Count == 1 && plan[0] is not TypedInputAction { Kind: ActionKind.Set })
            return (CorpusReasons.NonSetAction, $"{plan[0].Kind} on {plan[0].FieldName}");
        return null;
    }

    // ── Interval closure over per-term bounds ────────────────────────────────
    //
    // Each WP conjunct must close either by membership among the guard's facts
    // or by summing per-term bounds: declared arg bounds (min/max and sign
    // modifiers) and per-term bound conjuncts extracted from the guard. Sums
    // run in exact decimal arithmetic; a term with no bound fails the closure.

    private readonly record struct Bound(decimal Value, bool Inclusive, bool FromGuard, bool FromLowerBound);

    private static bool TryIntervalClosure(
        CanonExpr wp, TypedExpression? guard, CanonExpr? canonGuard,
        SemanticIndex semantics, out bool usedGuardBounds, out bool usedLowerBound)
    {
        usedGuardBounds = false;
        usedLowerBound = false;
        bool lowerBoundUsed = false;
        var facts = guard is null
            ? ImmutableArray<GuardBoundFact>.Empty
            : WpCalculator.ExtractBoundFacts(guard);
        var guardKeys = canonGuard is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : WpCalculator.ConjunctsOf(canonGuard).Select(c => c.Key).ToHashSet(StringComparer.Ordinal);

        bool usedGuard = false;
        bool usedInterval = false;
        foreach (var conjunct in WpCalculator.ConjunctsOf(wp))
        {
            if (guardKeys.Contains(conjunct.Key))
            {
                usedGuard = true;
                continue;
            }

            if (conjunct is not CanonCompare { Op: CanonCompareOp.Le or CanonCompareOp.Lt } compare)
                return false;

            bool strict = compare.Op == CanonCompareOp.Lt;
            bool closed;
            if (compare.Right is CanonNumber upperLimit)
                closed = TrySumBound(compare.Left, upper: true, facts, semantics, out var sum,
                        ref usedGuard, ref lowerBoundUsed)
                    && (strict
                        ? sum.Value < upperLimit.Value || (sum.Value == upperLimit.Value && !sum.Inclusive)
                        : sum.Value <= upperLimit.Value);
            else if (compare.Left is CanonNumber lowerLimit)
            {
                // The mirrored direction: the written class-(b) procedure sums declared
                // max bounds upward. Closing a `constant <= term` conjunct needs a
                // lower-bound sum, which no stated rule provides.
                lowerBoundUsed = true;
                closed = TrySumBound(compare.Right, upper: false, facts, semantics, out var sum,
                        ref usedGuard, ref lowerBoundUsed)
                    && (strict
                        ? sum.Value > lowerLimit.Value || (sum.Value == lowerLimit.Value && !sum.Inclusive)
                        : sum.Value >= lowerLimit.Value);
            }
            else
                return false;

            if (!closed)
                return false;
            usedInterval = true;
        }

        usedGuardBounds = usedGuard;
        usedLowerBound = lowerBoundUsed;
        return usedInterval; // pure membership closure is guard-fact coverage, reported upstream
    }

    private static bool TrySumBound(
        CanonExpr expression, bool upper, ImmutableArray<GuardBoundFact> facts,
        SemanticIndex semantics, out Bound sum, ref bool usedGuard, ref bool usedLowerBound)
    {
        ImmutableArray<CanonExpr> terms = expression is CanonNary { Op: CanonNaryOp.Add } add
            ? add.Operands
            : [expression];

        decimal total = 0m;
        bool inclusive = true;
        foreach (var term in terms)
        {
            if (TermBound(term, upper, facts, semantics) is not { } bound)
            {
                sum = default;
                return false;
            }
            total += bound.Value;
            inclusive &= bound.Inclusive;
            usedGuard |= bound.FromGuard;
            usedLowerBound |= bound.FromLowerBound;
        }

        sum = new Bound(total, inclusive, FromGuard: false, FromLowerBound: false);
        return true;
    }

    private static Bound? TermBound(
        CanonExpr term, bool upper, ImmutableArray<GuardBoundFact> facts, SemanticIndex semantics)
    {
        switch (term)
        {
            case CanonNumber n:
                // A literal is its own exact value in either direction — not a bound
                // read off a declaration, so it carries no lower-bound dependency.
                return new Bound(n.Value, Inclusive: true, FromGuard: false, FromLowerBound: false);

            case CanonNeg neg:
                // upper(-t) = -lower(t) and lower(-t) = -upper(t).
                return TermBound(neg.Operand, !upper, facts, semantics) is { } inner
                    ? new Bound(-inner.Value, inner.Inclusive, inner.FromGuard, inner.FromLowerBound)
                    : null;
        }

        Bound? best = null;
        void Consider(Bound candidate)
        {
            if (best is not { } current
                || (upper ? candidate.Value < current.Value : candidate.Value > current.Value)
                || (candidate.Value == current.Value && !candidate.Inclusive))
            {
                best = candidate;
            }
        }

        foreach (var fact in facts)
        {
            if (fact.Term.Key != term.Key)
                continue;
            switch (fact.Kind)
            {
                case BoundKind.UpperInclusive when upper:
                    Consider(new Bound(fact.Bound, Inclusive: true, FromGuard: true, FromLowerBound: false));
                    break;
                case BoundKind.UpperExclusive when upper:
                    Consider(new Bound(fact.Bound, Inclusive: false, FromGuard: true, FromLowerBound: false));
                    break;
                case BoundKind.LowerInclusive when !upper:
                    Consider(new Bound(fact.Bound, Inclusive: true, FromGuard: true, FromLowerBound: true));
                    break;
                case BoundKind.LowerExclusive when !upper:
                    Consider(new Bound(fact.Bound, Inclusive: false, FromGuard: true, FromLowerBound: true));
                    break;
                case BoundKind.Equal:
                    Consider(new Bound(fact.Bound, Inclusive: true, FromGuard: true, FromLowerBound: !upper));
                    break;
            }
        }

        if (term is CanonArg argRef
            && semantics.EventsByName.TryGetValue(argRef.EventName, out var declaringEvent)
            && declaringEvent.Args.FirstOrDefault(a => a.Name == argRef.ArgName) is { } arg
            // Exact lanes only: the interval rule's validity argument rests on exact
            // arithmetic, which the `number` lane does not provide.
            && arg.ResolvedType is TypeKind.Integer or TypeKind.Decimal)
        {
            foreach (var declared in CatalogSelfBounds(arg.Modifiers, arg.DeclaredMin, arg.DeclaredMax))
                if (declared.Upper == upper)
                    Consider(new Bound(declared.Value, declared.Inclusive, FromGuard: false, FromLowerBound: !upper));
        }

        return best;
    }

    /// <summary>
    /// The bounds a declaration's own modifiers assert about its value, derived from
    /// the Modifiers catalog's <c>ProofSatisfaction</c> metadata (projection,
    /// comparison, bound source) exactly as the obligation enumerator derives
    /// desugared rules. The harness keeps no private table of modifier semantics, so
    /// a catalog change to a sign or bound modifier reaches the measurement.
    /// </summary>
    private static IEnumerable<(bool Upper, decimal Value, bool Inclusive)> CatalogSelfBounds(
        ImmutableArray<ModifierKind> modifiers, decimal? declaredMin, decimal? declaredMax)
    {
        foreach (var modifierKind in modifiers)
        {
            if (Modifiers.GetMeta(modifierKind) is not ValueModifierMeta meta)
                continue;

            foreach (var satisfaction in meta.ProofSatisfactions)
            {
                if (satisfaction is not ProofSatisfaction.Numeric numeric
                    || numeric.Projection is not SatisfactionProjection.SelfValue)
                    continue;

                decimal? bound = numeric.Bound switch
                {
                    NumericBoundSource.Constant constant => constant.Value,
                    NumericBoundSource.DeclarationValue => modifierKind switch
                    {
                        ModifierKind.Min => declaredMin,
                        ModifierKind.Max => declaredMax,
                        _ => null,
                    },
                    _ => null,
                };
                if (bound is null)
                    continue;

                // The comparison decides the interval direction; comparisons that
                // assert no interval bound (≠, =) contribute nothing.
                switch (numeric.Comparison)
                {
                    case OperatorKind.GreaterThanOrEqual: yield return (false, bound.Value, true); break;
                    case OperatorKind.GreaterThan: yield return (false, bound.Value, false); break;
                    case OperatorKind.LessThanOrEqual: yield return (true, bound.Value, true); break;
                    case OperatorKind.LessThan: yield return (true, bound.Value, false); break;
                }
            }
        }
    }

    // ── The written-field-decrease derivation ────────────────────────────────
    //
    // Rule of the shape `F <= K`; plan `set F = F - t` with a declared
    // nonnegativity fact on t: the pre-state hypothesis carries the bound
    // (F_pre - t <= F_pre <= K).

    private static bool TryHypothesisDecrease(
        CanonExpr canonRule, string writtenField, CanonExpr wp, SemanticIndex semantics)
    {
        if (canonRule is not CanonCompare { Op: CanonCompareOp.Le or CanonCompareOp.Lt } rule
            || rule.Left is not CanonFieldPre ruleField
            || ruleField.Name != writtenField
            || rule.Right is not CanonNumber ruleBound)
            return false;

        if (wp is not CanonCompare wpCompare
            || wpCompare.Op != rule.Op
            || wpCompare.Right is not CanonNumber wpBound
            || wpBound.Value != ruleBound.Value
            || wpCompare.Left is not CanonNary { Op: CanonNaryOp.Add } sum
            || sum.Operands.Length != 2)
            return false;

        int fieldIndex = -1;
        for (int i = 0; i < sum.Operands.Length; i++)
        {
            if (sum.Operands[i] is CanonFieldPre pre && pre.Name == writtenField)
            {
                fieldIndex = i;
                break;
            }
        }
        if (fieldIndex < 0)
            return false;
        if (sum.Operands[1 - fieldIndex] is not CanonNeg negated)
            return false;

        return HasDeclaredNonnegativity(negated.Operand, semantics);
    }

    /// <summary>
    /// The declared nonnegativity fact the matrix's Base B decision procedure looks
    /// up ("a nonnegativity fact for the subtracted term in its declared modifiers").
    /// Derived from the catalog's satisfaction metadata, never a private table.
    /// </summary>
    private static bool HasDeclaredNonnegativity(CanonExpr term, SemanticIndex semantics)
    {
        var bounds = term switch
        {
            CanonArg argRef when semantics.EventsByName.TryGetValue(argRef.EventName, out var declaringEvent)
                && declaringEvent.Args.FirstOrDefault(a => a.Name == argRef.ArgName) is { } arg =>
                CatalogSelfBounds(arg.Modifiers, arg.DeclaredMin, arg.DeclaredMax),
            CanonFieldPre fieldRef when semantics.FieldsByName.TryGetValue(fieldRef.Name, out var field) =>
                CatalogSelfBounds(field.Modifiers, field.DeclaredMin, field.DeclaredMax),
            _ => null,
        };

        return bounds is not null && bounds.Any(b => !b.Upper && b.Value >= 0m);
    }

    /// <summary>True when the expression reads an event arg declared on the `number` lane.</summary>
    private static bool MentionsNumberLaneArg(CanonExpr expression, SemanticIndex semantics) =>
        Nodes(expression).OfType<CanonArg>().Any(a =>
            semantics.EventsByName.TryGetValue(a.EventName, out var declaringEvent)
            && declaringEvent.Args.FirstOrDefault(x => x.Name == a.ArgName)
                is { ResolvedType: TypeKind.Number });

    // ── Named-skip predicates for ruled-but-unimplemented normal-form rules ──
    //
    // Both predicates read the shape only, not the value lane. That is exact here
    // because the scope gate excludes the `number` lane before classification: an
    // in-scope obligation is always on an exact lane, where division folding and
    // the negated-ordering flip are ruled-but-unimplemented rather than ruled out.

    private static bool IsUnfoldedLiteralDivision(CanonExpr node) =>
        node is CanonBinary { Op: CanonBinaryOp.Divide, Left: CanonNumber, Right: CanonNumber divisor }
        && divisor.Value != 0m;

    private static bool IsNegatedOrderingComparison(CanonExpr node) =>
        node is CanonNot { Operand: CanonCompare { Op: CanonCompareOp.Lt or CanonCompareOp.Le } };

    // ── Site enumeration and canonical-tree walking ──────────────────────────

    private static List<(string Site, IReadOnlyList<TypedAction> Actions, TypedExpression? Guard)>
        PreservationSites(SemanticIndex semantics)
    {
        var sites = new List<(string, IReadOnlyList<TypedAction>, TypedExpression?)>();
        foreach (var row in semantics.TransitionRows.OfType<TypedTransitionRowSuccess>())
            sites.Add(($"preservation: from {row.FromState ?? "*"} on {row.EventName}", row.Actions, row.Guard));
        foreach (var row in semantics.EventHandlers.OfType<TypedEventRowSuccess>().Where(r => !r.IsConstruction))
            sites.Add(($"preservation: on {row.EventName}", row.Actions, row.Guard));
        return DisambiguateSites(sites);
    }

    /// <summary>
    /// First-match row tables may carry several rows for the same state/event
    /// pair; repeated site names get an ordinal so every row stays a distinct,
    /// citable record.
    /// </summary>
    private static List<(string Site, IReadOnlyList<TypedAction> Actions, TypedExpression? Guard)>
        DisambiguateSites(List<(string Site, IReadOnlyList<TypedAction> Actions, TypedExpression? Guard)> sites)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < sites.Count; i++)
        {
            var name = sites[i].Site;
            if (seen.TryGetValue(name, out var occurrences))
            {
                seen[name] = occurrences + 1;
                sites[i] = (name + $" (row {occurrences + 1})", sites[i].Actions, sites[i].Guard);
            }
            else
            {
                seen[name] = 1;
            }
        }
        return sites;
    }

    private static string ScopeWord(AnchorScope scope) => scope switch
    {
        AnchorScope.OnEntry => "entry",
        AnchorScope.OnExit => "exit",
        _ => "in",
    };

    private static IEnumerable<string> FieldNames(CanonExpr expression) =>
        Nodes(expression).OfType<CanonFieldPre>().Select(f => f.Name);

    private static IEnumerable<CanonExpr> Nodes(CanonExpr expression)
    {
        yield return expression;
        IEnumerable<CanonExpr> children = expression switch
        {
            CanonCompare c => [c.Left, c.Right],
            CanonNary n => n.Operands.AsEnumerable(),
            CanonBinary b => [b.Left, b.Right],
            CanonNeg n => [n.Operand],
            CanonNot n => [n.Operand],
            CanonIsSet s => [s.Operand],
            CanonImplies i => [i.Antecedent, i.Consequent],
            CanonConditional c => [c.Condition, c.Then, c.Else],
            CanonMember m => m.Arguments.Prepend(m.Receiver),
            CanonCall c => c.Arguments.AsEnumerable(),
            CanonQuantifier q => [q.Collection, q.Predicate],
            CanonList l => l.Elements.AsEnumerable(),
            CanonInterp i => i.Segments.AsEnumerable(),
            _ => Enumerable.Empty<CanonExpr>(),
        };
        foreach (var child in children)
        foreach (var node in Nodes(child))
            yield return node;
    }

    // ── Row constructors ─────────────────────────────────────────────────────

    private static CorpusObligationRow Licensed(
        string obligation, string rule, string site, string derivation, string detail, string wp) =>
        new(obligation, rule, site, CorpusClassification.LicensedAsWritten,
            Derivation: derivation, Reason: null, Respellings: [], Detail: detail, Wp: wp);

    private static CorpusObligationRow OutOfScope(
        string obligation, string rule, string site, string reason, string detail, string? wp = null) =>
        new(obligation, rule, site, CorpusClassification.OutOfScope,
            Derivation: null, Reason: reason, Respellings: [], Detail: detail, Wp: wp);

    private static void Bump(SortedDictionary<string, int> tally, string key) =>
        tally[key] = tally.TryGetValue(key, out var count) ? count + 1 : 1;

    private static string DisplayName(CorpusClassification classification) => classification switch
    {
        CorpusClassification.LicensedAsWritten => "licensed as written",
        CorpusClassification.RespellNeeded => "respell needed",
        CorpusClassification.NoLicensedRespelling => "no licensed respelling",
        _ => "out of scope",
    };

    // ── Serialization ────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string ToJson(CorpusReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);

    public static string ToMarkdown(CorpusReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Corpus measurement — rule × write-site obligations vs licensed derivations");
        sb.AppendLine();
        sb.AppendLine($"Generated {report.GeneratedAtUtc} over `{report.CorpusRoot}`.");
        sb.AppendLine();
        sb.AppendLine($"- Files measured: **{report.FileCount}**; rule-bearing: **{report.RuleBearingFileCount}**; "
            + $"with explicit `rule` statements: **{report.FilesWithExplicitRules}**; "
            + $"with error diagnostics: **{report.FilesWithErrorDiagnostics}**");
        sb.AppendLine($"- Frame-preserved pairs (plan does not touch the rule; no obligation minted): "
            + $"**{report.FramePreservedPairs}**");
        sb.AppendLine();

        sb.AppendLine("## Classification totals");
        sb.AppendLine();
        Table(sb, ["Classification", "Count"],
            report.ClassificationTotals.Select(kv => new[] { kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture) }));

        sb.AppendLine("## Licensed derivations");
        sb.AppendLine();
        Table(sb, ["Derivation", "Count"],
            report.DerivationTotals.Select(kv => new[] { kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture) }));

        sb.AppendLine("## Out-of-scope reasons (named)");
        sb.AppendLine();
        Table(sb, ["Reason", "Count"],
            report.OutOfScopeReasonTotals.Select(kv => new[] { kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture) }));

        sb.AppendLine("## Per-file rows");
        sb.AppendLine();
        Table(sb, ["File", "Rows", "Licensed", "Respell", "No respelling", "Out of scope", "Frame pairs", "Errors"],
            report.Files.Select(f => new[]
            {
                f.File,
                f.Rows.Length.ToString(CultureInfo.InvariantCulture),
                Count(f, CorpusClassification.LicensedAsWritten),
                Count(f, CorpusClassification.RespellNeeded),
                Count(f, CorpusClassification.NoLicensedRespelling),
                Count(f, CorpusClassification.OutOfScope),
                f.FramePreservedPairs.ToString(CultureInfo.InvariantCulture),
                f.ErrorDiagnostics.ToString(CultureInfo.InvariantCulture),
            }));

        var respellRows = report.Files
            .SelectMany(f => f.Rows
                .Where(r => r.Classification == CorpusClassification.RespellNeeded)
                .Select(r => (f.File, Row: r)))
            .ToArray();
        if (respellRows.Length > 0)
        {
            sb.AppendLine("## Respell-needed rows (the recorded suggestion-schema output)");
            sb.AppendLine();
            Table(sb, ["File", "Obligation", "Site", "WP", "Respelling"],
                respellRows.Select(r => new[]
                {
                    r.File, r.Row.Obligation, r.Row.Site, r.Row.Wp ?? "",
                    string.Join(" — or — ", r.Row.Respellings),
                }));
        }

        var noRespelling = report.Files
            .SelectMany(f => f.Rows
                .Where(r => r.Classification == CorpusClassification.NoLicensedRespelling)
                .Select(r => (f.File, Row: r)))
            .ToArray();
        if (noRespelling.Length > 0)
        {
            sb.AppendLine("## No-licensed-respelling rows");
            sb.AppendLine();
            Table(sb, ["File", "Obligation", "Site", "Detail"],
                noRespelling.Select(r => new[] { r.File, r.Row.Obligation, r.Row.Site, r.Row.Detail }));
        }

        return sb.ToString();

        static string Count(CorpusFileReport file, CorpusClassification classification) =>
            file.Rows.Count(r => r.Classification == classification).ToString(CultureInfo.InvariantCulture);
    }

    private static void Table(StringBuilder sb, string[] header, IEnumerable<string[]> rows)
    {
        sb.AppendLine("| " + string.Join(" | ", header) + " |");
        sb.AppendLine("|" + string.Concat(Enumerable.Repeat("---|", header.Length)));
        foreach (var row in rows)
            sb.AppendLine("| " + string.Join(" | ", row.Select(Escape)) + " |");
        sb.AppendLine();

        static string Escape(string cell) => cell.Replace("|", "\\|").Replace("\n", " ");
    }
}
