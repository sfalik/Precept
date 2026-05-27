using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// BUG-006 / W-C cross-row interval composition. When a sibling reject-row
/// above the current row narrows a field to interval I, the current row
/// implicitly satisfies "¬I" — the proof engine's BuildNarrowedIntervals
/// subtracts I from the current row's per-field interval. The classic
/// repro: a `reject when Counter >= MaxCount` row makes the next row's
/// `Counter + 1` provably non-overflowing.
/// </summary>
public class CrossRowIntervalCompositionTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void BUG006_LoanRenewalCap_NoOverflow()
    {
        // The verbatim BUG-006 repro shape from bugs.md: a reject-row above
        // narrows Counter; the success row's Counter + 1 should compile clean.
        var ledger = Prove("""
            precept LoanRenewal
            field Counter as integer default 0 nonnegative max 4 editable
            state Open initial
            state Closed terminal
            event Renew
            from Open on Renew when Counter >= 4 -> reject "cap reached"
            from Open on Renew -> set Counter = Counter + 1 -> no transition
            event Close
            from Open on Close -> transition Closed
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "the sibling reject-row above bounds Counter to [0, 3] before the increment, so Counter + 1 ≤ 4");
    }

    [Fact]
    public void SiblingRejectAbove_NarrowsToSafe()
    {
        // Minimal isolated case: a single reject-row above + a single success
        // row below that depends on the narrowing.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 nonnegative max 10 editable
            state Open initial
            state Done terminal
            event Inc
            from Open on Inc when X >= 10 -> reject "at max"
            from Open on Inc -> set X = X + 1 -> no transition
            event Finish
            from Open on Finish -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "X >= 10 is rejected first, so X + 1 in the next row is bounded");
    }

    [Fact]
    public void NoSiblingRejectAbove_StillOverflows()
    {
        // Soundness baseline: without the sibling reject, the increment
        // would overflow the declared max — overflow IS detected.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 nonnegative max 10 editable
            state Open initial
            state Done terminal
            event Inc
            from Open on Inc -> set X = X + 1 -> no transition
            event Finish
            from Open on Finish -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "without the reject-row, X = 10 leads to X + 1 = 11, overflowing max 10");
    }
}
