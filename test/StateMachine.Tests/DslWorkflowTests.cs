using System;
using System.Collections.Generic;
using FluentAssertions;
using StateMachine.Dsl;
using Xunit;

namespace StateMachine.Tests;

public class DslWorkflowTests
{
    [Fact]
    public void Parse_And_Compile_UnguardedTransition_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var workflow = DslWorkflowCompiler.Compile(machine);

        var inspection = workflow.Inspect("Red", "Advance");

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_GuardedTransition_WithMatchingContext_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["CarsWaiting"] = 2 };

        var inspection = workflow.Inspect("Red", "Advance", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_GuardedTransition_WithNonMatchingContext_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["CarsWaiting"] = 0 };

        var inspection = workflow.Inspect("Red", "Advance", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.TargetState.Should().BeNull();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("CarsWaiting > 0", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_GuardedTransition_WithoutRequiredContextKey_IsRejectedWithReason()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var inspection = workflow.Inspect("Red", "Advance");

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("context key 'CarsWaiting'", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_GuardedTransition_WithWrongContextType_IsRejectedWithReason()
    {
        const string dsl = """
            machine FeatureFlag
            state Disabled
            state Enabled
            event Evaluate
            transition Disabled -> Enabled on Evaluate when IsEnabled
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["IsEnabled"] = "yes" };

        var inspection = workflow.Inspect("Disabled", "Evaluate", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("not a boolean", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_MultipleGuardedTransitions_AllFail_AggregatesReasons()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            state Yellow
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            transition Red -> Yellow on Advance when IsManualOverride
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 0,
            ["IsManualOverride"] = false
        };

        var inspection = workflow.Inspect("Red", "Advance", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().HaveCount(2);
    }

    [Fact]
    public void Inspect_StringEqualityGuard_WithQuotedLiteral_IsAccepted()
    {
        const string dsl = """
            machine FeatureMode
            state Draft
            state Live
            event Publish
            transition Draft -> Live on Publish when Mode == "Manual"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["Mode"] = "Manual" };

        var inspection = workflow.Inspect("Draft", "Publish", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("Live");
    }

    [Fact]
    public void Inspect_NullEqualityGuard_IsAccepted_WhenContextValueIsNull()
    {
        const string dsl = """
            machine Tickets
            state Open
            state Pending
            event Escalate
            transition Open -> Pending on Escalate when Assignee == null
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["Assignee"] = null };

        var inspection = workflow.Inspect("Open", "Escalate", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("Pending");
    }

    [Fact]
    public void Inspect_NumericComparison_HandlesMixedNumericRuntimeTypes()
    {
        const string dsl = """
            machine Throughput
            state Low
            state High
            event Scale
            transition Low -> High on Scale when Qps >= 100
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["Qps"] = 100m };

        var inspection = workflow.Inspect("Low", "Scale", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("High");
    }

    [Fact]
    public void Inspect_UnsupportedGuardExpression_IsRejectedWithNotSupportedReason()
    {
        const string dsl = """
            machine Workflow
            state A
            state B
            event Go
            transition A -> B on Go when Flag && OtherFlag
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?>
        {
            ["Flag"] = true,
            ["OtherFlag"] = true
        };

        var inspection = workflow.Inspect("A", "Go", context);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("not supported", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_GuardedTransition_WithContext_AcceptsAndReturnsNewState()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var context = new Dictionary<string, object?> { ["CarsWaiting"] = 3 };

        var fire = workflow.Fire("Red", "Advance", context);

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void CreateInstance_Then_Fire_UpdatesPersistableInstance()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var fire = workflow.Fire(instance, "Advance");

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.CurrentState.Should().Be("Green");
        fire.UpdatedInstance!.LastEvent.Should().Be("Advance");
    }

    [Fact]
    public void Inspect_Instance_Context_UsesSnapshotAndOverlayContext()
    {
        const string dsl = """
            machine FeatureFlag
            state Disabled
            state Enabled
            event Evaluate
            transition Disabled -> Enabled on Evaluate when IsEnabled
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Disabled", new Dictionary<string, object?> { ["IsEnabled"] = false });

        var rejected = workflow.Inspect(instance, "Evaluate");
        var accepted = workflow.Inspect(instance, "Evaluate", new Dictionary<string, object?> { ["IsEnabled"] = true });

        rejected.IsDefined.Should().BeTrue();
        rejected.IsAccepted.Should().BeFalse();
        accepted.IsDefined.Should().BeTrue();
        accepted.IsAccepted.Should().BeTrue();
        accepted.TargetState.Should().Be("Enabled");
    }

    [Fact]
    public void Inspect_Instance_WithWorkflowMismatch_IsNotDefinedWithReason()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var incompatible = new DslWorkflowInstance(
            "OtherWorkflow",
            "Red",
            null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>());

        var inspection = workflow.Inspect(incompatible, "Advance");

        inspection.IsDefined.Should().BeFalse();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("workflow", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_AcceptedTransition_ReturnsNewState()
    {
        const string dsl = """
            machine TrafficLight
            state Green
            state Yellow
            event Advance
            transition Green -> Yellow on Advance
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var fire = workflow.Fire("Green", "Advance");

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeTrue();
        fire.NewState.Should().Be("Yellow");
        fire.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Fire_UndefinedTransition_ReturnsNotDefined()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var fire = workflow.Fire("Green", "Advance");

        fire.IsDefined.Should().BeFalse();
        fire.IsAccepted.Should().BeFalse();
        fire.NewState.Should().BeNull();
        fire.Reasons.Should().ContainSingle();
    }

    [Fact]
    public void Parse_DuplicateState_Throws()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Red
            event Advance
            transition Red -> Red on Advance
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate state*");
    }
}
