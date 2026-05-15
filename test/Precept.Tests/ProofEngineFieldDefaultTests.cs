using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Slice 25 — field-default proof coverage for interpolated typed constants.
/// Verifies that <c>CollectDefaultObligations</c> generates
/// <see cref="IntervalContainmentProofRequirement"/> obligations for fields whose
/// <see cref="TypedField.DefaultExpression"/> is an <see cref="InterpolatedTypedConstant"/>,
/// and that those obligations discharge (or not) correctly against the field's declared bounds.
/// </summary>
public class ProofEngineFieldDefaultTests
{
    // ── Test 1: magnitude within max (same unit) — Proved ────────────────────────────

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_MagnitudeWithinMax_CompilesClean()
    {
        // n max 3 → interval max = 3 * 1000 g/kg = 3000 g.
        // x max '5 kg' → NormalizedDeclaredMax = 5000 g.
        // 3000 ≤ 5000 → obligation Proved → no NumericOverflow.
        const string precept = """
            precept FieldDefaultBoundsOk
            field n as integer max 3 default 0
            field x as quantity in 'kg' max '5 kg' default '{n} kg'
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "n max 3 kg ≤ field max 5 kg — default is within bounds");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" }
                     && o.Context is FieldDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "the interpolated default '{n} kg' with n max 3 fits inside the 5 kg bound");
    }

    // ── Test 2: magnitude exceeds max (same unit) — NumericOverflow ──────────────────

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_MagnitudeExceedsMax_EmitsNumericOverflow()
    {
        // n max 10 → interval max = 10 * 1000 = 10000 g.
        // x max '5 kg' → NormalizedDeclaredMax = 5000 g.
        // 10000 > 5000 → obligation Unresolved → NumericOverflow.
        const string precept = """
            precept FieldDefaultBoundsExceeded
            field n as integer max 10 default 0
            field x as quantity in 'kg' max '5 kg' default '{n} kg'
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "n max 10 kg exceeds the field max of 5 kg — default can overflow");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" }
                     && o.Context is FieldDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Unresolved,
                because: "the default interval [−∞..10000 g] is not contained by the bound [−∞..5000 g]");
    }

    // ── Test 3: cross-unit default within bounds — Proved ────────────────────────────

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_CrossUnit_WithinBounds_CompilesClean()
    {
        // n max 2 → '{n} [lb_av]' → interval max = 2 * 453.59237 ≈ 907 g.
        // x max '5 kg' → NormalizedDeclaredMax = 5000 g.
        // 907 ≤ 5000 → Proved.
        const string precept = """
            precept FieldDefaultCrossUnitOk
            field n as integer max 2 default 0
            field x as quantity in 'kg' max '5 kg' default '{n} [lb_av]'
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "2 lb ≈ 907 g is well within the 5 kg max");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" }
                     && o.Context is FieldDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "2 lb ≈ 907 g fits inside the 5 kg bound");
    }

    // ── Test 4: cross-unit default exceeds bounds — NumericOverflow ──────────────────

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_CrossUnit_ExceedsBounds_EmitsNumericOverflow()
    {
        // n max 10 → '{n} [lb_av]' → interval max = 10 * 453.59237 ≈ 4535 g.
        // x max '2 kg' → NormalizedDeclaredMax = 2000 g.
        // 4535 > 2000 → Unresolved → NumericOverflow.
        const string precept = """
            precept FieldDefaultCrossUnitExceeded
            field n as integer max 10 default 0
            field x as quantity in 'kg' max '2 kg' default '{n} [lb_av]'
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "10 lb ≈ 4535 g exceeds the 2 kg max (2000 g)");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" }
                     && o.Context is FieldDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Unresolved,
                because: "10 lb ≈ 4535 g is not contained by the 2000 g bound");
    }

    // ── Test 5: interpolated default on unbounded field — no obligation ───────────────

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_NoBounds_NoObligation()
    {
        // x has no max (unbounded) → CollectDefaultObligations skips it → no FieldDefaultContext obligation.
        const string precept = """
            precept FieldDefaultNoBounds
            field n as integer max 3 default 0
            field x as quantity in 'kg' default '{n} kg'
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "x has no declared max — no bound to violate");

        result.Proof.Obligations
            .Where(o => o.Context is FieldDefaultContext fdc && fdc.Field.Name == "x")
            .Should().BeEmpty(because: "unbounded fields generate no default-proof obligations");
    }

    // ── Test 6: FoldValue Part A — fully-static StaticMagnitude, ensures satisfied ──

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_StaticMagnitude_EnsuresSatisfied()
    {
        // '3.5 {unit}' is Q3 form (numeric text + unit hole): TypeChecker sets StaticMagnitude=3.5,
        // StaticQualifier=null (dynamic unit). FoldValue Part A: case null → return mag = 3.5.
        // '5.0 {unit}' → StaticMagnitude=5.0, FoldValue Part A → 5.0.
        // CheckInitialStateSatisfiability: defaults["x"]=3.5, defaults["y"]=5.0.
        // ensure x <= y → 3.5 ≤ 5.0 → true → IsSatisfiable, no UnsatisfiableInitialState.
        const string precept = """
            precept StaticMagnitudeFoldPass
            field unit as unitofmeasure default 'kg'
            field x as quantity of 'mass' default '3.5 {unit}'
            field y as quantity of 'mass' default '5.0 {unit}'
            state Active initial
            in Active ensure x <= y because "static magnitudes fold: 3.5 ≤ 5.0"
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.UnsatisfiableInitialState))
            .Should().BeEmpty(because: "FoldValue Part A: x folds to 3.5 and y folds to 5.0 — x ≤ y holds");
    }

    // ── Test 7: FoldValue Part A — fully-static StaticMagnitude, ensures violated ───

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_StaticMagnitude_EnsuresViolated()
    {
        // '5.0 {unit}' → StaticMagnitude=5.0, FoldValue Part A → 5.0 (defaults["x"]=5.0).
        // '3.5 {unit}' → StaticMagnitude=3.5, FoldValue Part A → 3.5 (defaults["y"]=3.5).
        // ensure x <= y → 5.0 ≤ 3.5 → false → UnsatisfiableInitialState.
        const string precept = """
            precept StaticMagnitudeFoldFail
            field unit as unitofmeasure default 'kg'
            field x as quantity of 'mass' default '5.0 {unit}'
            field y as quantity of 'mass' default '3.5 {unit}'
            state Active initial
            in Active ensure x <= y because "static magnitudes fold: 5.0 > 3.5 → violated"
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.UnsatisfiableInitialState),
            because: "FoldValue Part A: x folds to 5.0 and y folds to 3.5 — x ≤ y is false → initial state unsatisfiable");
    }

    // ── Test 8: ordering sensitivity — forward reference degrades gracefully ────────

    [Fact]
    public void FieldDefault_InterpolatedTypedConstant_ForwardReference_DegradeGracefully()
    {
        // x uses '{n} kg' but n is declared AFTER x (forward reference).
        //
        // Ordering sensitivity in CheckInitialStateSatisfiability: when x is processed,
        // n is not yet in the accumulated defaults dict → FoldValue returns UnknownSentinel
        // → x is added to unfoldable. This is graceful degradation: ensures that reference
        // x are silently skipped rather than producing a false UnsatisfiableInitialState.
        //
        // CollectDefaultObligations is NOT affected by declaration order: it derives intervals
        // via IntervalOf (field-declared bounds), not the accumulated defaults dict.
        const string precept = """
            precept FieldDefaultForwardRef
            field x as quantity of 'mass' max '5 kg' default '{n} kg'
            field n as integer max 3 default 0
            state Active initial
            in Active ensure x <= x because "trivially true — verifies no false positive from forward ref"
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "no overflow — forward ref degrades gracefully, no false positive");

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.UnsatisfiableInitialState))
            .Should().BeEmpty(because: "x <= x is tautologically true; forward-ref unfoldability must not produce a false UnsatisfiableInitialState");
    }
}
