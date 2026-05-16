using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerFieldDefaultTests
{
    [Fact]
    public void FieldDefault_WrongDimension_EmitsDimensionCategoryMismatch()
    {
        var precept = """
            precept Widget
            field q as quantity of 'length' default '5 kg'
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().Contain(d => d.Code == DiagnosticCode.DimensionCategoryMismatch.ToString());
    }

    [Fact]
    public void FieldDefault_IntegerForQuantityField_EmitsTypeMismatch()
    {
        var precept = """
            precept Widget
            field q as quantity of 'mass' default 5
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void FieldDefault_ComputedFieldRefCrossDimension_EmitsDimensionCategoryMismatch()
    {
        var precept = """
            precept Widget
            field source as quantity of 'length' default '5 m'
            field target as quantity of 'mass' <- source
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DimensionCategoryMismatch);
    }

    [Fact]
    public void FieldDefault_MatchingDimension_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field q as quantity of 'mass' default '5 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void FieldMin_WrongDimension_EmitsDimensionCategoryMismatch()
    {
        var precept = """
            precept Widget
            field q as quantity of 'mass' min '0 m'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DimensionCategoryMismatch);
    }

    [Fact]
    public void FieldMax_MatchingDimension_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field q as quantity of 'mass' max '100 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
