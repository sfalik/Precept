using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class StatelessToolTests
{
    // ════════════════════════════════════════════════════════════════════
    // CompileTool — stateless precept compilation
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void CompileTool_StatelessPrecept_IsStatelessTrue()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Email as string default ""
            field Age as number default 0

            edit all
            """;

        var result = CompileTool.Run(dsl);

        result.Valid.Should().BeTrue();
        result.IsStateless.Should().BeTrue();
    }

    [Fact]
    public void CompileTool_StatelessPrecept_InitialStateIsNull()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Age as number default 0
            """;

        var result = CompileTool.Run(dsl);

        result.Valid.Should().BeTrue();
        result.InitialState.Should().BeNull();
    }

    [Fact]
    public void CompileTool_StatelessPrecept_StateCountIsZero()
    {
        const string dsl = """
            precept FeeSchedule

            field BaseFee as number default 0
            field Discount as number default 0

            edit BaseFee, Discount
            """;

        var result = CompileTool.Run(dsl);

        result.Valid.Should().BeTrue();
        result.StateCount.Should().Be(0);
        result.States.Should().NotBeNull();
        result.States.Should().BeEmpty();
    }

    [Fact]
    public void CompileTool_StatefulPrecept_IsStatelessFalse()
    {
        var text = File.ReadAllText(Path.Combine(TestPaths.SamplesDir, "maintenance-work-order.precept"));

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.IsStateless.Should().BeFalse();
        result.StateCount.Should().BeGreaterThan(0);
        result.InitialState.Should().NotBeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // InspectTool — stateless precept with null currentState
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void InspectTool_StatelessPrecept_WithNullCurrentState_Returns()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Age as number default 0

            edit all
            """;

        var data = new Dictionary<string, object?> { ["Name"] = "Alice", ["Age"] = 30.0 };

        var result = InspectTool.Inspect(dsl, null, data);

        result.Error.Should().BeNull();
        result.CurrentState.Should().BeNull();
        result.EditableFields.Should().NotBeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // FireTool — stateless precept with null currentState
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void FireTool_StatelessPrecept_WithNullCurrentState_ReturnsUndefined()
    {
        const string dsl = """
            precept Profile

            field Name as string default ""

            event Rename with NewName as string
            """;

        var data = new Dictionary<string, object?> { ["Name"] = "Alice" };
        var args = new Dictionary<string, object?> { ["NewName"] = "Bob" };

        var result = FireTool.Fire(dsl, null, "Rename", data, args);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Undefined");
    }

    // ════════════════════════════════════════════════════════════════════
    // UpdateTool — stateless precept with null currentState
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateTool_StatelessPrecept_WithNullCurrentState_EditableField_ReturnsUpdate()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Age as number default 0

            edit Name, Age
            """;

        var data = new Dictionary<string, object?> { ["Name"] = "Alice", ["Age"] = 25.0 };
        var fields = new Dictionary<string, object?> { ["Name"] = "Bob" };

        var result = UpdateTool.Update(dsl, null, data, fields);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Update");
        result.Data.Should().NotBeNull();
        result.Data!["Name"].Should().Be("Bob");
    }
}
