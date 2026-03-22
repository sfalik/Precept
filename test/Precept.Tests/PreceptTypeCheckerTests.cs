using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

public class PreceptTypeCheckerTests
{
    [Fact]
    public void Check_UnknownIdentifier_ProducesC38()
    {
        const string dsl = """
            precept M
            field Total as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Total = Missing -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C38");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT038");
        result.Diagnostics[0].Message.Should().Contain("unknown identifier 'Missing'");
    }

    [Fact]
    public void Check_NullableAssignmentWithoutGuard_ProducesC42()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C42");
        result.Diagnostics[0].Message.Should().Contain("set target 'Value' type mismatch");
    }

    [Fact]
    public void Check_NullableAssignmentWithGuard_NarrowsSuccessfully()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount != null -> set Value = RetryCount -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_TypeContext_CapturesScopedSymbolsForGuardedTransition()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount != null -> set Value = RetryCount -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.TypeContext.Scopes.Should().NotBeEmpty();
        var exactActionScope = result.TypeContext.Scopes.Single(scope =>
            scope.Line == 7 &&
            scope.ScopeKind == "transition-actions" &&
            scope.StateContext == "A" &&
            scope.EventName == "Go");
        exactActionScope.Symbols.Should().ContainKey("RetryCount");
        exactActionScope.Symbols["RetryCount"].Should().Be(StaticValueKind.Number);
    }

    [Fact]
    public void Validate_ReturnsAllTypeDiagnosticsWithoutThrowing()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field Name as string default ""
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> set Name = Missing -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var result = PreceptCompiler.Validate(model);

        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Select(d => d.Constraint.Id).Should().BeEquivalentTo(["C42", "C38"]);
        result.TypeContext.Expressions.Should().NotBeEmpty();
    }

    [Fact]
    public void Check_ContainsRhsTypeMismatch_ProducesC41()
    {
        const string dsl = """
            precept M
            field RequestedFloors as set of number
            state Draft initial
            event RemoveFloor with Floor as string
            from Draft on RemoveFloor when RequestedFloors contains RemoveFloor.Floor -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C41");
        result.Diagnostics[0].Message.Should().Contain("operator 'contains' requires RHS of type number");
    }

    [Fact]
    public void Check_PopIntoWrongTargetType_ProducesC43()
    {
        const string dsl = """
            precept M
            field LastStep as number default 0
            field RepairSteps as stack of string
            state InRepair initial
            event Undo
            from InRepair on Undo when RepairSteps.count > 0 -> pop RepairSteps into LastStep -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C43");
        result.Diagnostics[0].Message.Should().Contain("cannot assign string to target 'LastStep' of type number");
    }

    [Fact]
    public void Check_FromAny_UsesPerStateExpansionAndStateAssertNarrowing()
    {
        const string dsl = """
            precept M
            field AssignedTechnician as string nullable
            field TechnicianName as string default ""
            state Scheduled initial
            state Open
            in Scheduled assert AssignedTechnician != null because "scheduled work must have a technician"
            event Snapshot
            from any on Snapshot -> set TechnicianName = AssignedTechnician -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C42");
        result.Diagnostics[0].StateContext.Should().Be("Open");
    }

    [Fact]
    public void Compile_TypeError_IsCompileBlockingWithStableCode()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PRECEPT042*");
    }

    private static PreceptTypeCheckResult Check(string dsl)
    {
        var model = PreceptParser.Parse(dsl);
        return PreceptTypeChecker.Check(model);
    }
}