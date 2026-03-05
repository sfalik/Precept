using System;
using System.Collections.Generic;
using System.Linq;
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
            state Red initial
            state Green
            event Advance
            from Red on Advance
                transition Green
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var workflow = DslWorkflowCompiler.Compile(machine);

        var inspection = workflow.Inspect("Red", "Advance");

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.Outcome.Should().Be(DslOutcomeKind.Accepted);
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MachineWithNoEvents_IsValid()
    {
        const string dsl = """
            machine Minimal
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Name.Should().Be("Minimal");
        machine.InitialState.Name.Should().Be("Idle");
        machine.Events.Should().BeEmpty();
        machine.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void Compile_MachineWithNoEvents_Succeeds()
    {
        const string dsl = """
            machine Minimal
            state Idle initial
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        workflow.InitialState.Should().Be("Idle");
        workflow.States.Should().ContainSingle().Which.Should().Be("Idle");
        workflow.Events.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_Outcome_Maps_To_Undefined_Blocked_Enabled()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var undefined = workflow.Inspect("Red", "MissingEvent");
        var blocked = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });
        var enabled = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 2 });

        undefined.Outcome.Should().Be(DslOutcomeKind.NotDefined);
        blocked.Outcome.Should().Be(DslOutcomeKind.Rejected);
        enabled.Outcome.Should().Be(DslOutcomeKind.Accepted);
    }

    [Fact]
    public void Inspect_GuardedTransition_WithMatchingData_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 2 };

        var inspection = workflow.Inspect("Red", "Advance", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_GuardedTransition_WithNonMatchingData_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 0 };

        var inspection = workflow.Inspect("Red", "Advance", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.TargetState.Should().BeNull();
        inspection.Reasons.Should().ContainSingle("Cars waiting required");
    }

    [Fact]
    public void Inspect_GuardedTransition_WithoutRequiredDataKey_IsRejectedWithReason()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var inspection = workflow.Inspect("Red", "Advance");

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Cars waiting required");
    }

    [Fact]
    public void Inspect_GuardedTransition_WithWrongDataType_IsRejectedWithReason()
    {
        const string dsl = """
            machine FeatureFlag
            boolean IsEnabled = false
            state Disabled initial
            state Enabled
            event Evaluate
            from Disabled on Evaluate
                if IsEnabled
                    transition Enabled
                else
                    reject "Feature must be enabled"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["IsEnabled"] = "yes" };

        var inspection = workflow.Inspect("Disabled", "Evaluate", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Feature must be enabled");
    }

    [Fact]
    public void Inspect_MultipleGuardedTransitions_AllFail_AggregatesReasons()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            state Yellow
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else if IsManualOverride
                    transition Yellow
                else
                    reject "No eligible transition"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 0,
            ["IsManualOverride"] = false
        };

        var inspection = workflow.Inspect("Red", "Advance", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("No eligible transition");
    }

    [Fact]
    public void Inspect_IfBranch_NoTransition_Is_Allowed_And_Produces_NoTransition_When_Matched()
    {
        const string dsl = """
            machine Route
            boolean Hold = false
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if Hold
                    no transition
                else
                    transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var noTransition = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["Hold"] = true });
        var enabled = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["Hold"] = false });

        noTransition.Outcome.Should().Be(DslOutcomeKind.AcceptedInPlace);
        (noTransition.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        noTransition.TargetState.Should().Be("Red");

        enabled.Outcome.Should().Be(DslOutcomeKind.Accepted);
        enabled.TargetState.Should().Be("Green");
    }

    [Fact]
    public void Inspect_ElseIf_NoTransition_Is_Allowed_And_Preserves_Branch_Order()
    {
        const string dsl = """
            machine Route
            boolean PreferStop = false
            boolean PreferAlpha = false
            state Source initial
            state Alpha
            state Beta
            event Route
            from Source on Route
                if PreferStop
                    no transition
                else if PreferAlpha
                    transition Alpha
                else
                    transition Beta
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var firstBranchWins = workflow.Inspect("Source", "Route", new Dictionary<string, object?>
        {
            ["PreferStop"] = true,
            ["PreferAlpha"] = true
        });

        var secondBranchWins = workflow.Inspect("Source", "Route", new Dictionary<string, object?>
        {
            ["PreferStop"] = false,
            ["PreferAlpha"] = true
        });

        firstBranchWins.Outcome.Should().Be(DslOutcomeKind.AcceptedInPlace);
        (firstBranchWins.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        firstBranchWins.TargetState.Should().Be("Source");

        secondBranchWins.Outcome.Should().Be(DslOutcomeKind.Accepted);
        secondBranchWins.TargetState.Should().Be("Alpha");
    }

    [Fact]
    public void Inspect_StringEqualityGuard_WithQuotedLiteral_IsAccepted()
    {
        const string dsl = """
            machine FeatureMode
            state Draft initial
            state Live
            event Publish
            from Draft on Publish
                if Mode == "Manual"
                    transition Live
                else
                    reject "Manual mode required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Mode"] = "Manual" };

        var inspection = workflow.Inspect("Draft", "Publish", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("Live");
    }

    [Fact]
    public void Inspect_NullEqualityGuard_IsAccepted_WhenDataValueIsNull()
    {
        const string dsl = """
            machine Tickets
            state Open initial
            state Pending
            event Escalate
            from Open on Escalate
                if Assignee == null
                    transition Pending
                else
                    reject "Assignee must be empty"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Assignee"] = null };

        var inspection = workflow.Inspect("Open", "Escalate", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("Pending");
    }

    [Fact]
    public void Inspect_NumericComparison_HandlesMixedNumericRuntimeTypes()
    {
        const string dsl = """
            machine Throughput
            state Low initial
            state High
            event Scale
            from Low on Scale
                if Qps >= 100
                    transition High
                else
                    reject "Qps threshold not met"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Qps"] = 100m };

        var inspection = workflow.Inspect("Low", "Scale", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("High");
    }

    [Fact]
    public void Inspect_LogicalGuardExpression_IsAccepted_WhenTrue()
    {
        const string dsl = """
            machine Workflow
            state A initial
            state B
            event Go
            from A on Go
                if Flag && OtherFlag
                    transition B
                else
                    reject "Both flags must be true"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?>
        {
            ["Flag"] = true,
            ["OtherFlag"] = true
        };

        var inspection = workflow.Inspect("A", "Go", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("B");
    }

    [Fact]
    public void Inspect_InvalidGuardExpression_UsesConfiguredReason()
    {
        const string dsl = """
            machine Workflow
            state A initial
            state B
            event Go
            from A on Go
                if coalesce(Flag, OtherFlag)
                    transition B
                else
                    reject "Both flags must be true"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?>
        {
            ["Flag"] = true,
            ["OtherFlag"] = true
        };

        var inspection = workflow.Inspect("A", "Go", data);

        (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Both flags must be true");
    }

    [Fact]
    public void Fire_GuardedTransition_WithData_AcceptsAndReturnsNewState()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 3 };

        var fire = workflow.Fire("Red", "Advance", data);

        (fire.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void CreateInstance_Then_Fire_UpdatesPersistableInstance()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set CarsWaiting = 0
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.InstanceData["CarsWaiting"].Should().Be(0d);
    }

    [Fact]
    public void Fire_Instance_AppliesTransitionDataAssignment_FromEventArgument()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state FlashingRed
            event Emergency
            from Red on Emergency
                set EmergencyReason = Emergency.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        (fire.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.InstanceData["EmergencyReason"].Should().Be("Accident");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_FromMissingEventArgument_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state FlashingRed
            event Emergency
                string Reason
            from Red on Emergency
                set EmergencyReason = Emergency.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency");

        (fire.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("required argument 'Reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Instance_DataAssignment_FromInstanceDataReference_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
                number CarsWaiting = 0
                number LastCarsWaiting = 0
            event Advance
            from Red on Advance
                set LastCarsWaiting = CarsWaiting
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 3d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastCarsWaiting"].Should().Be(3d);
    }

    [Fact]
    public void Fire_Instance_MultipleSets_AreAppliedInOrder_WithReadYourWrites()
    {
        const string dsl = """
            machine Counters
            number CarsWaiting = 0
            number LastCarsWaiting = 0
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set CarsWaiting = CarsWaiting + 1
                set LastCarsWaiting = CarsWaiting + 1
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 1d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.UpdatedInstance!.InstanceData["CarsWaiting"].Should().Be(2d);
        fire.UpdatedInstance!.InstanceData["LastCarsWaiting"].Should().Be(3d);
    }

    [Fact]
    public void Fire_Instance_MultipleSets_Failure_RollsBackBatch()
    {
        const string dsl = """
            machine Counters
            number CarsWaiting = 0
            number LastCarsWaiting = 0
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set CarsWaiting = CarsWaiting + 1
                set LastCarsWaiting = "bad"
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 1d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.NewState.Should().BeNull();
        fire.UpdatedInstance.Should().BeNull();
        fire.Reasons.Should().ContainSingle(r => r.Contains("Data assignment failed", StringComparison.Ordinal));
        instance.InstanceData["CarsWaiting"].Should().Be(1d);
        instance.InstanceData["LastCarsWaiting"].Should().Be(0d);
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ArithmeticExpression_IsAccepted()
    {
        const string dsl = """
            machine Counters
            number CarsWaiting = 0
            number NextCarsWaiting = 0
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set NextCarsWaiting = CarsWaiting + 2
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 3d,
            ["NextCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["NextCarsWaiting"].Should().Be(5d);
    }

    [Fact]
    public void Fire_Instance_DataAssignment_StringConcatExpression_IsAccepted()
    {
        const string dsl = """
            machine Alerts
            string Prefix = ""
            string Message = ""
            state Red initial
            state FlashingRed
            event Emergency
                string Reason
            from Red on Emergency
                set Message = Prefix + Emergency.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["Prefix"] = "Reason: ",
            ["Message"] = ""
        });

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Message"].Should().Be("Reason: Accident");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_StringConcat_WithNullOperand_IsRejected()
    {
        const string dsl = """
            machine Alerts
            string Prefix = ""
            string? ReasonText
            string Message = ""
            state Red initial
            state FlashingRed
            event Emergency
                string? Reason
            from Red on Emergency
                set Message = Prefix + Emergency.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["Prefix"] = "Reason: ",
            ["ReasonText"] = null,
            ["Message"] = ""
        });

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = null });

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '+' requires number+number or string+string", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TypedEventArguments_AreRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance(AdvanceArgs)
            from Red on Advance
                transition Green
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*inline typed event arguments are not supported*");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_WithEventPrefix_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state FlashingRed
            event Emergency
                string Reason
                string? EmergencyReason
            from Red on Emergency
                set EmergencyReason = Emergency.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["EmergencyReason"] = null
        });
        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["EmergencyReason"].Should().Be("Accident");
    }

    [Fact]
    public void Inspect_AcceptedTransition_InfersRequiredEventArgumentKeys()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state FlashingRed
            event Emergency
                string Reason
            from Red on Emergency
                set EmergencyReason = Emergency.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var inspect = workflow.Inspect(instance, "Emergency");

        (inspect.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspect.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspect.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Reason");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ReservedBooleanLiterals_AreAppliedAsLiterals()
    {
        const string dsl = """
            machine Flags
            state Off initial
            state On
            event Enable
            event Disable
            from Off on Enable
                set IsEnabled = true
                transition On
            from On on Disable
                set IsEnabled = false
                transition Off
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var start = workflow.CreateInstance("Off", new Dictionary<string, object?>());

        var enabled = workflow.Fire(start, "Enable", new Dictionary<string, object?> { ["true"] = "not-used" });
        (enabled.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        enabled.UpdatedInstance!.InstanceData["IsEnabled"].Should().Be(true);

        var disabled = workflow.Fire(enabled.UpdatedInstance!, "Disable", new Dictionary<string, object?> { ["false"] = "not-used" });
        (disabled.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        disabled.UpdatedInstance!.InstanceData["IsEnabled"].Should().Be(false);
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ReservedNullLiteral_IsAppliedAsLiteral()
    {
        const string dsl = """
            machine Notes
            state Open initial
            state Cleared
            event Clear
            from Open on Clear
                set Note = null
                transition Cleared
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Open", new Dictionary<string, object?> { ["Note"] = "Active" });

        var cleared = workflow.Fire(instance, "Clear", new Dictionary<string, object?> { ["null"] = "not-used" });

        (cleared.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        cleared.UpdatedInstance!.InstanceData["Note"].Should().BeNull();
    }

    [Fact]
    public void Inspect_Instance_DoesNotApplyTransitionDataAssignment()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set CarsWaiting = 0
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var inspect = workflow.Inspect(instance, "Advance");

        (inspect.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (inspect.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        instance.InstanceData["CarsWaiting"].Should().Be(3);
    }

    [Fact]
    public void Inspect_Instance_EventArgs_DoNotMergeWithSnapshot()
    {
        const string dsl = """
            machine FeatureFlag
            state Disabled initial
            state Enabled
            event Evaluate
            from Disabled on Evaluate
                if IsEnabled
                    transition Enabled
                else
                    reject "Feature must be enabled"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Disabled", new Dictionary<string, object?> { ["IsEnabled"] = false });

        var rejected = workflow.Inspect(instance, "Evaluate");
        var withConflictingArg = workflow.Inspect(instance, "Evaluate", new Dictionary<string, object?> { ["IsEnabled"] = true });
        var rejectedWithUnrelatedArgs = workflow.Inspect(instance, "Evaluate", new Dictionary<string, object?> { ["Other"] = true });

        (rejected.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (rejected.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        (withConflictingArg.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (withConflictingArg.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        withConflictingArg.Reasons.Should().ContainSingle("Feature must be enabled");
        (rejectedWithUnrelatedArgs.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (rejectedWithUnrelatedArgs.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        rejectedWithUnrelatedArgs.Reasons.Should().NotBeEmpty();
    }

    [Fact]
    public void Inspect_Instance_WithWorkflowMismatch_IsNotDefinedWithReason()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var incompatible = new DslWorkflowInstance(
            "OtherWorkflow",
            "Red",
            null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>());

        var inspection = workflow.Inspect(incompatible, "Advance");

        inspection.Outcome.Should().Be(DslOutcomeKind.NotDefined);
        (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("workflow", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_AcceptedTransition_ReturnsNewState()
    {
        const string dsl = """
            machine TrafficLight
            state Green initial
            state Yellow
            event Advance
            from Green on Advance
                transition Yellow
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var fire = workflow.Fire("Green", "Advance");

        (fire.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Yellow");
        fire.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Fire_UndefinedTransition_ReturnsNotDefined()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var fire = workflow.Fire("Green", "Advance");

        fire.Outcome.Should().Be(DslOutcomeKind.NotDefined);
        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.NewState.Should().BeNull();
        fire.Reasons.Should().ContainSingle();
    }

    [Fact]
    public void Parse_DuplicateState_Throws()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Red
            event Advance
            from Red on Advance
                transition Red
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
            state Red initial
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    set CarsWaiting = 0
                    transition Green
                else
                    reject "Cars waiting required"
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle();
        var guardedClause = machine.Transitions[0].Clauses.Single(c => c.Outcome is DslStateTransition);
        guardedClause.Predicate.Should().Be("CarsWaiting > 0");
        guardedClause.SetAssignments.Should().ContainSingle();
        guardedClause.SetAssignments[0].Key.Should().Be("CarsWaiting");
        guardedClause.SetAssignments[0].ExpressionText.Should().Be("0");
        var rejectClause = machine.Transitions[0].Clauses.Single(c => c.Outcome is DslRejection);
        ((DslRejection)rejectClause.Outcome).Reason.Should().Be("Cars waiting required");
    }

    [Fact]
    public void Parse_FromOnBlock_WithMultipleSets_PreservesAssignmentOrder()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
                set CarsWaiting = CarsWaiting + 1
                set LastCarsWaiting = CarsWaiting
                transition Green
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle();
        machine.Transitions[0].Clauses[0].SetAssignments.Should().HaveCount(2);
        machine.Transitions[0].Clauses[0].SetAssignments[0].Key.Should().Be("CarsWaiting");
        machine.Transitions[0].Clauses[0].SetAssignments[0].ExpressionText.Should().Be("CarsWaiting + 1");
        machine.Transitions[0].Clauses[0].SetAssignments[1].Key.Should().Be("LastCarsWaiting");
        machine.Transitions[0].Clauses[0].SetAssignments[1].ExpressionText.Should().Be("CarsWaiting");
    }

    [Fact]
    public void Parse_GuardedTransition_WithoutReason_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red initial
            state Green
            event Advance
            transition Red -> Green on Advance reason "Cars waiting required"
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*inline transition declarations are not supported*from <State> on <Event>*");
    }

        [Fact]
        public void Parse_FromOnBlock_WithReject_ParsesOutcomeRule()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red initial
                        state Green
                        event Advance
                        from Red on Advance
                            if CarsWaiting > 0
                                transition Green
                            else
                                reject "No cars waiting"
                        """;

                var machine = StateMachineDslParser.Parse(dsl);

                machine.Transitions.Should().ContainSingle();
                machine.Transitions[0].FromState.Should().Be("Red");
                machine.Transitions[0].Clauses.Any(c => c.Outcome is DslStateTransition st && st.TargetState == "Green").Should().BeTrue();
                machine.Transitions[0].Clauses.Any(c => c.Outcome is DslRejection).Should().BeTrue();
                ((DslRejection)machine.Transitions[0].Clauses.Single(c => c.Outcome is DslRejection).Outcome).Reason.Should().Be("No cars waiting");
        }

        [Fact]
        public void Inspect_FromOnBlock_AllGuardsFail_UsesOutcomeRejectReason()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red initial
                        state Green
                        event Advance
                        from Red on Advance
                            if CarsWaiting > 0
                                transition Green
                            else
                                reject "No cars waiting"
                        """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                var inspection = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });

                (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
                (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
                inspection.Outcome.Should().Be(DslOutcomeKind.Rejected);
                inspection.Reasons.Should().ContainSingle("No cars waiting");
        }

        [Fact]
        public void Inspect_FromOnBlock_NoTransitionOutcome_IsNoTransition()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red initial
                        state Green
                        event Advance
                        from Red on Advance
                            if CarsWaiting > 0
                                transition Green
                            else
                                no transition
                        """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                var inspection = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });

                (inspection.Outcome is DslOutcomeKind.NotDefined).Should().BeFalse();
                (inspection.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
                inspection.Outcome.Should().Be(DslOutcomeKind.AcceptedInPlace);
                inspection.TargetState.Should().Be("Red");
        }

        [Fact]
        public void Parse_FromAny_ExpandsToAllStates()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red initial
                        state Green
                        state Yellow
                        state FlashingRed
                        event Emergency
                        from any on Emergency
                            if Emergency.Reason != ""
                                transition FlashingRed
                            else
                                reject "Emergency reason is required"
                        """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
                var data = new Dictionary<string, object?> { ["Reason"] = "Accident" };

                workflow.Inspect("Red", "Emergency", data).Outcome.Should().BeOneOf(DslOutcomeKind.Accepted, DslOutcomeKind.AcceptedInPlace);
                workflow.Inspect("Green", "Emergency", data).Outcome.Should().BeOneOf(DslOutcomeKind.Accepted, DslOutcomeKind.AcceptedInPlace);
                workflow.Inspect("Yellow", "Emergency", data).Outcome.Should().BeOneOf(DslOutcomeKind.Accepted, DslOutcomeKind.AcceptedInPlace);
        }

        [Fact]
        public void Parse_FromOnBlock_WithoutOutcome_IsRejected()
        {
            const string dsl = """
                machine TrafficLight
                state Red initial
                state Green
                event Advance
                from Red on Advance
                """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                        .Throw<InvalidOperationException>()
                    .WithMessage("*must end with an outcome statement*");
        }

        [Fact]
        public void Parse_IfChain_WithBlockLevelFallback_WithoutElse_Throws()
        {
            const string dsl = """
                machine TrafficLight
                state Red initial
                state Green
                event Advance
                from Red on Advance
                    if CarsWaiting > 0
                        transition Green
                    reject "Cars waiting required"
                """;

            var act = () => StateMachineDslParser.Parse(dsl);

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*block-level statement after an 'if' chain requires 'else'*");
        }

        [Fact]
        public void Parse_ElseIfChain_WithBlockLevelFallback_WithoutElse_Throws()
        {
            const string dsl = """
                machine TrafficLight
                state Red initial
                state Green
                state Yellow
                event Advance
                from Red on Advance
                    if CarsWaiting > 3
                        transition Green
                    else if CarsWaiting > 0
                        transition Yellow
                    reject "No cars waiting"
                """;

            var act = () => StateMachineDslParser.Parse(dsl);

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*block-level statement after an 'if' chain requires 'else'*");
        }

            [Fact]
            public void Parse_DuplicateOutcomeRule_ForSameFromOn_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                                reject "First"

                    from Red on Advance
                                reject "Second"
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*duplicate 'from Red on Advance' block*");
            }

            [Fact]
            public void Parse_FromOnBlock_StatementAfterOutcomeTransition_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Yellow initial
                    state Red
                    event Advance

                    from Yellow on Advance
                        transition Red
                        no transition
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*no statements are allowed after an outcome statement*");
            }

            [Fact]
            public void Inspect_FromOnBlock_ElseIfChain_SelectsFirstMatchingBranch()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red initial
                    state Green
                    state Yellow
                    event Advance

                    from Red on Advance
                        if CarsWaiting > 3
                            transition Green
                        else if CarsWaiting > 0
                            transition Yellow
                        else
                            reject "No cars waiting"
                    """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 5 }).TargetState.Should().Be("Green");
                workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 1 }).TargetState.Should().Be("Yellow");

                var rejected = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });
                rejected.Outcome.Should().Be(DslOutcomeKind.Rejected);
                rejected.Reasons.Should().ContainSingle("No cars waiting");
            }

            [Fact]
            public void Parse_FromOnBlock_UnknownBranchKeyword_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        unless CarsWaiting > 0
                            transition Green
                        reject "No cars waiting"
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*unrecognized statement 'unless CarsWaiting > 0' inside from/on block.*");
            }

            [Fact]
            public void Parse_IfBlock_WithReason_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        if CarsWaiting > 0 reason "No demand"
                            transition Green
                        reject "No cars waiting"
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*'reason' is not allowed on 'if' branches*");
            }

            [Fact]
            public void Parse_SetStatement_IsAccepted()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting = 0
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        set CarsWaiting = 0
                        transition Green
                    """;

                var machine = StateMachineDslParser.Parse(dsl);

                machine.Transitions.Should().ContainSingle();
                machine.Transitions[0].Clauses[0].SetAssignments.Should().ContainSingle();
                machine.Transitions[0].Clauses[0].SetAssignments[0].Key.Should().Be("CarsWaiting");
            }

            [Fact]
            public void Parse_DataField_DefaultLiteral_IsAccepted()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting = 3
                    string? Note = null
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var machine = StateMachineDslParser.Parse(dsl);

                machine.Fields.Should().HaveCount(2);
                machine.Fields[0].HasDefaultValue.Should().BeTrue();
                machine.Fields[0].DefaultValue.Should().Be(3d);
                machine.Fields[1].HasDefaultValue.Should().BeTrue();
                machine.Fields[1].DefaultValue.Should().BeNull();
            }

            [Fact]
            public void Parse_DataField_DefaultMustBeLiteral_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting = OtherCount + 1
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*default value for field 'CarsWaiting' must be a literal*");
            }

            [Fact]
            public void Parse_DataField_DefaultTypeMismatch_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting = "three"
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*default value for field 'CarsWaiting' does not match declared type*");
            }

            [Fact]
            public void Parse_NonNullableDataField_WithoutDefault_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*non-nullable field 'CarsWaiting' requires a default value*");
            }

            [Fact]
            public void Parse_NullableDataField_WithoutDefault_IsAccepted()
            {
                const string dsl = """
                    machine TrafficLight
                    string? Note
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var machine = StateMachineDslParser.Parse(dsl);

                machine.Fields.Should().ContainSingle();
                machine.Fields[0].Name.Should().Be("Note");
                machine.Fields[0].IsNullable.Should().BeTrue();
                machine.Fields[0].HasDefaultValue.Should().BeFalse();
            }

            [Fact]
            public void Parse_MissingInitialState_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*Exactly one state must be marked initial*");
            }

            [Fact]
            public void Parse_DuplicateInitialStateMarkers_AreRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red initial
                    state Green initial
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*duplicate initial state marker*already marked initial*");
            }

            [Fact]
            public void Parse_StatesPluralDeclaration_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    states Red, Green
                    event Advance
                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*'states' declaration is not supported*");
            }

            [Fact]
            public void Parse_EventsPluralDeclaration_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red initial
                    state Green
                    events Advance
                    from Red on Advance
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*'events' declaration is not supported*");
            }

            [Fact]
            public void CreateInstance_WithoutData_UsesDeclaredFieldDefaults()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting = 0
                    string? Note = null
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                var instance = workflow.CreateInstance("Red");

                instance.InstanceData["CarsWaiting"].Should().Be(0d);
                instance.InstanceData["Note"].Should().BeNull();
            }

            [Fact]
            public void CreateInstance_ProvidedData_OverridesDeclaredDefaults()
            {
                const string dsl = """
                    machine TrafficLight
                    number CarsWaiting = 0
                    state Red initial
                    state Green
                    event Advance

                    from Red on Advance
                        transition Green
                    """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 5d });

                instance.InstanceData["CarsWaiting"].Should().Be(5d);
            }

    [Fact]
    public void Parse_EventArg_WithDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string Reason = "none"
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.Name.Should().Be("Reason");
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be("none");
        arg.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Parse_NullableEventArg_WithDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string? Reason = "fallback"
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.Name.Should().Be("Reason");
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be("fallback");
        arg.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Parse_NullableEventArg_WithNullDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string? Reason = null
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Parse_NonNullableEventArg_WithoutDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string Reason
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.Name.Should().Be("Reason");
        arg.HasDefaultValue.Should().BeFalse();
        arg.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Parse_NullableEventArg_WithoutDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string? Reason
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeFalse();
        arg.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Parse_EventArg_NumberDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                number Priority = 5
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be(5d);
    }

    [Fact]
    public void Parse_EventArg_BooleanDefault_IsAccepted()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                boolean Urgent = false
            from A on Submit
                no transition
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be(false);
    }

    [Fact]
    public void Parse_NonNullableEventArg_WithNullDefault_IsRejected()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string Reason = null
            from A on Submit
                no transition
            """;

        var act = () => StateMachineDslParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*default value*does not match*");
    }

    [Fact]
    public void Fire_NonNullableEventArg_WithDefault_OmittedByCaller_UsesDefault()
    {
        const string dsl = """
            machine Workflow
            string? LastReason
            state A initial
            event Submit
                string Reason = "auto"
            from A on Submit
                set LastReason = Submit.Reason
                no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().Be("auto");
    }

    [Fact]
    public void Fire_NonNullableEventArg_WithDefault_SuppliedByCaller_UsesSupplied()
    {
        const string dsl = """
            machine Workflow
            string? LastReason
            state A initial
            event Submit
                string Reason = "auto"
            from A on Submit
                set LastReason = Submit.Reason
                no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["Reason"] = "manual" });

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().Be("manual");
    }

    [Fact]
    public void Fire_NullableEventArg_WithDefault_OmittedByCaller_UsesDefault()
    {
        const string dsl = """
            machine Workflow
            string? LastReason
            state A initial
            event Submit
                string? Reason = "fallback"
            from A on Submit
                set LastReason = Submit.Reason
                no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().Be("fallback");
    }

    [Fact]
    public void Fire_NullableEventArg_WithoutDefault_OmittedByCaller_UsesNull()
    {
        const string dsl = """
            machine Workflow
            string? LastReason
            state A initial
            event Submit
                string? Reason
            from A on Submit
                set LastReason = Submit.Reason
                no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().BeNull();
    }

    [Fact]
    public void Fire_NonNullableEventArg_WithoutDefault_OmittedByCaller_IsRejected()
    {
        const string dsl = """
            machine Workflow
            state A initial
            event Submit
                string Reason
            from A on Submit
                no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("required argument 'Reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_RequiredEventArgumentKeys_ExcludesArgsWithDefaults()
    {
        const string dsl = """
            machine Workflow
            state A initial
            state B
            event Submit
                string Reason
                number Priority = 1
                string? Note
            from A on Submit
                transition B
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var inspect = workflow.Inspect(instance, "Submit");

        inspect.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Reason");
    }

    // ── Duplicate from/on block detection ─────────────────────────────────────

    [Fact]
    public void Parse_DuplicateFromOnBlock_ThrowsParseError()
    {
        const string dsl = """
            machine M
            state A initial
            state B
            event Submit
            from A on Submit
                transition B
            from A on Submit
                transition B
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate 'from A on Submit' block*");
    }

    [Fact]
    public void Parse_FromAny_ThenSpecificState_ThrowsDuplicateError()
    {
        const string dsl = """
            machine M
            state A initial
            state B
            event Submit
            from any on Submit
                transition B
            from A on Submit
                transition B
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate 'from A on Submit' block*");
    }

    [Fact]
    public void Parse_CommaList_ThenOverlappingState_ThrowsDuplicateError()
    {
        const string dsl = """
            machine M
            state A initial
            state B
            state C
            event Submit
            from A,B on Submit
                transition C
            from B on Submit
                transition A
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate 'from B on Submit' block*");
    }

    [Fact]
    public void Parse_DifferentStates_SameEvent_IsValid()
    {
        const string dsl = """
            machine M
            state A initial
            state B
            event Submit
            from A on Submit
                transition B
            from B on Submit
                transition A
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_SameState_DifferentEvents_IsValid()
    {
        const string dsl = """
            machine M
            state A initial
            state B
            event EventX
            event EventY
            from A on EventX
                transition B
            from A on EventY
                transition B
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().NotThrow();
    }

    // Regression: when the 'when' predicate is false, Inspect must return NotApplicable
    // even when event args are omitted (discovery-mode / bulk-refresh callers).
    // Previously, arg-validation ran before the 'when' check, so an omitted required arg
    // caused Rejected instead of NotApplicable.
    [Fact]
    public void Inspect_Instance_WhenPredicateFalse_ReturnsNotApplicable_WithNoEventArgs()
    {
        const string dsl = """
            machine BankAccount
            boolean Frozen = false
            state Active initial
            event Deposit
              number Amount
            from Active on Deposit when !Frozen
              no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        // Frozen=true → 'when !Frozen' is false → NotApplicable regardless of args
        var frozen = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Frozen"] = true });

        // No event args supplied (simulates discovery-mode bulk refresh)
        var noArgs = workflow.Inspect(frozen, "Deposit");
        noArgs.Outcome.Should().Be(DslOutcomeKind.NotApplicable,
            because: "when predicate is false, NotApplicable must be returned before arg validation");

        // Explicit empty args dict — same expectation
        var emptyArgs = workflow.Inspect(frozen, "Deposit", new Dictionary<string, object?>());
        emptyArgs.Outcome.Should().Be(DslOutcomeKind.NotApplicable,
            because: "empty arg dict with false when predicate must still yield NotApplicable");
    }

    [Fact]
    public void Inspect_Instance_WhenPredicateTrue_ValidatesArgsNormally()
    {
        const string dsl = """
            machine BankAccount
            boolean Frozen = false
            state Active initial
            event Deposit
              number Amount
            from Active on Deposit when !Frozen
              no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        // Frozen=false → 'when !Frozen' is true → falls through to normal arg validation
        var unfrozen = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Frozen"] = false });

        // No args: predicate passes, but required 'Amount' is missing → Rejected
        var noArgs = workflow.Inspect(unfrozen, "Deposit");
        noArgs.Outcome.Should().NotBe(DslOutcomeKind.NotApplicable,
            because: "when predicate is true, the call proceeds to arg validation");

        // Correct args: should be accepted
        var withArgs = workflow.Inspect(unfrozen, "Deposit", new Dictionary<string, object?> { ["Amount"] = 100.0 });
        withArgs.Outcome.Should().BeOneOf(DslOutcomeKind.Accepted, DslOutcomeKind.AcceptedInPlace);
    }
}
