using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// F-LANG-BIZ-08: discrete equality narrowing. When a guard pins a field to
/// a singleton value (`F == V`), downstream proof obligations whose subject
/// is F discharge against the singleton — closing a known false-positive
/// class where previously the proof engine demanded redundant `nonzero`/
/// `positive` modifiers on fields already constrained by the guard.
///
/// Per Decision 5 (`docs/Working/biz-operator-extensions-design.md`), the
/// minimal sound surface ships first: `F == literal` only. Disjunctive
/// equality (`F == V1 or F == V2`) is deferred.
/// </summary>
public class DiscreteEqualityNarrowingTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void GuardEqualityToPositiveLiteral_DischargesGreaterThanZeroObligation()
    {
        // Severity == 1 narrows Severity to [1, 1]; the singleton 1 satisfies
        // a > 0 requirement that previously required `Severity positive` to
        // discharge.
        var ledger = Prove("""
            precept SeverityDemo
            field Severity as integer default 1 editable
            field Inverse as decimal <- 1 / Severity
            rule Severity == 1 because "Severity is constrained to 1"
            state Open initial
            """);

        // Inverse = 1 / Severity has a divisor-non-zero obligation.
        // The rule `Severity == 1` narrows Severity to 1; 1 != 0 ⇒ the
        // obligation discharges from the equality narrowing.
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "Severity == 1 narrows the divisor to a non-zero singleton");
    }

    [Fact]
    public void GuardEqualityWithinTransitionRow_DischargesObligationInActionChain()
    {
        // The canonical F-LANG-BIZ-08 example from the design's § Audience and
        // Teachability: `when Severity == 1 -> set Inverse = 1 / Severity`.
        // The transition-row guard narrows Severity; the action's divisor-
        // safety obligation discharges from the equality.
        var ledger = Prove("""
            precept Refund
            field Severity as integer default 3 editable
            field Inverse as decimal default 0 editable
            state Processing initial
            state Refunded terminal
            event Refund
            from Processing on Refund when Severity == 1
              -> set Inverse = 1 / Severity
              -> transition Refunded
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "the row guard Severity == 1 narrows the divisor to 1");
    }

    [Fact]
    public void GuardEqualityToZero_DoesNotDischargeNonzeroObligation()
    {
        // Soundness check: an equality guard pinning a value that does NOT
        // satisfy the requirement must NOT discharge. `Severity == 0` does
        // not satisfy `Severity != 0`, so the division IS undischarged.
        var ledger = Prove("""
            precept Bogus
            field Severity as integer default 0 editable
            field Inverse as decimal default 0 editable
            state Processing initial
            state Done terminal
            event Compute
            from Processing on Compute when Severity == 0
              -> set Inverse = 1 / Severity
              -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "Severity == 0 pins the divisor to zero — discharge would be unsound");
    }

    [Fact]
    public void GuardEqualityWithDifferentField_DoesNotDischargeObligation()
    {
        // Soundness check: a guard equality on field X does NOT discharge
        // obligations on field Y, even if X and Y happen to share a value.
        var ledger = Prove("""
            precept FieldScopeCheck
            field A as integer default 1 editable
            field B as integer default 0 editable
            field Quotient as decimal default 0 editable
            state Processing initial
            state Done terminal
            event Compute
            from Processing on Compute when A == 5
              -> set Quotient = 1 / B
              -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "the guard narrows A, not B — B's safety is unrelated");
    }
}
