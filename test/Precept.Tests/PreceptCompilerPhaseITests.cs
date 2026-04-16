using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

public class PreceptCompilerPhaseITests
{
    [Fact]
    public void Validate_AggregatesCompileAndGraphDiagnostics()
    {
        const string dsl = """
            precept M
            field Score as number default 0
            rule Score > 0 because "must be positive"
            state A initial
            state B
            state Unreachable
            event Go
            from A on Go -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var result = PreceptCompiler.Validate(model);

        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Select(d => d.Constraint.Id).Should().Contain(["C29", "C48"]);
    }

    [Fact]
    public void Validate_AggregatesFormerThrowBasedCompileChecks()
    {
        const string dsl = """
            precept M
            field Score as number default 0
            rule Score > 0 because "must be positive"
            state A initial
            state B
            in B ensure Score > 0 because "first"
            in B ensure Score > 0 because "duplicate"
            event Go with Amount as number default 0
            on Go ensure Amount > 1 because "amount must be positive"
            from A on Go -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var result = PreceptCompiler.Validate(model);

        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Select(d => d.Constraint.Id).Should().Contain(["C29", "C31", "C44"]);
    }

    [Fact]
    public void CompileFromText_WarningOnlyResult_ReturnsEngine()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            state Unreachable
            event Go
            from A on Go -> transition B
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.HasErrors.Should().BeFalse();
        result.Model.Should().NotBeNull();
        result.Engine.Should().NotBeNull();
        result.Diagnostics.Should().ContainSingle(d => d.Code == "PRECEPT048");
    }

    [Fact]
    public void CompileFromText_ParseFailure_ReturnsDiagnosticsOnly()
    {
        var result = PreceptCompiler.CompileFromText("precept Test\nstate");

        result.HasErrors.Should().BeTrue();
        result.Model.Should().BeNull();
        result.Engine.Should().BeNull();
        result.Diagnostics.Should().NotBeEmpty();
    }
}