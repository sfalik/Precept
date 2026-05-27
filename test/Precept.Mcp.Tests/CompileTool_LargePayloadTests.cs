using System.Text;
using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

/// <summary>
/// BUG-009 follow-up — confirm the compile tool handles a ~20 KB precept
/// without throwing a raw exception. Earlier reports indicated that
/// files in the 12–15 KB range caused the MCP tool to return the bare
/// "An error occurred invoking 'precept_compile'" string. The direct
/// in-process call path stays well above any plausible stdio framing
/// limit because <see cref="CompileTool.Compile"/> never crosses the
/// wire here.
/// </summary>
public class CompileTool_LargePayloadTests
{
    [Fact]
    public void Compile_TwentyKilobytePrecept_ReturnsStructuredResult()
    {
        var source = BuildLargePrecept(fieldCount: 600);
        Encoding.UTF8.GetByteCount(source).Should().BeGreaterThan(20 * 1024);

        var result = CompileTool.Compile(source);

        result.Should().NotBeNull();
        result.Diagnostics.Should().NotBeNull();
        result.DiagnosticCount.Should().Be(result.Diagnostics.Length);
        result.Summary.Should().NotBeNullOrEmpty();

        // Either the compile succeeds, or every reported diagnostic is a
        // structured PRE-coded entry — never a raw transport error.
        result.Diagnostics.Should().AllSatisfy(d => d.Code.Should().StartWith("PRE"));
    }

    [Fact]
    public void Compile_TwentyKilobyteValidPrecept_CompilesClean()
    {
        var source = BuildLargePrecept(fieldCount: 600);
        Encoding.UTF8.GetByteCount(source).Should().BeGreaterThan(20 * 1024);

        var result = CompileTool.Compile(source);

        result.Success.Should().BeTrue();
        result.DiagnosticCount.Should().Be(0);
    }

    private static string BuildLargePrecept(int fieldCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("precept LargePayload");
        for (var i = 0; i < fieldCount; i++)
        {
            sb.Append("field F").Append(i).Append(" as integer default ").Append(i).AppendLine(" nonnegative editable");
        }
        sb.AppendLine("state Draft initial");
        sb.AppendLine("state Done terminal");
        sb.AppendLine("event E");
        sb.AppendLine("from Draft on E -> transition Done");
        return sb.ToString();
    }
}
