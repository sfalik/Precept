using System;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 17 — quantity normalization integration coverage.
/// These tests are intentionally allowed to stay red until the normalization path lands.
/// </summary>
public class TypeCheckerQuantityNormalizationTests
{
    [Fact]
    public void QuantityBound_CrossUnitWithinMax_DoesNotEmitNumericOverflow()
    {
        var result = CompileAssignment(
            targetDeclaration: "field weight as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'6 [lb_av]'",
            targetField: "weight");

        NumericOverflowDiagnostics(result)
            .Should().BeEmpty(because: "6 lb ≈ 2.72 kg, which is below the 5 kg max");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "weight" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "cross-unit comparison should normalize the assigned literal before checking the bound");
    }

    [Fact]
    public void QuantityBound_CrossUnitExceedsMax_EmitsNumericOverflow()
    {
        var result = CompileAssignment(
            targetDeclaration: "field weight as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'12 [lb_av]'",
            targetField: "weight");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "12 lb ≈ 5.44 kg, which exceeds the 5 kg max");
    }

    [Fact]
    public void QuantityBound_SameUnitExceedsMax_StillEmitsNumericOverflow()
    {
        var result = CompileAssignment(
            targetDeclaration: "field weight as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'6 kg'",
            targetField: "weight");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "same-unit overflow behavior must remain unchanged");
    }

    [Fact]
    public void PriceBound_CrossUnitDenominatorNormalization_EmitsNumericOverflow()
    {
        var result = CompileAssignment(
            targetDeclaration: "field unitPrice as price in 'USD' of 'mass' max '10 USD/kg' default '0 USD/kg'",
            assignment: "'6 USD/[lb_av]'",
            targetField: "unitPrice");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "6 USD/lb ≈ 13.23 USD/kg, which exceeds the 10 USD/kg max after normalization");
    }

    [Fact]
    public void MoneyBound_Overflow_DoesNotDependOnUnitNormalization()
    {
        var result = CompileAssignment(
            targetDeclaration: "field amount as money in 'USD' max '100 USD' default '0 USD'",
            assignment: "'200 USD'",
            targetField: "amount");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "currencies are not UCUM-normalizable; plain money overflow must still be enforced");
    }

    [Fact]
    public void QuantityBound_CrossDimensionAssignment_IsBlockedByDimensionCheck()
    {
        var result = CompileAssignment(
            targetDeclaration: "field weight as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'3 m'",
            targetField: "weight");

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.DimensionCategoryMismatch),
            because: "a length literal assigned to a mass field should fail qualifier compatibility before normalization logic matters");
        NumericOverflowDiagnostics(result)
            .Should().BeEmpty(because: "cross-dimension rejection should not be reported as a normalization overflow");
    }

    [Fact]
    public void QuantityBound_WholeValueInterpolation_UsesSourceQuantityIntervalWithoutDoubleNormalization()
    {
        var result = CompileAssignment(
            targetDeclaration: "field weight as quantity of 'mass' max '5 kg' default '0 kg'",
            assignment: "'{qtyField}'",
            targetField: "weight",
            "field qtyField as quantity of 'mass' max '3 kg' default '3 kg'");

        NumericOverflowDiagnostics(result)
            .Should().BeEmpty(because: "the WholeValue interpolation should reuse qtyField's bounded quantity interval");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "weight" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "WholeValue interpolation should extract the source field interval directly, without double-normalizing it");
    }

    private static Compilation CompileAssignment(string targetDeclaration, string assignment, string targetField, params string[] extraDeclarations)
    {
        var extra = string.Join(Environment.NewLine, extraDeclarations.Where(static s => !string.IsNullOrWhiteSpace(s)));

        var precept = $$"""
            precept QuantityNormalization
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

    private static Diagnostic[] NumericOverflowDiagnostics(Compilation result) =>
        result.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow)).ToArray();
}
