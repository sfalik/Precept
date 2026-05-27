using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// W-C — the proof engine's satisfiability scan flags guards whose
/// conjunction with the fields' declared bounds is empty, and rule pairs
/// whose per-field interval intersections are empty.
/// </summary>
public class SatisfiabilityScanTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void UnsatisfiableGuard_EmitsPRE0082_OnContradictoryGuard()
    {
        // F-LANG-SPEC-02 — `X > 100 and X < 50` narrows X to an empty interval.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X > 100 and X < 50 -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "the guard conjunction is empty under any X value");
    }

    [Fact]
    public void UnsatisfiableGuard_DisjunctiveBranchSatisfiable_DoesNotEmit()
    {
        // F-LANG-SPEC-02 § disjunctive branch handling — `(X > 100 and X < 50) or (X == 7)`
        // has at least one satisfiable branch, so no PRE0082.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when (X > 100 and X < 50) or X == 7 -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "the second disjunct (X == 7) is satisfiable, so the whole disjunction is too");
    }

    [Fact]
    public void UnsatisfiableGuard_AgainstMinMaxFieldModifier_EmitsPRE0082()
    {
        // F-LANG-SPEC-02 § field-modifier composition — `field X min 5 max 100`
        // narrows X to [5, 100]; the guard `when X < 5` is unsatisfiable.
        // (The strict-comparison modifiers like `positive` aren't picked up by
        // the existing GetFieldBounds — that helper handles >= / <= bounds.)
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 min 5 max 100 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X < 5 -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "min 5 narrows X to [5, 100]; X < 5 falls outside that interval");
    }

    [Fact]
    public void SatisfiableGuard_DoesNotEmitPRE0082()
    {
        // Soundness baseline: a satisfiable guard does not emit.
        var ledger = Prove("""
            precept Repro
            field X as integer default 5 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X > 0 -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "X > 0 admits many values from the integer domain");
    }

    [Fact]
    public void ContradictoryRule_EmitsPRE0155_OnDisjointRuleIntervals()
    {
        // F-LANG-SPEC-03 — `rule X > 10; rule X <= 5` have empty intersection.
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 editable
            rule X > 10 because "X must exceed 10"
            rule X <= 5 because "X must be at most 5"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "X > 10 and X <= 5 have empty intersection — no valid X satisfies both");
    }

    [Fact]
    public void NonContradictoryRulePair_DoesNotEmitPRE0155()
    {
        // Soundness baseline: overlapping rule intervals do not emit.
        var ledger = Prove("""
            precept Repro
            field X as integer default 5 editable
            rule X > 0 because "positive"
            rule X <= 100 because "bounded"
            state Open initial
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "X > 0 and X <= 100 intersect on (0, 100]");
    }

    [Fact]
    public void ContradictoryRule_DifferentFields_DoesNotEmitPRE0155()
    {
        // F-LANG-SPEC-03 scope-cut: only fires when rules share a field.
        var ledger = Prove("""
            precept Repro
            field A as integer default 0 editable
            field B as integer default 0 editable
            rule A > 10 because "A is large"
            rule B < 5 because "B is small"
            state Open initial
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "A and B are independent fields");
    }
}
