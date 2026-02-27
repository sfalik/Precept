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
    public void Inspect_GuardedTransition_WithMatchingData_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 2 };

        var inspection = workflow.Inspect("Red", "Advance", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_GuardedTransition_WithNonMatchingData_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 0 };

        var inspection = workflow.Inspect("Red", "Advance", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.TargetState.Should().BeNull();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("CarsWaiting > 0", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_GuardedTransition_WithoutRequiredDataKey_IsRejectedWithReason()
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
        inspection.Reasons.Should().ContainSingle(r => r.Contains("data key 'CarsWaiting'", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_GuardedTransition_WithWrongDataType_IsRejectedWithReason()
    {
        const string dsl = """
            machine FeatureFlag
            state Disabled
            state Enabled
            event Evaluate
            transition Disabled -> Enabled on Evaluate when IsEnabled
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["IsEnabled"] = "yes" };

        var inspection = workflow.Inspect("Disabled", "Evaluate", data);

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
        var data = new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 0,
            ["IsManualOverride"] = false
        };

        var inspection = workflow.Inspect("Red", "Advance", data);

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
        var data = new Dictionary<string, object?> { ["Mode"] = "Manual" };

        var inspection = workflow.Inspect("Draft", "Publish", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("Live");
    }

    [Fact]
    public void Inspect_NullEqualityGuard_IsAccepted_WhenDataValueIsNull()
    {
        const string dsl = """
            machine Tickets
            state Open
            state Pending
            event Escalate
            transition Open -> Pending on Escalate when Assignee == null
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Assignee"] = null };

        var inspection = workflow.Inspect("Open", "Escalate", data);

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
        var data = new Dictionary<string, object?> { ["Qps"] = 100m };

        var inspection = workflow.Inspect("Low", "Scale", data);

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
        var data = new Dictionary<string, object?>
        {
            ["Flag"] = true,
            ["OtherFlag"] = true
        };

        var inspection = workflow.Inspect("A", "Go", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("not supported", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_GuardedTransition_WithData_AcceptsAndReturnsNewState()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 3 };

        var fire = workflow.Fire("Red", "Advance", data);

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
    public void Fire_Instance_AppliesTransitionDataAssignment_Literal()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance set CarsWaiting = 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var fire = workflow.Fire(instance, "Advance");

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.InstanceData["CarsWaiting"].Should().Be(0d);
    }

    [Fact]
    public void Fire_Instance_AppliesTransitionDataAssignment_FromEventArgument()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state FlashingRed
            event Emergency
            transition Red -> FlashingRed on Emergency set EmergencyReason = Reason
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.InstanceData["EmergencyReason"].Should().Be("Accident");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_FromMissingEventArgument_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state FlashingRed
            event Emergency
            transition Red -> FlashingRed on Emergency set EmergencyReason = Reason
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency");

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("event argument 'Reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TransitionDataAssignment_FromInstanceDataReference_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance set LastCarsWaiting = data.CarsWaiting
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*unsupported transform expression*data.CarsWaiting*");
    }

    [Fact]
    public void Parse_TypedEventArguments_AreRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance(AdvanceArgs)
            transition Red -> Green on Advance
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*typed event arguments are deprecated*");
    }

    [Fact]
    public void Parse_TransitionDataAssignment_WithArgPrefix_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state FlashingRed
            event Emergency
            transition Red -> FlashingRed on Emergency set EmergencyReason = arg.Reason
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*deprecated transform expression*arg.Reason*");
    }

    [Fact]
    public void Inspect_AcceptedTransition_InfersRequiredEventArgumentKeys()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state FlashingRed
            event Emergency
            transition Red -> FlashingRed on Emergency set EmergencyReason = Reason
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var inspect = workflow.Inspect(instance, "Emergency");

        inspect.IsDefined.Should().BeTrue();
        inspect.IsAccepted.Should().BeTrue();
        inspect.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Reason");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ReservedBooleanLiterals_AreAppliedAsLiterals()
    {
        const string dsl = """
            machine Flags
            state Off
            state On
            event Enable
            event Disable
            transition Off -> On on Enable set IsEnabled = true
            transition On -> Off on Disable set IsEnabled = false
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var start = workflow.CreateInstance("Off", new Dictionary<string, object?>());

        var enabled = workflow.Fire(start, "Enable", new Dictionary<string, object?> { ["true"] = "not-used" });
        enabled.IsAccepted.Should().BeTrue();
        enabled.UpdatedInstance!.InstanceData["IsEnabled"].Should().Be(true);

        var disabled = workflow.Fire(enabled.UpdatedInstance!, "Disable", new Dictionary<string, object?> { ["false"] = "not-used" });
        disabled.IsAccepted.Should().BeTrue();
        disabled.UpdatedInstance!.InstanceData["IsEnabled"].Should().Be(false);
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ReservedNullLiteral_IsAppliedAsLiteral()
    {
        const string dsl = """
            machine Notes
            state Open
            state Cleared
            event Clear
            transition Open -> Cleared on Clear set Note = null
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Open", new Dictionary<string, object?> { ["Note"] = "Active" });

        var cleared = workflow.Fire(instance, "Clear", new Dictionary<string, object?> { ["null"] = "not-used" });

        cleared.IsAccepted.Should().BeTrue();
        cleared.UpdatedInstance!.InstanceData["Note"].Should().BeNull();
    }

    [Fact]
    public void Inspect_Instance_DoesNotApplyTransitionDataAssignment()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance set CarsWaiting = 0
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var inspect = workflow.Inspect(instance, "Advance");

        inspect.IsDefined.Should().BeTrue();
        inspect.IsAccepted.Should().BeTrue();
        instance.InstanceData["CarsWaiting"].Should().Be(3);
    }

    [Fact]
    public void Inspect_Instance_EventArgs_DoNotMergeWithSnapshot()
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
        var rejectedWithUnrelatedArgs = workflow.Inspect(instance, "Evaluate", new Dictionary<string, object?> { ["Other"] = true });

        rejected.IsDefined.Should().BeTrue();
        rejected.IsAccepted.Should().BeFalse();
        accepted.IsDefined.Should().BeTrue();
        accepted.IsAccepted.Should().BeTrue();
        accepted.TargetState.Should().Be("Enabled");
        rejectedWithUnrelatedArgs.IsDefined.Should().BeTrue();
        rejectedWithUnrelatedArgs.IsAccepted.Should().BeFalse();
        rejectedWithUnrelatedArgs.Reasons.Should().ContainSingle(r => r.Contains("IsEnabled", StringComparison.Ordinal));
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

    [Fact]
    public void Parse_Transition_WithGuardAndDataAssignment_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            transition Red -> Green on Advance when CarsWaiting > 0 set CarsWaiting = 0
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle();
        machine.Transitions[0].GuardExpression.Should().Be("CarsWaiting > 0");
        machine.Transitions[0].DataAssignmentKey.Should().Be("CarsWaiting");
        machine.Transitions[0].DataAssignmentExpression.Should().Be("0");
    }
}
