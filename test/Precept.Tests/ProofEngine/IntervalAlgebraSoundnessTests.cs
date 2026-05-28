using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Regression coverage for three soundness violations in the proof engine's
/// interval algebra (BuildSiblingRejectExclusions, BuildNarrowedIntervals,
/// NegateConstraintToInterval). Each test exhibits a scenario where the prior
/// implementation produced a narrowed interval that was a SUBSET of the
/// truthful reachable-value set — admitting a false discharge of an overflow
/// or boundary obligation. See docs/compiler/proof-engine.md § Interval
/// Algebra for the per-branch AND vs OR semantics and decimal-vs-integer
/// negation rules.
/// </summary>
public class IntervalAlgebraSoundnessTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void MultiLeafAndRejectAbove_DoesNotNarrowEitherField()
    {
        // Reject guard is a conjunction A ∧ B on TWO fields. Its negation is
        // the disjunction ¬A ∨ ¬B, which can't soundly narrow either field
        // individually (Counter can still be 4 if Tier != 1, etc.). The
        // narrowing must forfeit on multi-leaf branches.
        //
        // Pre-fix: numeric leaves were intersected per-field as if ¬(A∧B) ≡
        // ¬A ∧ ¬B, narrowing Counter to [0, 3] when truth allows Counter = 4.
        // The increment Counter + 1 then falsely discharged the overflow
        // check against max 4.
        var ledger = Prove("""
            precept MultiLeafReject
            field Counter as integer default 0 nonnegative max 4 editable
            field Tier as integer default 1 min 1 max 3 editable
            state Open initial
            state Done terminal
            event Renew
            from Open on Renew when Counter >= 4 and Tier == 1 -> reject "premium-only cap"
            from Open on Renew -> set Counter = Counter + 1 -> no transition
            event Close
            from Open on Close -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "the multi-leaf AND reject can't narrow Counter — when Tier != 1, Counter can reach 4, " +
                     "and Counter + 1 overflows max 4");
    }

    [Fact]
    public void OrGuardBranchMissingField_DoesNotNarrowThatField()
    {
        // OR guard with two branches: branch A constrains X >= 5; branch B
        // constrains Y (nothing about X). Under OR, the row reaches whenever
        // EITHER branch holds. Branch B says nothing about X, so X must be
        // admitted as its full declared range.
        //
        // Pre-fix: cross-branch union only iterated fields present in each
        // branch, so X's union only saw branch A's [5, MAX] (missing the
        // implicit "X unconstrained" from branch B). The body's X arithmetic
        // then discharged using the over-tight narrowing, missing the case
        // where the OR reached via branch B with X = 0.
        var ledger = Prove("""
            precept OrBranchDisjoint
            field X as integer default 0 nonnegative max 10 editable
            field Y as integer default 0 nonnegative max 10 editable
            field Result as integer default 0 nonnegative max 100 editable
            state Open initial
            state Done terminal
            event Compute
            from Open on Compute when X >= 5 or Y <= 3
                -> set Result = 100 / X
                -> no transition
            event Close
            from Open on Close -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "branch B (Y <= 3) admits X = 0; the OR-union must back-fill X's interval " +
                     "with the base range, not assume the branch-A narrowing applies universally");
    }

    [Fact]
    public void DecimalSiblingReject_DoesNotClipBoundaryInteger()
    {
        // For decimal-typed fields, negating `X >= 10.0` to the integer-style
        // `X <= 9` is UNSOUND: truthfully X can be 9.5, 9.9, etc., but the
        // tight bound excludes them. The fix uses a closed bound at 10 (a
        // sound superset of the strict `< 10`) so the narrowed interval
        // covers all reachable values.
        //
        // Pre-fix: X narrowed to [0, 9]. X * 1.1 ∈ [0, 9.9], which fits
        // Result's max 10 — overflow falsely discharged. Post-fix: X
        // narrowed to [0, 10] (closed superset), X * 1.1 ∈ [0, 11], overflow
        // correctly detected.
        var ledger = Prove("""
            precept DecimalRejectBoundary
            field X as decimal default 0 min 0 max 20 editable
            field Result as decimal default 0 min 0 max 10 editable
            state Open initial
            state Done terminal
            event Scale
            from Open on Scale when X >= 10 -> reject "cap reached"
            from Open on Scale -> set Result = X * 1.1 -> no transition
            event Close
            from Open on Close -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "X is decimal-typed; the sibling reject narrows X to [0, 10] (closed superset), " +
                     "and X * 1.1 reaches 11 which overflows Result max 10");
    }
}
