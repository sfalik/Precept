using FluentAssertions;
using Precept.Mcp.Dtos;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class McpFieldStateTests
{
    [Fact]
    public void Mcp_D130_OmittedFieldRead_SurfacesAsPRE0130()
    {
        var diagnostic = AssertDiagnosticCode("""
            precept Widget
            field F as number default 0
            state Draft initial
            event E
            in Draft omit F
            from Draft on E when F > 0 -> no transition
            """, "PRE0130");

        diagnostic.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Mcp_D131_OmittedFieldSet_SurfacesAsPRE0131()
    {
        var diagnostic = AssertDiagnosticCode("""
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            in Review omit F
            from Draft on E -> set F = 1 -> transition Review
            """, "PRE0131");

        diagnostic.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Mcp_D132_RequiredFieldUnassigned_SurfacesAsPRE0132()
    {
        var diagnostic = AssertDiagnosticCode("""
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> transition Review
            """, "PRE0132");

        diagnostic.Message.Should().NotBeNullOrWhiteSpace();
    }

    private static CompileDiagnosticDto AssertDiagnosticCode(string preceptText, string expectedCode)
    {
        var result = CompileTool.Compile(preceptText);

        result.Success.Should().BeFalse();
        result.DiagnosticCount.Should().Be(result.Diagnostics.Length);

        return result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == expectedCode).Which;
    }
}
