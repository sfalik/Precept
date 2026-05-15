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
/// Missing implementation should fail honestly at runtime, not via skipped tests.
/// </summary>
public class TypeCheckerInterpolatedQuantityTests
{
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
}
