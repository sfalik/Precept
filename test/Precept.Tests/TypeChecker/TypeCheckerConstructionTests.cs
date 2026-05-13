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

    private static Diagnostic AssertSingleD93(string precept)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString())
            .Should().ContainSingle();

        return diagnostics.Single(d => d.Code == DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString());
    }
}
