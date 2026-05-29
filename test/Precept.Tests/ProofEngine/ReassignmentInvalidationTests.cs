using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// BUG-014 — sequential proof flow (spec § 0.6 item 7): when a field is reassigned
/// (`set`/`clear`) earlier in an action chain, prior guard facts about that field are
/// invalidated. A guard `when X != 0` must NOT discharge a `100 / X` obligation that comes
/// after `set X = 0`. These are the red-before/green-after cases: each compiled clean
/// (unsound) before the fix; each must now reject (or refuse to discharge) the stale fact.
/// </summary>
public class ReassignmentInvalidationTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void NumericGuard_InvalidatedByReassignment_DivisorNoLongerProved()
    {
        // when X != 0 -> set X = 0 -> set R = 100 / X
        // The guard proved X != 0 at entry, but `set X = 0` invalidates it; the division
        // after the reassignment must surface as a divide-by-zero risk, not "proved safe".
        var ledger = Prove("""
            precept ReassignProbe
            field X as integer default 1
            field R as integer default 0
            state S initial
            event E
            from S on E when X != 0
                -> set X = 0
                -> set R = 100 / X
                -> no transition
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "the X != 0 guard fact is stale after set X = 0 (spec § 0.6 item 7)");
        ledger.Obligations.Should().NotContain(
            o => o.Requirement is NumericProofRequirement && o.Disposition == ProofDisposition.Proved
                 && o.Strategy == ProofStrategy.GuardInPath,
            because: "a reassigned subject's guard fact must not discharge via GuardInPath");
    }

    [Fact]
    public void NumericGuard_NoReassignment_StillProves()
    {
        // Control / non-regression: with no intervening reassignment, the guard proves safe.
        var ledger = Prove("""
            precept GuardOnly
            field R as integer default 0
            state S initial
            event E(D as integer)
            from S on E when E.D != 0
                -> set R = 100 / E.D
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "the guard holds throughout — no reassignment invalidates it");
    }

    [Fact]
    public void NumericGuard_ReassignmentOfDifferentField_DoesNotInvalidate()
    {
        // Per-field invalidation: reassigning Y must not invalidate the guard fact about X.
        var ledger = Prove("""
            precept OtherFieldReassign
            field X as integer default 1
            field Y as integer default 0
            field R as integer default 0
            state S initial
            event E
            from S on E when X != 0
                -> set Y = 5
                -> set R = 100 / X
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "only the reassigned field's facts are invalidated; X was not reassigned");
    }

    [Fact]
    public void PresenceGuard_InvalidatedByClear_PresenceNoLongerProved()
    {
        // when X is set -> clear X -> set R = X + 1
        // The `X is set` guard is invalidated by `clear X`; reading X afterward is unproven.
        var ledger = Prove("""
            precept ReassignPresence
            field X as integer optional
            field R as integer default 0
            state S initial
            event E
            from S on E when X is set
                -> clear X
                -> set R = X + 1
                -> no transition
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnprovedPresenceRequirement),
            because: "the `X is set` fact is stale after clear X (spec § 0.6 item 7)");
        ledger.Obligations.Should().NotContain(
            o => o.Requirement is PresenceProofRequirement && o.Disposition == ProofDisposition.Proved
                 && o.Strategy == ProofStrategy.GuardInPath,
            because: "a cleared subject's presence guard must not discharge via GuardInPath");
    }
}
