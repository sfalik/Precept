using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Slice 2c-i, cell 11 — satisfiability isolation (Decision 4, ADVERSARIAL).
///
/// The diagnostic-emitting satisfiability / vacuity / contradiction scans read the BARE
/// <c>ExtractFieldInterval</c> and must NEVER receive the relational <c>narrowed</c>
/// dictionary. If a declared relational rule <c>rule X &gt;= Y</c> leaked into the bare
/// satisfiability scan, it would tighten X's interval and could flip a satisfiability
/// disposition (here: falsely fire <c>VacuousRule</c> on <c>rule X &gt;= 5</c>). The test
/// asserts the emitted satisfiability-family diagnostic multiset is IDENTICAL with and
/// without the relational rule present — byte-identity is POSITIVE evidence the narrowing
/// never reached the scan, BECAUSE the fixture is adversarially chosen so a leak WOULD
/// flip it.
///
/// This is a standing regression guard. It is GREEN today (the current engine does no
/// relational narrowing, so the scans are trivially identical) and must STAY GREEN after
/// the discharge-side narrowing lands — proving the narrowing was confined to the
/// discharge-time <c>narrowed</c> dictionary and never reached <c>ExtractFieldInterval</c>.
/// </summary>
public class RelationalNarrowingCoreSatisfiabilityTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    // The satisfiability-family diagnostic codes (Pass 1.5): vacuity, tautology,
    // unsatisfiability, contradiction, and reachability. The non-interference invariant
    // is over THIS set — these are the dispositions a leaked tighter interval would flip.
    private static readonly string[] SatisfiabilityFamily =
    {
        nameof(DiagnosticCode.VacuousRule),
        nameof(DiagnosticCode.TautologicalGuard),
        nameof(DiagnosticCode.UnsatisfiableGuard),
        nameof(DiagnosticCode.ContradictoryRule),
        nameof(DiagnosticCode.UnsatisfiableRule),
        // (UnreachableRowFact is a ProofForwardingFact, not a DiagnosticCode — the
        // UnsatisfiableGuard diagnostic is its emitting partner and is covered above.)
    };

    /// <summary>
    /// The satisfiability-family diagnostic CODE MULTISET (sorted) — the strongest stable
    /// comparison available from the public ledger surface. Spans carry offsets that the
    /// (identical-except-one-rule-line) sources shift, so a full Diagnostic-equality
    /// comparison would be brittle for reasons unrelated to the invariant; the code
    /// multiset is exactly the set of dispositions a leaked interval would flip, and is
    /// position-stable. Sorted so ordering differences never mask a genuine flip.
    /// </summary>
    private static List<string> SatisfiabilityCodeMultiset(ProofLedger ledger)
        => ledger.Diagnostics
            .Where(d => SatisfiabilityFamily.Contains(d.Code))
            .Select(d => d.Code)
            .OrderBy(c => c)
            .ToList();

    [Fact]
    public void RelationalRule_DoesNotLeakIntoSatisfiabilityScan_AdversarialVacuousRuleFlip()
    {
        // ── WHY THIS FIXTURE IS ADVERSARIAL (non-vacuity of the test) ──────────────
        // field X integer min 0 max 100  -> X's NON-relational interval is [0, 100].
        // rule X >= 5  -> its predicate (X >= 5) is NOT implied by [0, 100] (0 < 5), so it
        //   is genuinely NON-VACUOUS today (no VacuousRule). [Verified: compiling the
        //   WITHOUT source below emits no VacuousRule.]
        // field Y integer min 5 max 100, rule X >= Y  -> IF this relation leaked into the
        //   bare ExtractFieldInterval scan, it would tighten X's lower bound to >= Ylo = 5,
        //   making X's scanned interval [5, 100], under which `rule X >= 5` becomes
        //   ALWAYS-TRUE and the scan would falsely fire VacuousRule (PRE0154).
        //   [Verified: a field X min 5 max 100 with `rule X >= 5` DOES emit VacuousRule —
        //   so the leaked tightening would genuinely flip this disposition.]
        // Therefore byte-identical satisfiability output between WITH and WITHOUT the
        // relation is POSITIVE evidence of non-leak; a vacuous fixture (where the relation
        // could not flip anything) would pass even under a broken isolation and is rejected.

        var withRelation = Prove("""
            precept Cell11With

            field X as integer min 0 max 100 default 5 editable
            field Y as integer min 5 max 100 default 5 editable

            rule X >= 5 because "magnitude floor"
            rule X >= Y because "relational floor (adversarial: would tighten X to >= 5 if leaked)"

            state Open initial terminal
            """);

        // The baseline differs ONLY by the one relational rule line.
        var withoutRelation = Prove("""
            precept Cell11Without

            field X as integer min 0 max 100 default 5 editable
            field Y as integer min 5 max 100 default 5 editable

            rule X >= 5 because "magnitude floor"

            state Open initial terminal
            """);

        // Confirm the WITHOUT baseline is genuinely NON-vacuous today — the test would be
        // vacuous (could not detect a leak) if `rule X >= 5` already fired VacuousRule
        // without the relation present.
        SatisfiabilityCodeMultiset(withoutRelation).Should().NotContain(
            nameof(DiagnosticCode.VacuousRule),
            because: "without the relation, rule X >= 5 is genuinely non-vacuous under X's declared [0, 100] " +
                     "— so a flip-to-vacuous under leakage is detectable, making this fixture non-vacuous");

        // The isolation invariant: the satisfiability-family diagnostic multiset is
        // identical with and without the adversarially-chosen relational rule.
        SatisfiabilityCodeMultiset(withRelation).Should().Equal(
            SatisfiabilityCodeMultiset(withoutRelation),
            because: "the relational rule must NOT reach the bare-ExtractFieldInterval satisfiability scan; " +
                     "since it was chosen to flip VacuousRule on X under leakage, byte-identity proves non-leak");
    }

    [Fact]
    public void SatisfiableTighterRelation_SatisfiabilityByteIdentical()
    {
        // Decision-4 isolation, contradiction-reject amendment: the emptiness/contradiction
        // guard lives ONLY on the discharge path; it must not change what the satisfiability
        // scans emit. The relation here TIGHTENS X non-emptily (it is NOT a contradiction):
        //   field X integer min 0 max 100  -> X's NON-relational interval is [0, 100].
        //   rule X >= 3  -> NOT implied by [0, 100] (0 < 3), genuinely NON-VACUOUS today.
        //   field Y integer min 5 max 100, rule X >= Y  -> a NON-EMPTY tightening: IF it
        //     leaked into the bare scan it would tighten X to [5, 100], making `rule X >= 3`
        //     ALWAYS-TRUE and falsely firing VacuousRule. Byte-identity proves the non-empty
        //     narrowing was confined to the discharge folds and never reached the scan.
        var withRelation = Prove("""
            precept TightenWith

            field X as integer min 0 max 100 default 3 editable
            field Y as integer min 5 max 100 default 5 editable

            rule X >= 3 because "magnitude floor"
            rule X >= Y because "non-empty tightening (would flip VacuousRule on X >= 3 if leaked)"

            state Open initial terminal
            """);

        var withoutRelation = Prove("""
            precept TightenWithout

            field X as integer min 0 max 100 default 3 editable
            field Y as integer min 5 max 100 default 5 editable

            rule X >= 3 because "magnitude floor"

            state Open initial terminal
            """);

        // Non-vacuity guard: the WITHOUT baseline must NOT already fire VacuousRule, else a
        // leak could not be detected.
        SatisfiabilityCodeMultiset(withoutRelation).Should().NotContain(
            nameof(DiagnosticCode.VacuousRule),
            because: "without the relation, rule X >= 3 is genuinely non-vacuous under X's declared [0, 100]");

        SatisfiabilityCodeMultiset(withRelation).Should().Equal(
            SatisfiabilityCodeMultiset(withoutRelation),
            because: "the non-empty tightening relation must NOT reach the satisfiability scan; " +
                     "byte-identity proves the discharge-path guard left the scans untouched");
    }
}
