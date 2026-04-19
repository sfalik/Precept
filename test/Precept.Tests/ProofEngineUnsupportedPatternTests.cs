using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Soundness anchors — tests that confirm C93 correctly fires on expression patterns
/// the unified proof engine <b>cannot</b> prove safe.
///
/// <para>
/// These tests are not regression tests; they are structural constraints on the proof
/// engine's completeness boundary. A proof engine that is sound but incomplete will
/// emit C93 for the patterns below, because:
/// <list type="bullet">
///   <item>
///     <b>Non-linear multiplication</b> — <c>A * B - C</c> cannot be expressed as a
///     <see cref="LinearForm"/> (product of two non-constant fields is not affine).
///   </item>
///   <item>
///     <b>Function opacity</b> — <c>abs(X) - B</c> is non-linear;
///     <see cref="LinearForm.TryNormalize"/> returns <c>null</c> for function calls.
///   </item>
///   <item>
///     <b>Inequality without ordering</b> — <c>rule A != B</c> proves nonzero
///     but does NOT provide a directional bound; <c>A - B</c> could be positive or negative.
///     The relational injection only handles <c>&gt;</c> and <c>&gt;=</c>.
///   </item>
///   <item>
///     <b>Modulo divisor</b> — <c>A % B</c> is not a linear expression; the divisor
///     is not normalizable; interval arithmetic alone does not exclude zero.
///   </item>
/// </list>
/// </para>
///
/// <para>
/// If any of these tests start PASSING (C93 suppressed) after a change to the proof engine,
/// that is a bug UNLESS the underlying semantics have been deliberately extended and reviewed.
/// </para>
/// </summary>
public class ProofEngineUnsupportedPatternTests
{
    [Fact]
    public void Check_NonLinear_ProductDivisor_WithRule_EmitsC93()
    {
        // Divisor: A * B - C  with  rule A * B > C because "product exceeds C".
        // A * B is the product of two non-constant fields → non-linear → not normalizable.
        // LinearForm.TryNormalize(A * B) returns null.
        // Even though the rule semantically implies A*B - C > 0, the engine cannot extract
        // a LinearForm key from the rule → no relational fact stored → C93 fires.
        //
        // Soundness guarantee: the engine must not fabricate a proof from non-linear rules.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 2
            field C as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when A * B > C -> set Y = Y / (A * B - C) -> no transition
            from Open on Go -> reject "not proved"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "non-linear divisor A*B-C cannot be proven nonzero by the LinearForm engine");
    }

    [Fact]
    public void Check_FunctionOpacity_AbsMinusB_WithRule_EmitsC93()
    {
        // Divisor: abs(X) - B  with guard  when abs(X) > B.
        // abs(X) is a function call — LinearForm.TryNormalize returns null.
        // The guard cannot provide a relational fact for the abs(X) - B form.
        // C93 fires: the engine is opaque to function call results.
        //
        // Soundness guarantee: function calls must not be treated as transparent.
        const string dsl = """
            precept Test
            field X as number default 5
            field B as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when abs(X) > B -> set Y = Y / (abs(X) - B) -> no transition
            from Open on Go -> reject "not proved"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "function call abs(X) is opaque to LinearForm; divisor proof not possible");
    }

    [Fact]
    public void Check_InequalityWithoutOrdering_NotEq_EmitsC93()
    {
        // Divisor: A - B  with  rule A != B because "distinct".
        // rule A != B proves that A and B are distinct, hence A - B != 0.
        // HOWEVER: the proof engine's relational injection only handles > and >=.
        // The != operator does NOT produce a relational bound usable for interval lookup.
        // C93 must fire because there is no directional interval to intersect with.
        //
        // Design note: != is NOT sound for interval intersection because A - B could be
        // positive or negative, and the engine cannot determine which.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A != B because "A and B are distinct"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "rule A != B does not provide a directional interval bound; C93 must fire");
    }

    [Fact]
    public void Check_Modulo_DivisorModB_EmitsC93()
    {
        // Divisor: A % B — the result of a modulo is non-normalizable.
        // A % B ∈ [0, |B|) when A ≥ 0 and B > 0, which includes 0 → not proven nonzero.
        // Without knowing both A ≥ 0 and B > 0 AND that the result is not zero,
        // the engine cannot prove the divisor of the outer expression is safe.
        // C93 fires: (Y / (A % B)) has an unproven inner divisor.
        //
        // Note: this tests the INNER divisor A % B used in a nested position.
        const string dsl = """
            precept Test
            field A as number default 7
            field B as number default 3
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A % B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "A % B is not proven nonzero; the outer division has an unproven divisor");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
