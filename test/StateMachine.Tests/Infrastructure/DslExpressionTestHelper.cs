using FluentAssertions;
using StateMachine.Dsl;

namespace StateMachine.Tests.Infrastructure;

internal static class DslExpressionTestHelper
{
    internal static DslExpression ParseFirstTransformExpression(string expressionText)
    {
        var dsl = $$"""
            machine Parser
            number Value
            event Advance
            state Red
            state Green
            from Red on Advance
                transform Value = {{expressionText}}
                transition Green
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        machine.Transitions.Should().ContainSingle();
        machine.Transitions[0].TransformAssignments.Should().ContainSingle();
        return machine.Transitions[0].TransformAssignments[0].Expression;
    }

    internal static System.Action ParseFirstTransformExpressionAction(string expressionText)
        => () => _ = ParseFirstTransformExpression(expressionText);
}
