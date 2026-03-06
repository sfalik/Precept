using FluentAssertions;
using Precept;

namespace Precept.Tests.Infrastructure;

internal static class PreceptExpressionTestHelper
{
    internal static PreceptExpression ParseFirstSetExpression(string expressionText)
    {
        var dsl = $$"""
            precept Parser
            field Value as number default 0
            event Advance
            state Red initial
            state Green
            from Red on Advance -> set Value = {{expressionText}} -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);
        machine.TransitionRows.Should().ContainSingle();
        machine.TransitionRows![0].SetAssignments.Should().ContainSingle();
        return machine.TransitionRows[0].SetAssignments[0].Expression;
    }

    internal static System.Action ParseFirstSetExpressionAction(string expressionText)
        => () => _ = ParseFirstSetExpression(expressionText);
}
