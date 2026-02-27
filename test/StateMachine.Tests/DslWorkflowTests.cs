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
            from Red on Advance
                transition Green
            """;

        var machine = StateMachineDslParser.Parse(dsl);
        var workflow = DslWorkflowCompiler.Compile(machine);

        var inspection = workflow.Inspect("Red", "Advance");

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.Outcome.Should().Be(DslOutcomeKind.Enabled);
        inspection.TargetState.Should().Be("Green");
        inspection.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_Outcome_Maps_To_Undefined_Blocked_Enabled()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var undefined = workflow.Inspect("Red", "MissingEvent");
        var blocked = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });
        var enabled = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 2 });

        undefined.Outcome.Should().Be(DslOutcomeKind.Undefined);
        blocked.Outcome.Should().Be(DslOutcomeKind.Blocked);
        enabled.Outcome.Should().Be(DslOutcomeKind.Enabled);
    }

    [Fact]
    public void Inspect_GuardedTransition_WithMatchingData_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                reject "Cars waiting required"
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
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["CarsWaiting"] = 0 };

        var inspection = workflow.Inspect("Red", "Advance", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.TargetState.Should().BeNull();
        inspection.Reasons.Should().ContainSingle("Cars waiting required");
    }

    [Fact]
    public void Inspect_GuardedTransition_WithoutRequiredDataKey_IsRejectedWithReason()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                reject "Cars waiting required"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var inspection = workflow.Inspect("Red", "Advance");

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Cars waiting required");
    }

    [Fact]
    public void Inspect_GuardedTransition_WithWrongDataType_IsRejectedWithReason()
    {
        const string dsl = """
            machine FeatureFlag
            state Disabled
            state Enabled
            event Evaluate
            from Disabled on Evaluate
                if IsEnabled
                    transition Enabled
                reject "Feature must be enabled"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["IsEnabled"] = "yes" };

        var inspection = workflow.Inspect("Disabled", "Evaluate", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeFalse();
        inspection.Reasons.Should().ContainSingle("Feature must be enabled");
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
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                else if IsManualOverride
                    transition Yellow
                reject "No eligible transition"
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
        inspection.Reasons.Should().ContainSingle("No eligible transition");
    }

    [Fact]
    public void Inspect_StringEqualityGuard_WithQuotedLiteral_IsAccepted()
    {
        const string dsl = """
            machine FeatureMode
            state Draft
            state Live
            event Publish
            from Draft on Publish
                if Mode == "Manual"
                    transition Live
                reject "Manual mode required"
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
            from Open on Escalate
                if Assignee == null
                    transition Pending
                reject "Assignee must be empty"
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
            from Low on Scale
                if Qps >= 100
                    transition High
                reject "Qps threshold not met"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var data = new Dictionary<string, object?> { ["Qps"] = 100m };

        var inspection = workflow.Inspect("Low", "Scale", data);

        inspection.IsDefined.Should().BeTrue();
        inspection.IsAccepted.Should().BeTrue();
        inspection.TargetState.Should().Be("High");
    }

    [Fact]
    public void Inspect_UnsupportedGuardExpression_UsesConfiguredReason()
    {
        const string dsl = """
            machine Workflow
            state A
            state B
            event Go
            from A on Go
                if Flag && OtherFlag
                    transition B
                reject "Both flags must be true"
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
        inspection.Reasons.Should().ContainSingle("Both flags must be true");
    }

    [Fact]
    public void Fire_GuardedTransition_WithData_AcceptsAndReturnsNewState()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                reject "Cars waiting required"
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
            from Red on Advance
                if CarsWaiting > 0
                    transition Green
                reject "Cars waiting required"
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
            from Red on Advance
                transform CarsWaiting = 0
                transition Green
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
            from Red on Emergency
                transform EmergencyReason = Reason
                transition FlashingRed
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
                args
                    Reason: string
            from Red on Emergency
                transform EmergencyReason = Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>());

        var fire = workflow.Fire(instance, "Emergency");

        fire.IsDefined.Should().BeTrue();
        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("required argument 'Reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Instance_DataAssignment_FromInstanceDataReference_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state Green
            data
                CarsWaiting: number
                LastCarsWaiting: number
            event Advance
            from Red on Advance
                transform LastCarsWaiting = data.CarsWaiting
                transition Green
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["CarsWaiting"] = 3d,
            ["LastCarsWaiting"] = 0d
        });

        var fire = workflow.Fire(instance, "Advance");

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["LastCarsWaiting"].Should().Be(3d);
    }

    [Fact]
    public void Parse_TypedEventArguments_AreRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
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
    public void Fire_Instance_DataAssignment_WithArgPrefix_IsAccepted()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state FlashingRed
            event Emergency
                args
                    Reason: string
            data
                EmergencyReason: string?
            from Red on Emergency
                transform EmergencyReason = arg.Reason
                transition FlashingRed
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Red", new Dictionary<string, object?>
        {
            ["EmergencyReason"] = null
        });
        var fire = workflow.Fire(instance, "Emergency", new Dictionary<string, object?> { ["Reason"] = "Accident" });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["EmergencyReason"].Should().Be("Accident");
    }

    [Fact]
    public void Inspect_AcceptedTransition_InfersRequiredEventArgumentKeys()
    {
        const string dsl = """
            machine TrafficLight
            state Red
            state FlashingRed
            event Emergency
                args
                    Reason: string
            from Red on Emergency
                transform EmergencyReason = Reason
                transition FlashingRed
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
            from Off on Enable
                transform IsEnabled = true
                transition On
            from On on Disable
                transform IsEnabled = false
                transition Off
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
            from Open on Clear
                transform Note = null
                transition Cleared
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
            from Red on Advance
                transform CarsWaiting = 0
                transition Green
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
            from Disabled on Evaluate
                if IsEnabled
                    transition Enabled
                reject "Feature must be enabled"
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
        rejectedWithUnrelatedArgs.Reasons.Should().ContainSingle("Feature must be enabled");
    }

    [Fact]
    public void Inspect_Instance_WithWorkflowMismatch_IsNotDefinedWithReason()
    {
        const string dsl = """
            machine TrafficLight
            state Red
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
            from Green on Advance
                transition Yellow
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
            from Red on Advance
                transition Green
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
            state Red
            state Green
            event Advance
            from Red on Advance
                if CarsWaiting > 0
                    transform CarsWaiting = 0
                    transition Green
                reject "Cars waiting required"
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Transitions.Should().ContainSingle();
        machine.Transitions[0].GuardExpression.Should().Be("CarsWaiting > 0");
        machine.Transitions[0].DataAssignmentKey.Should().Be("CarsWaiting");
        machine.Transitions[0].DataAssignmentExpression.Should().Be("0");
        machine.TerminalRules.Should().ContainSingle();
        machine.TerminalRules[0].Reason.Should().Be("Cars waiting required");
    }

    [Fact]
    public void Parse_GuardedTransition_WithoutReason_IsRejected()
    {
        const string dsl = """
            machine TrafficLight
            state Red
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
                        state Red
                        state Green
                        event Advance
                        from Red on Advance
                            if CarsWaiting > 0
                                transition Green
                            reject "No cars waiting"
                        """;

                var machine = StateMachineDslParser.Parse(dsl);

                machine.Transitions.Should().ContainSingle();
                machine.Transitions[0].FromState.Should().Be("Red");
                machine.Transitions[0].ToState.Should().Be("Green");
                machine.TerminalRules.Should().ContainSingle();
                machine.TerminalRules[0].Kind.Should().Be(DslTerminalKind.Reject);
                machine.TerminalRules[0].Reason.Should().Be("No cars waiting");
        }

        [Fact]
        public void Inspect_FromOnBlock_AllGuardsFail_UsesOutcomeRejectReason()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red
                        state Green
                        event Advance
                        from Red on Advance
                            if CarsWaiting > 0
                                transition Green
                            reject "No cars waiting"
                        """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                var inspection = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });

                inspection.IsDefined.Should().BeTrue();
                inspection.IsAccepted.Should().BeFalse();
                inspection.Outcome.Should().Be(DslOutcomeKind.Blocked);
                inspection.Reasons.Should().ContainSingle("No cars waiting");
        }

        [Fact]
        public void Inspect_FromOnBlock_NoTransitionOutcome_IsUndefined()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red
                        state Green
                        event Advance
                        from Red on Advance
                            if CarsWaiting > 0
                                transition Green
                            no transition
                        """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

                var inspection = workflow.Inspect("Red", "Advance", new Dictionary<string, object?> { ["CarsWaiting"] = 0 });

                inspection.IsDefined.Should().BeFalse();
                inspection.IsAccepted.Should().BeFalse();
                inspection.Outcome.Should().Be(DslOutcomeKind.Undefined);
                inspection.Reasons.Should().ContainSingle(r => r.Contains("No transition", StringComparison.Ordinal));
        }

        [Fact]
        public void Parse_FromAny_ExpandsToAllStates()
        {
                const string dsl = """
                        machine TrafficLight
                        state Red
                        state Green
                        state Yellow
                        state FlashingRed
                        event Emergency
                        from any on Emergency
                            if Reason != ""
                                transition FlashingRed
                            reject "Emergency reason is required"
                        """;

                var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
                var data = new Dictionary<string, object?> { ["Reason"] = "Accident" };

                workflow.Inspect("Red", "Emergency", data).IsAccepted.Should().BeTrue();
                workflow.Inspect("Green", "Emergency", data).IsAccepted.Should().BeTrue();
                workflow.Inspect("Yellow", "Emergency", data).IsAccepted.Should().BeTrue();
        }

        [Fact]
        public void Parse_FromOnBlock_WithoutOutcome_IsRejected()
        {
            const string dsl = """
                machine TrafficLight
                state Red
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
            public void Parse_DuplicateOutcomeRule_ForSameFromOn_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red
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
                    .WithMessage("*Duplicate outcome rule for state 'Red' and event 'Advance'*");
            }

            [Fact]
            public void Parse_FromOnBlock_StatementAfterOutcomeTransition_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Yellow
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
                    state Red
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
                rejected.Outcome.Should().Be(DslOutcomeKind.Blocked);
                rejected.Reasons.Should().ContainSingle("No cars waiting");
            }

            [Fact]
            public void Parse_FromOnBlock_UnknownBranchKeyword_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red
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
                    state Red
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
            public void Parse_SetAlias_IsRejected()
            {
                const string dsl = """
                    machine TrafficLight
                    state Red
                    state Green
                    event Advance

                    from Red on Advance
                        set CarsWaiting = 0
                        transition Green
                    """;

                var act = () => StateMachineDslParser.Parse(dsl);

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*unrecognized statement 'set CarsWaiting = 0' inside from/on block*");
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
                    state Red
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
}
