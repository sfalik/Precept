using System.Text;
using System.Text.Json;
using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class CompileToolTests
{
    [Fact]
    public void Compile_ValidPrecept_ReturnsSuccessAndCompactSummary()
    {
        var result = CompileTool.Compile(ValidSource);

        result.Success.Should().BeTrue();
        result.DiagnosticCount.Should().Be(0);
        result.Diagnostics.Should().BeEmpty();
        result.ProofObligations.Should().BeEmpty();
        result.Summary.Should().Be("LoanApplication: 2 states, 1 events, 1 transitions, 0 rules, 0 ensures, 0 type errors.");
    }

    [Fact]
    public void Compile_InvalidPrecept_ReturnsMinimalDiagnostics()
    {
        var result = CompileTool.Compile(InvalidSource);

        result.Success.Should().BeFalse();
        result.DiagnosticCount.Should().Be(result.Diagnostics.Length);
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "PRE0009");

        var diagnostic = result.Diagnostics[0];
        diagnostic.Line.Should().Be(2);
        diagnostic.Column.Should().Be(17);
        diagnostic.Severity.Should().Be("error");
        diagnostic.Code.Should().Be("PRE0009");
        diagnostic.Message.Should().Be("Expected type keyword here, but found 'moneys'");
        result.Summary.Should().StartWith("Broken: 0 states, 0 events, 0 transitions, 0 rules, 0 ensures, ");
        result.Summary.Should().Contain(" type errors.");
    }

    [Fact]
    public void Compile_UsesExpectedJsonShape()
    {
        var result = CompileTool.Compile(InvalidSource);
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        document.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal(
            "success",
            "diagnosticCount",
            "diagnostics",
            "summary",
            "proofObligations");

        var diagnostic = document.RootElement.GetProperty("diagnostics")[0];
        diagnostic.EnumerateObject().Select(property => property.Name).Should().Equal(
            "line",
            "column",
            "severity",
            "code",
            "message");

        var proofObligation = document.RootElement.GetProperty("proofObligations");
        proofObligation.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void Compile_InventoryItemSample_ReturnsReasonableSummary()
    {
        var result = CompileTool.Compile(GetSampleText("inventory-item.precept"));

        result.Summary.Should().StartWith("InventoryItem: ");
        result.Summary.Should().Contain(" states, ");
        result.Summary.Should().Contain(" events, ");
        result.Summary.Should().Contain(" transitions, ");
        result.Summary.Should().Contain(" rules, ");
        result.Summary.Should().Contain(" ensures, ");
        result.DiagnosticCount.Should().Be(result.Diagnostics.Length);
    }

    [Fact]
    public void Compile_LargeDiagnosticPayload_StaysReasonableSize()
    {
        var result = CompileTool.Compile(BuildLargeErrorSource(120));
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        result.Success.Should().BeFalse();
        result.DiagnosticCount.Should().Be(result.Diagnostics.Length);
        Encoding.UTF8.GetByteCount(json).Should().BeLessThan(60 * 1024);
    }

    [Fact]
    public void CompileTool_BoundedFieldOverflow_ObligationKindIsIntervalContainment()
    {
        var result = CompileTool.Compile(BoundedOverflowSource);

        var obligation = result.ProofObligations.Should()
            .ContainSingle(entry => entry.Kind == "IntervalContainment")
            .Which;

        obligation.Disposition.Should().Be("Unresolved");
        obligation.ComputedInterval.Should().NotBeNullOrWhiteSpace();
        obligation.TargetField.Should().Be("total");
        obligation.DeclaredMin.Should().Be(0m);
        obligation.DeclaredMax.Should().Be(100m);
    }

    private static string BuildLargeErrorSource(int fieldCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("precept BigBroken");

        for (var i = 0; i < fieldCount; i++)
        {
            sb.Append("field F").Append(i).AppendLine(" as moneys");
        }

        return sb.ToString();
    }

    private static string GetSampleText(string fileName)
        => File.ReadAllText(Path.Combine(GetRepositoryRoot(), "samples", fileName));

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private const string ValidSource = """
        precept LoanApplication
        field Amount as number default 0
        state Pending initial
        state Approved terminal
        event Approve
        from Pending on Approve
            -> transition Approved
        """;

    private const string InvalidSource = """
        precept Broken
        field Amount as moneys
        """;

    private const string BoundedOverflowSource = """
        precept Simple
        field total as decimal min 0 max 100
        state Active initial
        event Add(A as decimal min 0 max 80, B as decimal min 0 max 80)
        from Active on Add
            -> set total = Add.A + Add.B
            -> no transition
        """;
}
