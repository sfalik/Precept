using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Computed-field bound-containment emission (BUG-017): the
/// <see cref="IntervalContainmentProofRequirement"/> for a computed field with
/// explicit declared bounds is created unconditionally — even when the operand
/// interval is unbounded — and discharge may return unresolved (→ NumericOverflow).
/// <see cref="ProofEngine"/>'s operand-interval read path folds flag-modifier lower
/// bounds (nonnegative / positive ⇒ ≥ 0) so that provably-safe computed fields
/// (e.g. <c>(A + B) / 2</c> over nonnegative operands) discharge clean via the
/// division interval-propagation that already exists. Uses
/// <see cref="Compiler.Compile"/> (full pipeline).
/// </summary>
public class BoundContainmentEmissionTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool HasOverflow(ImmutableArray<Diagnostic> diagnostics)
        => diagnostics.Any(d => d.Code == nameof(DiagnosticCode.NumericOverflow));

    [Fact]
    public void ComputedField_UnboundedOperand_EmitsOverflow()
    {
        // A unbounded above (only min 0). A * 10 is unbounded above, so it
        // cannot be proven <= 100. The obligation must be emitted and fail.
        var diagnostics = Compile("""
            precept T
            field A as integer min 0 default 0
            field C as integer min 0 max 100 <- A * 10
            """);

        HasOverflow(diagnostics).Should().BeTrue(
            because: "A is unbounded above, so A * 10 cannot be proven within max 100");
    }

    [Fact]
    public void ComputedField_DivisionReachDecimal_Clean()
    {
        // The supplier-quality-management shape: nonnegative operands with an
        // upper bound. Folding the nonnegative flag lower bound (0) into the
        // operand interval gives (Q + D) / 2 in [0, 100].
        var diagnostics = Compile("""
            precept T
            field Q as decimal nonnegative max 100 default 0
            field D as decimal nonnegative max 100 default 0
            field O as decimal nonnegative max 100 <- (Q + D) / 2
            """);

        HasOverflow(diagnostics).Should().BeFalse(
            because: "(Q + D) / 2 is provably within [0, 100] once nonnegative folds the lower bound");
    }

    [Fact]
    public void ComputedField_DivisionReachInteger_Clean()
    {
        var diagnostics = Compile("""
            precept T
            field Q as integer nonnegative max 100 default 0
            field D as integer nonnegative max 100 default 0
            field O as integer nonnegative max 100 <- (Q + D) / 2
            """);

        HasOverflow(diagnostics).Should().BeFalse(
            because: "(Q + D) / 2 is provably within [0, 100] once nonnegative folds the lower bound");
    }

    [Fact]
    public void ComputedField_DivisionReach_RegressionStillEmits()
    {
        // Operands bounded [0,200]. (A + B) / 2 in [0, 200] > 100 — must still emit.
        var diagnostics = Compile("""
            precept T
            field A as integer nonnegative max 200 default 0
            field B as integer nonnegative max 200 default 0
            field C as integer nonnegative max 100 <- (A + B) / 2
            """);

        HasOverflow(diagnostics).Should().BeTrue(
            because: "(A + B) / 2 can reach 200, exceeding max 100");
    }

    [Fact]
    public void ComputedField_ProvablySafeMultiply_Clean()
    {
        // A bounded [0,5]. A * 10 in [0,50] <= 100 — control that must stay clean.
        var diagnostics = Compile("""
            precept T
            field A as integer min 0 max 5 default 0
            field C as integer min 0 max 100 <- A * 10
            """);

        HasOverflow(diagnostics).Should().BeFalse(
            because: "A * 10 is provably within [0, 50], inside max 100");
    }

    [Fact]
    public void SetAction_NonnegativeTarget_NoSpuriousObligation_Clean()
    {
        // Hazard guard: the flag lower-bound fold must NOT leak into GetFieldBounds
        // or the set-action obligation-creation path. A nonnegative set-action
        // target creates no set-action containment obligation, so this stays clean.
        var diagnostics = Compile("""
            precept T
            field A as integer nonnegative default 0
            field X as integer nonnegative default 0

            state Open initial

            in Open modify A editable

            event E

            from Open on E
                -> set X = X + A
                -> no transition
            """);

        HasOverflow(diagnostics).Should().BeFalse(
            because: "no set-action containment obligation is created for a nonnegative target; the flag-fold must not leak into the set-action path");
    }
}
