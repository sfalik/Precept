using System;
using System.Linq;
using FluentAssertions;
using StateMachine.Dsl;
using Xunit;

namespace StateMachine.Tests;

public class DslSetParsingTests
{
    [Fact]
    public void Parse_IfBranch_WithSetAndNoTransition_Throws()
    {
        const string dsl = """
            machine Sample
            number Count
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if Count > 0
                    set Count = Count - 1
                    no transition
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*set requires a transition outcome and cannot be used with 'no transition'*");
    }

    [Fact]
    public void Parse_BlockSetWithoutOutcome_Throws()
    {
        const string dsl = """
            machine Sample
            number Count
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set Count = Count + 1
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*set requires a following transition*");
    }

    [Fact]
    public void Parse_TransitionWithUnknownSetField_Throws()
    {
        const string dsl = """
            machine Sample
            number Count
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set MissingField = Count
                transition Green
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*assigns unknown data field 'MissingField'*");
    }

    [Fact]
    public void Parse_ElseBranch_WithTwoSets_PreservesOrder()
    {
        const string dsl = """
            machine Sample
            number Count
            string Label
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if Count > 0
                    transition Green
                else
                    set Count = Count + 1
                    set Label = "Retry"
                    transition Red
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle(t => t.ToState == "Red");
        var transition = machine.Transitions.Single(t => t.ToState == "Red");
        transition.SetAssignments.Should().HaveCount(2);
        transition.SetAssignments[0].Key.Should().Be("Count");
        transition.SetAssignments[1].Key.Should().Be("Label");
    }
}
