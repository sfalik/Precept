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

                .Build(initialData);

            machine.Transitioned += args => observedTransitions.Add(args);
            machine.DataTransitioned += args => observedDataTransitions.Add(args);

            machine.State.Should().Be(Light.Off);

            powerOn();
            machine.State.Should().Be(Light.Red);

            requestWalk();
            machine.State.Should().Be(Light.Red);
            machine.Data.PedestrianWaiting.Should().BeTrue();

            elapse(7);
            machine.State.Should().Be(Light.Red);
            machine.Data.SecondsInCurrentPhase.Should().Be(7);

            // Not enough time yet — stays Red
            advance();
            machine.State.Should().Be(Light.Red);

            elapse(3);
            machine.Data.SecondsInCurrentPhase.Should().Be(10);

            // Meets guard — transitions Red -> Green and clears the request
            advance();
            machine.State.Should().Be(Light.Green);
            machine.Data.PedestrianWaiting.Should().BeFalse();
            machine.Data.SecondsInCurrentPhase.Should().Be(0);

            elapse(20);
            advance();
            machine.State.Should().Be(Light.Yellow);

            elapse(3);
            advance();
            machine.State.Should().Be(Light.Red);

            // Emergency can be triggered from any state
            emergency();
            machine.State.Should().Be(Light.FlashingRed);

            elapse(30);
            advance();
            machine.State.Should().Be(Light.Red);

            shutdown();
            machine.State.Should().Be(Light.Off);

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
                .Build(Light.Red);

            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsNotAccepted_WhenNoTransitionDefined()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Green); // Start in Green — next only handles Red

            machine.CanHandle(next, out _, out _).Should().BeFalse();
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_ReturnsAccepted_WhenTransitionDefined()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            machine.CanHandle(next, out var state, out _).Should().BeTrue();
            state.Should().Be(Light.Green);
        }

        [Fact(Skip = "Implementation not ready")]
        public void KeepSameState_StateDoesNotChange()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var hold)
                    .WhenStateIs(Light.Red)
                    .KeepSameState()
                .Build(Light.Red);

            machine.TryHandle(hold, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void SimpleTrafficLightCycle()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Green).TransitionTo(Light.Yellow)
                    .WhenStateIs(Light.Yellow).TransitionTo(Light.Red)
                .Build(Light.Red);

            machine.State.Should().Be(Light.Red);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Yellow);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
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
                .Build(Light.Red);

            machine.Transitioned += args => transitions.Add(args);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.TryHandle(next, out _).Should().BeTrue();

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
                .Build(Light.Red);

            machine.TryHandle(shutdown, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void UndefinedTransition_Throws()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);

            // Triggering again from Green — not defined for this event
            machine.TryHandle(next, out _).Should().BeFalse();
            var act = () => next();
            act.Should().Throw<InvalidTransitionException>();
        }

        [Fact(Skip = "Implementation not ready")]
        public void DuplicateTransition_ThrowsOnBuild()
        {
            var act = () => StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Yellow) // duplicate
                .Build(Light.Red);

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
                .Build(Light.Red);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);

            machine.TryHandle(back, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
        }

        [Fact(Skip = "Implementation not ready")]
        public void EventName_CapturedFromVariable()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

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
                .Build(Light.Red);

            machine.Transitioned += args => transitions.Add(args);

            machine.TryHandle(hold, out _).Should().BeTrue();

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
                .Build(Light.Red);

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
                .Build(Light.Red);

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
                .Build(Light.Green);

            machine.TryHandle(shutdown, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Off);
        }

        [Fact(Skip = "Implementation not ready")]
        public void Test_DoesNotMutateState()
        {
            var machine = StateMachine.CreateBuilder<Light>()
                .On(out var next)
                    .WhenStateIs(Light.Red)
                    .TransitionTo(Light.Green)
                .Build(Light.Red);

            machine.CanHandle(next, out var state, out _).Should().BeTrue();
            state.Should().Be(Light.Green);

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

                .Build(initialData);

            // Starts off; pedestrian button is set from initial data
            machine.State.Should().Be(Light.Off);
            machine.Data.Intersection.Should().Be("Main St & 1st Ave");
            machine.Data.PedestrianWaiting.Should().BeTrue();
            machine.Data.SecondsInCurrentPhase.Should().Be(0);

            // Power on — light comes up red, data unchanged
            machine.TryHandle(powerOn, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
            machine.Data.PedestrianWaiting.Should().BeTrue();

            // Red → Green: walk signal granted, vehicle sensor cleared
            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
            machine.Data.PedestrianWaiting.Should().BeFalse();
            machine.Data.SecondsInCurrentPhase.Should().Be(0);

            // Green → Yellow
            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Yellow);

            // Yellow → Red
            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);

            // Second green phase — no pedestrian waiting, no queued vehicles
            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
            machine.Data.PedestrianWaiting.Should().BeFalse();
            machine.Data.SecondsInCurrentPhase.Should().Be(0);

            // Emergency override — first responder requested flashing red
            machine.TryHandle(emergency, new EmergencyOverride("Officer Smith", "Accident at intersection"), out _).Should().BeTrue();
            machine.State.Should().Be(Light.FlashingRed);
            machine.Data.LastTransitionAt.Should().NotBeNull();

            // Shutdown
            machine.TryHandle(shutdown, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Off);
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
                .Build(initialData);

            machine.State.Should().Be(Light.Red);
            machine.Data.PedestrianWaiting.Should().BeTrue();
            machine.Data.SecondsInCurrentPhase.Should().Be(25);
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
                .Build(initialData);

            machine.TryHandle(hold, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
            machine.Data.PedestrianWaiting.Should().BeTrue();
            machine.Data.SecondsInCurrentPhase.Should().Be(3);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
            machine.Data.SecondsInCurrentPhase.Should().Be(0);
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
                .Build(initialData);

            // First button press — light stays red, walk request registered
            machine.TryHandle(pedestrianRequest, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
            machine.Data.PedestrianWaiting.Should().BeTrue();

            // Second press — light still red, request already set
            machine.TryHandle(pedestrianRequest, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
            machine.Data.PedestrianWaiting.Should().BeTrue();
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
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
                .Build(initialData);

            machine.TryHandle(next, out var reasons).Should().BeFalse();
            reasons.Should().Contain("Minimum red time (10s) not reached");
            machine.State.Should().Be(Light.Red);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
            machine.Data.SecondsInCurrentPhase.Should().Be(0);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Red);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.FlashingRed);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.FlashingRed);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.FlashingRed);
            machine.Data.SecondsInCurrentPhase.Should().Be(0);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.FlashingRed);
            machine.Data.SecondsInCurrentPhase.Should().Be(999);
            machine.Data.LastTransitionAt.Should().NotBeNull();
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
                .Build(initialData);

            machine.TryHandle(next, out var reasons).Should().BeFalse();
            reasons.Should().HaveCount(3)
                .And.Contain("Minimum red time (60s) not reached")
                .And.Contain("Minimum red time (30s) not reached")
                .And.Contain("Minimum red time (10s) not reached");
            machine.State.Should().Be(Light.Red);
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
                .Build(initialData);

            var act = () => machine.TryHandle(next, out _);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("timer update failed");

            machine.State.Should().Be(Light.Red);
            machine.Data.SecondsInCurrentPhase.Should().Be(15);
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
                .Build(initialData);

            var act = () => machine.TryHandle(next, out _);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("timer hardware fault");

            machine.State.Should().Be(Light.Red);
            machine.Data.SecondsInCurrentPhase.Should().Be(12);
        }

        [Fact(Skip = "Implementation not ready")]
        public void DuplicateTransition_ThrowsOnBuild()
        {
            var act = () => StateMachine.CreateBuilder<Light>()
                .WithData<TrafficLightData>(d => d.Light)
                .On(out var next)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Green)
                    .WhenStateIs(Light.Red).TransitionTo(Light.Yellow) // duplicate
                .Build(new TrafficLightData(Light.Red));

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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();

            // The original record is untouched — records are immutable
            initialData.Light.Should().Be(Light.Red);
            initialData.PedestrianWaiting.Should().BeTrue();
            initialData.SecondsInCurrentPhase.Should().Be(0);

            // The machine holds the updated snapshot
            machine.Data.Light.Should().Be(Light.Green);
            machine.Data.PedestrianWaiting.Should().BeFalse();
            machine.Data.SecondsInCurrentPhase.Should().Be(0);
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
                .Build(initialData);

            // Missing badge ID — override should be rejected
            machine.TryHandle(emergency, new EmergencyOverride("", "Unauthorized"), out var reasons).Should().BeFalse();
            reasons.Should().Contain("Emergency override requires authorization");
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
                .Build(initialData);

            // Test with missing badge ID — guard should reject without changing state
            machine.CanHandle(emergency, new EmergencyOverride("", "Unauthorized"), out _, out var invalidReasons).Should().BeFalse();
            invalidReasons.Should().Contain("Emergency override requires authorization");
            machine.State.Should().Be(Light.Red);

            // Test with valid override — guard should accept
            machine.CanHandle(emergency, new EmergencyOverride("Officer Smith", "Accident"), out var state, out var validReasons).Should().BeTrue();
            state.Should().Be(Light.FlashingRed);
            validReasons.Should().BeEmpty();

            // State should still not have changed
            machine.State.Should().Be(Light.Red);
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
                .Build(initialData);

            machine.DataTransitioned += args => transitions.Add(args);

            machine.TryHandle(next, out _).Should().BeTrue();

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
                .Build(initialData);

            machine.TryHandle(elapse, 6, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
            machine.Data.SecondsInCurrentPhase.Should().Be(11);
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
                .Build(initialData);

            // Negative time delta is invalid
            machine.TryHandle(elapse, -3, out var reasons).Should().BeFalse();
            reasons.Should().Contain("Elapsed seconds must be positive");
            machine.State.Should().Be(Light.Red);
            machine.Data.SecondsInCurrentPhase.Should().Be(5);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);           // Machine wins — declared transition target
            machine.Data.Light.Should().Be(Light.Green);      // Data agrees with machine state
            machine.Data.SecondsInCurrentPhase.Should().Be(123);   // Transform side-effect preserved
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Green);
            machine.Data.Light.Should().Be(Light.Green);
            machine.Data.PedestrianWaiting.Should().BeTrue();   // unchanged — no Execute to clear it
            machine.Data.SecondsInCurrentPhase.Should().Be(42);  // unchanged
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue();
            machine.State.Should().Be(Light.FlashingRed);
            machine.Data.SecondsInCurrentPhase.Should().Be(0);
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
                .Build(initialData);

            machine.TryHandle(shutdown, out _).Should().BeTrue();
            machine.State.Should().Be(Light.Off);
            machine.Data.PedestrianWaiting.Should().BeFalse();
            machine.Data.SecondsInCurrentPhase.Should().Be(0);
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
                .Build(initialData);

            machine.TryHandle(next, out _).Should().BeTrue(); // Red → Green

            // Now in Green — next only handles Red
            machine.TryHandle(next, out _).Should().BeFalse();
            var act = () => next();
            act.Should().Throw<InvalidTransitionException>();
        }
    }
}
