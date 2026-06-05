using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Slice 2c-i — the proof engine uses a declared UNCONDITIONAL relational rule
/// (<c>rule X &gt;= Y</c> and <c>&gt;</c>/<c>&lt;=</c>/<c>&lt;</c>) as a provable bound on X,
/// sourced from Y's declared interval (depth-1, single-pass), so a downstream
/// value-discharge obligation on X (divisor, overflow/OutOfRange, sign) discharges —
/// while guarded relational/magnitude rules do NOT leak as unconditional global facts
/// (BUG-023, magnitude arm), the related-field read stays one hop (depth-1), and the
/// satisfiability/contradiction scans stay isolated from the narrowing.
///
/// These are FAILING-FIRST (TDD) tests encoding the EXPECTED post-build behavior. The
/// MUST-be-red cells (#1 demonstrator, #3 range, #4 LE, #5 conjunction, #3b sign-set,
/// #8 strict arm, #13-guarded magnitude, the guard-sourced subtraction-divisor sibling,
/// and the modulo/sqrt family-generality cells) assert a discharge the current engine
/// does not yet produce and therefore fail today; the GREEN-now cells (#2, #6, #7,
/// #8-decimal, #9-global, #10, #12, #3c, the reassigned-operand staleness guard, and
/// the #13 unguarded/no-rule controls) are regression guards whose current behavior
/// already matches the post-build assertion.
///
/// Two complementary divisor-discharge paths are exercised and must not be conflated:
/// the SIGN-SET path discharges a BARE divisor (`100/X`) when the related field is
/// sign-constrained (cell #3b: `rule X &gt; Y`, `Y nonnegative` ⇒ X &gt; 0); the
/// FLOW-NARROWING path discharges a SUBTRACTION divisor (`Z/(X−Y)`) via subject
/// resolution + the operator truth-table. Cell #3c is the sign-set falsifier: a bare
/// divisor with Y sign-UNCONSTRAINED stays Unresolved (X could be 0).
/// </summary>
public class RelationalNarrowingCoreTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    /// <summary>
    /// "Compiles clean" for these tests = no ERROR-severity diagnostics. Unset helper
    /// fields raise FieldNeverSet WARNINGS that are not proof failures and must not count.
    /// </summary>
    private static void ShouldCompileClean(ProofLedger ledger, string because)
        => ledger.Diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty(because);

    private static void ShouldEmit(ProofLedger ledger, DiagnosticCode code, string because)
        => ledger.Diagnostics.Should().Contain(
            d => d.Code == code.ToString() && d.Severity == Severity.Error, because);

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 1 — the design demonstrator (MUST be RED now: currently DivisionByZero)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Demonstrator_RelationGT_DischargesSubtractionDivisor()
    {
        // `rule OnHand > Reserved` with both fields `nonnegative` establishes
        // OnHand - Reserved >= 1 (integer strict), so `BatchCost / (OnHand - Reserved)`
        // is provably non-zero and compiles clean with no author-added guard.
        var ledger = Prove("""
            precept ReorderLine

            field OnHand as integer nonnegative default 0
            field Reserved as integer nonnegative default 0
            field BatchCost as money in 'USD' nonnegative default '0 USD'
            field UnitCost as money in 'USD' nonnegative default '0 USD'

            rule OnHand > Reserved because "there must be unreserved stock before a per-unit cost is meaningful"

            state Open initial
            in Open modify OnHand, Reserved, BatchCost editable

            event Recost
            from Open on Recost
                -> set UnitCost = BatchCost / (OnHand - Reserved)
                -> no transition
            """);

        ShouldCompileClean(ledger,
            because: "rule OnHand > Reserved (both nonnegative) makes (OnHand - Reserved) provably non-zero");

        // ProofStrategy attribution IS accessible from the test surface: the divisor
        // obligation is a NumericProofRequirement(!= 0) carried on the ProofLedger.
        // The design's Acceptance wants it attributed to FlowNarrowing.
        var divisor = ledger.Obligations.FirstOrDefault(o =>
            o.Requirement is NumericProofRequirement { Comparison: OperatorKind.NotEquals, Threshold: 0m });

        divisor.Should().NotBeNull("a divisor != 0 obligation should be generated for the subtraction divisor");
        divisor!.Disposition.Should().Be(ProofDisposition.Proved,
            because: "the relational rule discharges the subtraction divisor");
        divisor.Strategy.Should().Be(ProofStrategy.FlowNarrowing,
            because: "the declared relation discharges via the field-to-field flow-narrowing path");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 2 — unbounded Y; identity-degradation manufactures no bound (GREEN guard)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RelationGT_UnboundedReserved_DivisionByBareOnHand_StillRejects()
    {
        // Reserved is unbounded below, so `rule OnHand > Reserved` cannot give OnHand a
        // positive lower bound (OnHand could be 0 with Reserved negative). Division by
        // the bare OnHand operand must stay unsafe — identity degradation, no false bound.
        var ledger = Prove("""
            precept Cell2

            field OnHand as integer nonnegative default 0 editable
            field Reserved as integer default 0 editable
            field BatchCost as money in 'USD' nonnegative default '0 USD' editable
            field UnitCost as money in 'USD' nonnegative default '0 USD'

            rule OnHand > Reserved because "rel"

            state Open initial
            event Recost
            from Open on Recost
                -> set UnitCost = BatchCost / OnHand
                -> no transition
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "Reserved is unbounded below, so the relation cannot exclude OnHand == 0");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 3 — rule X >= Y, Y bounded below; lower-bound-dependent obligation on X
    //           (MUST be RED now)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RelationGE_YBoundedBelow_LowerBoundObligation_Discharges()
    {
        // C <- X with C declared min 10 max 50. X is nonnegative max 50 -> [0, 50], so
        // C's lower bound (10) is NOT covered today (X could be 0). `rule X >= Y` with
        // Y min 10 narrows X's lower bound to >= 10, covering C's [10, 50] -> discharges.
        var ledger = Prove("""
            precept Cell3

            field X as integer nonnegative max 50 default 10 editable
            field Y as integer min 10 max 50 default 10 editable
            field C as integer min 10 max 50 <- X

            rule X >= Y because "X bounded below by Y"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "rule X >= Y (Y min 10) raises X's lower bound to >= 10, covering C's [10, 50]");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 4 — rule X <= Y, Y max 100; upper-bound obligation on X (R-NARROW-LE)
    //           (MUST be RED now) — exercises IntervalContainment upper-bound narrowing
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RelationLE_YBoundedAbove_UpperBoundObligation_Discharges()
    {
        // C <- X with C declared max 100. X is nonnegative (unbounded above) -> the
        // computed-field bound obligation on C cannot discharge today. `rule X <= Y`
        // with Y max 100 caps X's upper bound at 100 (R-NARROW-LE) -> C in [0, 100]
        // <= [-inf, 100] -> discharges through the IntervalContainment narrowed-dict
        // path (TryIntervalContainmentProofNarrowed), the computed-field bound obligation.
        var ledger = Prove("""
            precept Cell4

            field X as integer nonnegative default 0 editable
            field Y as integer nonnegative max 100 default 0 editable
            field C as integer max 100 <- X

            rule X <= Y because "X bounded above by Y"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "rule X <= Y (Y max 100) caps X's upper bound at 100, covering C's [-inf, 100]");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 5 — conjunction: X >= Y AND X >= W, X lower = max(Ylo, Wlo)
    //           (MUST be RED now)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TwoRelations_Conjunction_NarrowsLowerBoundToMaxOfFloors()
    {
        // C <- X with C min 20 max 50. X nonnegative max 50 -> [0, 50]. `rule X >= Y`
        // (Y min 10) alone gives X >= 10 (insufficient: 10 < 20). `rule X >= W`
        // (W min 20) gives X >= 20. The conjunction max(10, 20) = 20 covers C's [20, 50].
        // The fixture is adversarial: the Y-relation alone does NOT discharge it; only
        // conjoining both floors does.
        var ledger = Prove("""
            precept Cell5

            field X as integer nonnegative max 50 default 30 editable
            field Y as integer min 10 max 50 default 10 editable
            field W as integer min 20 max 50 default 20 editable
            field C as integer min 20 max 50 <- X

            rule X >= Y because "floor Y"
            rule X >= W because "floor W"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "conjoining X >= Y (>= 10) and X >= W (>= 20) raises X's lower bound to max = 20");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 6 — mutual A >= B, B >= A: compiles, terminates (no hang) (GREEN guard)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MutualRelation_AGEB_AND_BGEA_CompilesAndTerminates()
    {
        // A >= B and B >= A conjoin to A == B; each read uses the other's NON-relational
        // interval, so there is no fixpoint re-entry. Must compile clean and terminate.
        var ledger = Prove("""
            precept Cell6

            field A as integer nonnegative default 0 editable
            field B as integer nonnegative default 0 editable

            rule A >= B because "rel1"
            rule B >= A because "rel2"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "mutual relations conjoin to A == B without fixpoint re-entry");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 7 — self X >= X: dropped at extraction (vacuous), terminates (GREEN guard)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SelfRelation_XGEX_DroppedAtExtraction_CompilesAndTerminates()
    {
        // X >= X is a self-relation, dropped at extraction (vacuous). No error, no hang.
        var ledger = Prove("""
            precept Cell7

            field X as integer nonnegative default 0 editable

            rule X >= X because "self"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "a self-relation is dropped at extraction and produces no error");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 8 — strict (integer) vs non-strict (decimal) discharge boundary
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StrictIntegerRelation_DischargesSubtractionDivisor_NonStrictDecimalDoesNot()
    {
        // Strict-integer arm: `rule X > Y` on integers gives X - Y >= 1 -> the
        // subtraction divisor `(X - Y)` discharges (MUST be RED now). This is the same
        // shape as the demonstrator; the strictness is what makes the integer >= 1 floor.
        var strictInteger = Prove("""
            precept Cell8Strict

            field X as integer nonnegative default 1 editable
            field Y as integer nonnegative default 0 editable
            field BatchCost as integer nonnegative default 0 editable
            field UnitCost as integer default 0

            rule X > Y because "strict integer floor"

            state Open initial
            event Recost
            from Open on Recost
                -> set UnitCost = BatchCost / (X - Y)
                -> no transition
            """);

        ShouldCompileClean(strictInteger,
            because: "rule X > Y on integers gives X - Y >= 1, so (X - Y) is provably non-zero");

        // Non-strict decimal arm: `rule X >= Y` on decimals (X - Y >= 0) must NOT
        // over-discharge the strict `(X - Y) != 0` divisor — X - Y can be exactly 0.
        // This stays a DivisionByZero rejection (GREEN guard / never-over-prove floor).
        var nonStrictDecimal = Prove("""
            precept Cell8Decimal

            field X as number default 1 editable
            field Y as number default 0 editable
            field BatchCost as number default 0 editable
            field UnitCost as number default 0

            rule X >= Y because "non-strict decimal floor"

            state Open initial
            event Recost
            from Open on Recost
                -> set UnitCost = BatchCost / (X - Y)
                -> no transition
            """);

        ShouldEmit(nonStrictDecimal, DiagnosticCode.DivisionByZero,
            because: "rule X >= Y (non-strict, decimal) allows X - Y == 0, so the divisor stays unsafe");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 3b — bare divisor 100/X from rule X > Y, Y nonnegative => X positive
    //            (sign-set path; MUST be RED now)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RelationGT_YNonnegative_BareDivisor_DischargesViaSignSet()
    {
        // `rule X > Y` with Y nonnegative gives X > Y >= 0 => X >= 1 (positive). The
        // BARE divisor `100 / X` (not a subtraction) must discharge through the sign-set
        // path (relation => X positive), distinct from the subtraction path in cell 1.
        var ledger = Prove("""
            precept Cell3b

            field X as integer nonnegative default 1 editable
            field Y as integer nonnegative default 0 editable
            field Q as integer <- 100 / X

            rule X > Y because "X strictly exceeds nonnegative Y, so X is positive"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "X > Y >= 0 establishes X positive, so the bare divisor 100 / X discharges via the sign-set");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 3c — bare divisor 100/X from rule X > Y, Y sign-UNCONSTRAINED => Unresolved
    //            (GREEN regression guard — distinct from cell #3b)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Divisor_BareFieldFromRelation_YUnconstrained_StaysUnresolved()
    {
        // Bare divisor `100/X` from `rule X > Y` with `Y` sign-UNCONSTRAINED → X could be 0
        // (if Y negative) → stays Unresolved. The flow-narrowing path rejects the bare
        // `TypedFieldRef` subject (not a subtraction), and the sign-set path can't help
        // because Y's sign is unknown. Distinct from cell #3b (Y nonnegative ⇒ discharges).
        // GREEN regression guard (rejects now and after build).
        var ledger = Prove("""
            precept Cell3c

            field X as integer default 1 editable
            field Y as integer default 0 editable
            field Q as integer <- 100 / X

            rule X > Y because "X strictly exceeds Y, but Y sign is unknown"

            state Open initial terminal
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "Y is sign-unconstrained, so X > Y cannot exclude X == 0 (Y could be negative)");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 8b — R-NARROW-LE strict direction (rule X < Y) closes the operator x type grid
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RelationLT_StrictUpperBound_Integer_Discharges()
    {
        // Concrete assertion chosen for 8b: the STRICT less-than direction (rule X < Y)
        // with Y bounded above (Y max 10) on an INTEGER subject. R-NARROW-LE: X < Y <= 10
        // caps X's upper bound; on integers the strict `<` floors to <= 9, but the closed
        // <= 10 cap is already sound and sufficient to cover C's max 10. This closes the
        // operator x type grid cell (strict-LE, integer) not covered by cell #4 (non-strict
        // <=) or cell #8 (the GE/subtraction direction). The bounded case discharges.
        var ledger = Prove("""
            precept Cell8b

            field X as integer nonnegative default 0 editable
            field Y as integer nonnegative max 10 default 0 editable
            field C as integer max 10 <- X

            rule X < Y because "X strictly below Y (Y max 10)"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "rule X < Y (Y max 10) caps X's upper bound at <= 10, covering C's [-inf, 10] (R-NARROW-LE, strict)");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 9 — guarded relational rule: global rejects; in-scope discharges
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GuardedRelationalRule_GlobalObligation_DoesNotDischarge()
    {
        // `rule X >= Y when Flag` holds ONLY under Flag; it is NOT a global fact about X.
        // A GLOBAL (guard-false-reachable) divisor `100 / X` must NOT discharge from it.
        // GREEN guard: must stay a DivisionByZero rejection (no global fact leak).
        var ledger = Prove("""
            precept Cell9Global

            field X as integer nonnegative default 1 editable
            field Y as integer min 5 max 100 default 5 editable
            field Flag as boolean default false editable
            field Q as integer <- 100 / X

            rule X >= Y when Flag because "guarded relational"

            state Open initial terminal
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "a guarded relational rule contributes no unconditional global fact about X");
    }

    [Fact]
    public void Divisor_SubtractionOfTwoFields_DischargesFromRowGuard()
    {
        // Guard-sourced sibling of the demonstrator — a row guard `when X > Y` discharges
        // the subtraction divisor `(X−Y) != 0` via the flow-narrowing path once the
        // obligation Subject is resolved to the subtraction. RED today (probe-confirmed
        // the guard-sourced subtraction divisor is equally broken), GREEN after the build.
        var ledger = Prove("""
            precept Cell9InScope

            field X as integer nonnegative default 1 editable
            field Y as integer nonnegative default 0 editable
            field BatchCost as integer nonnegative default 0 editable
            field UnitCost as integer default 0

            state Open initial
            event Recost
            from Open on Recost when X > Y
                -> set UnitCost = BatchCost / (X - Y)
                -> no transition
            """);

        ShouldCompileClean(ledger,
            because: "the row guard X > Y on integers establishes X - Y >= 1 in scope, discharging the subtraction divisor via the flow-narrowing path");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 10 — transitive X >= Y, Y >= Z, Z min 0, Y unbounded: one hop only
    //            (GREEN guard) — must terminate
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitiveChain_StopsAtOneHop_StillRejects()
    {
        // X gets Y's NON-relational bound only. Y is unbounded (only Z is min 0, one hop
        // further). X must NOT inherit Z's bound transitively, so `100 / X` stays unsafe.
        // GREEN guard: DivisionByZero, and the chain must terminate (no hang).
        var ledger = Prove("""
            precept Cell10

            field X as integer nonnegative default 0 editable
            field Y as integer default 0 editable
            field Z as integer min 0 default 0 editable
            field Q as integer <- 100 / X

            rule X >= Y because "rel1"
            rule Y >= Z because "rel2"

            state Open initial terminal
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "depth-1: X gets Y's (unbounded) non-relational bound, NOT Z's transitively");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 12 — magnitude + relation, 0 reachable through composition (Q1 fold probe)
    //            (GREEN guard) — must NOT prove the divisor safe
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MagnitudePlusRelation_ZeroReachable_DivisorNotProvedSafe()
    {
        // Adversarial fold-composition: `rule X >= -5` (magnitude, allows 0) AND
        // `rule X >= Y` (Y min -10, allows 0). NEITHER fact alone excludes 0
        // (magnitude floor -5 < 0; relation floor Y >= -10 < 0). The composed sign set
        // must NOT drop the reachable zero sign -> `100 / X` is NOT proved safe.
        // GREEN guard / Q1 falsifier: must stay a DivisionByZero rejection.
        var ledger = Prove("""
            precept Cell12

            field X as integer default 0 editable
            field Y as integer min -10 max 10 default 0 editable
            field Q as integer <- 100 / X

            rule X >= -5 because "magnitude floor allows zero"
            rule X >= Y because "relational floor allows zero"

            state Open initial terminal
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "neither the magnitude floor (-5) nor the relation floor (>= -10) excludes 0 — 0 stays reachable");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cell 13 — BUG-023: guarded MAGNITUDE rule must NOT discharge a global divisor;
    //            unguarded control still discharges; no-rule control rejects
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Bug023_GuardedMagnitudeRule_DoesNotDischargeGlobalDivisor()
    {
        // BUG-023 repro (MUST be RED now: currently leaks clean). A guarded magnitude
        // rule `rule X >= 5 when Flag` holds only under Flag and must NOT fold as an
        // unconditional ScopedNumericFact discharging the global `100 / X` divisor.
        // After the shared-loop guard filter lands, this MUST emit DivisionByZero.
        var ledger = Prove("""
            precept Bug023Guarded

            field X as integer nonnegative default 1 editable
            field Flag as boolean default false editable
            field Q as integer <- 100 / X

            rule X >= 5 when Flag because "guarded magnitude"

            state Open initial terminal
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "a guarded magnitude rule must NOT fold as an unconditional fact (BUG-023)");
    }

    [Fact]
    public void Bug023_UnguardedMagnitudeControl_StillDischargesGlobalDivisor()
    {
        // The legitimate unconditional fact must continue to reach and discharge: the
        // guard filter tightens ONLY the guarded path. `rule X >= 5` (no `when`) gives
        // X >= 5 > 0, so `100 / X` discharges -> clean. GREEN now and post-fix.
        var ledger = Prove("""
            precept Bug023Control

            field X as integer nonnegative default 1 editable
            field Q as integer <- 100 / X

            rule X >= 5 because "unguarded magnitude"

            state Open initial terminal
            """);

        ShouldCompileClean(ledger,
            because: "an unguarded magnitude rule X >= 5 is a legitimate unconditional fact (X >= 5 > 0)");
    }

    [Fact]
    public void Bug023_NoRuleControl_RejectsGlobalDivisor()
    {
        // No-rule control: with no fact about X at all, the global `100 / X` divisor must
        // reject. GREEN guard (anchors the BUG-023 triad — proves the divisor is genuinely
        // unsafe absent any rule, so the guarded case's clean today is the leak).
        var ledger = Prove("""
            precept Bug023NoRule

            field X as integer nonnegative default 1 editable
            field Q as integer <- 100 / X

            state Open initial terminal
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "with no rule about X, the global divisor 100 / X is genuinely unsafe");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Staleness — operand reassigned before the divisor makes the relation stale
    //              (GREEN regression guard — must stay rejecting)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Divisor_RelationOnReassignedOperand_StaysStale()
    {
        // ReassignedBefore staleness gate — an operand reassigned earlier in the chain
        // makes the relation stale; the divisor must NOT discharge. GREEN regression guard
        // (must stay rejecting through the build).
        var ledger = Prove("""
            precept StaleReassign

            field X as integer nonnegative default 1 editable
            field Y as integer nonnegative default 0 editable
            field Z as integer nonnegative default 0 editable
            field U as integer default 0

            state Open initial
            event E
            from Open on E when X > Y
                -> set X = Y
                -> set U = Z / (X - Y)
                -> no transition
            """);

        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "after `set X = Y` the relation X > Y is stale for X, so the subtraction divisor must not discharge");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Family generality — modulo's right-operand divisor obligation is the same
    //                      NumericProofRequirement(!= 0); operator-agnostic seam
    //                      (MUST be RED now)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Modulo_SubtractionDivisor_DischargesFromRelation()
    {
        // `Z % (X − Y)` — modulo's right operand is its divisor, the same `!= 0`
        // obligation as division. With `rule X > Y` on nonnegative integers, X - Y >= 1,
        // so the modulo divisor is provably non-zero. The seam is operator-agnostic.
        // RED now, GREEN after the build.
        var ledger = Prove("""
            precept ModuloFamily

            field X as integer nonnegative default 1 editable
            field Y as integer nonnegative default 0 editable
            field Z as integer nonnegative default 0 editable
            field R as integer default 0

            rule X > Y because "strict integer floor"

            state Open initial
            event Recost
            from Open on Recost
                -> set R = Z % (X - Y)
                -> no transition
            """);

        ShouldCompileClean(ledger,
            because: "rule X > Y on integers gives X - Y >= 1, so the modulo divisor (X - Y) is provably non-zero");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Family generality — sqrt's non-negativity obligation discharges from a relation
    //                      (MUST be RED now)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Sqrt_SubtractionOfTwoFields_DischargesFromRelation()
    {
        // `sqrt(X − Y)` carries a non-negativity obligation on its argument
        // (NumericProofRequirement(>= 0)). With `rule X > Y` on nonnegative integers,
        // X - Y >= 1 >= 0, so the non-negative obligation discharges. RED now.
        var ledger = Prove("""
            precept SqrtFamily

            field X as integer nonnegative default 1 editable
            field Y as integer nonnegative default 0 editable
            field R as number default 0

            rule X > Y because "strict integer floor"

            state Open initial
            event Recost
            from Open on Recost
                -> set R = sqrt(X - Y)
                -> no transition
            """);

        ShouldCompileClean(ledger,
            because: "rule X > Y on integers gives X - Y >= 1 >= 0, so the sqrt non-negativity obligation discharges");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Contradiction-reject — a rule that contradicts the subject's own declared
    //  bounds (⟦X⟧₀ ⊓ narrowing = ∅) must REJECT the dependent fault-prone op at
    //  Error, never vacuously "prove safe".
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RelationContradictsSubjectBounds_DivisorSubject_Rejects()
    {
        // `rule X >= Y` with X max 5 and Y min 10 is self-contradictory: no X in [0,5]
        // can be >= Y in [10,20]. The empty narrowed interval [10,5] must NOT vacuously
        // discharge `100 / X` — the relation contributes no discharge fact, so the divisor
        // falls back to its real proof state (X can be 0 at default) and REJECTS.
        var ledger = Prove("""
            precept ContraDivisor

            field X as integer min 0 max 5 default 0 editable
            field Y as integer min 10 max 20 default 10 editable
            field Q as integer <- 100 / X

            rule X >= Y because "contradicts X's declared bounds"

            state Open initial terminal
            """);

        ledger.Diagnostics.Any(d => d.Severity == Severity.Error)
            .Should().BeTrue("a contradicted relation cannot prove the divisor safe");
        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "the contradicted rule X >= Y contributes no discharge fact, so X can be zero");
    }

    [Fact]
    public void MagnitudeContradicts_DivisorSubject_Rejects()
    {
        // `rule X >= 10` with X max 5 is self-unsatisfiable. The magnitude arm converges
        // on the same outcome as the relational arm: the self-unsat rule's ScopedNumericFact
        // is blocked, so `100 / X` falls back and REJECTS. UnsatisfiableRule/Warning may also
        // emit — the two are not exclusive; assert the DivisionByZero Error and HasErrors.
        var ledger = Prove("""
            precept ContraMagnitude

            field X as integer min 0 max 5 default 0 editable
            field Q as integer <- 100 / X

            rule X >= 10 because "contradicts X's declared bounds"

            state Open initial terminal
            """);

        ledger.Diagnostics.Any(d => d.Severity == Severity.Error)
            .Should().BeTrue("a self-unsatisfiable magnitude rule cannot prove the divisor safe");
        ShouldEmit(ledger, DiagnosticCode.DivisionByZero,
            because: "the self-unsat rule X >= 10 contributes no discharge fact, so X can be zero (convergence with the relational arm)");
    }

    [Fact]
    public void RelationContradictsSubjectBounds_AssignmentRange_Rejects()
    {
        // Interval-only channel (no sign-set involvement): `rule X <= Y`, X min 10 max 20,
        // Y min 0 max 5. The narrowing X <= Y <= 5 contradicts X >= 10, so ⟦X⟧₀ ⊓ (-∞,5] = ∅.
        // The only path to "proving" Q min 0 max 3 <- X containment was the contradicted
        // (empty) narrowing; with it suppressed, the assignment-range obligation REJECTS.
        var ledger = Prove("""
            precept ContraRange

            field X as integer min 10 max 20 default 10 editable
            field Y as integer min 0 max 5 default 0 editable
            field Q as integer min 0 max 3 <- X

            rule X <= Y because "contradicts X's declared bounds"

            state Open initial terminal
            """);

        ledger.Diagnostics.Any(d => d.Severity == Severity.Error)
            .Should().BeTrue("a contradicted relation cannot prove the assignment-range obligation");
    }

    [Fact]
    public void RelationTightensNonEmpty_StillCompilesClean()
    {
        // The must-NOT-false-reject guard: a relation that TIGHTENS X to a non-empty interval
        // must still discharge. `rule OnHand > Reserved`, both nonnegative, makes
        // (OnHand - Reserved) provably non-zero — the emptiness guard does not fire because
        // [0,∞) ⊓ [Reserved.lo+1,∞) is non-empty. Same precept as the demonstrator.
        var ledger = Prove("""
            precept TightenNonEmpty

            field OnHand as integer nonnegative default 0
            field Reserved as integer nonnegative default 0
            field BatchCost as money in 'USD' nonnegative default '0 USD'
            field UnitCost as money in 'USD' nonnegative default '0 USD'

            rule OnHand > Reserved because "there must be unreserved stock before a per-unit cost is meaningful"

            state Open initial
            in Open modify OnHand, Reserved, BatchCost editable

            event Recost
            from Open on Recost
                -> set UnitCost = BatchCost / (OnHand - Reserved)
                -> no transition
            """);

        ShouldCompileClean(ledger,
            because: "a non-empty tighter relational narrowing must still discharge (no false rejection)");

        var divisor = ledger.Obligations.FirstOrDefault(o =>
            o.Requirement is NumericProofRequirement { Comparison: OperatorKind.NotEquals, Threshold: 0m });
        divisor.Should().NotBeNull("a divisor != 0 obligation should be generated for the subtraction divisor");
        divisor!.Strategy.Should().Be(ProofStrategy.FlowNarrowing,
            because: "the non-empty relation discharges via the field-to-field flow-narrowing path");
    }

    [Fact]
    public void EmptyResultInterval_ContainmentReaderReturnsUnprovable()
    {
        // Reader backstop: a containment obligation whose ONLY path to discharge is an empty
        // narrowed interval must NOT pass. `rule X <= Y` (X min 100 max 200, Y min 0 max 5)
        // empties X against its bounds; the computed-field bound on `Q max 50 <- X` would
        // vacuously pass under Contains(⊥) => true if the reader did not guard emptiness.
        // The reader-side IsEmpty guard forces the containment to fail → OutOfRange Error.
        var ledger = Prove("""
            precept EmptyReader

            field X as integer min 100 max 200 default 100 editable
            field Y as integer min 0 max 5 default 0 editable
            field Q as integer max 50 <- X

            rule X <= Y because "contradicts X's declared bounds"

            state Open initial terminal
            """);

        ledger.Diagnostics.Any(d => d.Severity == Severity.Error)
            .Should().BeTrue("an empty narrowed interval must not vacuously discharge a containment obligation");
    }
}
