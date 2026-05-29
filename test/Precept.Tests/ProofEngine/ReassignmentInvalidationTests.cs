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

    // ── Index-bounds reassignment (accessor site `.at(N)` and action site `remove F at N`) ──
    // The guard `N >= 0 and N < F.count` discharges the index obligation. A `count > 0` guard is
    // also present so the non-empty obligation discharges independently — isolating the index
    // bound as the only thing a reassignment can invalidate.

    [Fact]
    public void IndexBounds_AtAccessor_NoReassignment_Discharges()
    {
        // Control / non-regression: with no intervening reassignment, the index obligation proves
        // and no IndexBoundsGuard diagnostic surfaces.
        var ledger = Prove("""
            precept IdxAtOnly
            field Items as list of string
            field Idx as integer default 0
            field Picked as string default ""
            state Open initial
            event Pick
            from Open on Pick when Idx >= 0 and Idx < Items.count and Items.count > 0
                -> set Picked = Items.at(Idx)
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.IndexBoundsGuard),
            because: "the bounds guard holds throughout — no reassignment invalidates it");
        ledger.Obligations.Should().Contain(
            o => o.Requirement is IndexBoundsProofRequirement && o.Disposition == ProofDisposition.Proved,
            because: "the explicit lower/upper bound guard discharges the index obligation");
    }

    [Fact]
    public void IndexBounds_AtAccessor_InvalidatedByIndexReassignment_NoLongerDischarges()
    {
        // `set Idx = 0` reassigns the index subject before `Items.at(Idx)`; the engine cannot carry
        // the stale `Idx >= 0 and Idx < count` bound across the write, so the index obligation must
        // surface. (The `Items.count > 0` obligation still proves — Items was not reassigned.)
        var ledger = Prove("""
            precept IdxAtReassign
            field Items as list of string
            field Idx as integer default 0
            field Picked as string default ""
            state Open initial
            event Pick
            from Open on Pick when Idx >= 0 and Idx < Items.count and Items.count > 0
                -> set Idx = 0
                -> set Picked = Items.at(Idx)
                -> no transition
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.IndexBoundsGuard),
            because: "the index bound is stale after set Idx = 0");
        ledger.Obligations.Should().NotContain(
            o => o.Requirement is IndexBoundsProofRequirement && o.Disposition == ProofDisposition.Proved,
            because: "a reassigned index's bound guard must not discharge");
    }

    [Fact]
    public void IndexBounds_RemoveAtAction_NoReassignment_Discharges()
    {
        // Control / non-regression for the action-site shape (`remove L at Idx`).
        var ledger = Prove("""
            precept IdxRemoveOnly
            field L as list of integer
            field Idx as integer default 0
            state Open initial
            event E
            from Open on E when Idx >= 0 and Idx < L.count and L.count > 0
                -> remove L at Idx
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.IndexBoundsGuard),
            because: "the index and non-empty guards both hold — nothing invalidates them");
        ledger.Obligations.Should().Contain(
            o => o.Requirement is IndexBoundsProofRequirement && o.Disposition == ProofDisposition.Proved,
            because: "the bounds guard discharges the remove-at index obligation");
    }

    [Fact]
    public void IndexBounds_RemoveAtAction_InvalidatedByIndexReassignment_NoLongerDischarges()
    {
        // The collection L is untouched (its `count > 0` non-empty obligation still discharges),
        // but the reassigned index Idx invalidates the index-in-bounds guard.
        var ledger = Prove("""
            precept IdxRemoveReassign
            field L as list of integer
            field Idx as integer default 0
            state Open initial
            event E
            from Open on E when Idx >= 0 and Idx < L.count and L.count > 0
                -> set Idx = 0
                -> remove L at Idx
                -> no transition
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.IndexBoundsGuard),
            because: "the index-in-bounds guard is stale after set Idx = 0, even though L is unchanged");
        ledger.Obligations.Should().NotContain(
            o => o.Requirement is IndexBoundsProofRequirement && o.Disposition == ProofDisposition.Proved,
            because: "a reassigned index's bound guard must not discharge");
    }

    [Fact]
    public void KeyPresence_AppendByUniqueness_InvalidatedByCollectionReassignment_NoLongerDischarges()
    {
        // when not (AuditLog contains Seq) -> clear AuditLog -> append AuditLog Entry by Seq
        // The append-by uniqueness obligation discharges from the `not contains` guard. Once the
        // collection is fully replaced (clear), that membership fact is about the old value and is
        // stale. The engine does not model `clear ⟹ empty ⟹ key absent`, so it conservatively
        // refuses to discharge (sound: it never claims uniqueness it cannot establish). This locks
        // the filter into TryKeyPresenceProof; a `set`-to-a-populated-collection would be the
        // genuinely-unsound case the same filter rejects.
        var ledger = Prove("""
            precept KeyReassign
            field AuditLog as log of string by integer
            state Open initial
            event Record(Entry as string, Seq as integer)
            from Open on Record when not (AuditLog contains Seq)
                -> clear AuditLog
                -> append AuditLog Entry by Seq
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is KeyPresenceProofRequirement { RequireAbsence: true })
            .Should().NotContain(o => o.Disposition == ProofDisposition.Proved,
                because: "a reassigned collection's `contains` guard fact must not discharge the uniqueness obligation");
    }
}
