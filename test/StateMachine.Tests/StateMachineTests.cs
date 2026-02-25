using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Xunit;

namespace StateMachine.Tests
{
    // ═══════════════════════════════════════════════════════════════════
    // Shared test types
    // ═══════════════════════════════════════════════════════════════════

    internal enum Light { Off, Red, Green, Yellow, FlashingRed }

    internal record TrafficLightData(
        Light Light,
        bool PedestrianWaiting = false,
        int SecondsInCurrentPhase = 0,
        string? Intersection = null,
        DateTime? LastTransitionAt = null
    );

    internal record EmergencyOverride(string AuthorizedBy, string Reason);

    // ═══════════════════════════════════════════════════════════════════
    // Comprehensive Scenario Example
    // ═══════════════════════════════════════════════════════════════════

    public class ComprehensiveScenarioTests
    {
        [Fact(Skip = "Implementation not ready")]
        public void ComprehensiveScenario_CoversAllStatesAndCommonBranches()
        {
            var observedTransitions = new List<TransitionedEventArgs<Light>>();
            var observedDataTransitions = new List<DataTransitionedEventArgs<Light, TrafficLightData>>();

            var initialData = new TrafficLightData(
                Light.Off,
                PedestrianWaiting: false,
                SecondsInCurrentPhase: 0,
                Intersection: "Main St & 1st Ave");

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)

                // Power on: controller comes online into Red
                .On(out var powerOn)
                    .WhenStateIs(Light.Off)
                    .Transform(d => d with { SecondsInCurrentPhase = 0, PedestrianWaiting = false })
                    .ThenTransitionTo(Light.Red)

                // Tick: time passes in the current phase (data update only)
                .On<int>(out var elapse)
                    .WhenStateIs(Light.Red, Light.Green, Light.Yellow, Light.FlashingRed)
                    .If((d, seconds) => seconds > 0, "Elapsed seconds must be positive")
                    .Transform((d, seconds) => d with { SecondsInCurrentPhase = d.SecondsInCurrentPhase + seconds })
                    .AndKeepSameState()

                // Request: pedestrian presses the button (state unchanged)
                .On(out var requestWalk)
                    .WhenStateIs(Light.Red)
                    .Transform(d => d with { PedestrianWaiting = true })
                    .AndKeepSameState()

                // Advance: controller evaluates whether to move to the next phase
                .On(out var advance)
                    .WhenStateIs(Light.Red)
                    .If(d => d.SecondsInCurrentPhase >= 10 && d.PedestrianWaiting, "Not ready to advance from red")
                    .Transform(d => d with { SecondsInCurrentPhase = 0, PedestrianWaiting = false })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .KeepSameState()
                    .WhenStateIs(Light.Green)
                    .If(d => d.SecondsInCurrentPhase >= 20, "Minimum green time (20s) not reached")
                    .Transform(d => d with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Yellow)
                    .Else
                    .KeepSameState()
                    .WhenStateIs(Light.Yellow)
                    .If(d => d.SecondsInCurrentPhase >= 3, "Minimum yellow time (3s) not reached")
                    .Transform(d => d with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Red)
                    .Else
                    .KeepSameState()
                    .WhenStateIs(Light.FlashingRed)
                    .If(d => d.SecondsInCurrentPhase >= 30, "Hold in flashing-red for 30s before resuming")
                    .Transform(d => d with { SecondsInCurrentPhase = 0, PedestrianWaiting = false })
                    .ThenTransitionTo(Light.Red)
                    .Else
                    .KeepSameState()

                // Emergency: can be invoked from any state
                .On(out var emergency)
                    .RegardlessOfState()
                    .Transform(d => d with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.FlashingRed)

                // Shutdown: can be invoked from any state
                .On(out var shutdown)
                    .RegardlessOfState()
                    .Transform(d => d with { SecondsInCurrentPhase = 0, PedestrianWaiting = false })
                    .ThenTransitionTo(Light.Off)

                .Build()
                .CreateInstance(initialData);

            machine.Transitioned += args => observedTransitions.Add(args);
            machine.DataTransitioned += args => observedDataTransitions.Add(args);

            machine.Should().BeInState(Light.Off);

            machine.Should().Accept(powerOn).WithState(Light.Red);
            machine.Inspect(powerOn).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);

