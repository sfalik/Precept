using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerConstructionTests
{
    [Fact]
    public void D93_StatefulPrecept_NoInitialEvent_RequiredField_Fires()
    {
        var diagnostic = AssertSingleD93("""
            precept Widget
            field Name as string
            state Draft initial terminal
            """);

        diagnostic.Message.Should().Be("Required field(s) Name have no initial event to assign them");
    }

    [Fact]
    public void D93_StatefulPrecept_NoInitialEvent_AllFieldsHaveDefaults_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string default ""
            field Age as integer default 0
            state Draft initial terminal
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D93_StatefulPrecept_NoInitialEvent_AllFieldsOptional_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string optional
            field Age as integer optional
            state Draft initial terminal
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D93_StatefulPrecept_NoInitialEvent_ComputedField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Seed as integer default 1
            field Total as integer <- Seed + 1
            state Draft initial terminal
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D93_StatefulPrecept_NoInitialEvent_CollectionField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Items as set of string
            state Draft initial terminal
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D93_StatefulPrecept_WithInitialEvent_RequiredField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Draft initial terminal
            event Start(Name as string) initial
            from Draft on Start -> set Name = Start.Name -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D93_StatefulPrecept_NoInitialEvent_MultipleRequiredFields_ListsAll()
    {
        var diagnostic = AssertSingleD93("""
            precept Widget
            field Name as string
            field Age as integer
            state Draft initial terminal
            """);

        diagnostic.Message.Should().Be("Required field(s) Name, Age have no initial event to assign them");
    }

    [Fact]
    public void D93_StatelessPrecept_NoInitialEvent_RequiredField_Fires()
    {
        var diagnostic = AssertSingleD93("""
            precept Widget
            field Name as string
            """);

        diagnostic.Message.Should().Be("Required field(s) Name have no initial event to assign them");
    }

    [Fact]
    public void D93_StatelessPrecept_NoInitialEvent_AllDefaults_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string default ""
            field Age as integer default 0
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D93_MixedFields_OnlyRequiredFieldsListed()
    {
        var diagnostic = AssertSingleD93("""
            precept Widget
            field RequiredName as string
            field OptionalName as string optional
            field DefaultName as string default ""
            field Seed as integer default 1
            field ComputedCount as integer <- Seed + 1
            field Items as set of string
            field RequiredCount as integer
            state Draft initial terminal
            """);

        diagnostic.Message.Should().Be("Required field(s) RequiredName, RequiredCount have no initial event to assign them");
    }

    [Fact]
    public void D94_InitialEvent_AssignsAllRequiredFields_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            field Age as integer
            state Draft initial terminal
            event Start(InputName as string, InputAge as integer) initial
            from Draft on Start -> set Name = InputName -> set Age = InputAge -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_InitialEvent_MissesRequiredField_Fires()
    {
        var diagnostic = AssertSingleD94("""
            precept Widget
            field Name as string
            field Age as integer
            state Draft initial terminal
            event Start(InputName as string, InputAge as integer) initial
            from Draft on Start -> set Name = InputName -> no transition
            """);

        diagnostic.Message.Should().Be("Initial event 'Start' does not assign required field(s): Age");
    }

    [Fact]
    public void D94_InitialEvent_MissesMultipleFields_ListsAll()
    {
        var diagnostic = AssertSingleD94("""
            precept Widget
            field Name as string
            field Age as integer
            state Draft initial terminal
            event Start(InputName as string, InputAge as integer) initial
            from Draft on Start -> no transition
            """);

        diagnostic.Message.Should().Be("Initial event 'Start' does not assign required field(s): Name, Age");
    }

    [Fact]
    public void D94_InitialEvent_OptionalField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            field Nickname as string optional
            state Draft initial terminal
            event Start(InputName as string) initial
            from Draft on Start -> set Name = InputName -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_InitialEvent_DefaultField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            field Age as integer default 0
            state Draft initial terminal
            event Start(InputName as string) initial
            from Draft on Start -> set Name = InputName -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_InitialEvent_ComputedField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Seed as integer default 1
            field Name as string
            field Total as integer <- Seed + 1
            state Draft initial terminal
            event Start(InputName as string) initial
            from Draft on Start -> set Name = InputName -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_InitialEvent_CollectionField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            field Items as set of string
            state Draft initial terminal
            event Start(InputName as string) initial
            from Draft on Start -> set Name = InputName -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_InitialEvent_MultipleRows_OneRowMissesField_Fires()
    {
        var diagnostics = AssertD94s("""
            precept Widget
            field Name as string
            state Draft initial terminal
            event Start(InputName as string, Choice as integer) initial
            from Draft on Start when Choice = 1 -> set Name = InputName -> no transition
            from Draft on Start when Choice = 2 -> no transition
            """);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Be("Initial event 'Start' does not assign required field(s): Name");
    }

    [Fact]
    public void D94_InitialEvent_AllRowsSetField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Draft initial terminal
            event Start(InputName as string, Choice as integer) initial
            from Draft on Start when Choice = 1 -> set Name = InputName -> no transition
            from Draft on Start when Choice = 2 -> set Name = InputName -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_NonInitialStateRow_NotChecked()
    {
        var precept = """
            precept Widget
            field Name as string
            state Draft initial
            state Active terminal
            event Start(InputName as string) initial
            from Draft on Start -> set Name = InputName -> transition Active
            from Active on Start -> no transition
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_FieldOmittedInInitialState_NotRequired()
    {
        var precept = """
            precept Widget
            field Name as string
            state Draft initial
            state Active terminal
            in Draft omit Name
            event Start initial
            from Draft on Start -> transition Active
            """;

        AssertNoD94(precept);
    }

    [Fact]
    public void D94_MultipleInitialStates_AllRowsChecked()
    {
        var diagnostic = AssertSingleD94("""
            precept Widget
            field Name as string
            state A initial
            state B initial
            state Done terminal
            event Start(InputName as string) initial
            from A on Start -> set Name = InputName -> transition Done
            from B on Start -> transition Done
            """);

        diagnostic.Message.Should().Be("Initial event 'Start' does not assign required field(s): Name");
    }

    [Fact]
    public void D94_StatelessPrecept_WithInitialEvent_SkipsCheck()
    {
        var precept = """
            precept Widget
            field Name as string
            event Start(InputName as string) initial
            """;

        // Pre-existing gap: stateless precepts with initial events escape both D93 and D94.
        AssertNoD94(precept);
    }

    [Fact]
    public void D94_NoTransitionRows_InitialEvent_RequiredField_Fires()
    {
        var diagnostic = AssertSingleD94("""
            precept Widget
            field Name as string
            state Draft initial terminal
            event Start(InputName as string) initial
            """);

        diagnostic.Message.Should().Be("Initial event 'Start' does not assign required field(s): Name");
    }

    private static Diagnostic AssertSingleD93(string precept)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString())
            .Should().ContainSingle();

        return diagnostics.Single(d => d.Code == DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString());
    }

    private static void AssertNoD94(string precept)
    {
        AssertD94s(precept).Should().BeEmpty();
    }

    private static Diagnostic AssertSingleD94(string precept)
    {
        var diagnostics = AssertD94s(precept);
        diagnostics.Should().ContainSingle();
        return diagnostics[0];
    }

    private static Diagnostic[] AssertD94s(string precept)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        return diagnostics.Where(d => d.Code == DiagnosticCode.InitialEventMissingAssignments.ToString()).ToArray();
    }
}
