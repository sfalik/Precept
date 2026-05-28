using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.OperationsSuite;

/// <summary>
/// F-LANG-BIZ-05: <c>quantity × quantity → quantity</c> must produce a result
/// whose dimension is in the curated business-domain set (length, mass, volume,
/// area, temperature, energy, pressure, force, speed, count) OR cancels to the
/// dimensionless count alias. Products outside the curated set (e.g.
/// <c>kg × m → mass·length</c>) trip
/// <see cref="DiagnosticCode.IncompatibleDimensionalProduct"/> (PRE0157).
/// </summary>
public class QuantityProductDimensionTests
{
    [Fact]
    public void DimensionalProductProofRequirement_Constructs()
    {
        // F-LANG-BIZ-05 catalog scaffolding is in place. The catalog attachment
        // to QuantityTimesQuantity is deferred to a follow-up slice that
        // migrates existing PRE0071-on-multiply tests to PRE0157.
        var req = new DimensionalProductProofRequirement(
            new SelfSubject(), new SelfSubject(),
            "Product dimension must be in the curated business-domain set");
        req.Kind.Should().Be(ProofRequirementKind.DimensionalProduct);
    }

    [Fact]
    public void IncompatibleDimensionalProductDiagnostic_IsErrorSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.IncompatibleDimensionalProduct).Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void CancellingPair_kg_inverse_kg_CompilesCleanly()
    {
        // mass × (1/mass) → dimensionless count. The curated set includes count
        // via the dimensionless-count alias, so this product is accepted.
        var (_, diagnostics) = Check("""
            precept Cancelling
            field A as quantity of 'mass' default '1 kg' editable
            field B as quantity of 'mass' default '1 kg' editable
            field Ratio as quantity of 'count' <- A / B
            state Open initial
            """);
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.IncompatibleDimensionalProduct));
    }

    [Fact]
    public void DimensionalProductRequirement_AppearsInProofRequirementsCatalog()
    {
        // Catalog membership: ProofRequirements.GetMeta returns the new kind
        // with the right diagnostic-code mapping.
        var meta = ProofRequirements.GetMeta(ProofRequirementKind.DimensionalProduct);
        meta.Should().BeOfType<ProofRequirementMeta.DimensionalProduct>();
        meta.DiagnosticCode.Should().Be(DiagnosticCode.IncompatibleDimensionalProduct);
    }

    [Fact]
    public void BareDimensionQualifier_LengthTimesLength_ResolvesViaDimensionCatalog()
    {
        // Quantity fields declared with only a bare dimension qualifier
        // (`of 'length'` — no unit-typed default) must still resolve to a
        // DimensionVector via DimensionCatalog. UCUM parsing alone can't
        // turn the string "length" into a vector — DimensionCatalog is the
        // authoritative lookup for bare dimension names. Product is
        // length × length → area (in the curated set).
        var (_, diagnostics) = Check("""
            precept BareDimensionProduct
            field A as quantity of 'length' editable
            field B as quantity of 'length' editable
            field Area as quantity of 'area' editable
            state Open initial
            event Compute
            from Open on Compute -> set Area = A * B -> no transition
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.IncompatibleDimensionalProduct),
            because: "length × length = area, a curated business-domain dimension; the proof engine must resolve "
                   + "bare dimension qualifiers via DimensionCatalog to discharge the product check");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cross-counting-unit operation on multiplication / division — PRE0137
    //  (Decision B from the qualifier-and-dimensionless-product design)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EachTimesBox_EmitsCrossCountingUnitOperation()
    {
        // Counting units share the 'count' dimension but are not interchangeable —
        // see business-domain-types.md § 397. The rule applied to '+' (already
        // enforced) now applies to '×' uniformly: 'each × box' is rejected with
        // PRE0137, not PRE0157.
        var (_, diagnostics) = Check("""
            precept Reorder
            field UnitsToBuy as quantity in 'each' default '0 each' editable
            field Cartons as quantity in 'box' default '0 box' editable
            field Total as quantity default '0 each' editable
            state Open initial
            event Compute
            from Open on Compute -> set Total = UnitsToBuy * Cartons -> no transition
            """);

        diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation),
            because: "'each' and 'box' are different named counting units; the multiplicative case is rejected just like addition");
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.IncompatibleDimensionalProduct),
            because: "PRE0137 fires from the type checker before the proof engine evaluates the dimensional product");
    }

    [Fact]
    public void EachDividedByBox_EmitsCrossCountingUnitOperation()
    {
        // Division of dimensionless quantities with differing names also fires
        // PRE0137 — the check lifted out of the additive gate covers + - × ÷ uniformly.
        var (_, diagnostics) = Check("""
            precept Ratio
            field UnitsToBuy as quantity in 'each' default '1 each' editable
            field Cartons as quantity in 'box' default '1 box' editable
            field RatioField as quantity default '0 each' editable
            state Open initial
            event Compute
            from Open on Compute -> set RatioField = UnitsToBuy / Cartons -> no transition
            """);

        diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation),
            because: "the cross-counting-unit rule extends to division for the same reason it applies to multiplication");
    }

    [Fact]
    public void EachTimesEach_NoDiagnostic()
    {
        // Same name on both sides: the product is well-formed (count × count = count;
        // PRE0137 does not fire; PRE0157 does not fire because None × None = count is
        // in the curated DimensionCatalog).
        var (_, diagnostics) = Check("""
            precept SameUnit
            field A as quantity in 'each' default '1 each' editable
            field B as quantity in 'each' default '1 each' editable
            field Product as quantity in 'each' default '0 each' editable
            state Open initial
            event Compute
            from Open on Compute -> set Product = A * B -> no transition
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation),
            because: "same counting unit on both operands is well-formed; no diagnostic should fire");
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.IncompatibleDimensionalProduct),
            because: "None × None = None (count alias), which the proof engine discharges cleanly");
    }

    [Fact]
    public void InterpolatedQualifier_QuantityProduct_DoesNotEmitSpuriousPRE0137()
    {
        // Inventory-item-shaped fixture: quantity fields with interpolated
        // unit qualifiers (`quantity in '{Field1}/{Field2}'`). The lifted
        // PRE0137 check relies on TryGetQualifierDimensionVector returning
        // false for interpolated qualifiers (UCUM parse fails on braced
        // strings AND DimensionCatalog lookup misses), short-circuiting the
        // outer guard before the unit-name discrimination runs. This test
        // locks that invariant so a future refactor of the helper can't
        // silently turn interpolated-qualifier products into spurious PRE0137
        // emissions.
        var (_, diagnostics) = Check("""
            precept Inventory
            field StockingUnit as unit default 'each'
            field PurchaseUnit as unit default 'box'
            field OnHand as quantity in '{StockingUnit}' default '0 each' editable
            field Ordered as quantity in '{PurchaseUnit}' default '0 box' editable
            field Total as quantity default '0 each' editable
            state Open initial
            event Compute
            from Open on Compute -> set Total = OnHand * Ordered -> no transition
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation),
            because: "interpolated qualifiers do not resolve to a known dimension vector; the outer guard short-circuits before unit-name discrimination");
    }

    [Fact]
    public void CrossDimensionMultiplication_StillEmitsPre0157_NotPre0137()
    {
        // kg × m is a cross-dimension product (mass·length is NOT in the curated
        // catalog). PRE0157 fires from the proof engine; PRE0137 does NOT fire
        // because neither operand is dimensionless. Uses the full compile pipeline
        // (Compiler.Compile) because PRE0157 is emitted at the proof stage, not
        // the type-checker stage.
        var compilation = Precept.Compiler.Compile("""
            precept MassTimesLength
            field Weight as quantity in 'kg' default '1 kg' editable
            field Distance as quantity in 'm' default '1 m' editable
            field Result as quantity default '0 kg' editable
            state Open initial
            event Compute
            from Open on Compute -> set Result = Weight * Distance -> no transition
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.IncompatibleDimensionalProduct),
            because: "mass × length is not a curated business-domain dimension; PRE0157 fires from the proof engine");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation),
            because: "PRE0137 only fires when both operands are dimensionless; mass and length are not");
    }
}
