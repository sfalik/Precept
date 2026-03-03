using FluentAssertions;
using StateMachine.Dsl;

namespace StateMachine.Tests.Infrastructure;

internal static class DslExpressionTestHelper
{
    internal static DslExpression ParseFirstSetExpression(string expressionText)
    {
        var dsl = $$"""
            machine Parser
            number Value
            event Advance
            state Red
            state Green
            from Red on Advance
                set Value = {{expressionText}}
                transition Green
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        machine.Transitions.Should().ContainSingle();
        machine.Transitions[0].SetAssignments.Should().ContainSingle();
        return machine.Transitions[0].SetAssignments[0].Expression;
    }

    internal static System.Action ParseFirstSetExpressionAction(string expressionText)
        => () => _ = ParseFirstSetExpression(expressionText);
}
