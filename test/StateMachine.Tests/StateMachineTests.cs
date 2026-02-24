using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace StateMachine.Tests
{
    // ═══════════════════════════════════════════════════════════════════
    // Shared test types
    // ═══════════════════════════════════════════════════════════════════

    internal enum Light { Off, Red, Green, Yellow, FlashingRed }

    internal record TrafficLightData(
        Light Light,
        int CycleCount = 0,
        string? Intersection = null,
        DateTime? LastTransitionAt = null
    );

    internal record EmergencyOverride(string AuthorizedBy, string Reason);

    // ═══════════════════════════════════════════════════════════════════
    // Data-Less State Machine Tests
    // ═══════════════════════════════════════════════════════════════════

    public class DataLessStateMachineTests
    {
        [Fact(Skip = "Implementation not ready")]
        public void InitialState_IsSetCorrectly()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsNotAccepted_WhenNoTransitionDefined()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Green); // Start in Green — next only handles Red

            var result = next.Test();
            result.IsAccepted.Should().BeFalse();
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsAccepted_WhenTransitionDefined()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            var result = next.Test();
            result.IsAccepted.Should().BeTrue();
            result.NewState.Should().Be(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_StateDoesNotChange()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build(Light.Red);

            hold.Trigger();
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void SimpleTrafficLightCycle()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Green).TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow).TransitionTo(Light.Red)
                .Build(Light.Red);

            machine.State.Should().Be(Light.Red);

            next.Trigger();
            machine.State.Should().Be(Light.Green);

            next.Trigger();
            machine.State.Should().Be(Light.Yellow);

            next.Trigger();
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void TransitionObservation()
        {
            var transitions = new List<TransitionedEventArgs<Light>>();

            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Green).TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow).TransitionTo(Light.Red)
                .Build(Light.Red);

            machine.Transitioned += args => transitions.Add(args);

            next.Trigger();
            next.Trigger();

            transitions.Should().HaveCount(2);
            transitions[0].FromState.Should().Be(Light.Red);
            transitions[0].ToState.Should().Be(Light.Green);
            transitions[1].FromState.Should().Be(Light.Green);
            transitions[1].ToState.Should().Be(Light.Yellow);
        }

        [Fact(Skip = "Implementation not ready")]
        public void RegardlessOfState_AllowsTransitionFromAnyState()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .DefineEvent(out var shutdown)
                    .RegardlessOfState()
                    .TransitionTo(Light.Off)
                .Build(Light.Red);

            shutdown.Trigger();
            machine.State.Should().Be(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void UndefinedTransition_Throws()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            next.Trigger();
            machine.State.Should().Be(Light.Green);

            // Triggering again from Green — not defined for this event
            var act = () => next.Trigger();
            act.Should().Throw<InvalidTransitionException>();
        }

        [Fact(Skip = "Implementation not ready")]
        public void DuplicateTransition_ThrowsOnBuild()
        {
            var act = () => StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Yellow) // duplicate
                .Build(Light.Red);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact(Skip = "Implementation not ready")]
        public void MultipleEvents_IndependentTransitions()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .DefineEvent(out var back)
                    .WhenStateIs(Light.Green)
                    .TransitionTo(Light.Red)
                .Build(Light.Red);

            next.Trigger();
            machine.State.Should().Be(Light.Green);

            back.Trigger();
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void EventName_CapturedFromVariable()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            next.Name.Should().Be("next");
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_FiresTransitionedEvent()
        {
            var transitions = new List<TransitionedEventArgs<Light>>();

            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build(Light.Red);

            machine.Transitioned += args => transitions.Add(args);

            hold.Trigger();

            transitions.Should().HaveCount(1);
            transitions[0].FromState.Should().Be(Light.Red);
            transitions[0].ToState.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void States_ReturnsAllEnumValues()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            machine.States.Should().BeEquivalentTo(
                new[] { Light.Off, Light.Red, Light.Green, Light.Yellow, Light.FlashingRed });
        }

        [Fact(Skip = "Implementation not ready")]
        public void Events_ContainsAllDefinedEvents()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .DefineEvent(out var shutdown)
                    .WhenStateIs(Light.Green)
                    .TransitionTo(Light.Off)
                .Build(Light.Red);

            machine.Events.Should().HaveCount(2);
            machine.Events.Should().Contain(e => e.Name == "next");
            machine.Events.Should().Contain(e => e.Name == "shutdown");
        }

        [Fact(Skip = "Implementation not ready")]
        public void WhenStateIs_Params_AllowsMultipleFromStates()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var shutdown)
                    .WhenStateIs(Light.Red, Light.Green, Light.Yellow)
                    .TransitionTo(Light.Off)
                .Build(Light.Green);

            shutdown.Trigger();
            machine.State.Should().Be(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_DoesNotMutateState()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            var result = next.Test();
            result.IsAccepted.Should().BeTrue();
            result.NewState.Should().Be(Light.Green);

            machine.State.Should().Be(Light.Red);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Data-Ful State Machine Tests
    // ═══════════════════════════════════════════════════════════════════

    public class DataFulStateMachineTests
    {
        [Fact(Skip = "Implementation not ready")]
        public void TrafficLightLifecycle()
        {
            var initialData = new TrafficLightData(Light.Off, Intersection: "Main St & 1st Ave");

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)

                .DefineEvent(out var powerOn)
                    .WhenStateIs(Light.Off)
                    .TransitionTo(Light.Red)

                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .Execute(data => data with { CycleCount = data.CycleCount + 1 })
                    .ThenTransitionTo(Light.Green)
                    .WhenStateIs(Light.Green)
                    .TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow)
                    .TransitionTo(Light.Red)

                .DefineEvent<EmergencyOverride>(out var emergency)
                    .WhenStateIs(Light.Red, Light.Green, Light.Yellow)
                    .If((data, e) => !string.IsNullOrEmpty(e.AuthorizedBy),
                        "Emergency override requires authorization")
                    .Execute((data, e) => data with { LastTransitionAt = DateTime.Now })
                    .ThenTransitionTo(Light.FlashingRed)

                .DefineEvent(out var shutdown)
                    .RegardlessOfState()
                    .TransitionTo(Light.Off)

                .Build(initialData);

            // Starts off
            machine.State.Should().Be(Light.Off);
            machine.Data.Intersection.Should().Be("Main St & 1st Ave");

            // Power on
            powerOn.Trigger();
            machine.State.Should().Be(Light.Red);

            // Red → Green (CycleCount increments)
            next.Trigger();
            machine.State.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(1);

            // Green → Yellow
            next.Trigger();
            machine.State.Should().Be(Light.Yellow);

            // Yellow → Red
            next.Trigger();
            machine.State.Should().Be(Light.Red);

            // Second cycle
            next.Trigger();
            machine.State.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(2);

            // Emergency override
            emergency.Trigger(new EmergencyOverride("Officer Smith", "Accident"));
            machine.State.Should().Be(Light.FlashingRed);
            machine.Data.LastTransitionAt.Should().NotBeNull();

            // Shutdown
            shutdown.Trigger();
            machine.State.Should().Be(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void InitialState_And_InitialData()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 42);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(initialData);

            machine.State.Should().Be(Light.Red);
            machine.Data.CycleCount.Should().Be(42);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_DataUnchanged()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 5);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build(initialData);

            hold.Trigger();
            machine.State.Should().Be(Light.Red);
            machine.Data.CycleCount.Should().Be(5);
        }

        [Fact(Skip = "Implementation not ready")]
        public void SimpleTransition_WithExecute()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .Execute(data => data with { CycleCount = data.CycleCount + 1 })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(1);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_WithExecute()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var tick)
                    .WhenStateIs(Light.Red)
                    .Execute(data => data with { CycleCount = data.CycleCount + 1 })
                    .AndKeepSameState()
                .Build(initialData);

            tick.Trigger();
            machine.State.Should().Be(Light.Red);
            machine.Data.CycleCount.Should().Be(1);

            tick.Trigger();
            machine.Data.CycleCount.Should().Be(2);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Passes_TransitionsState()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 10);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .TransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_NoElse_ThrowsGuardFailedException()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 1);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .TransitionTo(Light.Green)
                .Build(initialData);

            var act = () => next.Trigger();
            act.Should().Throw<GuardFailedException>()
                .Which.Reasons.Should().Contain("Must complete at least 5 cycles");

            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Passes_ExecuteAndTransition()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 10);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .Execute(data => data with { CycleCount = data.CycleCount + 100 })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(110);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_Else_KeepsSameState()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 1);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .TransitionTo(Light.Green)
                    .Else
                    .KeepSameState()
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_Else_TransitionsToAlternate()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 1);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .TransitionTo(Light.Green)
                    .Else
                    .TransitionTo(Light.FlashingRed)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.FlashingRed);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_ElseIf_Passes_Transitions()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 3);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .TransitionTo(Light.Green)
                    .Else
                    .If(data => data.CycleCount > 1, "Must complete at least 1 cycle")
                    .TransitionTo(Light.Yellow)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Yellow);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_Else_ExecuteAndTransition()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 1);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .Execute(data => data with { CycleCount = 999 })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .Execute(data => data with { CycleCount = -1 })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.FlashingRed);
            machine.Data.CycleCount.Should().Be(-1);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_ElseIf_Passes_ExecuteAndTransition()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 3);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .TransitionTo(Light.Green)
                    .Else
                    .If(data => data.CycleCount > 1, "Must complete at least 1 cycle")
                    .Execute(data => data with { CycleCount = data.CycleCount * 10 })
                    .ThenTransitionTo(Light.Yellow)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Yellow);
            machine.Data.CycleCount.Should().Be(30);
        }

        [Fact(Skip = "Implementation not ready")]
        public void AllGuards_Fail_ThrowsGuardFailedWithAllReasons()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 100, "Must complete at least 100 cycles")
                    .TransitionTo(Light.Green)
                    .Else
                    .If(data => data.CycleCount > 50, "Must complete at least 50 cycles")
                    .TransitionTo(Light.Yellow)
                    .Else
                    .If(data => data.CycleCount > 10, "Must complete at least 10 cycles")
                    .TransitionTo(Light.FlashingRed)
                .Build(initialData);

            var act = () => next.Trigger();
            act.Should().Throw<GuardFailedException>()
                .Which.Reasons.Should().HaveCount(3)
                .And.Contain("Must complete at least 100 cycles")
                .And.Contain("Must complete at least 50 cycles")
                .And.Contain("Must complete at least 10 cycles");

            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void ExecuteTransform_Throws_StateAndDataUnchanged()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 5);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .Execute(data => throw new InvalidOperationException("transform failed"))
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            var act = () => next.Trigger();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("transform failed");

            machine.State.Should().Be(Light.Red);
            machine.Data.CycleCount.Should().Be(5);
        }

        [Fact(Skip = "Implementation not ready")]
        public void GuardedExecuteTransform_Throws_StateAndDataUnchanged()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 10);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .Execute(data => throw new InvalidOperationException("guarded transform failed"))
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            var act = () => next.Trigger();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("guarded transform failed");

            machine.State.Should().Be(Light.Red);
            machine.Data.CycleCount.Should().Be(10);
        }

        [Fact(Skip = "Implementation not ready")]
        public void DuplicateTransition_ThrowsOnBuild()
        {
            var act = () => StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Yellow) // duplicate
                .Build(new TrafficLightData(Light.Red));

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact(Skip = "Implementation not ready")]
        public void ImmutableData_OriginalRecordUnchanged()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .Execute(data => data with { CycleCount = data.CycleCount + 1 })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();

            // The original record is untouched
            initialData.Light.Should().Be(Light.Red);
            initialData.CycleCount.Should().Be(0);

            // The machine has the new data
            machine.Data.Light.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(1);
        }

        [Fact(Skip = "Implementation not ready")]
        public void GuardFails_ThrowsGuardFailedException()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent<EmergencyOverride>(out var emergency)
                    .WhenStateIs(Light.Red)
                    .If((data, e) => !string.IsNullOrEmpty(e.AuthorizedBy),
                        "Emergency override requires authorization")
                    .Execute((data, e) => data with { LastTransitionAt = DateTime.Now })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build(initialData);

            // Empty authorization should fail
            var act = () => emergency.Trigger(new EmergencyOverride("", "No auth"));
            act.Should().Throw<GuardFailedException>()
                .Which.Reasons.Should().Contain("Emergency override requires authorization");
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsExpectedOutcomeWithoutFiring()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent<EmergencyOverride>(out var emergency)
                    .WhenStateIs(Light.Red)
                    .If((data, e) => !string.IsNullOrEmpty(e.AuthorizedBy),
                        "Emergency override requires authorization")
                    .Execute((data, e) => data with { LastTransitionAt = DateTime.Now })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build(initialData);

            // Test with valid override
            var validResult = emergency.Test(new EmergencyOverride("Officer Smith", "Accident"));
            validResult.IsAccepted.Should().BeTrue();
            validResult.NewState.Should().Be(Light.FlashingRed);
            validResult.Reason.Should().BeNull();

            // Test with empty authorization
            var invalidResult = emergency.Test(new EmergencyOverride("", "No auth"));
            invalidResult.IsAccepted.Should().BeFalse();
            invalidResult.Reason.Should().Contain("Emergency override requires authorization");

            // State should not have changed
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void DataTransitioned_ProvidesOldAndNewData()
        {
            var transitions = new List<DataTransitionedEventArgs<Light, TrafficLightData>>();
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .Execute(data => data with { CycleCount = data.CycleCount + 1 })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            machine.DataTransitioned += args => transitions.Add(args);

            next.Trigger();

            transitions.Should().HaveCount(1);
            transitions[0].OldData.CycleCount.Should().Be(0);
            transitions[0].NewData.CycleCount.Should().Be(1);
            transitions[0].FromState.Should().Be(Light.Red);
            transitions[0].ToState.Should().Be(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void ConditionalEvent_WithElseChain()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent<int>(out var addCycles)
                    .WhenStateIs(Light.Red)
                    .If((data, count) => count > 0, "Cycle count must be positive")
                    .And((data, count) => count < 100, "Cannot add more than 99 cycles at once")
                        .Execute((data, count) => data with { CycleCount = data.CycleCount + count })
                        .ThenTransitionTo(Light.Green)
                    .Else
                        .KeepSameState()
                .Build(initialData);

            addCycles.Trigger(10);
            machine.State.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(10);
        }

        [Fact(Skip = "Implementation not ready")]
        public void And_CombinesMultipleGuards()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 10);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .And(data => data.CycleCount < 20, "Must have fewer than 20 cycles")
                    .TransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void And_SecondGuardFails_BranchFails()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 25);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 5, "Must complete at least 5 cycles")
                    .And(data => data.CycleCount < 20, "Must have fewer than 20 cycles")
                    .TransitionTo(Light.Green)
                .Build(initialData);

            var act = () => next.Trigger();
            act.Should().Throw<GuardFailedException>()
                .Which.Reasons.Should().Contain("Must have fewer than 20 cycles");
        }

        [Fact(Skip = "Implementation not ready")]
        public void ParameterizedEvent_PassesArgToGuardAndTransform()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent<int>(out var addCycles)
                    .WhenStateIs(Light.Red)
                    .If((data, count) => count > 0, "Cycle count must be positive")
                    .Execute((data, count) => data with { CycleCount = data.CycleCount + count })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            addCycles.Trigger(42);
            machine.State.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(42);
        }

        [Fact(Skip = "Implementation not ready")]
        public void ParameterizedEvent_GuardRejectsInvalidArg()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent<int>(out var addCycles)
                    .WhenStateIs(Light.Red)
                    .If((data, count) => count > 0, "Cycle count must be positive")
                    .Execute((data, count) => data with { CycleCount = data.CycleCount + count })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            var act = () => addCycles.Trigger(-5);
            act.Should().Throw<GuardFailedException>()
                .Which.Reasons.Should().Contain("Cycle count must be positive");

            machine.State.Should().Be(Light.Red);
            machine.Data.CycleCount.Should().Be(0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Machine_StampsState_OverridingTransform()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    // User sets Light to FlashingRed — machine should overwrite with Green
                    .Execute(data => data with { Light = Light.FlashingRed, CycleCount = 1 })
                    .ThenTransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Green);         // Machine wins
            machine.Data.Light.Should().Be(Light.Green);    // Data agrees
            machine.Data.CycleCount.Should().Be(1);         // Transform ran
        }

        [Fact(Skip = "Implementation not ready")]
        public void TransitionWithoutExecute_OnlyStateChanges()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 42);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Green);
            machine.Data.Light.Should().Be(Light.Green);
            machine.Data.CycleCount.Should().Be(42); // unchanged
        }

        [Fact(Skip = "Implementation not ready")]
        public void Else_ExecutesBranch_WhenAllGuardsFail()
        {
            var initialData = new TrafficLightData(Light.Red, CycleCount: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.CycleCount > 100, "Must complete at least 100 cycles")
                    .Execute(data => data with { CycleCount = 999 })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .Execute(data => data with { CycleCount = -1 })
                    .ThenTransitionTo(Light.Off)
                .Build(initialData);

            next.Trigger();
            machine.State.Should().Be(Light.Off);
            machine.Data.CycleCount.Should().Be(-1);
        }

        [Fact(Skip = "Implementation not ready")]
        public void RegardlessOfState_DataFul()
        {
            var initialData = new TrafficLightData(Light.Green, CycleCount: 50);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var shutdown)
                    .RegardlessOfState()
                    .Execute(data => data with { CycleCount = 0 })
                    .ThenTransitionTo(Light.Off)
                .Build(initialData);

            shutdown.Trigger();
            machine.State.Should().Be(Light.Off);
            machine.Data.CycleCount.Should().Be(0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void UndefinedTransition_DataFul_Throws()
        {
            var initialData = new TrafficLightData(Light.Red);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .DefineEvent(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(initialData);

            next.Trigger(); // Red → Green

            // Now in Green — next only handles Red
            var act = () => next.Trigger();
            act.Should().Throw<InvalidTransitionException>();
        }
    }
}
