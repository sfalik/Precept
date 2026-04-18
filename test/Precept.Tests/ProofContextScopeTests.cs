using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Full-pipeline tests for proof-context scope and cross-event/cross-row isolation.
///
/// <para>Each test compiles a complete precept via <see cref="PreceptTypeChecker.Check"/>
/// and asserts whether C93 (unproven divisor) is suppressed or emitted.</para>
///
/// Coverage:
/// <list type="bullet">
///   <item>Global rule facts are visible in every event's proof context</item>
///   <item>Event-derived facts (from assignment narrowing) do NOT cross event boundaries</item>
///   <item>Guard narrowing is local to the row that holds the guard — sibling rows don't see it</item>
///   <item>State ensure narrowing applies when transitioning FROM the named state</item>
///   <item>State ensure narrowing does NOT apply in other states</item>
///   <item>Field constraint (positive) is globally visible in all events</item>
///   <item>Row-local assignment narrowing does NOT cross into sibling rows</item>
///   <item>Rule-based facts are global across all from-states</item>
///   <item>Guard narrowing in Event1 is invisible in Event2 (independent proof contexts)</item>
///   <item>Reassignment kills a previously derived interval</item>
///   <item>Transitive-chain global rules are visible in all events</item>
///   <item>Guard-local fact plus global rule combine for transitive proof in that row only</item>
/// </list>
/// </summary>
/// <remarks>
/// Tests written against <c>docs/ProofEngineDesign.md</c> and the scope-isolation
/// requirements of the unified proof engine (Commit 6).
/// </remarks>
public class ProofContextScopeTests
{
    // ── Global rule facts: visible in every event ─────────────────────────────

