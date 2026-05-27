using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// BUG-004: event ensures must contribute to transition-row body narrowing.
/// `on Event ensure Field is set` guarantees presence for every row body that
/// fires on Event; the proof engine's `TryGuardInPathProof` must consult these
/// ensures as additional narrowing sources, AND-combined with the row's
/// explicit guard.
/// </summary>
public class EventEnsureNarrowingTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void EventEnsure_PresenceAssertion_NarrowsTransitionRowBody()
    {
        // The minimal BUG-004 repro from bugs.md. `on Submit ensure Amount is set`
        // provably establishes Amount's presence before any `from <state> on Submit`
        // row body runs.
        var ledger = Prove("""
            precept Repro
            field Amount as money in 'USD' optional
            state Draft initial
            state Done terminal
            event Submit
            on Submit ensure Amount is set because "Required"
            from Draft on Submit -> set Amount = Amount + '1.00 USD' -> transition Done
            """);

        ledger.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "the event ensure `on Submit ensure Amount is set` narrows Amount's presence into the row body");
    }

    [Fact]
    public void EventEnsure_WithRowGuard_BothContribute()
    {
        // The row has its own `when` guard AND an event ensure; both must compose
        // (AND) into the narrowing context.
        var ledger = Prove("""
            precept Repro
            field Amount as money in 'USD' optional
            field Limit as money in 'USD' default '100.00 USD'
            state Draft initial
            state Done terminal
            event Submit
            on Submit ensure Amount is set because "Required"
            from Draft on Submit when Amount <= Limit -> set Amount = Amount + '1.00 USD' -> transition Done
            from Draft on Submit -> reject "Over limit"
            """);

        ledger.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "the explicit `when Amount <= Limit` guard references Amount; presence must discharge from the event ensure");
    }

    [Fact]
    public void EventEnsure_WithGuardOnEnsure_DoesNotNarrow()
    {
        // A guarded ensure (`when ... ensure ...`) is conditional and does NOT
        // unconditionally narrow downstream — the proof engine must conservatively
        // refuse to fold its condition.
        var ledger = Prove("""
            precept Repro
            field Force as boolean default false
            field Amount as money in 'USD' optional
            state Draft initial
            state Done terminal
            event Submit
            on Submit when Force ensure Amount is set because "Required when forced"
            from Draft on Submit -> set Amount = Amount + '1.00 USD' -> transition Done
            """);

        ledger.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                because: "a guarded ensure is conditional; the proof engine cannot unconditionally narrow on it");
    }

    [Fact]
    public void EventEnsure_DifferentEvent_DoesNotNarrow()
    {
        // An ensure anchored to a different event must NOT narrow this row's body.
        var ledger = Prove("""
            precept Repro
            field Amount as money in 'USD' optional
            state Draft initial
            state Done terminal
            event Quote
            event Submit
            on Quote ensure Amount is set because "Quote requires amount"
            from Draft on Submit -> set Amount = Amount + '1.00 USD' -> transition Done
            """);

        ledger.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                because: "the ensure is anchored to Quote, not Submit; it does not narrow the Submit row body");
    }
}
