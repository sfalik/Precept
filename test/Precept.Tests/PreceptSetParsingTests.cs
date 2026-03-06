using System;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public class PreceptSetParsingTests
{
    [Fact]
    public void Parse_IfBranch_WithSetAndNoTransition_PreservesAssignments()
    {
        const string dsl = """
            precept Sample
            field Count as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when Count > 0 -> set Count = Count - 1 -> no transition
            from Red on Advance -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TransitionRows.Should().HaveCount(2);
        var noTransRow = machine.TransitionRows!.Single(r => r.Outcome is PreceptNoTransition);
        noTransRow.SetAssignments.Should().ContainSingle();
        noTransRow.SetAssignments[0].Key.Should().Be("Count");
    }

    [Fact]
    public void Parse_BlockSetWithoutOutcome_Throws()
    {
        const string dsl = """
            precept Sample
            field Count as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set Count = Count + 1
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing an outcome*");
    }

    [Fact]
    public void Parse_TransitionWithUnknownSetField_Throws()
    {
        const string dsl = """
            precept Sample
            field Count as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set MissingField = Count -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(machine);
        var inst = engine.CreateInstance();
        var result = engine.Fire(inst, "Advance");

        // After firing, the unknown field ends up in instance data;
        // next operation catches the unknown field via data-contract validation.
        var updated = result.UpdatedInstance!;
        var compat = engine.CheckCompatibility(updated);
        compat.IsCompatible.Should().BeFalse();
        compat.Reason.Should().Contain("unknown data field 'MissingField'");
    }

    [Fact]
    public void Parse_ElseBranch_WithTwoSets_PreservesOrder()
    {
        const string dsl = """
            precept Sample
            field Count as number default 0
            field Label as string default ""
            state Red initial
            state Green
            event Advance
            from Red on Advance when Count > 0 -> transition Green
            from Red on Advance -> set Count = Count + 1 -> set Label = "Retry" -> transition Red
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TransitionRows.Should().HaveCount(2);
        var elseRow = machine.TransitionRows!.Single(r => r.Outcome is PreceptStateTransition st && st.TargetState == "Red");
        elseRow.SetAssignments.Should().HaveCount(2);
        elseRow.SetAssignments[0].Key.Should().Be("Count");
        elseRow.SetAssignments[1].Key.Should().Be("Label");
    }
}
