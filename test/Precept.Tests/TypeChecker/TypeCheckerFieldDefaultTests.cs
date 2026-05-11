using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerFieldDefaultTests
{
    [Fact]
    public void FieldDefault_WrongDimension_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field q as quantity of 'length' default '5 kg'
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().Contain(d => d.Code == DiagnosticCode.InvalidTypedConstantContent.ToString());
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
}
