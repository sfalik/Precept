using FluentAssertions;
using Precept;

namespace Precept.Tests.Infrastructure;

internal static class PreceptExpressionTestHelper
{
    internal static PreceptExpression ParseFirstSetExpression(string expressionText)
    {
        var dsl = $$"""
            precept Parser
            number Value = 0
            event Advance
            state Red initial
            state Green
            from Red on Advance
                set Value = {{expressionText}}
                transition Green
            """;

        var machine = PreceptParser.Parse(dsl);
        machine.Transitions.Should().ContainSingle();
        machine.Transitions[0].Clauses.Should().ContainSingle();
        machine.Transitions[0].Clauses[0].SetAssignments.Should().ContainSingle();
        return machine.Transitions[0].Clauses[0].SetAssignments[0].Expression;
    }

    internal static System.Action ParseFirstSetExpressionAction(string expressionText)
        => () => _ = ParseFirstSetExpression(expressionText);
}
