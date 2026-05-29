using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// BUG-016 — collection-mutation forward-propagation (spec § 0.6 item 7, "before the new
/// assignment's facts are stored"). The non-empty (<c>count &gt; 0</c>) fact required by
/// <c>dequeue</c>/<c>pop</c>/<c>remove at</c> is effect-adjusted across the action chain:
/// a grow (<c>enqueue</c>/<c>push</c>/<c>append</c>/…) establishes it; a shrink
/// (<c>dequeue</c>/<c>pop</c>/<c>remove</c>/…) invalidates it. These cases are red-before/
/// green-after: the soundness cases over-proved before the fix; the completeness cases
/// over-rejected.
/// </summary>
public class CollectionCountEffectTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    private static bool ProvedCountReq(ProofObligation o) =>
        o.Requirement is NumericProofRequirement && o.Disposition == ProofDisposition.Proved;

    [Fact]
    public void Shrink_InvalidatesNonEmptyGuard_SecondDequeueNoLongerProves()
    {
        // when Q.count > 0 -> dequeue Q -> dequeue Q
        // The first dequeue is safe (guard holds); the second is NOT — the first may have emptied Q.
        // The stale `Q.count > 0` guard must not discharge the second dequeue's non-empty obligation.
        var ledger = Prove("""
            precept ShrinkSoundness
            field Q as queue of integer
            field X as integer optional
            field Y as integer optional
            state S initial
            event E
            from S on E when Q.count > 0
                -> dequeue Q into X
                -> dequeue Q into Y
                -> no transition
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation),
            because: "the Q.count > 0 fact is stale after the first dequeue (a shrink) — the second dequeue may hit an empty queue");
        ledger.Obligations.Count(ProvedCountReq).Should().Be(1,
            because: "only the first dequeue's non-empty obligation discharges; the second is unresolved");
    }

    [Fact]
    public void NonEmptyGuard_NoShrink_SingleDequeueStillProves()
    {
        // Control / non-regression: a single dequeue under the guard discharges.
        var ledger = Prove("""
            precept ShrinkControl
            field Q as queue of integer
            field X as integer optional
            state S initial
            event E
            from S on E when Q.count > 0
                -> dequeue Q into X
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation),
            because: "the non-empty guard holds for the single dequeue");
    }

    [Fact]
    public void Grow_EstablishesNonEmpty_DequeueProvesWithoutGuard()
    {
        // enqueue Q V -> dequeue Q : the enqueue guarantees count >= 1, so the dequeue is safe
        // even with no guard. Before the fix this over-rejected (UnguardedCollectionMutation).
        var ledger = Prove("""
            precept GrowCompleteness
            field Q as queue of integer
            field X as integer optional
            state S initial
            event E(V as integer)
            from S on E
                -> enqueue Q E.V
                -> dequeue Q into X
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation),
            because: "the prior enqueue establishes count >= 1, so the dequeue is provably safe");
        ledger.Obligations.Should().Contain(
            o => o.Requirement is NumericProofRequirement && o.Disposition == ProofDisposition.Proved
                 && o.Strategy == ProofStrategy.CollectionGrowth,
            because: "the dequeue's non-empty obligation discharges via the prior grow, not a guard");
    }

    [Fact]
    public void Grow_EstablishesNonEmpty_PopProvesWithoutGuard()
    {
        // Same forward-propagation for a stack: push then pop.
        var ledger = Prove("""
            precept GrowStack
            field St as stack of integer
            field X as integer optional
            state S initial
            event E(V as integer)
            from S on E
                -> push St E.V
                -> pop St into X
                -> no transition
            """);

        ledger.Obligations.Should().Contain(
            o => o.Requirement is NumericProofRequirement && o.Disposition == ProofDisposition.Proved
                 && o.Strategy == ProofStrategy.CollectionGrowth,
            because: "the prior push establishes the stack non-empty for the pop");
    }

    [Fact]
    public void GrowAfterShrink_ReestablishesNonEmpty()
    {
        // when Q.count > 0 -> dequeue Q -> enqueue Q V -> dequeue Q
        // 1st dequeue: guard. The shrink invalidates the non-empty fact, but the following enqueue
        // re-establishes it, so the 3rd action (2nd dequeue) is provably safe via the grow.
        var ledger = Prove("""
            precept GrowAfterShrink
            field Q as queue of integer
            field X as integer optional
            field Y as integer optional
            state S initial
            event E(V as integer)
            from S on E when Q.count > 0
                -> dequeue Q into X
                -> enqueue Q E.V
                -> dequeue Q into Y
                -> no transition
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation),
            because: "the enqueue re-establishes count >= 1 after the first dequeue's shrink");
        ledger.Obligations.Should().Contain(
            o => o.Requirement is NumericProofRequirement && o.Disposition == ProofDisposition.Proved
                 && o.Strategy == ProofStrategy.CollectionGrowth,
            because: "the second dequeue discharges via the intervening enqueue");
    }

    [Fact]
    public void Clear_InvalidatesNonEmptyGuard_DequeueNoLongerProves()
    {
        // Full-replacement clear is handled by the ReplacesEntireValue / ReassignedBefore path
        // (BUG-014), not the shrink path — but the count guard must still go stale after clear.
        var ledger = Prove("""
            precept ClearSoundness
            field Q as queue of integer
            field X as integer optional
            state S initial
            event E
            from S on E when Q.count > 0
                -> clear Q
                -> dequeue Q into X
                -> no transition
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation),
            because: "clear empties Q, so the Q.count > 0 guard is stale for the following dequeue");
    }
}
