using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Partition-aware <c>dimension</c> typed-constant validation. A <c>dimension</c> typed
/// constant is partitioned by the type it is observed on: a physical measurement
/// (quantity / unitofmeasure / price) names a UCUM family; a <c>period</c> names a
/// temporal category (date / time / datetime). A value drawn from the wrong partition
/// is a compile error. See business-domain-types.md § dimension (partitioned registry).
/// </summary>
public class DimensionPartitionValidationTests
{
    private static string GuardedPrecept(string fieldType, string accessorOwner, string literal) => $$"""
        precept Widget
        field {{accessorOwner}} as {{fieldType}}
        state Open initial
        state Done
        event Go
        from Open on Go when {{accessorOwner}}.dimension == '{{literal}}'
            -> transition Done
        """;

    // ── Temporal partition: period.dimension accepts date/time/datetime ──────

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("datetime")]
    public void PeriodDimensionComparison_TemporalPartitionValue_Compiles(string literal)
    {
        // period.dimension validates against the temporal partition, so a temporal
        // category compiles where the UCUM partition would have rejected it.
        TypeCheckerTestHelpers.CheckExpectingClean(GuardedPrecept("period", "P", literal));
    }

    [Fact]
    public void PeriodDimensionComparison_UcumPartitionValue_IsCrossPartitionError()
    {
        // 'mass' is a UCUM family — not in the temporal partition. Reusing
        // InvalidDimensionString (the dimension-not-recognized diagnostic) for the
        // cross-partition rejection.
        TypeCheckerTestHelpers.CheckExpectingError(
            GuardedPrecept("period", "P", "mass"),
            DiagnosticCode.InvalidDimensionString);
    }

    // ── UCUM partition: quantity/uom/price.dimension accepts UCUM families ───

    [Theory]
    [InlineData("quantity", "Q")]
    [InlineData("unitofmeasure", "U")]
    public void PhysicalDimensionComparison_UcumPartitionValue_Compiles(string fieldType, string owner)
    {
        TypeCheckerTestHelpers.CheckExpectingClean(GuardedPrecept(fieldType, owner, "mass"));
    }

    [Theory]
    [InlineData("quantity", "Q")]
    [InlineData("unitofmeasure", "U")]
    public void PhysicalDimensionComparison_TemporalPartitionValue_IsCrossPartitionError(string fieldType, string owner)
    {
        // quantity.dimension == 'date' is cross-partition: 'date' is a temporal category,
        // not a UCUM physical-dimension family.
        TypeCheckerTestHelpers.CheckExpectingError(
            GuardedPrecept(fieldType, owner, "date"),
            DiagnosticCode.InvalidDimensionString);
    }

    [Fact]
    public void PriceDimensionComparison_UcumPartitionValue_Compiles()
    {
        var precept = """
            precept Widget
            field Pr as price in 'USD/kg'
            state Open initial
            state Done
            event Go
            from Open on Go when Pr.dimension == 'mass'
                -> transition Done
            """;
        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ── 'time' collision is disambiguated by partition ──────────────────────

    [Fact]
    public void TimeSpelling_ResolvesPerPartition()
    {
        // 'time' exists in BOTH registries (UCUM physical time + temporal time). The
        // partition disambiguates: period accepts it as a temporal category, quantity
        // accepts it as the UCUM physical-time family. Both compile, by different routes.
        TypeCheckerTestHelpers.CheckExpectingClean(GuardedPrecept("period", "P", "time"));
        TypeCheckerTestHelpers.CheckExpectingClean(GuardedPrecept("quantity", "Q", "time"));
    }

    // ── of-constraint behavior is unchanged ─────────────────────────────────

    [Fact]
    public void PeriodOfDatetime_StillRejected()
    {
        // datetime is return-only: a both-spanning category cannot be a declarable
        // constraint. The of-path rejection must be unaffected by the comparison-path
        // partition validation.
        var precept = """
            precept Widget
            field Offset as period of 'datetime'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidTemporalDimensionString);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    public void PeriodOfDateOrTime_StillAccepted(string literal)
    {
        var precept = $"""
            precept Widget
            field Offset as period of '{literal}'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ── catalog metadata wiring ─────────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Period, "dimension", DimensionPartition.Temporal)]
    [InlineData(TypeKind.Quantity, "dimension", DimensionPartition.Ucum)]
    [InlineData(TypeKind.UnitOfMeasure, "dimension", DimensionPartition.Ucum)]
    [InlineData(TypeKind.Price, "dimension", DimensionPartition.Ucum)]
    public void DimensionAccessor_DeclaresPartition(TypeKind type, string accessorName, DimensionPartition expected)
    {
        var accessor = Types.GetMeta(type).Accessors!
            .OfType<FixedReturnAccessor>()
            .Single(a => a.Name == accessorName);

        accessor.DimensionPartition.Should().Be(expected);
    }

    [Theory]
    [InlineData(TypeKind.Period, "dimension", QualifierAxis.TemporalDimension)]
    [InlineData(TypeKind.Quantity, "dimension", QualifierAxis.Dimension)]
    [InlineData(TypeKind.UnitOfMeasure, "dimension", QualifierAxis.Dimension)]
    [InlineData(TypeKind.Price, "dimension", QualifierAxis.Dimension)]
    public void DimensionAccessor_DeclaresReturnsQualifier(TypeKind type, string accessorName, QualifierAxis expected)
    {
        var accessor = Types.GetMeta(type).Accessors!
            .OfType<FixedReturnAccessor>()
            .Single(a => a.Name == accessorName);

        accessor.ReturnsQualifier.Should().Be(expected);
    }

    [Fact]
    public void PeriodBasisAccessor_StaysAxisNone()
    {
        // .basis returns String and must NOT carry a qualifier axis — it is a discrete
        // string accessor, not a qualifier-axis accessor.
        var basis = Types.GetMeta(TypeKind.Period).Accessors!
            .OfType<FixedReturnAccessor>()
            .Single(a => a.Name == "basis");

        basis.ReturnsQualifier.Should().Be(QualifierAxis.None);
        basis.DimensionPartition.Should().BeNull();
    }
}
