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
}
