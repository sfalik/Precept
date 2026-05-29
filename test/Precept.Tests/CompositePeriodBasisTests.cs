using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// F-LANG-BIZ-07 W-A — composite period basis (`period in 'hours + minutes'`).
/// Covers parse, lenient whitespace, coarse-to-fine canonicalization, the combined
/// dimension (date / time / datetime), the three malformed-basis diagnostics
/// (PRE0160/0161/0162), and single-basis non-regression.
/// </summary>
public class CompositePeriodBasisTests
{
    private static DeclaredQualifierMeta.TemporalUnit ResolveBasis(string fieldDecl, string fieldName)
    {
        var precept = $"""
            precept Widget
            {fieldDecl}
            state Open initial
            """;
        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        return index.Fields.Single(f => f.Name == fieldName)
            .DeclaredQualifiers.Should().ContainSingle().Which
            .Should().BeOfType<DeclaredQualifierMeta.TemporalUnit>().Which;
    }

    [Fact]
    public void Composite_TimeOnly_ParsesAndDerivesTimeDimension()
    {
        var tu = ResolveBasis("field Elapsed as period in 'hours + minutes'", "Elapsed");
        tu.UnitName.Should().Be("hours + minutes");
        tu.Components.Should().Equal("hours", "minutes");
        tu.DerivedDimension.Should().Be(PeriodDimension.Time);
    }

    [Fact]
    public void Composite_LenientWhitespace_CanonicalizesToSpaced()
    {
        // No spaces around '+' on input → canonical spaced form on output.
        var tu = ResolveBasis("field Elapsed as period in 'hours+minutes'", "Elapsed");
        tu.UnitName.Should().Be("hours + minutes");
        tu.Components.Should().Equal("hours", "minutes");
    }

    [Fact]
    public void Composite_NonCanonicalOrder_IsCanonicalizedCoarseToFine()
    {
        // Author writes fine-to-coarse; canonical form is coarse-to-fine.
        var tu = ResolveBasis("field Elapsed as period in 'minutes + hours'", "Elapsed");
        tu.UnitName.Should().Be("hours + minutes");
        tu.Components.Should().Equal("hours", "minutes");
    }

    [Fact]
    public void Composite_DateOnly_DerivesDateDimension()
    {
        var tu = ResolveBasis("field Term as period in 'years + months'", "Term");
        tu.UnitName.Should().Be("years + months");
        tu.Components.Should().Equal("years", "months");
        tu.DerivedDimension.Should().Be(PeriodDimension.Date);
    }

    [Fact]
    public void Composite_SpanningDateAndTime_DerivesDatetimeDimension()
    {
        var tu = ResolveBasis("field Span as period in 'days + hours'", "Span");
        tu.UnitName.Should().Be("days + hours");
        tu.Components.Should().Equal("days", "hours");
        tu.DerivedDimension.Should().Be(PeriodDimension.Datetime);
    }

    [Fact]
    public void Composite_ThreeComponents_CanonicalizesAllInOrder()
    {
        var tu = ResolveBasis("field Span as period in 'minutes + days + hours'", "Span");
        tu.UnitName.Should().Be("days + hours + minutes");
        tu.Components.Should().Equal("days", "hours", "minutes");
        tu.DerivedDimension.Should().Be(PeriodDimension.Datetime);
    }

    [Fact]
    public void Composite_DuplicateComponent_EmitsDuplicateDiagnostic()
    {
        var precept = """
            precept Widget
            field Elapsed as period in 'hours + minutes + hours'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DuplicateCompositeBasisComponent);
    }

    [Fact]
    public void Composite_SingularPluralOfSameUnit_IsADuplicate()
    {
        // 'hour' and 'hours' resolve to the same unit — listing both is a duplicate.
        var precept = """
            precept Widget
            field Elapsed as period in 'hours + hour'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DuplicateCompositeBasisComponent);
    }

    [Fact]
    public void Composite_UnknownComponent_EmitsUnknownDiagnostic()
    {
        var precept = """
            precept Widget
            field Term as period in 'years + fortnights'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UnknownCompositeBasisComponent);
    }

    [Fact]
    public void Composite_TrailingSeparator_EmitsEmptyComponentDiagnostic()
    {
        var precept = """
            precept Widget
            field Term as period in 'years +'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.EmptyCompositeBasisComponent);
    }

    [Fact]
    public void Composite_DoubledSeparator_EmitsEmptyComponentDiagnostic()
    {
        var precept = """
            precept Widget
            field Term as period in 'years + + days'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.EmptyCompositeBasisComponent);
    }

    [Fact]
    public void SingleBasis_StillParsesUnchanged()
    {
        // Non-regression: the single-component path is behavior-identical to before W-A.
        var tu = ResolveBasis("field Grace as period in 'months'", "Grace");
        tu.UnitName.Should().Be("months");
        tu.Components.Should().Equal("months");
        tu.DerivedDimension.Should().Be(PeriodDimension.Date);
    }

    [Fact]
    public void SingleBasis_InvalidUnit_StillEmitsInvalidTemporalUnitString()
    {
        // Non-regression: a single unrecognized unit keeps PRE0118, not the composite codes.
        var precept = """
            precept Widget
            field Offset as period in 'fortnights'
            state Open initial
            """;
        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidTemporalUnitString);
    }
}
