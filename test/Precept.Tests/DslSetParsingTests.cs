using System;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public class DslSetParsingTests
{
    [Fact]
    public void Parse_IfBranch_WithSetAndNoTransition_PreservesAssignments()
    {
        const string dsl = """
            precept Sample
            number Count = 0
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if Count > 0
                    set Count = Count - 1
                    no transition
                else
                    transition Green
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle();
        var noTransClause = machine.Transitions[0].Clauses.Single(c => c.Outcome is DslNoTransition);
        noTransClause.SetAssignments.Should().ContainSingle();
        noTransClause.SetAssignments[0].Key.Should().Be("Count");
    }

    [Fact]
    public void Parse_BlockSetWithoutOutcome_Throws()
    {
        const string dsl = """
            precept Sample
            number Count = 0
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set Count = Count + 1
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*set requires a following transition*");
    }

    [Fact]
    public void Parse_TransitionWithUnknownSetField_Throws()
    {
        const string dsl = """
            precept Sample
            number Count = 0
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set MissingField = Count
                transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*assigns unknown data field 'MissingField'*");
    }

    [Fact]
    public void Parse_ElseBranch_WithTwoSets_PreservesOrder()
    {
        const string dsl = """
            precept Sample
            number Count = 0
            string Label = ""
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

        var machine = PreceptParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle();
        var elseClause = machine.Transitions[0].Clauses.Single(c => c.Outcome is DslStateTransition st && st.TargetState == "Red");
        elseClause.SetAssignments.Should().HaveCount(2);
        elseClause.SetAssignments[0].Key.Should().Be("Count");
        elseClause.SetAssignments[1].Key.Should().Be("Label");
    }
}