    [Fact]
    public void Check_GlobalRuleFact_VisibleInAllEvents_NoC93()
    {
        // rule A > B is a global relational fact injected by WithRule at checker startup.
        // Both Event1 and Event2 build their proof contexts from the same global base,
        // so both see {A:1, B:-1} GT → Y / (A - B) is provably safe in both events.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Event1
            event Event2
            from Open on Event1 -> set Y = Y / (A - B) -> no transition
            from Open on Event2 -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "global rule A > B is visible in every event's proof context");
    }

    // ── Cross-event isolation: event-derived facts stay local ─────────────────

    [Fact]
    public void Check_EventDerivedFact_NotVisibleInSiblingEvent_C93()
    {
        // SetNet: set Net = Gross - Tax → rule Gross > Tax makes Net positive in SetNet's row.
        // UseNet: builds a fresh proof context from the global state — Net has no positive
        // marker globally (it was only narrowed inside SetNet's row-local context).
        // Y / Net in UseNet → C93.
        //
        // If this test starts PASSING (C93 suppressed), that indicates cross-event leakage.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Y as number default 1
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event SetNet
            event UseNet
            from Open on SetNet -> set Net = Gross - Tax -> no transition
            from Open on UseNet -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "proof derived in SetNet's row must not leak to UseNet — event isolation soundness");
    }

    // ── Guard-narrowing scope: local to its row, invisible in sibling rows ────

    [Fact]
    public void Check_GuardNarrowing_EventLocal_NotVisibleInSiblingRow_C93()
    {
        // Row1: when A > 0 → guard narrowing adds $positive:A to Row1's context.
        //   Y / A in Row1: identifier check finds $positive:A → no C93.
        // Row2 (same event, no guard): starts from the global context where A has no proof.
        //   Y / A in Row2 → C93.
        //
        // The two rows share the same event but each builds an independent proof context.
        const string dsl = """
            precept Test
            field A as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when A > 0 -> set Y = Y / A -> no transition
            from Open on Go -> set Y = Y / A -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "the unguarded sibling row has no narrowing for A; the guarded row's narrowing does not leak");
    }

    // ── State ensure: narrowing applies in transitions FROM that state ─────────

    [Fact]
    public void Check_StateEnsure_AppliesInTransitionFromThatState_NoC93()
    {
        // 'in Open ensure A > 0 because "..."': BuildStateEnsureNarrowings calls
        // ApplyNarrowing(A > 0, ..., assumeTrue: true) → stores $positive:A for state Open.
        // ValidateTransitionRows for 'from Open on Go' injects this narrowing into the
        // row's proof context → identifier check finds $positive:A → no C93.
        const string dsl = """
            precept Test
            field A as number default 1
            field Y as number default 1
            in Open ensure A > 0 because "A is always positive in Open"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / A -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "state ensure 'A > 0' adds $positive:A to the Open state's proof context");
    }

    [Fact]
    public void Check_StateEnsure_AppliesInCorrectState_C93InOtherState()
    {
        // 'in Active ensure X > 0 because "..."' is only applied to transitions FROM Active.
        // From Active on Go: X has $positive:X in context → Y / X → no C93.
        // From Pending on Go: no state ensure for Pending → X has no proof → Y / X → C93.
        const string dsl = """
            precept Test
            field X as number default 1
            field Y as number default 1
            in Active ensure X > 0 because "X is positive in Active"
            state Open initial
            state Active
            state Pending
            event Go
            from Active on Go -> set Y = Y / X -> no transition
            from Pending on Go -> set Y = Y / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "Pending row has no state ensure for X; C93 fires only in the Pending row");
        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().HaveCount(1,
            "exactly the Pending row fires C93; the Active row is protected by the state ensure");
    }

    // ── Field constraints: globally visible in all events ────────────────────

    [Fact]
    public void Check_ConstraintFact_GloballyVisible_NoC93()
    {
        // 'field A as number default 1 positive' creates $positive:A in the global
        // dataFieldKinds context. All event proof contexts are built from this base,
        // so $positive:A is available in every event — Y / A is safe everywhere.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field Y as number default 1
            state Open initial
            event Event1
            event Event2
            from Open on Event1 -> set Y = Y / A -> no transition
            from Open on Event2 -> set Y = Y / A -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "field-level positive constraint is visible as a global proof fact in all events");
    }

    // ── Row-local assignment: scope does not cross to sibling rows ────────────

    [Fact]
    public void Check_SequentialAssignment_ScopeLocal_ToRow_C93InOtherRow()
    {
        // Row1 (guarded): set X = 5 → IntervalOf(5) = [5,5] → $positive:X in Row1's context.
        //   Z / X in Row1: identifier check finds $positive:X → no C93.
        // Row2 (fallthrough): X has no row-local assignment → $positive:X absent.
        //   Z / X in Row2 → C93.
        //
        // Row-local assignment narrowing is scoped to the row that contains the set.
        const string dsl = """
            precept Test
            field X as number default 0
            field Z as number default 10
            field Flag as number default 1 positive
            state Open initial
            event Calc
            from Open on Calc when Flag > 0 -> set X = 5 -> set Z = Z / X -> no transition
            from Open on Calc -> set Z = Z / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().HaveCount(1,
            "only the second row (no assignment) fires C93; the first row's set X = 5 is local");
    }

    // ── Rule-based facts: global across all from-states ───────────────────────

    [Fact]
    public void Check_RuleBasedFact_GlobalAcrossStates_NoC93()
    {
        // rule P > Q is stored as a global relational fact and is visible regardless of
        // which state the transition originates from. Both Open and Active transitions
        // can use the fact to prove (P - Q) > 0.
        const string dsl = """
            precept Test
            field P as number default 5
            field Q as number default 1
            field Y as number default 1
            rule P > Q because "P exceeds Q"
            state Open initial
            state Active
            event Go
            event Reset
            from Open on Go -> set Y = Y / (P - Q) -> no transition
            from Active on Reset -> set Y = Y / (P - Q) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "rule P > Q is a global fact available in transitions from any state");
    }

    // ── Independent proof contexts per event ─────────────────────────────────

    [Fact]
    public void Check_MultipleEvents_IndependentProofContexts_Mixed()
    {
        // Event1: guard 'when A > 0' → $positive:A in Event1's guarded row context.
        //   Y / A in Event1's guarded row → no C93.
        // Event2: no guard → A has no proof in Event2's context (the guard from Event1
        //   did NOT contaminate Event2's proof context).
        //   Y / A in Event2 → C93.
        const string dsl = """
            precept Test
            field A as number default 1
            field Y as number default 1
            state Open initial
            event Event1
            event Event2
            from Open on Event1 when A > 0 -> set Y = Y / A -> no transition
            from Open on Event1 -> reject "not positive"
            from Open on Event2 -> set Y = Y / A -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "Event2 has no guard narrowing for A; cross-event proof contexts are independent");
        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().HaveCount(1,
            "only Event2's row fires C93; Event1's guarded row is protected");
    }

    // ── Reassignment kills derived interval ───────────────────────────────────

    [Fact]
    public void Check_DerivedInterval_KilledOnReassignment_C93()
    {
        // rule Gross > Tax: set Net = Gross - Tax → Net is positive ($positive:Net).
        // set Net = X: X has no proof → ApplyAssignmentNarrowing kills Net's positive marker.
        // Y / Net → identifier check: $positive:Net absent → C93.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field X as number default 3
            field Y as number default 1
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event Go
            from Open on Go -> set Net = Gross - Tax -> set Net = X -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "reassigning Net to X (no proof) kills the positive interval derived from Gross - Tax");
    }

    // ── Transitive chain: global rules visible in all events ─────────────────

    [Fact]
    public void Check_TransitiveChain_GlobalRules_VisibleInAllEvents_NoC93()
    {
        // rule A > B and rule B > C store two global relational facts.
        // BFS in RelationalGraph derives A > C transitively.
        // Both Event1 and Event2 use Y / (A - C) — each event's proof context is built
        // from the same global relational facts, so BFS succeeds in both.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            state Open initial
            event Event1
            event Event2
            from Open on Event1 -> set Y = Y / (A - C) -> no transition
            from Open on Event2 -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "transitive chain A > B > C is derived from global facts visible in every event");
    }

    // ── Guard-local fact enables transitive proof in that row only ────────────

    [Fact]
    public void Check_MixedGlobalAndGuard_TransitiveChain_NoC93AndC93InSiblingRow()
    {
        // Global: rule A > B → {A:1, B:-1} GT in all contexts.
        // Row1 guard: when B > C → adds {B:1, C:-1} GT to Row1's proof context only.
        //   BFS for A - C: C → B (guard fact) → A (global fact). Proves A > C. No C93.
        // Row2 (fallthrough): no guard → {B:1, C:-1} absent → BFS cannot reach A from C.
        //   Y / (A - C) in Row2 → C93.
        //
        // The guard-derived fact is row-local; the global rule alone is not enough.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 5
            field C as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go when B > C -> set Y = Y / (A - C) -> no transition
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "the fallthrough row lacks the guard fact B > C; A > C cannot be transitively proved");
        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().HaveCount(1,
            "only the fallthrough row fires C93; the guarded row has both facts for the chain");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