            machine.Should().Accept(requestWalk).WithState(Light.Red);
            machine.Inspect(requestWalk).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.PedestrianWaiting);

            machine.Inspect(elapse).WithArg(7).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.SecondsInCurrentPhase == 7);

            // Not enough time yet — stays Red
            machine.Should().Accept(advance).WithState(Light.Red);
            machine.Inspect(advance).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);

            machine.Inspect(elapse).WithArg(3).IfAccepted().Fire();
            machine.Should().HaveData(d => d.SecondsInCurrentPhase == 10);

            // Meets guard — transitions Red -> Green and clears the request
            machine.Should().Accept(advance).WithState(Light.Green);
            machine.Inspect(advance).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => !d.PedestrianWaiting && d.SecondsInCurrentPhase == 0);

            machine.Inspect(elapse).WithArg(20).IfAccepted().Fire();
            machine.Should().Accept(advance).WithState(Light.Yellow);
            machine.Inspect(advance).IfAccepted().Fire();
            machine.Should().BeInState(Light.Yellow);

            machine.Inspect(elapse).WithArg(3).IfAccepted().Fire();
            machine.Should().Accept(advance).WithState(Light.Red);
            machine.Inspect(advance).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);

            // Emergency can be triggered from any state
            machine.Should().Accept(emergency).WithState(Light.FlashingRed);
            machine.Inspect(emergency).IfAccepted().Fire();
            machine.Should().BeInState(Light.FlashingRed);

            machine.Inspect(elapse).WithArg(30).IfAccepted().Fire();
            machine.Should().Accept(advance).WithState(Light.Red);
            machine.Inspect(advance).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);

            machine.Should().Accept(shutdown).WithState(Light.Off);
            machine.Inspect(shutdown).IfAccepted().Fire();
            machine.Should().BeInState(Light.Off);

            // Includes every state at least once in the observed transitions
            observedTransitions.Should().Contain(t => t.ToState == Light.Red);
            observedTransitions.Should().Contain(t => t.ToState == Light.Green);
            observedTransitions.Should().Contain(t => t.ToState == Light.Yellow);
            observedTransitions.Should().Contain(t => t.ToState == Light.FlashingRed);
            observedTransitions.Should().Contain(t => t.ToState == Light.Off);
            observedDataTransitions.Should().HaveSameCount(observedTransitions);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Data-Less State Machine Tests
    // ═══════════════════════════════════════════════════════════════════

    public class DataLessStateMachineTests
    {
        [Fact(Skip = "Implementation not ready")]
        public void InitialState_IsSetCorrectly()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsNotAccepted_WhenNoTransitionDefined()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Green); // Start in Green — next only handles Red

            machine.Should().Reject(next);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsAccepted_WhenTransitionDefined()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().Accept(next).WithState(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_StateDoesNotChange()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().Accept(hold).WithState(Light.Red);
            machine.Inspect(hold).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void SimpleTrafficLightCycle()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Green).TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow).TransitionTo(Light.Red)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().BeInState(Light.Red);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Green);

            machine.Should().Accept(next).WithState(Light.Yellow);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Yellow);

            machine.Should().Accept(next).WithState(Light.Red);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void TransitionObservation()
        {
            var transitions = new List<TransitionedEventArgs<Light>>();

            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Green).TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow).TransitionTo(Light.Red)
                .Build()
                .CreateInstance(Light.Red);

            machine.Transitioned += args => transitions.Add(args);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().Accept(next).WithState(Light.Yellow);
            machine.Inspect(next).IfAccepted().Fire();

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
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .On(out var shutdown)
                    .RegardlessOfState()
                    .TransitionTo(Light.Off)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().Accept(shutdown).WithState(Light.Off);
            machine.Inspect(shutdown).IfAccepted().Fire();
            machine.Should().BeInState(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void UndefinedTransition_Throws()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Green);

            // Triggering again from Green — not defined for this event
            machine.Should().Reject(next);
            var act = () => machine.Inspect(next).IfAccepted().Fire();
            act.Should().NotThrow();
        }

        [Fact(Skip = "Implementation not ready")]
        public void DuplicateTransition_ThrowsOnBuild()
        {
            var act = () => StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Yellow) // duplicate
                .Build();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact(Skip = "Implementation not ready")]
        public void MultipleEvents_IndependentTransitions()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .On(out var back)
                    .WhenStateIs(Light.Green)
                    .TransitionTo(Light.Red)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Green);

            machine.Should().Accept(back).WithState(Light.Red);
            machine.Inspect(back).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void EventName_CapturedFromVariable()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Red);

            machine.Events.Should().ContainSingle().Which.Name.Should().Be("next");
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_FiresTransitionedEvent()
        {
            var transitions = new List<TransitionedEventArgs<Light>>();

            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build()
                .CreateInstance(Light.Red);

            machine.Transitioned += args => transitions.Add(args);

            machine.Should().Accept(hold).WithState(Light.Red);
            machine.Inspect(hold).IfAccepted().Fire();

            transitions.Should().HaveCount(1);
            transitions[0].FromState.Should().Be(Light.Red);
            transitions[0].ToState.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void States_ReturnsAllEnumValues()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Red);

            machine.States.Should().BeEquivalentTo(
                new[] { Light.Off, Light.Red, Light.Green, Light.Yellow, Light.FlashingRed });
        }

        [Fact(Skip = "Implementation not ready")]
        public void Events_ContainsAllDefinedEvents()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .On(out var shutdown)
                    .WhenStateIs(Light.Green)
                    .TransitionTo(Light.Off)
                .Build()
                .CreateInstance(Light.Red);

            machine.Events.Should().HaveCount(2);
            machine.Events.Should().Contain(e => e.Name == "next");
            machine.Events.Should().Contain(e => e.Name == "shutdown");
        }

        [Fact(Skip = "Implementation not ready")]
        public void WhenStateIs_Params_AllowsMultipleFromStates()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var shutdown)
                    .WhenStateIs(Light.Red, Light.Green, Light.Yellow)
                    .TransitionTo(Light.Off)
                .Build()
                .CreateInstance(Light.Green);

            machine.Should().Accept(shutdown).WithState(Light.Off);
            machine.Inspect(shutdown).IfAccepted().Fire();
            machine.Should().BeInState(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_DoesNotMutateState()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(Light.Red);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Should().BeInState(Light.Red);
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
            // A pedestrian has pressed the walk button; controller is powered off.
            var initialData = new TrafficLightData(
                Light.Off,
                PedestrianWaiting: true,
                SecondsInCurrentPhase: 0,
                Intersection: "Main St & 1st Ave");

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)

                .On(out var powerOn)
                    .WhenStateIs(Light.Off)
                    .TransitionTo(Light.Red)

                .On(out var next)
                    // Red → Green: clear the walk request and reset the phase timer
                    .WhenStateIs(Light.Red)
                    .Transform(data => data with { PedestrianWaiting = false, SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                    .WhenStateIs(Light.Green)
                    .TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow)
                    .TransitionTo(Light.Red)

                .On<EmergencyOverride>(out var emergency)
                    .WhenStateIs(Light.Red, Light.Green, Light.Yellow)
                    .If((data, e) => !string.IsNullOrEmpty(e.AuthorizedBy),
                        "Emergency override requires authorization")
                    .Transform((data, e) => data with { LastTransitionAt = DateTime.Now })
                    .ThenTransitionTo(Light.FlashingRed)

                .On(out var shutdown)
                    .RegardlessOfState()
                    .TransitionTo(Light.Off)

                .Build()
                .CreateInstance(initialData);

            // Starts off; pedestrian button is set from initial data
            machine.Should()
                .BeInState(Light.Off).And
                .HaveData(d => d.Intersection == "Main St & 1st Ave" && d.PedestrianWaiting && d.SecondsInCurrentPhase == 0);

            // Power on — light comes up red, data unchanged
            machine.Should().Accept(powerOn).WithState(Light.Red);
            machine.Inspect(powerOn).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.PedestrianWaiting);

            // Red → Green: walk signal granted, vehicle sensor cleared
            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => !d.PedestrianWaiting && d.SecondsInCurrentPhase == 0);

            // Green → Yellow
            machine.Should().Accept(next).WithState(Light.Yellow);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Yellow);

            // Yellow → Red
            machine.Should().Accept(next).WithState(Light.Red);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);

            // Second green phase — no pedestrian waiting, no queued vehicles
            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => !d.PedestrianWaiting && d.SecondsInCurrentPhase == 0);

            // Emergency override — first responder requested flashing red
            machine.Should().Accept(emergency, new EmergencyOverride("Officer Smith", "Accident at intersection")).WithState(Light.FlashingRed);
            machine.Inspect(emergency).WithArg(new EmergencyOverride("Officer Smith", "Accident at intersection")).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.FlashingRed).And
                .HaveData(d => d.LastTransitionAt != null);

            // Shutdown
            machine.Should().Accept(shutdown).WithState(Light.Off);
            machine.Inspect(shutdown).IfAccepted().Fire();
            machine.Should().BeInState(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void InitialState_And_InitialData()
        {
            // Light has been red for 25 seconds; pedestrian has pressed the button
            var initialData = new TrafficLightData(Light.Red, PedestrianWaiting: true, SecondsInCurrentPhase: 25);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.PedestrianWaiting && d.SecondsInCurrentPhase == 25);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_DataUnchanged()
        {
            // Pedestrian has pressed the walk button; holding red does not clear requests or timers
            var initialData = new TrafficLightData(Light.Red, PedestrianWaiting: true, SecondsInCurrentPhase: 3);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(hold).WithState(Light.Red);
            machine.Inspect(hold).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.PedestrianWaiting && d.SecondsInCurrentPhase == 3);
        }

        [Fact(Skip = "Implementation not ready")]
        public void SimpleTransition_WithExecute()
        {
            // Light has been red long enough; advancing to green resets the phase timer
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 30);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => d.SecondsInCurrentPhase == 0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_WithExecute()
        {
            // Pedestrian button presses accumulate while the light is red
            var initialData = new TrafficLightData(Light.Red, PedestrianWaiting: false);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var pedestrianRequest)
                    .WhenStateIs(Light.Red)
                    .Transform(data => data with { PedestrianWaiting = true }) // button pressed
                    .AndKeepSameState()
                .Build()
                .CreateInstance(initialData);

            // First button press — light stays red, walk request registered
            machine.Should().Accept(pedestrianRequest).WithState(Light.Red);
            machine.Inspect(pedestrianRequest).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.PedestrianWaiting);

            // Second press — light still red, request already set
            machine.Should().Accept(pedestrianRequest).WithState(Light.Red);
            machine.Inspect(pedestrianRequest).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.PedestrianWaiting);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Passes_TransitionsState()
        {
            // Minimum red time satisfied — controller may advance to green
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 12);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_NoElse_ThrowsGuardFailedException()
        {
            // Red has not been active long enough — should not advance
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 3);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Reject(next).WithReason("Minimum red time (10s) not reached");
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Passes_ExecuteAndTransition()
        {
            // Minimum red time satisfied; advancing to green also resets phase timer
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 15);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => d.SecondsInCurrentPhase == 0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_Else_KeepsSameState()
        {
            // Too early to change; Else branch keeps the light red
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 3);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .TransitionTo(Light.Green)
                    .Else
                    .KeepSameState()
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Red);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_Else_TransitionsToAlternate()
        {
            // If red has been active far too long, fail-safe enters flashing-red mode
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 999);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase <= 120, "Red phase exceeded maximum duration")
                    .TransitionTo(Light.Green)
                    .Else
                    .TransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.FlashingRed);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.FlashingRed);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_ElseIf_Passes_Transitions()
        {
            // Red is stuck beyond the normal operating window; Else.If routes to fail-safe flashing-red
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 999);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10 && data.SecondsInCurrentPhase <= 120,
                        "Red phase not within normal operating window")
                    .TransitionTo(Light.Green)
                    .Else
                    .If(data => data.SecondsInCurrentPhase > 120, "Red phase exceeded maximum duration")
                    .TransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.FlashingRed);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should().BeInState(Light.FlashingRed);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_Else_ExecuteAndTransition()
        {
            // Red has been active far too long; Else branch enters flashing-red and resets timer
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 999);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase <= 120, "Red phase exceeded maximum duration")
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.FlashingRed);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.FlashingRed).And
                .HaveData(d => d.SecondsInCurrentPhase == 0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Guard_Fails_ElseIf_Passes_ExecuteAndTransition()
        {
            // Red has exceeded maximum duration; Else.If records a timestamp and enters flashing-red
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 999, LastTransitionAt: null);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10 && data.SecondsInCurrentPhase <= 120,
                        "Red phase not within normal operating window")
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .If(data => data.SecondsInCurrentPhase > 120, "Red phase exceeded maximum duration")
                    .Transform(data => data with { LastTransitionAt = DateTime.UtcNow })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.FlashingRed);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.FlashingRed).And
                .HaveData(d => d.SecondsInCurrentPhase == 999 && d.LastTransitionAt != null);
        }

        [Fact(Skip = "Implementation not ready")]
        public void AllGuards_Fail_ThrowsGuardFailedWithAllReasons()
        {
            // Red just started; all minimum-time guards fail
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 60, "Minimum red time (60s) not reached")
                    .TransitionTo(Light.Green)
                    .Else
                    .If(data => data.SecondsInCurrentPhase >= 30, "Minimum red time (30s) not reached")
                    .TransitionTo(Light.Green)
                    .Else
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .TransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Reject(next)
                .WithReason("Minimum red time (60s) not reached")
                .WithReason("Minimum red time (30s) not reached")
                .WithReason("Minimum red time (10s) not reached");
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void ExecuteTransform_Throws_StateAndDataUnchanged()
        {
            // If a timing update transform throws, state and timer should be unchanged
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 15);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .Transform(data => throw new InvalidOperationException("timer update failed"))
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            var act = () => machine.Inspect(next).IfAccepted().Fire();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("timer update failed");

            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.SecondsInCurrentPhase == 15);
        }

        [Fact(Skip = "Implementation not ready")]
        public void GuardedExecuteTransform_Throws_StateAndDataUnchanged()
        {
            // Guard passes, but the timing transform throws
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 12);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .Transform(data => throw new InvalidOperationException("timer hardware fault"))
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            var act = () => machine.Inspect(next).IfAccepted().Fire();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("timer hardware fault");

            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.SecondsInCurrentPhase == 12);
        }

        [Fact(Skip = "Implementation not ready")]
        public void DuplicateTransition_ThrowsOnBuild()
        {
            var act = () => StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Yellow) // duplicate
                .Build();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact(Skip = "Implementation not ready")]
        public void ImmutableData_OriginalRecordUnchanged()
        {
            // Pedestrian button was pressed (latched). When the controller grants a green phase,
            // it clears the pedestrian request in the machine's data snapshot.
            var initialData = new TrafficLightData(Light.Red, PedestrianWaiting: true, SecondsInCurrentPhase: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .Transform(data => data with { PedestrianWaiting = false })
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();

            // The original record is untouched — records are immutable
            initialData.Light.Should().Be(Light.Red);
            initialData.PedestrianWaiting.Should().BeTrue();
            initialData.SecondsInCurrentPhase.Should().Be(0);

            // The machine holds the updated snapshot
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => d.Light == Light.Green && !d.PedestrianWaiting && d.SecondsInCurrentPhase == 0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void GuardFails_ThrowsGuardFailedException()
        {
            var initialData = new TrafficLightData(Light.Red);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On<EmergencyOverride>(out var emergency)
                    .WhenStateIs(Light.Red)
                    .If((data, e) => !string.IsNullOrEmpty(e.AuthorizedBy),
                        "Emergency override requires authorization")
                    .Transform((data, e) => data with { LastTransitionAt = DateTime.Now })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            // Missing badge ID — override should be rejected
            machine.Should().Reject(emergency, new EmergencyOverride("", "Unauthorized")).WithReason("Emergency override requires authorization");
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsExpectedOutcomeWithoutFiring()
        {
            var initialData = new TrafficLightData(Light.Red);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On<EmergencyOverride>(out var emergency)
                    .WhenStateIs(Light.Red)
                    .If((data, e) => !string.IsNullOrEmpty(e.AuthorizedBy),
                        "Emergency override requires authorization")
                    .Transform((data, e) => data with { LastTransitionAt = DateTime.Now })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            // Test with missing badge ID — guard should reject without changing state
            machine.Should().Reject(emergency, new EmergencyOverride("", "Unauthorized")).WithReason("Emergency override requires authorization");
            machine.Should().BeInState(Light.Red);

            // Test with valid override — guard should accept
            machine.Should().Accept(emergency, new EmergencyOverride("Officer Smith", "Accident")).WithState(Light.FlashingRed);

            // State should still not have changed
            machine.Should().BeInState(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void DataTransitioned_ProvidesOldAndNewData()
        {
            var transitions = new List<DataTransitionedEventArgs<Light, TrafficLightData>>();
            // Red has been active for 12 seconds; going green resets timer
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 12);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.DataTransitioned += args => transitions.Add(args);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();

            transitions.Should().HaveCount(1);
            transitions[0].OldData.SecondsInCurrentPhase.Should().Be(12);
            transitions[0].NewData.SecondsInCurrentPhase.Should().Be(0);
            transitions[0].FromState.Should().Be(Light.Red);
            transitions[0].ToState.Should().Be(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void ParameterizedEvent_PassesArgToGuardAndTransform()
        {
            // Simulate time elapsing; transition only if the minimum red time will be satisfied
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 5);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On<int>(out var elapse)
                    .WhenStateIs(Light.Red)
                    .If((data, seconds) => seconds > 0 && data.SecondsInCurrentPhase + seconds >= 10,
                        "Minimum red time (10s) not reached")
                    .Transform((data, seconds) => data with { SecondsInCurrentPhase = data.SecondsInCurrentPhase + seconds })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .If((data, seconds) => seconds > 0, "Elapsed seconds must be positive")
                    .Transform((data, seconds) => data with { SecondsInCurrentPhase = data.SecondsInCurrentPhase + seconds })
                    .AndKeepSameState()
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(elapse, 6).WithState(Light.Green);
            machine.Inspect(elapse).WithArg(6).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => d.SecondsInCurrentPhase == 11);
        }

        [Fact(Skip = "Implementation not ready")]
        public void ParameterizedEvent_GuardRejectsInvalidArg()
        {
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 5);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On<int>(out var elapse)
                    .WhenStateIs(Light.Red)
                    .If((data, seconds) => seconds > 0 && data.SecondsInCurrentPhase + seconds >= 10,
                        "Minimum red time (10s) not reached")
                    .Transform((data, seconds) => data with { SecondsInCurrentPhase = data.SecondsInCurrentPhase + seconds })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .If((data, seconds) => seconds > 0, "Elapsed seconds must be positive")
                    .Transform((data, seconds) => data with { SecondsInCurrentPhase = data.SecondsInCurrentPhase + seconds })
                    .AndKeepSameState()
                .Build()
                .CreateInstance(initialData);

            // Negative time delta is invalid
            machine.Should().Reject(elapse, -3).WithReason("Elapsed seconds must be positive");
            machine.Should()
                .BeInState(Light.Red).And
                .HaveData(d => d.SecondsInCurrentPhase == 5);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Machine_StampsState_OverridingTransform()
        {
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    // Transform tries to set Light to FlashingRed — machine must overwrite with the declared target (Green)
                    .Transform(data => data with { Light = Light.FlashingRed, SecondsInCurrentPhase = 123 })
                    .ThenTransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And // Machine wins — declared transition target
                .HaveData(d => d.Light == Light.Green && d.SecondsInCurrentPhase == 123); // Data agrees with machine state; transform side-effect preserved
        }

        [Fact(Skip = "Implementation not ready")]
        public void TransitionWithoutExecute_OnlyStateChanges()
        {
            // Plain transition must not touch data fields
            var initialData = new TrafficLightData(Light.Red, PedestrianWaiting: true, SecondsInCurrentPhase: 42);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.Green);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Green).And
                .HaveData(d => d.Light == Light.Green && d.PedestrianWaiting && d.SecondsInCurrentPhase == 42); // unchanged — no Execute to clear it
        }

        [Fact(Skip = "Implementation not ready")]
        public void Else_ExecutesBranch_WhenAllGuardsFail()
        {
            // If the controller can't satisfy the minimum-time guard, it can fall back to flashing-red
            var initialData = new TrafficLightData(Light.Red, SecondsInCurrentPhase: 0);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .If(data => data.SecondsInCurrentPhase >= 10, "Minimum red time (10s) not reached")
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Green)
                    .Else
                    .Transform(data => data with { SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.FlashingRed)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(next).WithState(Light.FlashingRed);
            machine.Inspect(next).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.FlashingRed).And
                .HaveData(d => d.SecondsInCurrentPhase == 0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void RegardlessOfState_DataFul()
        {
            // Emergency shutdown clears pending requests and resets timer
            var initialData = new TrafficLightData(Light.Green, PedestrianWaiting: true, SecondsInCurrentPhase: 17);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var shutdown)
                    .RegardlessOfState()
                    .Transform(data => data with { PedestrianWaiting = false, SecondsInCurrentPhase = 0 })
                    .ThenTransitionTo(Light.Off)
                .Build()
                .CreateInstance(initialData);

            machine.Should().Accept(shutdown).WithState(Light.Off);
            machine.Inspect(shutdown).IfAccepted().Fire();
            machine.Should()
                .BeInState(Light.Off).And
                .HaveData(d => !d.PedestrianWaiting && d.SecondsInCurrentPhase == 0);
        }

        [Fact(Skip = "Implementation not ready")]
        public void UndefinedTransition_DataFul_Throws()
        {
            var initialData = new TrafficLightData(Light.Red);

            var machine = StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build()
                .CreateInstance(initialData);

            machine.Inspect(next).IfAccepted().Fire(); // Red → Green

            // Now in Green — next only handles Red
            machine.Should().Reject(next);
            var act = () => machine.Inspect(next).IfAccepted().Fire();
            act.Should().NotThrow();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Custom FluentAssertions extensions for IStateMachine<TState>
    // ═══════════════════════════════════════════════════════════════════

    public static class StateMachineAssertionsExtensions
    {
        public static StateMachineAssertions<TState> Should<TState>(this IStateMachine<TState> machine)
            where TState : notnull, Enum
            => new StateMachineAssertions<TState>(machine);

        public static DatafulStateMachineAssertions<TState, TData> Should<TState, TData>(this IStateMachine<TState, TData> machine)
            where TState : notnull, Enum
            => new DatafulStateMachineAssertions<TState, TData>(machine);
    }

    /// <summary>Fluent result returned by <see cref="StateMachineAssertions{TState}.Accept(StateMachine.Event{TState}, string, object[])"/>.</summary>
    public sealed class AcceptedAssertions<TState>
        where TState : notnull, Enum
    {
        private readonly TState _resolvedState;
        private readonly AssertionChain _chain;

        internal AcceptedAssertions(TState resolvedState, AssertionChain chain)
        {
            _resolvedState = resolvedState;
            _chain = chain;
        }

        public AcceptedAssertions<TState> WithState(TState expected, string because = "", params object[] becauseArgs)
        {
            _chain
                .BecauseOf(because, becauseArgs)
                .ForCondition(_resolvedState.Equals(expected))
                .FailWith("Expected {{context}} to resolve to state {0}{reason}, but resolved to {1}", expected, _resolvedState);
            return this;
        }
    }

    /// <summary>Fluent result returned by <see cref="StateMachineAssertions{TState}.NotAccept"/>.</summary>
    public sealed class RejectedAssertions<TState>
        where TState : notnull, Enum
    {
        private readonly IReadOnlyList<string> _reasons;
        private readonly AssertionChain _chain;

        internal RejectedAssertions(IReadOnlyList<string> reasons, AssertionChain chain)
        {
            _reasons = reasons;
            _chain = chain;
        }

        public RejectedAssertions<TState> WithReason(string expected, string because = "", params object[] becauseArgs)
        {
            _chain
                .BecauseOf(because, becauseArgs)
                .ForCondition(_reasons.Contains(expected))
                .FailWith("Expected {{context}} rejection reasons to contain {0}{reason}, but got: [{1}]", expected, string.Join(", ", _reasons));
            return this;
        }
    }

    /// <summary>Custom assertions for <see cref="IStateMachine{TState}"/>.</summary>
    public class StateMachineAssertions<TState>
        : ReferenceTypeAssertions<IStateMachine<TState>, StateMachineAssertions<TState>>
        where TState : notnull, Enum
    {
        public StateMachineAssertions(IStateMachine<TState> subject)
            : base(subject, AssertionChain.GetOrCreate()) { }

        protected override string Identifier => "state machine";

        public AndConstraint<StateMachineAssertions<TState>> BeInState(
            TState expected, string because = "", params object[] becauseArgs)
        {
            AssertionChain.GetOrCreate()
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.State.Equals(expected))
                .FailWith("Expected {context} to be in state {0}{reason}, but was in {1}", expected, Subject.State);
            return new AndConstraint<StateMachineAssertions<TState>>(this);
        }

        public AcceptedAssertions<TState> Accept(
            Event<TState> trigger,
            string because = "",
            params object[] becauseArgs)
        {
            var inspection = Inspect(trigger);
            var chain = AssertionChain.GetOrCreate();
            chain.BecauseOf(because, becauseArgs)
                .ForCondition(inspection.IsAccepted)
                .FailWith("Expected {context} to accept event {0}{reason}, but it was rejected with: [{1}]",
                    trigger.Name,
                    string.Join(", ", inspection.Reasons));

            return new AcceptedAssertions<TState>(inspection.TargetState, chain);
        }

        public AcceptedAssertions<TState> Accept<TArg>(
            Event<TState, TArg> trigger,
            TArg arg,
            string because = "",
            params object[] becauseArgs)
        {
            var inspection = Inspect(trigger, arg);
            var chain = AssertionChain.GetOrCreate();
            chain.BecauseOf(because, becauseArgs)
                .ForCondition(inspection.IsAccepted)
                .FailWith("Expected {context} to accept event {0}{reason}, but it was rejected with: [{1}]",
                    trigger.Name,
                    string.Join(", ", inspection.Reasons));

            return new AcceptedAssertions<TState>(inspection.TargetState, chain);
        }

        public RejectedAssertions<TState> Reject(
            Event<TState> trigger,
            string because = "",
            params object[] becauseArgs)
        {
            var inspection = Inspect(trigger);
            var chain = AssertionChain.GetOrCreate();
            chain.BecauseOf(because, becauseArgs)
                .ForCondition(!inspection.IsAccepted)
                .FailWith("Expected {context} to reject event {0}{reason}, but it was accepted with target state {1}",
                    trigger.Name,
                    inspection.TargetState);

            return new RejectedAssertions<TState>(inspection.Reasons, chain);
        }

        public RejectedAssertions<TState> Reject<TArg>(
            Event<TState, TArg> trigger,
            TArg arg,
            string because = "",
            params object[] becauseArgs)
        {
            var inspection = Inspect(trigger, arg);
            var chain = AssertionChain.GetOrCreate();
            chain.BecauseOf(because, becauseArgs)
                .ForCondition(!inspection.IsAccepted)
                .FailWith("Expected {context} to reject event {0}{reason}, but it was accepted with target state {1}",
                    trigger.Name,
                    inspection.TargetState);

            return new RejectedAssertions<TState>(inspection.Reasons, chain);
        }

        private EventInspection<TState> Inspect(Event<TState> trigger)
        {
            if (Subject is null)
            {
                throw new NullReferenceException("Subject should not be null");
            }

            return Subject.Inspect(trigger);
        }

        private EventInspection<TState> Inspect<TArg>(Event<TState, TArg> trigger, TArg arg)
        {
            if (Subject is null)
            {
                throw new NullReferenceException("Subject should not be null");
            }

            return Subject.Inspect(trigger).WithArg(arg);
        }
    }

    /// <summary>Custom assertions for <see cref="IStateMachine{TState, TData}"/>, adding <see cref="HaveData"/>.</summary>
    public sealed class DatafulStateMachineAssertions<TState, TData>
        : StateMachineAssertions<TState>
        where TState : notnull, Enum
    {
        private readonly IStateMachine<TState, TData> _dataSubject;

        public DatafulStateMachineAssertions(IStateMachine<TState, TData> subject)
            : base(subject)
        {
            _dataSubject = subject;
        }

        public new AndConstraint<DatafulStateMachineAssertions<TState, TData>> BeInState(
            TState expected, string because = "", params object[] becauseArgs)
        {
            AssertionChain.GetOrCreate()
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.State.Equals(expected))
                .FailWith("Expected {context} to be in state {0}{reason}, but was in {1}", expected, Subject.State);
            return new AndConstraint<DatafulStateMachineAssertions<TState, TData>>(this);
        }

        /// <summary>Assert that the current data record satisfies <paramref name="predicate"/>.</summary>
        public AndConstraint<DatafulStateMachineAssertions<TState, TData>> HaveData(
            Func<TData, bool> predicate, string because = "", params object[] becauseArgs)
        {
            AssertionChain.GetOrCreate()
                .BecauseOf(because, becauseArgs)
                .ForCondition(predicate(_dataSubject.Data))
                .FailWith("Expected {context} data to satisfy the predicate{reason}, but it did not");
            return new AndConstraint<DatafulStateMachineAssertions<TState, TData>>(this);
        }
    }

}