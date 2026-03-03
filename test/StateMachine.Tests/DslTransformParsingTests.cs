using System;
using System.Linq;
using FluentAssertions;
using StateMachine.Dsl;
using Xunit;

namespace StateMachine.Tests;

public class DslTransformParsingTests
{
    [Fact]
    public void Parse_IfBranch_WithTransformAndNoTransition_Throws()
    {
        const string dsl = """
            machine Sample
            number Count
            state Red
            state Green
            event Advance
            from Red on Advance
                if Count > 0
                    transform Count = Count - 1
                    no transition
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*transform requires a transition outcome and cannot be used with 'no transition'*");
    }

    [Fact]
    public void Parse_BlockTransformWithoutOutcome_Throws()
    {
        const string dsl = """
            machine Sample
            number Count
            state Red
            state Green
            event Advance
            from Red on Advance
                transform Count = Count + 1
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*transform requires a following transition*");
    }

    [Fact]
    public void Parse_TransitionWithUnknownTransformField_Throws()
    {
        const string dsl = """
            machine Sample
            number Count
            state Red
            state Green
            event Advance
            from Red on Advance
                transform MissingField = Count
                transition Green
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*assigns unknown data field 'MissingField'*");
    }

    [Fact]
    public void Parse_ElseBranch_WithTwoTransforms_PreservesOrder()
    {
        const string dsl = """
            machine Sample
            number Count
            string Label
            state Red
            state Green
            event Advance
            from Red on Advance
                if Count > 0
                    transition Green
                else
                    transform Count = Count + 1
                    transform Label = "Retry"
                    transition Red
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle(t => t.ToState == "Red");
        var transition = machine.Transitions.Single(t => t.ToState == "Red");
        transition.TransformAssignments.Should().HaveCount(2);
        transition.TransformAssignments[0].Key.Should().Be("Count");
        transition.TransformAssignments[1].Key.Should().Be("Label");
    }
}
