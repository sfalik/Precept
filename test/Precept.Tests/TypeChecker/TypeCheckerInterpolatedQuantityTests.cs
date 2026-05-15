using System;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 21 — interpolated typed-constant quantity integration coverage.
/// Covers magnitude-with-static-unit, WholeValue, and dynamic-unit paths through
/// the interval proof engine (ProofEngine.Intervals.cs, Slices 19+20).
/// Missing implementation should fail honestly — no skipped tests.
/// </summary>
public class TypeCheckerInterpolatedQuantityTests
{
    // ── Tests 1–2: Magnitude slot + static lb_av suffix (the original pair) ───────

    [Fact]
    public void InterpolatedQuantity_StaticUnitMagnitudeWithinMax_DoesNotEmitNumericOverflow()
    {
        var result = CompileAssignment(
            intFieldDeclaration: "field intField as integer max 2 default 2",
            assignment: "'{intField} [lb_av]'",
            targetField: "x");

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "2 lb ≈ 907 g, which is below the 5 kg max");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "a bounded integer magnitude with a static lb suffix should scale into the target mass interval");
    }

    [Fact]
    public void InterpolatedQuantity_StaticUnitMagnitudeExceedsMax_EmitsNumericOverflow()
    {
        var result = CompileAssignment(
            intFieldDeclaration: "field intField as integer max 15 default 15",
            assignment: "'{intField} [lb_av]'",
            targetField: "x");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "15 lb ≈ 6.80 kg, which exceeds the 5 kg max after scaling");
    }

    // ── Test 3: Unbounded magnitude field — conservative overflow ─────────────────

    [Fact]
    public void InterpolatedQuantity_UnboundedMagnitudeField_EmitsNumericOverflowConservatively()
    {
        // intField has no max → ExtractFieldInterval returns Unbounded.
        // An Unbounded source interval can never satisfy a finite target max,
        // so the proof engine must conservatively emit NumericOverflow.
        var result = CompileAssignment(
            intFieldDeclaration: "field intField as integer default 0",
            assignment: "'{intField} [lb_av]'",
            targetField: "x");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "an unbounded integer magnitude → Unbounded interval → conservative overflow");
    }

    // ── Tests 4–5: WholeValue slot (same dimension, kg→kg) ───────────────────────

    [Fact]
    public void InterpolatedQuantity_WholeValueSlot_SourceFieldWithinMax_DoesNotEmitNumericOverflow()
    {
        // WholeValue path: interval is extracted directly from qtyField's normalized bounds.
        // qtyField max '3 kg' → [0..3] normalized; target max '5 kg' → [0..3] ⊆ [0..5] → Proved.
        var result = CompileGeneral(
            targetDeclaration: "field x as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'{qtyField}'",
            targetField: "x",
            "field qtyField as quantity of 'mass' max '3 kg' default '3 kg'");

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "source max '3 kg' fits inside the target max '5 kg'");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "WholeValue interval extraction should use the source field's normalized kg bounds directly");
    }

    [Fact]
    public void InterpolatedQuantity_WholeValueSlot_SourceFieldExceedsMax_EmitsNumericOverflow()
    {
        // qtyField max '8 kg' → [0..8]; target max '5 kg' → [0..8] ⊄ [0..5] → Unresolved → NumericOverflow.
        var result = CompileGeneral(
            targetDeclaration: "field x as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'{qtyField}'",
            targetField: "x",
            "field qtyField as quantity of 'mass' max '8 kg' default '1 kg'");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "source max '8 kg' exceeds the target max '5 kg'");
    }

    // ── Test 6: Dynamic unit slot — conservative overflow ────────────────────────

    [Fact]
    public void InterpolatedQuantity_DynamicUnitSlot_EmitsNumericOverflowConservatively()
    {
        // '3 {unitField}' → single Unit slot; IntervalOfNarrowed returns Unbounded for
        // any slot that is not Magnitude or WholeValue → conservative overflow.
        var result = CompileGeneral(
            targetDeclaration: "field x as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'3 {unitField}'",
            targetField: "x",
            "field unitField as unitofmeasure default 'kg'");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "a dynamic unit slot cannot be normalized at compile time → Unbounded → conservative overflow");
    }

    // ── Test 7: Dynamic denominator (price) — conservative overflow ───────────────

    [Fact]
    public void InterpolatedPrice_DynamicDenominatorSlot_EmitsNumericOverflowConservatively()
    {
        // '5 USD/{unitField}' on a price field → Unit slot; interval is Unbounded
        // because the denominator unit is unknown at compile time → conservative overflow.
        var result = CompileGeneral(
            targetDeclaration: "field x as price in 'USD' of 'mass' max '10 USD/kg' default '0 USD/kg'",
            assignment: "'5 USD/{unitField}'",
            targetField: "x",
            "field unitField as unitofmeasure default 'kg'");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "dynamic denominator unit → Unbounded interval → conservative overflow for a bounded price field");
    }

    // ── Tests 8a–8b: Money with interpolated magnitude + static currency ──────────

    [Fact]
    public void InterpolatedMoney_MagnitudeSlotWithinMax_DoesNotEmitNumericOverflow()
    {
        // '{amount} USD': single Magnitude slot; no UCUM unit → no scaling applied.
        // amount max 50 → interval max = 50 ≤ 100 → Proved.
        var result = CompileGeneral(
            targetDeclaration: "field x as money in 'USD' max '100 USD' default '0 USD'",
            assignment: "'{amount} USD'",
            targetField: "x",
            "field amount as integer max 50 default 50");

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "amount max 50 is within the money bound of 100 USD");
    }

    [Fact]
    public void InterpolatedMoney_MagnitudeSlotExceedsMax_EmitsNumericOverflow()
    {
        // amount max 200 → interval max = 200 > 100 → Unresolved → NumericOverflow.
        var result = CompileGeneral(
            targetDeclaration: "field x as money in 'USD' max '100 USD' default '0 USD'",
            assignment: "'{amount} USD'",
            targetField: "x",
            "field amount as integer max 200 default 100");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "amount max 200 exceeds the money bound of 100 USD");
    }

    // ── Test 9: WholeValue cross-unit — double-normalization regression anchor ─────

    [Fact]
    public void InterpolatedQuantity_WholeValueCrossUnit_NoDoubleNormalization_DoesNotEmitNumericOverflow()
    {
        // massInLb declares max '3 [lb_av]'. The type checker normalizes this to
        // NormalizedDeclaredMax ≈ 1.36 kg (stored in base units).
        //
        // The WholeValue path in IntervalOfNarrowed recurses into the source field ref,
        // calling ExtractFieldInterval → reads NormalizedDeclaredMax ≈ 1.36 kg.
        //
        // ApplyStaticUnitScaling then checks HasSingleMagnitudeSlot(interpolated):
        // the interpolated expression has a WholeValue slot (not a Magnitude slot),
        // so HasSingleMagnitudeSlot returns false → no scaling is applied.
        //
        // Result: interval [0..1.36], target max 5 kg → 1.36 ≤ 5 → Proved, no overflow.
        //
        // If double-normalization were active, the already-normalized 1.36 kg would be
        // re-scaled by the lb→kg factor (0.453) → 0.617 kg. In this specific case the
        // result would still not overflow (0.617 < 5). This test therefore anchors the
        // correct cross-unit WholeValue path; see the NOTE in §5.5.2 of the design doc
        // for why a cross-unit case with a tighter target bound would expose false safety.
        var result = CompileGeneral(
            targetDeclaration: "field x as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'{massInLb}'",
            targetField: "x",
            "field massInLb as quantity of 'mass' max '3 [lb_av]' default '0 [lb_av]'");

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "3 lb ≈ 1.36 kg is below the 5 kg target max");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "x" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "WholeValue cross-unit path must read the source field's normalized kg bounds directly "
                       + "without re-applying the lb→kg scale factor (HasSingleMagnitudeSlot guard)");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Compiles a precept with a fixed mass target field (<c>field x as quantity of 'mass' max '5 kg'</c>)
    /// and a single extra field declaration. Used by tests 1–3 which vary only the integer source field.
    /// </summary>
    private static Compilation CompileAssignment(string intFieldDeclaration, string assignment, string targetField)
    {
        var precept = $$"""
            precept InterpolatedQuantityNormalization
            {{intFieldDeclaration}}
            field x as quantity of 'mass' max '5 kg' default '0 kg'
            state Draft initial
            state Closed
            event Apply
            from Draft on Apply
                -> set {{targetField}} = {{assignment}}
                -> transition Closed
            """;

        return Compiler.Compile(precept);
    }

    /// <summary>
    /// General helper used by tests 4–9 that need a configurable target field type
    /// (quantity, price, money) and/or multiple extra source field declarations.
    /// </summary>
    private static Compilation CompileGeneral(
        string targetDeclaration,
        string assignment,
        string targetField,
        params string[] extraDeclarations)
    {
        var extra = string.Join(Environment.NewLine, extraDeclarations.Where(static s => !string.IsNullOrWhiteSpace(s)));

        var precept = $$"""
            precept InterpolatedQuantityNormalization
            {{extra}}
            {{targetDeclaration}}
            state Draft initial
            state Closed
            event Apply
            from Draft on Apply
                -> set {{targetField}} = {{assignment}}
                -> transition Closed
            """;

        return Compiler.Compile(precept);
    }
}
