using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Slice 11B — Temporal Price Denominator Type System Extension.
/// Covers:
///   A. ExtractQualifiers temporal routing for price (of 'time'/'date' → TemporalDimension)
///   B. ExtractComparableValue temporal arms (TemporalUnit, TemporalDimension)
///   C. ResolveQualifierOnAxis Dimension→TemporalDimension fallback
///   D. ImpliedQualifiers on TypeMeta + Duration entry
/// </summary>
public class Slice11B_TemporalPriceDenominatorTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  A. ExtractQualifiers — temporal routing for price
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Price_Of_Time_CompilesClean_And_Stores_TemporalDimension_Time()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field Rate as price in 'USD' of 'time'
            state Open initial
            """);

        var field = index.FieldsByName["Rate"];
        var temporal = field.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.TemporalDimension>()
            .ToList();
        temporal.Should().ContainSingle(because: "price of 'time' should store TemporalDimension");
        temporal[0].Value.Should().Be(PeriodDimension.Time);
    }

    [Fact]
    public void Price_Of_Date_CompilesClean_And_Stores_TemporalDimension_Date()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field Rate as price in 'USD' of 'date'
            state Open initial
            """);

        var field = index.FieldsByName["Rate"];
        var temporal = field.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.TemporalDimension>()
            .ToList();
        temporal.Should().ContainSingle(because: "price of 'date' should store TemporalDimension");
        temporal[0].Value.Should().Be(PeriodDimension.Date);
    }

    [Fact]
    public void Price_Of_Mass_CompilesClean_And_Stores_Physical_Dimension()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field Rate as price in 'USD' of 'mass'
            state Open initial
            """);

        var field = index.FieldsByName["Rate"];
        var physical = field.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.Dimension>()
            .ToList();
        physical.Should().ContainSingle(because: "price of 'mass' should still store physical Dimension");
        physical[0].DimensionName.Should().Be("mass");
    }

    [Fact]
    public void Quantity_Of_Time_CompilesClean_And_Stores_Physical_Dimension()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field Qty as quantity of 'time'
            state Open initial
            """);

        var field = index.FieldsByName["Qty"];
        var physical = field.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.Dimension>()
            .ToList();
        physical.Should().ContainSingle(because: "quantity of 'time' should now resolve through the physical dimension catalog");
        physical[0].DimensionName.Should().Be("time");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  B. ExtractComparableValue — temporal arms
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractComparableValue_TemporalUnit_Returns_UnitName()
    {
        var qualifier = new DeclaredQualifierMeta.TemporalUnit("hours", PeriodDimension.Time);
        ProofEngine.ExtractComparableValueForTest(qualifier)
            .Should().Be("hours");
    }

    [Fact]
    public void ExtractComparableValue_TemporalDimension_Time_Returns_Time_String()
    {
        var qualifier = new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Time);
        ProofEngine.ExtractComparableValueForTest(qualifier)
            .Should().Be("time");
    }

    [Fact]
    public void ExtractComparableValue_TemporalDimension_Date_Returns_Date_String()
    {
        var qualifier = new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Date);
        ProofEngine.ExtractComparableValueForTest(qualifier)
            .Should().Be("date");
    }

    [Fact]
    public void ExtractComparableValue_TemporalDimension_Any_Returns_Null()
    {
        // PeriodDimension.Any cannot satisfy chain comparisons (locked decision).
        var qualifier = new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Any);
        ProofEngine.ExtractComparableValueForTest(qualifier)
            .Should().BeNull(because: "PeriodDimension.Any cannot satisfy chain proofs");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  D. ImpliedQualifiers — Duration entry carries TemporalDimension(Time)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Duration_TypeMeta_Has_Implied_TemporalDimension_Time()
    {
        var typeMeta = Types.GetMeta(TypeKind.Duration);
        var implied = typeMeta.ImpliedQualifiers
            .OfType<DeclaredQualifierMeta.TemporalDimension>()
            .ToList();
        implied.Should().ContainSingle(because: "duration carries implied TemporalDimension");
        implied[0].Value.Should().Be(PeriodDimension.Time);
        implied[0].Origin.Should().Be(QualifierOrigin.Baseline);
    }

    [Fact]
    public void ResolveQualifierOnAxis_DurationField_TemporalDimension_Returns_ImpliedTime()
    {
        // Verifies the Dimension→TemporalDimension fallback + implied qualifier path together.
        // A bare duration field (no explicit qualifiers) should resolve to TemporalDimension(Time)
        // on the TemporalDimension axis via implied qualifiers.
        var result = ProofEngine.GetImpliedQualifierOnAxis(TypeKind.Duration, QualifierAxis.TemporalDimension);
        result.Should().BeOfType<DeclaredQualifierMeta.TemporalDimension>(
            because: "Duration has an implied TemporalDimension qualifier");
        var td = (DeclaredQualifierMeta.TemporalDimension)result!;
        td.Value.Should().Be(PeriodDimension.Time);
        td.Origin.Should().Be(QualifierOrigin.Baseline);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression — existing behavior must be unchanged
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Regression_Period_Of_Time_Still_Stores_TemporalDimension()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field P as period of 'time'
            state Open initial
            """);

        var field = index.FieldsByName["P"];
        field.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.TemporalDimension>()
            .Should().ContainSingle()
            .Which.Value.Should().Be(PeriodDimension.Time);
    }

    [Fact]
    public void Regression_Quantity_Of_Mass_Still_Stores_Physical_Dimension()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field Qty as quantity in 'kg' default '0 kg'
            state Open initial
            """);

        var field = index.FieldsByName["Qty"];
        field.DeclaredQualifiers
            .OfType<DeclaredQualifierMeta.Unit>()
            .Should().ContainSingle()
            .Which.UnitCode.Should().Be("kg");
    }

    [Fact]
    public void Regression_Non_Duration_Types_Have_No_ImpliedQualifiers()
    {
        // Only duration should have implied qualifiers from Slice 11B.
        foreach (var kind in new[] { TypeKind.Period, TypeKind.Money, TypeKind.Price, TypeKind.Quantity })
        {
            Types.GetMeta(kind).ImpliedQualifiers
                .Should().BeEmpty(because: $"{kind} should not have implied qualifiers");
        }
    }
}
