using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Slice 7 — Proof engine construction row context tests.
/// Verifies that guard-aware proof strategies (Strategy 3 GuardInPath, Strategy 4 FlowNarrowing,
/// and BuildNarrowedIntervals for interval containment) handle EventHandlerContext from
/// construction rows (<c>on EventName initial when ...</c>) symmetrically with transition rows.
/// </summary>
public class ProofEngineConstructionTests
{
    private static ProofLedger Prove(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    private static (SemanticIndex Index, ProofLedger Ledger) ProveAllowingDiagnostics(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return (index, ProofEngine.Prove(index, graph));
    }

    /// <summary>
    /// Strategy 3 (GuardInPath) must discharge a proof obligation when the construction row
    /// guard directly implies the required constraint (D != 0 discharges the division obligation).
    /// </summary>
    [Fact]
    public void ProofEngine_ConstructionRow_GuardExtractsConstraints()
    {
        // `on Create initial when D != 0 -> set X = Y / D`
        // Guard D != 0 must discharge the divisor-nonzero obligation via GuardInPath.
        var (_, ledger) = ProveAllowingDiagnostics("""
            precept Widget
            field X as number default 0 writable
            field Y as integer default 1 writable
            field D as number default 1 writable
            state Draft initial terminal
            event Create initial
            on Create initial when D != 0 -> set X = Y / D
            """);

        var obligation = ledger.Obligations.FirstOrDefault(o =>
            o.Requirement is NumericProofRequirement { Comparison: OperatorKind.NotEquals, Threshold: 0m } &&
            o.Context is EventHandlerContext);

        obligation.Should().NotBeNull("a divisor != 0 obligation should be generated for the construction row");
        obligation!.Context.Should().BeOfType<EventHandlerContext>();
        ((EventHandlerContext)obligation.Context).Handler.IsConstruction.Should().BeTrue(
            "the context must originate from a construction row");
        obligation.Disposition.Should().Be(ProofDisposition.Proved,
            "guard `D != 0` must discharge the division obligation via GuardInPath");
        obligation.Strategy.Should().Be(ProofStrategy.GuardInPath,
            "Strategy 3 must fire when guard implies the proof condition");
    }

    /// <summary>
    /// BuildNarrowedIntervals must narrow the interval of a field referenced in a construction
    /// row guard, enabling interval-containment proofs within that row.
    /// Guard `Amount >= 10 and Amount <= 100` narrows Amount from [1,200] down to [10,100],
    /// which satisfies the Result field's [1,100] bound — the obligation is proved.
    /// Note: field refs in construction guards trigger PRE0148 (uninitialized field read);
    /// we use ProveAllowingDiagnostics to test the proof engine in isolation of that type error.
    /// </summary>
    [Fact]
    public void ProofEngine_ConstructionRow_GuardIntervalsCorrect()
    {
        // Field refs in construction guards emit PRE0148, so we allow diagnostics here
        // and focus solely on proof engine interval-narrowing behavior.
        var (_, ledger) = ProveAllowingDiagnostics("""
            precept Widget
            field Amount as integer min 1 max 200 writable
            field Result as integer min 1 max 100 writable
            state Draft initial terminal
            event Create initial
            on Create initial when Amount >= 10 and Amount <= 100 -> set Result = Amount
            """);

        // The interval-containment obligation (Result ← Amount) must be proved by interval
        // narrowing: Amount is narrowed to [10,100] by the guard, which fits in [1,100].
        var obligation = ledger.Obligations.FirstOrDefault(o =>
            o.Requirement is IntervalContainmentProofRequirement &&
            o.Context is EventHandlerContext);

        obligation.Should().NotBeNull("an interval containment obligation should be generated for the construction row");
        obligation!.Context.Should().BeOfType<EventHandlerContext>();
        ((EventHandlerContext)obligation.Context).Handler.IsConstruction.Should().BeTrue();
        obligation.Disposition.Should().Be(ProofDisposition.Proved,
            "guard narrows Amount to [10,100] which fits within Result's [1,100] bound");
        obligation.ComputedInterval.Should().NotBeNull("the computed interval must be populated");
        obligation.ComputedInterval!.Value.Min.Should().Be(10m,
            "lower bound narrowed by guard `Amount >= 10`");
        obligation.ComputedInterval!.Value.Max.Should().Be(100m,
            "upper bound narrowed by guard `Amount <= 100`");
    }

    /// <summary>
    /// A construction row with no guard must not extract any guard constraints — the obligations
    /// it generates are not discharged by GuardInPath (Strategy 3 finds no guard to extract from).
    /// </summary>
    [Fact]
    public void ProofEngine_ConstructionRow_NoGuard_NoConstraints()
    {
        var (_, ledger) = ProveAllowingDiagnostics("""
            precept Widget
            field X as number default 0 writable
            field Y as integer default 1 writable
            field D as number default 1 writable
            state Draft initial terminal
            event Create initial
            on Create initial -> set X = Y / D
            """);

        var obligation = ledger.Obligations.FirstOrDefault(o =>
            o.Requirement is NumericProofRequirement { Comparison: OperatorKind.NotEquals, Threshold: 0m } &&
            o.Context is EventHandlerContext);

        // Obligation exists but GuardInPath must not fire — no guard to extract from.
        if (obligation is not null)
        {
            obligation.Strategy.Should().NotBe(ProofStrategy.GuardInPath,
                "no guard on the construction row means Strategy 3 cannot apply");
        }
    }

    /// <summary>
    /// End-to-end: a guarded construction row where the guard fully implies the proof condition
    /// flows all the way through the proof obligation validator producing a clean ledger.
    /// Specifically, Strategy 3 fires and the obligation is Proved, just as it would be for
    /// a transition row with an equivalent guard.
    /// </summary>
    [Fact]
    public void ProofEngine_ConstructionRow_IntegrationWithValidator()
    {
        // Exact parallel of the transition-row guard test:
        // from Draft on Submit when D != 0 -> set X = Y / D -> no transition
        // but spelled as a construction row instead.
        var (_, ledger) = ProveAllowingDiagnostics("""
            precept Widget
            field X as number default 0 writable
            field Y as integer default 1 writable
            field D as number default 1 writable
            state Draft initial terminal
            event Create initial
            on Create initial when D != 0 -> set X = Y / D
            """);

        // Full pipeline must produce: context=EventHandlerContext, disposition=Proved,
        // strategy=GuardInPath — identical outcome to the equivalent transition-row test.
        var obligation = ledger.Obligations.FirstOrDefault(o =>
            o.Requirement is NumericProofRequirement { Comparison: OperatorKind.NotEquals, Threshold: 0m } &&
            o.Context is EventHandlerContext { Handler.IsConstruction: true });

        obligation.Should().NotBeNull("a divisor != 0 obligation must be generated and tracked");
        obligation!.Disposition.Should().Be(ProofDisposition.Proved);
        obligation.Strategy.Should().Be(ProofStrategy.GuardInPath);

        // No proof-failure diagnostics should be emitted for the guarded construction row.
        ledger.Diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            "the guard fully proves the divisor constraint; no division-by-zero diagnostic should appear");
    }
}
