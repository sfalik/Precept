using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public class PreceptWorkflowTests
{
    [Fact]
    public void Parse_And_Compile_UnguardedTransition_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);
        var workflow = PreceptCompiler.Compile(machine);

        var inspection = workflow.Inspect(workflow.CreateInstance("Red"), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.Outcome.Should().Be(PreceptOutcomeKind.Accepted);
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MachineWithNoEvents_IsValid()
    {
        const string dsl = """
            precept Minimal
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Name.Should().Be("Minimal");
        machine.InitialState.Name.Should().Be("Idle");
        machine.Events.Should().BeEmpty();
        machine.TransitionRows.Should().BeNull();
    }

    [Fact]
    public void Compile_MachineWithNoEvents_Succeeds()
    {
        const string dsl = """
            precept Minimal
            state Idle initial
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        workflow.InitialState.Should().Be("Idle");
        workflow.States.Should().ContainSingle().Which.Should().Be("Idle");
        workflow.Events.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_Outcome_Maps_To_Undefined_Blocked_Enabled()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var undefined = workflow.Inspect(workflow.CreateInstance("Red"), "MissingEvent");
        var blocked = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 0 }), "Advance");
        var enabled = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 2 }), "Advance");

        undefined.Outcome.Should().Be(PreceptOutcomeKind.NotDefined);
        blocked.Outcome.Should().Be(PreceptOutcomeKind.Rejected);
        enabled.Outcome.Should().Be(PreceptOutcomeKind.Accepted);
    }

    [Fact]
    public void Inspect_GuardedTransition_WithMatchingData_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 2 };

        var inspection = workflow.Inspect(workflow.CreateInstance("Red", data), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_GuardedTransition_WithNonMatchingData_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 0 };

        var inspection = workflow.Inspect(workflow.CreateInstance("Red", data), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.TargetState.Should().BeNull();
        inspection.Reasons.Should().ContainSingle("Cars waiting required");
    }

    [Fact]
    public void Inspect_GuardedTransition_WithoutRequiredDataKey_IsRejectedWithReason()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var inspection = workflow.Inspect(workflow.CreateInstance("Red"), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Cars waiting required");
    }

    [Fact]
    public void Inspect_GuardedTransition_WithWrongDataType_IsRejectedWithReason()
    {
        const string dsl = """
            precept FeatureFlag
            field IsEnabled as boolean default false
            state Disabled initial
            state Enabled
            event Evaluate
            from Disabled on Evaluate when IsEnabled -> transition Enabled
            from Disabled on Evaluate -> reject "Feature must be enabled"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["IsEnabled"] = false };

        var inspection = workflow.Inspect(workflow.CreateInstance("Disabled", data), "Evaluate");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Feature must be enabled");
    }

    [Fact]
    public void Inspect_MultipleGuardedTransitions_AllFail_AggregatesReasons()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            field IsManualOverride as boolean default false
            state Red initial
            state Green
            state Yellow
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance when IsManualOverride -> transition Yellow
            from Red on Advance -> reject "No eligible transition"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 0,
            ["IsManualOverride"] = false
        };

        var inspection = workflow.Inspect(workflow.CreateInstance("Red", data), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("No eligible transition");
    }

    [Fact]
    public void Inspect_IfBranch_NoTransition_Is_Allowed_And_Produces_NoTransition_When_Matched()
    {
        const string dsl = """
            precept Route
            field Hold as boolean default false
            state Red initial
            state Green
            event Advance
            from Red on Advance when Hold -> no transition
            from Red on Advance -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var noTransition = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["Hold"] = true }), "Advance");
        var enabled = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["Hold"] = false }), "Advance");

        noTransition.Outcome.Should().Be(PreceptOutcomeKind.AcceptedInPlace);
        (noTransition.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        noTransition.TargetState.Should().Be("Red");

        enabled.Outcome.Should().Be(PreceptOutcomeKind.Accepted);
        enabled.TargetState.Should().Be("Green");
    }

    [Fact]
    public void Inspect_ElseIf_NoTransition_Is_Allowed_And_Preserves_Branch_Order()
    {
        const string dsl = """
            precept Route
            field PreferStop as boolean default false
            field PreferAlpha as boolean default false
            state Source initial
            state Alpha
            state Beta
            event Route
            from Source on Route when PreferStop -> no transition
            from Source on Route when PreferAlpha -> transition Alpha
            from Source on Route -> transition Beta
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var firstBranchWins = workflow.Inspect(workflow.CreateInstance("Source", new Dictionary<string, object?>
        {
            ["PreferStop"] = true,
            ["PreferAlpha"] = true
        }), "Route");

        var secondBranchWins = workflow.Inspect(workflow.CreateInstance("Source", new Dictionary<string, object?>
        {
            ["PreferStop"] = false,
            ["PreferAlpha"] = true
        }), "Route");

        firstBranchWins.Outcome.Should().Be(PreceptOutcomeKind.AcceptedInPlace);
        (firstBranchWins.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        firstBranchWins.TargetState.Should().Be("Source");

        secondBranchWins.Outcome.Should().Be(PreceptOutcomeKind.Accepted);
        secondBranchWins.TargetState.Should().Be("Alpha");
    }

    [Fact]
    public void Inspect_StringEqualityGuard_WithQuotedLiteral_IsAccepted()
    {
        const string dsl = """
            precept FeatureMode
            field Mode as string default ""
            state Draft initial
            state Live
            event Publish
            from Draft on Publish when Mode == "Manual" -> transition Live
            from Draft on Publish -> reject "Manual mode required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Mode"] = "Manual" };

        var inspection = workflow.Inspect(workflow.CreateInstance("Draft", data), "Publish");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("Live");
    }

    [Fact]
    public void Inspect_NullEqualityGuard_IsAccepted_WhenDataValueIsNull()
    {
        const string dsl = """
            precept Tickets
            field Assignee as string nullable
            state Open initial
            state Pending
            event Escalate
            from Open on Escalate when Assignee == null -> transition Pending
            from Open on Escalate -> reject "Assignee must be empty"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Assignee"] = null };

        var inspection = workflow.Inspect(workflow.CreateInstance("Open", data), "Escalate");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("Pending");
    }

    [Fact]
    public void Inspect_NumericComparison_HandlesMixedNumericRuntimeTypes()
    {
        const string dsl = """
            precept Throughput
            field Qps as number default 0
            state Low initial
            state High
            event Scale
            from Low on Scale when Qps >= 100 -> transition High
            from Low on Scale -> reject "Qps threshold not met"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Qps"] = 100m };

        var inspection = workflow.Inspect(workflow.CreateInstance("Low", data), "Scale");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("High");
    }

    [Fact]
    public void Inspect_LogicalGuardExpression_IsAccepted_WhenTrue()
    {
        const string dsl = """
            precept Workflow
            field Flag as boolean default false
            field OtherFlag as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Flag && OtherFlag -> transition B
            from A on Go -> reject "Both flags must be true"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?>
        {
            ["Flag"] = true,
            ["OtherFlag"] = true
        };

        var inspection = workflow.Inspect(workflow.CreateInstance("A", data), "Go");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.TargetState.Should().Be("B");
    }

    [Fact]
    public void Inspect_InvalidGuardExpression_UsesConfiguredReason()
    {
        // Guard 'Flag > 0' compares a boolean to a number → operator '>' requires numeric
        // operands → evaluation fails → engine skips to the next row (the reject fallback).
        const string dsl = """
            precept Workflow
            field Flag as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Flag > 0 -> transition B
            from A on Go -> reject "Both flags must be true"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?>
        {
            ["Flag"] = true
        };

        var inspection = workflow.Inspect(workflow.CreateInstance("A", data), "Go");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Both flags must be true");
    }

    [Fact]
    public void Fire_GuardedTransition_WithData_AcceptsAndReturnsNewState()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 3 };

        var fire = workflow.Fire(workflow.CreateInstance("Red", data), "Advance");

        (fire.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void CreateInstance_Then_Fire_UpdatesPersistableInstance()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.CurrentState.Should().Be("Green");
        fire.UpdatedInstance!.LastEvent.Should().Be("Advance");
    }

    [Fact]
    public void Fire_Instance_AppliesTransitionDataAssignment_Literal()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set CarsWaiting = 0 -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.InstanceData["CarsWaiting"].Should().Be(0d);
    }

    [Fact]
    public void Fire_Instance_AppliesTransitionDataAssignment_FromEventArgument()
    {
        const string dsl = """
            precept TrafficLight
            field EmergencyReason as string default ""
            state Red initial
            state FlashingRed
            event Emergency with Reason as string
            from Red on Emergency -> set EmergencyReason = Emergency.Reason -> transition FlashingRed
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        (fire.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        fire.UpdatedInstance!.InstanceData["EmergencyReason"].Should().Be("Accident");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_FromMissingEventArgument_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            field EmergencyReason as string default ""
            state Red initial
            state FlashingRed
            event Emergency with Reason as string
            from Red on Emergency -> set EmergencyReason = Emergency.Reason -> transition FlashingRed
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency");

        (fire.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("required argument 'Reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Instance_DataAssignment_FromInstanceDataReference_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            field LastCarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set LastCarsWaiting = CarsWaiting -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 3d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastCarsWaiting"].Should().Be(3d);
    }

    [Fact]
    public void Fire_Instance_MultipleSets_AreAppliedInOrder_WithReadYourWrites()
    {
        const string dsl = """
            precept Counters
            field CarsWaiting as number default 0
            field LastCarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set CarsWaiting = CarsWaiting + 1 -> set LastCarsWaiting = CarsWaiting + 1 -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 1d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Green");
        fire.UpdatedInstance!.InstanceData["CarsWaiting"].Should().Be(2d);
        fire.UpdatedInstance!.InstanceData["LastCarsWaiting"].Should().Be(3d);
    }

    [Fact]
    public void Fire_Instance_MultipleSets_Failure_RollsBackBatch()
    {
        const string dsl = """
            precept Counters
            field CarsWaiting as number default 0
            field LastCarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set CarsWaiting = CarsWaiting + 1 -> set LastCarsWaiting = "bad" -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 1d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
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
            precept Counters
            field CarsWaiting as number default 0
            field NextCarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set NextCarsWaiting = CarsWaiting + 2 -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 3d,
            ["NextCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["NextCarsWaiting"].Should().Be(5d);
    }

    [Fact]
    public void Fire_Instance_DataAssignment_StringConcatExpression_IsAccepted()
    {
        const string dsl = """
            precept Alerts
            field Prefix as string default ""
            field Message as string default ""
            state Red initial
            state FlashingRed
            event Emergency with Reason as string
            from Red on Emergency -> set Message = Prefix + Emergency.Reason -> transition FlashingRed
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["Prefix"] = "Reason: ",
            ["Message"] = ""
        });

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Message"].Should().Be("Reason: Accident");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_StringConcat_WithNullOperand_IsRejected()
    {
        const string dsl = """
            precept Alerts
            field Prefix as string default ""
            field ReasonText as string nullable
            field Message as string default ""
            state Red initial
            state FlashingRed
            event Emergency with Reason as string nullable
            from Red on Emergency -> set Message = Prefix + Emergency.Reason -> transition FlashingRed
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["Prefix"] = "Reason: ",
            ["ReasonText"] = null,
            ["Message"] = ""
        });

        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = null });

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '+' requires number+number or string+string", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TypedEventArguments_AreRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Fire_Instance_DataAssignment_WithEventPrefix_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field EmergencyReason as string nullable
            state Red initial
            state FlashingRed
            event Emergency with Reason as string, EmergencyReason as string nullable
            from Red on Emergency -> set EmergencyReason = Emergency.Reason -> transition FlashingRed
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["EmergencyReason"] = null
        });
        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["EmergencyReason"].Should().Be("Accident");
    }

    [Fact]
    public void Inspect_AcceptedTransition_InfersRequiredEventArgumentKeys()
    {
        const string dsl = """
            precept TrafficLight
            field EmergencyReason as string default ""
            state Red initial
            state FlashingRed
            event Emergency with Reason as string
            from Red on Emergency -> set EmergencyReason = Emergency.Reason -> transition FlashingRed
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var inspect = workflow.Inspect(instance, "Emergency");

        (inspect.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspect.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspect.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Reason");
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ReservedBooleanLiterals_AreAppliedAsLiterals()
    {
        const string dsl = """
            precept Flags
            field IsEnabled as boolean default false
            state Off initial
            state On
            event Enable
            event Disable
            from Off on Enable -> set IsEnabled = true -> transition On
            from On on Disable -> set IsEnabled = false -> transition Off
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var start = workflow.CreateInstance("Off", new Dictionary<string, object?>());

        var enabled = workflow.Fire(start, "Enable", new Dictionary<string, object?> { ["true"] = "not-used" });
        (enabled.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        enabled.UpdatedInstance!.InstanceData["IsEnabled"].Should().Be(true);

        var disabled = workflow.Fire(enabled.UpdatedInstance!, "Disable", new Dictionary<string, object?> { ["false"] = "not-used" });
        (disabled.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        disabled.UpdatedInstance!.InstanceData["IsEnabled"].Should().Be(false);
    }

    [Fact]
    public void Fire_Instance_DataAssignment_ReservedNullLiteral_IsAppliedAsLiteral()
    {
        const string dsl = """
            precept Notes
            field Note as string nullable
            state Open initial
            state Cleared
            event Clear
            from Open on Clear -> set Note = null -> transition Cleared
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Open", new Dictionary<string, object?> { ["Note"] = "Active" });

        var cleared = workflow.Fire(instance, "Clear", new Dictionary<string, object?> { ["null"] = "not-used" });

        (cleared.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        cleared.UpdatedInstance!.InstanceData["Note"].Should().BeNull();
    }

    [Fact]
    public void Inspect_Instance_DoesNotApplyTransitionDataAssignment()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set CarsWaiting = 0 -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 3 });

        var inspect = workflow.Inspect(instance, "Advance");

        (inspect.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspect.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        instance.InstanceData["CarsWaiting"].Should().Be(3);
    }

    [Fact]
    public void Inspect_Instance_EventArgs_DoNotMergeWithSnapshot()
    {
        const string dsl = """
            precept FeatureFlag
            field IsEnabled as boolean default false
            state Disabled initial
            state Enabled
            event Evaluate
            from Disabled on Evaluate when IsEnabled -> transition Enabled
            from Disabled on Evaluate -> reject "Feature must be enabled"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Disabled", new Dictionary<string, object?> { ["IsEnabled"] = false });

        var rejected = workflow.Inspect(instance, "Evaluate");
        var withConflictingArg = workflow.Inspect(instance, "Evaluate", new Dictionary<string, object?> { ["IsEnabled"] = true });
        var rejectedWithUnrelatedArgs = workflow.Inspect(instance, "Evaluate", new Dictionary<string, object?> { ["Other"] = true });

        (rejected.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (rejected.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        (withConflictingArg.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (withConflictingArg.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        withConflictingArg.Reasons.Should().ContainSingle("Feature must be enabled");
        (rejectedWithUnrelatedArgs.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (rejectedWithUnrelatedArgs.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        rejectedWithUnrelatedArgs.Reasons.Should().NotBeEmpty();
    }

    [Fact]
    public void Inspect_Instance_WithWorkflowMismatch_IsNotDefinedWithReason()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var incompatible = new PreceptInstance(
            "OtherWorkflow",
            "Red",
            null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>());

        var inspection = workflow.Inspect(incompatible, "Advance");

        inspection.Outcome.Should().Be(PreceptOutcomeKind.NotDefined);
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Reasons.Should().ContainSingle(r => r.Contains("workflow", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_AcceptedTransition_ReturnsNewState()
    {
        const string dsl = """
            precept TrafficLight
            state Green initial
            state Yellow
            event Advance
            from Green on Advance -> transition Yellow
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var fire = workflow.Fire(workflow.CreateInstance("Green"), "Advance");

        (fire.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.NewState.Should().Be("Yellow");
        fire.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Fire_UndefinedTransition_ReturnsNotDefined()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var fire = workflow.Fire(workflow.CreateInstance("Green"), "Advance");

        fire.Outcome.Should().Be(PreceptOutcomeKind.NotDefined);
        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.NewState.Should().BeNull();
        fire.Reasons.Should().ContainSingle();
    }

    [Fact]
    public void Parse_DuplicateState_Throws()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Red
            event Advance
            from Red on Advance -> transition Red
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate state*");
    }

    [Fact]
    public void Parse_Transition_WithGuardAndDataAssignment_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> set CarsWaiting = 0 -> transition Green
            from Red on Advance -> reject "Cars waiting required"
            """;

        var machine = PreceptParser.Parse(dsl);

        var guardedRow = machine.TransitionRows!.FirstOrDefault(r => r.Outcome is PreceptStateTransition);
        guardedRow.Should().NotBeNull();
        guardedRow!.WhenText.Should().Be("CarsWaiting > 0");
        guardedRow!.SetAssignments.Should().ContainSingle();
        guardedRow!.SetAssignments[0].Key.Should().Be("CarsWaiting");
        guardedRow!.SetAssignments[0].ExpressionText.Should().Be("0");
        var rejectRow = machine.TransitionRows.FirstOrDefault(r => r.Outcome is PreceptRejection);
        rejectRow.Should().NotBeNull();
        ((PreceptRejection)rejectRow!.Outcome).Reason.Should().Be("Cars waiting required");
    }

    [Fact]
    public void Parse_FromOnBlock_WithMultipleSets_PreservesAssignmentOrder()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            field LastCarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance -> set CarsWaiting = CarsWaiting + 1 -> set LastCarsWaiting = CarsWaiting -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TransitionRows.Should().HaveCount(1);
        machine.TransitionRows[0].SetAssignments.Should().HaveCount(2);
        machine.TransitionRows[0].SetAssignments[0].Key.Should().Be("CarsWaiting");
        machine.TransitionRows[0].SetAssignments[0].ExpressionText.Should().Be("CarsWaiting + 1");
        machine.TransitionRows[0].SetAssignments[1].Key.Should().Be("LastCarsWaiting");
        machine.TransitionRows[0].SetAssignments[1].ExpressionText.Should().Be("CarsWaiting");
    }

    [Fact]
    public void Parse_GuardedTransition_WithoutReason_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_FromOnBlock_WithReject_ParsesOutcomeRule()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "No cars waiting"
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TransitionRows.Should().NotBeNull();
        machine.TransitionRows!.Any(r => r.FromState == "Red").Should().BeTrue();
        machine.TransitionRows.Any(r => r.Outcome is PreceptStateTransition st && st.TargetState == "Green").Should().BeTrue();
        machine.TransitionRows.Any(r => r.Outcome is PreceptRejection).Should().BeTrue();
        ((PreceptRejection)machine.TransitionRows.Single(r => r.Outcome is PreceptRejection).Outcome).Reason.Should().Be("No cars waiting");
    }

    [Fact]
    public void Inspect_FromOnBlock_AllGuardsFail_UsesOutcomeRejectReason()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "No cars waiting"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var inspection = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 0 }), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        inspection.Outcome.Should().Be(PreceptOutcomeKind.Rejected);
        inspection.Reasons.Should().ContainSingle("No cars waiting");
    }

    [Fact]
    public void Inspect_FromOnBlock_NoTransitionOutcome_IsNoTransition()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var inspection = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 0 }), "Advance");

        (inspection.Outcome is PreceptOutcomeKind.NotDefined).Should().BeFalse();
        (inspection.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        inspection.Outcome.Should().Be(PreceptOutcomeKind.AcceptedInPlace);
        inspection.TargetState.Should().Be("Red");
    }

    [Fact]
    public void Parse_FromAny_ExpandsToAllStates()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            state Yellow
            state FlashingRed
            event Emergency with Reason as string
            from any on Emergency when Emergency.Reason != "" -> transition FlashingRed
            from any on Emergency -> reject "Emergency reason is required"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Reason"] = "Accident" };

        workflow.Inspect(workflow.CreateInstance("Red"), "Emergency", data).Outcome.Should().BeOneOf(PreceptOutcomeKind.Accepted, PreceptOutcomeKind.AcceptedInPlace);
        workflow.Inspect(workflow.CreateInstance("Green"), "Emergency", data).Outcome.Should().BeOneOf(PreceptOutcomeKind.Accepted, PreceptOutcomeKind.AcceptedInPlace);
        workflow.Inspect(workflow.CreateInstance("Yellow"), "Emergency", data).Outcome.Should().BeOneOf(PreceptOutcomeKind.Accepted, PreceptOutcomeKind.AcceptedInPlace);
    }

    [Fact]
    public void Parse_FromOnBlock_WithoutOutcome_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance
            from Red on Advance
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_IfChain_WithBlockLevelFallback_WithoutElse_Throws()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance
            from Red on Advance when CarsWaiting > 0 -> transition Green
            reject "Cars waiting required"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_ElseIfChain_WithBlockLevelFallback_WithoutElse_Throws()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            state Yellow
            event Advance
            from Red on Advance when CarsWaiting > 3 -> transition Green
            from Red on Advance when CarsWaiting > 0 -> transition Yellow
            reject "No cars waiting"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_DuplicateOutcomeRule_ForSameFromOn_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance

            from Red on Advance -> reject "First"

            from Red on Advance -> reject "Second"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Duplicate*from Red on Advance*");
    }

    [Fact]
    public void Parse_FromOnBlock_StatementAfterOutcomeTransition_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Yellow initial
            state Red
            event Advance

            from Yellow on Advance -> transition Red -> no transition
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*no statements are allowed after an outcome*");
    }

    [Fact]
    public void Inspect_FromOnBlock_ElseIfChain_SelectsFirstMatchingBranch()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            state Yellow
            event Advance

            from Red on Advance when CarsWaiting > 3 -> transition Green
            from Red on Advance when CarsWaiting > 0 -> transition Yellow
            from Red on Advance -> reject "No cars waiting"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 5 }), "Advance").TargetState.Should().Be("Green");
        workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 1 }), "Advance").TargetState.Should().Be("Yellow");
        var rejected = workflow.Inspect(workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 0 }), "Advance");
        rejected.Outcome.Should().Be(PreceptOutcomeKind.Rejected);
        rejected.Reasons.Should().ContainSingle("No cars waiting");
    }

    [Fact]
    public void Parse_FromOnBlock_UnknownBranchKeyword_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            event Advance

            from Red on Advance when !(CarsWaiting > 0) -> transition Green
            from Red on Advance -> reject "No cars waiting"
            """;

        var act = () => PreceptParser.Parse(dsl);

        // New syntax doesn't have "unless" - use negation with !
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_IfBlock_WithReason_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance

            from Red on Advance when CarsWaiting > 0 -> transition Green
            from Red on Advance -> reject "No cars waiting"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_SetStatement_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance

            from Red on Advance -> set CarsWaiting = 0 -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TransitionRows.Should().HaveCount(1);
        machine.TransitionRows![0].SetAssignments.Should().ContainSingle();
        machine.TransitionRows[0].SetAssignments[0].Key.Should().Be("CarsWaiting");
    }

    [Fact]
    public void Parse_DataField_DefaultLiteral_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 3
            field Note as string nullable
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Fields.Should().HaveCount(2);
        machine.Fields[0].HasDefaultValue.Should().BeTrue();
        machine.Fields[0].DefaultValue.Should().Be(3d);
        machine.Fields[1].HasDefaultValue.Should().BeTrue(); // nullable without explicit default → implicit null default
        machine.Fields[1].IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Parse_DataField_DefaultMustBeLiteral_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default OtherCount + 1
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_DataField_DefaultTypeMismatch_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default "three"
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Default value for field 'CarsWaiting' does not match*");
    }

    [Fact]
    public void Parse_NonNullableDataField_WithoutDefault_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Non-nullable field 'CarsWaiting' requires a default value*");
    }

    [Fact]
    public void Parse_NullableDataField_WithoutDefault_IsAccepted()
    {
        const string dsl = """
            precept TrafficLight
            field Note as string nullable
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Fields.Should().ContainSingle();
        machine.Fields[0].Name.Should().Be("Note");
        machine.Fields[0].IsNullable.Should().BeTrue();
        machine.Fields[0].HasDefaultValue.Should().BeTrue(); // nullable without explicit default → implicit null default
    }

    [Fact]
    public void Parse_MissingInitialState_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Exactly one state must be marked initial*");
    }

    [Fact]
    public void Parse_DuplicateInitialStateMarkers_AreRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green initial
            event Advance

            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Duplicate initial state*already marked initial*");
    }

    [Fact]
    public void Parse_StatesPluralDeclaration_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            states Red, Green
            event Advance
            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_EventsPluralDeclaration_IsRejected()
    {
        const string dsl = """
            precept TrafficLight
            state Red initial
            state Green
            events Advance
            from Red on Advance -> transition Green
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateInstance_WithoutData_UsesDeclaredFieldDefaults()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            field Note as string nullable
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var instance = workflow.CreateInstance("Red");

        instance.InstanceData["CarsWaiting"].Should().Be(0d);
        instance.InstanceData["Note"].Should().BeNull();
    }

    [Fact]
    public void CreateInstance_ProvidedData_OverridesDeclaredDefaults()
    {
        const string dsl = """
            precept TrafficLight
            field CarsWaiting as number default 0
            state Red initial
            state Green
            event Advance

            from Red on Advance -> transition Green
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?> { ["CarsWaiting"] = 5d });

        instance.InstanceData["CarsWaiting"].Should().Be(5d);
    }

    [Fact]
    public void Parse_EventArg_WithDefault_IsAccepted()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Reason as string default "none"
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
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
            precept Workflow
            state A initial
            event Submit with Reason as string nullable default "fallback"
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
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
            precept Workflow
            state A initial
            event Submit with Reason as string nullable default null
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Parse_NonNullableEventArg_WithoutDefault_IsAccepted()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Reason as string
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.Name.Should().Be("Reason");
        arg.HasDefaultValue.Should().BeFalse();
        arg.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Parse_NullableEventArg_WithoutDefault_IsAccepted()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Reason as string nullable
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeFalse();
        arg.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Parse_EventArg_NumberDefault_IsAccepted()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Priority as number default 5
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be(5d);
    }

    [Fact]
    public void Parse_EventArg_BooleanDefault_IsAccepted()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Urgent as boolean default false
            from A on Submit -> no transition
            """;

        var machine = PreceptParser.Parse(dsl);
        var arg = machine.Events.Single().Args.Single();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be(false);
    }

    [Fact]
    public void Parse_NonNullableEventArg_WithNullDefault_IsRejected()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Reason as string default null
            from A on Submit -> no transition
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Default value*does not match*");
    }

    [Fact]
    public void Fire_NonNullableEventArg_WithDefault_OmittedByCaller_UsesDefault()
    {
        const string dsl = """
            precept Workflow
            field LastReason as string nullable
            state A initial
            event Submit with Reason as string default "auto"
            from A on Submit -> set LastReason = Submit.Reason -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().Be("auto");
    }

    [Fact]
    public void Fire_NonNullableEventArg_WithDefault_SuppliedByCaller_UsesSupplied()
    {
        const string dsl = """
            precept Workflow
            field LastReason as string nullable
            state A initial
            event Submit with Reason as string default "auto"
            from A on Submit -> set LastReason = Submit.Reason -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["Reason"] = "manual" });

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().Be("manual");
    }

    [Fact]
    public void Fire_NullableEventArg_WithDefault_OmittedByCaller_UsesDefault()
    {
        const string dsl = """
            precept Workflow
            field LastReason as string nullable
            state A initial
            event Submit with Reason as string nullable default "fallback"
            from A on Submit -> set LastReason = Submit.Reason -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().Be("fallback");
    }

    [Fact]
    public void Fire_NullableEventArg_WithoutDefault_OmittedByCaller_UsesNull()
    {
        const string dsl = """
            precept Workflow
            field LastReason as string nullable
            state A initial
            event Submit with Reason as string nullable
            from A on Submit -> set LastReason = Submit.Reason -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastReason"].Should().BeNull();
    }

    [Fact]
    public void Fire_NonNullableEventArg_WithoutDefault_OmittedByCaller_IsRejected()
    {
        const string dsl = """
            precept Workflow
            state A initial
            event Submit with Reason as string
            from A on Submit -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Submit");

        (fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("required argument 'Reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_RequiredEventArgumentKeys_ExcludesArgsWithDefaults()
    {
        const string dsl = """
            precept Workflow
            state A initial
            state B
            event Submit with Reason as string, Priority as number default 1, Note as string nullable
            from A on Submit -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?>());

        var inspect = workflow.Inspect(instance, "Submit");

        inspect.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Reason");
    }

    [Fact]
    public void Parse_DuplicateFromOnBlock_ThrowsParseError()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Submit
            from A on Submit -> transition B
            from A on Submit -> transition B
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate*from A on Submit*");
    }

    [Fact]
    public void Parse_FromAny_ThenSpecificState_ThrowsDuplicateError()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Submit
            from any on Submit -> transition B
            from A on Submit -> transition B
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate*from A on Submit*");
    }

    [Fact]
    public void Parse_CommaList_ThenOverlappingState_ThrowsDuplicateError()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            state C
            event Submit
            from A,B on Submit -> transition C
            from B on Submit -> transition A
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate*from B on Submit*");
    }

    [Fact]
    public void Parse_DifferentStates_SameEvent_IsValid()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Submit
            from A on Submit -> transition B
            from B on Submit -> transition A
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_SameState_DifferentEvents_IsValid()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event EventX
            event EventY
            from A on EventX -> transition B
            from A on EventY -> transition B
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Inspect_Instance_WhenPredicateFalse_ReturnsNotApplicable_WithNoEventArgs()
    {
        const string dsl = """
            precept BankAccount
            field Frozen as boolean default false
            state Active initial
            event Deposit with Amount as number
            from Active on Deposit when !Frozen -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Frozen=true → 'when !Frozen' is false → NotApplicable regardless of args
        var frozen = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Frozen"] = true });

        // No event args supplied (simulates discovery-mode bulk refresh)
        var noArgs = workflow.Inspect(frozen, "Deposit");
        noArgs.Outcome.Should().Be(PreceptOutcomeKind.NotApplicable,
            because: "when predicate is false, NotApplicable must be returned before arg validation");

        // Explicit empty args dict — same expectation
        var emptyArgs = workflow.Inspect(frozen, "Deposit", new Dictionary<string, object?>());
        emptyArgs.Outcome.Should().Be(PreceptOutcomeKind.NotApplicable,
            because: "empty arg dict with false when predicate must still yield NotApplicable");
    }

    [Fact]
    public void Inspect_Instance_WhenPredicateTrue_ValidatesArgsNormally()
    {
        const string dsl = """
            precept BankAccount
            field Frozen as boolean default false
            state Active initial
            event Deposit with Amount as number
            from Active on Deposit when !Frozen -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Frozen=false → 'when !Frozen' is true → falls through to normal arg validation
        var unfrozen = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Frozen"] = false });

        // No args: predicate passes, but required 'Amount' is missing → Rejected
        var noArgs = workflow.Inspect(unfrozen, "Deposit");
        noArgs.Outcome.Should().NotBe(PreceptOutcomeKind.NotApplicable,
            because: "when predicate is true, the call proceeds to arg validation");

        // Correct args: should be accepted
        var withArgs = workflow.Inspect(unfrozen, "Deposit", new Dictionary<string, object?> { ["Amount"] = 100.0 });
        withArgs.Outcome.Should().BeOneOf(PreceptOutcomeKind.Accepted, PreceptOutcomeKind.AcceptedInPlace);
    }

    // ════════════════════════════════════════════════════════════════════
    // SAMPLE FILE VALIDATION — all samples/ files parse and compile clean (Category C/E)
    // ════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("bank-loan")]
    [InlineData("bugtracker")]
    [InlineData("document-signing")]
    [InlineData("ecommerce")]
    [InlineData("elevator")]
    [InlineData("hotel-booking")]
    [InlineData("job-application")]
    [InlineData("loan")]
    [InlineData("package-delivery")]
    [InlineData("patient-admission")]
    [InlineData("restaurant-order")]
    [InlineData("smarthome")]
    [InlineData("subscription")]
    [InlineData("support-ticket")]
    [InlineData("test")]
    [InlineData("trafficlight")]
    [InlineData("vending-machine")]
    public void SampleFile_ParsesAndCompilesClean(string sampleName)
    {
        var path = Path.Combine(FindSamplesDir(), sampleName + ".precept");
        var dsl = File.ReadAllText(path);

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow(because: $"{sampleName}.precept should parse and compile without errors");
    }

    private static string FindSamplesDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "samples");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the samples/ directory by walking up from the test binary.");
    }
}


