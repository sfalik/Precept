using System;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Xunit;

namespace StateMachine.Tests
{
    public class FiniteStateMachineTests
    {
        enum State
        {
            InitialState,
            State1,
            State2,
            State3,
            Error
        }

        enum Event
        {
            Event1,
            Event2,
            Event3
        }


        FiniteStateMachine<State, Event> _stateMachine = new FiniteStateMachine<State, Event>(State.InitialState);

        /// <summary>
        /// Test that the initial state is set correctly
        /// </summary>
        [Fact]
        public void TestInitialState()
        {
            _stateMachine.Should().BeInState(State.InitialState);
        }

        [Fact]
        public void TestEventsNotAccepted()
        {
            _stateMachine.Should().NotAcceptEvent(Event.Event1);
            _stateMachine.Should().NotAcceptEvent(Event.Event2);
            _stateMachine.Should().NotAcceptEvent(Event.Event3);

            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<InvalidTransitionException>("because the transition was not defined");
        }

        /// <summary>
        /// Test that a transition that does not trigger an actual state change works correctly (state stays the same)
        /// </summary>
        [Fact]
        public void TestKeepSameState()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1).KeepCurrentState();

            _stateMachine.Should().AcceptEvent(Event.Event1);
            _stateMachine.Should().NotAcceptEvent(Event.Event2);
            _stateMachine.Should().NotAcceptEvent(Event.Event3);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.InitialState);
        }

        /// <summary>
        /// Test that a simple state transition works correctly (state changes)
        /// </summary>
        [Fact]
        public void TestSimpleTransition()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1).TransitionTo(State.State1);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State1);
        }

        /// <summary>
        /// Test that a state transition with an action works correctly (action is run, state changes)
        /// </summary>
        [Fact]
        public void TestSimpleTransitionWithAction()
        {
            bool completed = false;

            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .TransitionTo(State.State1)
                .WithAction(() => completed = true, State.InitialState);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State1);
            completed.Should().BeTrue();
        }

        /// <summary>
        /// Test that a state transition fails when the condition on the transition is not satisified
        /// </summary>
        [Fact]
        public void TestConditionalTransitionInvalid()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1);

            _stateMachine.Should().NotAcceptEvent(Event.Event1);

            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<ConditionFailedException>();
        }

        /// <summary>
        /// Test that the event is accepted and the state does not change when the condition is satisified
        /// </summary>
        [Fact]
        public void TestConditionalKeepSameState()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => true)
                    .KeepCurrentState();

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.InitialState);
        }

        /// <summary>
        /// Test that the event is accepted and the state transitions when the condition is satisified
        /// </summary>
        [Fact]
        public void TestConditionalTransition()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => true)
                    .TransitionTo(State.State1);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State1);
        }

        /// <summary>
        /// Test that the event is accepted, the action is executed, and the state transitions when the condition is satisified
        /// </summary>
        [Fact]
        public void TestConditionalTransitionWithAction()
        {
            bool completed = false;

            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => true)
                    .TransitionTo(State.State1)
                    .WithAction(() => completed = true, State.InitialState);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State1);
            completed.Should().BeTrue();
        }

        /// <summary>
        /// Test that the Else clause works to accept the event and keep the state the same
        /// </summary>
        [Fact]
        public void TestConditionalTransitionElseKeepSame()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1)
                .Else
                    .KeepCurrentState();

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.InitialState);
        }

        /// <summary>
        /// Test that the Else clause works to transition the state
        /// </summary>
        [Fact]
        public void TestConditionalTransitionElse()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1)
                .Else
                    .TransitionTo(State.State2);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State2);
        }

        /// <summary>
        /// Test that the Else.If works to define and drive a second state transition
        /// </summary>
        [Fact]
        public void TestConditionalTransitionSecondIf()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1)
                .Else.If(() => true)
                    .TransitionTo(State.State2);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State2);
        }

        /// <summary>
        /// Test that the Else works to define and drive a second state transition and action
        /// </summary>
        [Fact]
        public void TestConditionalTransitionElseWithAction()
        {
            bool completed = false;

            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1)
                    .WithAction(() => completed = false, State.InitialState)
                .Else
                    .TransitionTo(State.State2)
                    .WithAction(() => completed = true, State.InitialState);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State2);
            completed.Should().BeTrue();
        }

        /// <summary>
        /// Test that the Else.If works to define and drive a second state transition and action
        /// </summary>
        [Fact]
        public void TestConditionalTransitionSecondIfWithAction()
        {
            bool completed = false;

            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1)
                .Else.If(() => true)
                    .TransitionTo(State.State2)
                    .WithAction(() => completed = true, State.InitialState);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.TriggerEvent(Event.Event1);

            _stateMachine.Should().BeInState(State.State2);
            completed.Should().BeTrue();
        }

        /// <summary>
        /// Test that failing a match after multiple else.if statements throws the proper exception
        /// </summary>
        [Fact]
        public void TestConditionNoMatch()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => false)
                    .TransitionTo(State.State1)
                .Else.If(() => false)
                    .TransitionTo(State.State2)
                .Else.If(() => false)
                    .TransitionTo(State.State3);

            _stateMachine.Should().NotAcceptEvent(Event.Event1);
            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<ConditionFailedException>();
        }

        /// <summary>
        /// Test that adding the same transition twice (without conditional .If) throws an exception
        /// </summary>
        [Fact]
        public void TestAddingSameTransitionTwice()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1).TransitionTo(State.State1);

            _stateMachine.Invoking(sm => sm.WhenStateIs(State.InitialState).AndEventIs(Event.Event1).TransitionTo(State.State2))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        /// <summary>
        /// Test that throwing an exception during the state transition propogates the exception and keeps the initial state
        /// </summary>
        [Fact]
        public void TestActionWithError()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .TransitionTo(State.State1)
                .WithAction(() => throw new ExpectedException());

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<ExpectedException>();

            _stateMachine.Should().BeInState(State.InitialState);
        }

        /// <summary>
        /// Test that when an error state is not explicitly passed, the default is the transition's from state
        /// </summary>
        [Fact]
        public void NonNullableErrorState()
        {
            var transition = _stateMachine
                .WhenStateIs(State.State2).AndEventIs(Event.Event2)
                .TransitionTo(State.State3);

            transition.ActionErrorState.Should().NotBe(null);
            transition.ActionErrorState.Should().Be(State.State2);
        }

        /// <summary>
        /// Test that throwing an exception during the state transition drives to the 'error' state and propogates the exception
        /// </summary>
        [Fact]
        public void TestActionWithErrorAndExplicitErrorState()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .TransitionTo(State.State1)
                .WithAction(() => throw new ExpectedException(), State.Error);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<ExpectedException>();

            _stateMachine.Should().BeInState(State.Error);
        }


        /// <summary>
        /// Test that throwing an exception during the state transition propogates the exception and keeps the initial state
        /// </summary>
        [Fact]
        public void TestConditionalActionWithError()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => true)
                    .TransitionTo(State.State1)
                    .WithAction(() => throw new ExpectedException());

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<ExpectedException>();

            _stateMachine.Should().BeInState(State.InitialState);
        }

        /// <summary>
        /// Test that when an error state is not explicitly passed, the default is the transition's from state
        /// </summary>
        [Fact]
        public void NonNullableConditionalErrorState()
        {
            var transition = _stateMachine
                .WhenStateIs(State.State2).AndEventIs(Event.Event2)
                .If(() => true)
                    .TransitionTo(State.State3);

            transition.ActionErrorState.Should().NotBe(null);
            transition.ActionErrorState.Should().Be(State.State2);
        }

        /// <summary>
        /// Test that throwing an exception during the state transition drives to the 'error' state and propogates the exception
        /// </summary>
        [Fact]
        public void TestConditionalActionWithErrorAndExplicitErrorState()
        {
            _stateMachine.WhenStateIs(State.InitialState).AndEventIs(Event.Event1)
                .If(() => true)
                    .TransitionTo(State.State1)
                    .WithAction(() => throw new ExpectedException(), State.Error);

            _stateMachine.Should().AcceptEvent(Event.Event1);

            _stateMachine.Invoking(sm => sm.TriggerEvent(Event.Event1))
                .Should().ThrowExactly<ExpectedException>();

            _stateMachine.Should().BeInState(State.Error);
        }

        /// <summary>
        /// If not using Enums for states and events, then you cannot use the 'short form' constructor -- should throw an exception
        /// </summary>
        [Fact]
        public void TestInvalidConstructor()
        {
            Action act = () => new FiniteStateMachine<int, string>(0);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        /// <summary>
        /// If only one of the type parameters is an enum, should still throw an exception as above
        /// </summary>
        [Fact]
        public void TestInvalidConstructor2()
        {
            Action act = () => new FiniteStateMachine<System.DayOfWeek, string>(0);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        /// <summary>
        /// If only one of the type parameters is an enum, should still throw an exception as above
        /// </summary>
        [Fact]
        public void TestInvalidConstructor3()
        {
            Action act = () => new FiniteStateMachine<int, System.DayOfWeek>(0);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        /// <summary>
        /// Pass an invalid state
        /// </summary>
        [Fact]
        public void TestInvalidInitialState()
        {
            Action act = () => new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 7);

            act.Should().ThrowExactly<ArgumentException>();
        }

        /// <summary>
        /// Pass an invalid trigger
        /// </summary>
        [Fact]
        public void TestInvalidEvent()
        {
            var stateMachine = new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 1);

            stateMachine.Invoking(sm => sm.IsEventAccepted(9, out _))
                .Should().ThrowExactly<ArgumentException>("because 9 is not a valid event");

            stateMachine.Invoking(sm => sm.TriggerEvent(9))
                .Should().ThrowExactly<ArgumentException>("because 9 is not a valid event");
        }

        /// <summary>
        /// Pass state or trigger that are invalid
        /// </summary>
        [Fact]
        public void TestInvalidWhenEventClause()
        {
            var stateMachine = new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 1);

            stateMachine.Invoking(sm => sm.WhenStateIs(2).AndEventIs(9))
                .Should().ThrowExactly<ArgumentException>("because 9 is not a valid event");

            Action act = () => new FiniteStateMachine<int, int>.WhenEventClause(9, 4, stateMachine);
            act.Should().ThrowExactly<ArgumentException>("because 9 is not a valid state");
        }

        /// <summary>
        /// Pass an invalid trigger
        /// </summary>
        [Fact]
        public void TestInvalidTransitionToState()
        {
            var stateMachine = new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 1);

            stateMachine.Invoking(sm => sm.WhenStateIs(1).AndEventIs(4).TransitionTo(9))
                .Should().ThrowExactly<ArgumentException>("because 9 is not a valid state");
        }

        /// <summary>
        /// Pass state or trigger that are invalid
        /// </summary>
        [Fact]
        public void TestInvalidWhenEventIfClause()
        {
            var stateMachine = new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 1);

            Action act = () => new FiniteStateMachine<int, int>.WhenEventIfClause(9, 4, () => true, stateMachine);
            act.Should().ThrowExactly<ArgumentException>("because 9 is not a valid state");

            act = () => new FiniteStateMachine<int, int>.WhenEventIfClause(1, 9, () => true, stateMachine);
            act.Should().ThrowExactly<ArgumentException>("because 9 is not a valid event");
        }

        /// <summary>
        /// Pass an invalid trigger
        /// </summary>
        [Fact]
        public void TestInvalidConditionalTransitionToState()
        {
            var stateMachine = new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 1);

            stateMachine.Invoking(sm => sm.WhenStateIs(1).AndEventIs(4).If(() => true).TransitionTo(9))
                .Should().ThrowExactly<ArgumentException>("because 9 is not a valid state");
        }

        /// <summary>
        /// Pass state or trigger that are invalid
        /// </summary>
        [Fact]
        public void TestInvalidTransitionConstructor()
        {
            var stateMachine = new FiniteStateMachine<int, int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, 1);

            Action act = () => new FiniteStateMachine<int, int>.Transition(9, 4, 2, stateMachine);
            act.Should().ThrowExactly<ArgumentException>("because 9 is not a valid state");

            act = () => new FiniteStateMachine<int, int>.Transition(1, 9, 2, stateMachine);
            act.Should().ThrowExactly<ArgumentException>("because 9 is not a valid event");

            act = () => new FiniteStateMachine<int, int>.Transition(1, 4, 9, stateMachine);
            act.Should().ThrowExactly<ArgumentException>("because 9 is not a valid state");
        }


        /// <summary>
        /// When not using enums for states and event types, then you have to pass in the valid values.  Should construct properly without an exception
        /// </summary>
        [Fact]
        public void TestValidConstructor()
        {
            var stateMachine = new FiniteStateMachine<int, string>(new[] { 0, 1, 2, 3 }, new[] { "Event1", "Event2", "Event3" }, 0);
        }

        /// <summary>
        /// Test a simple transition not using enums -- should work the same, the only difference is in the constructor
        /// </summary>
        [Fact]
        public void TestSimpleTransitionNonEnum()
        {
            var stateMachine = new FiniteStateMachine<int, string>(new[] { 0, 1, 2, 3 }, new[] { "Event1", "Event2", "Event3" }, 0);
            stateMachine.WhenStateIs(0).AndEventIs("Event1")
                .TransitionTo(1);

            stateMachine.Should().BeInState(0);

            stateMachine.TriggerEvent("Event1");

            stateMachine.Should().BeInState(1);
        }
    }


    [Serializable]
    public class ExpectedException : Exception
    {
        public ExpectedException() { }
        public ExpectedException(string message) : base(message) { }
        public ExpectedException(string message, Exception inner) : base(message, inner) { }
        protected ExpectedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public static class StateMachineExtensions
    {
        public static StateMachineAssertions<TState, TEvent> Should<TState, TEvent>(this IStateful<TState, TEvent> instance)
            where TState : notnull
            where TEvent : notnull
        {
            return new Tests.StateMachineAssertions<TState, TEvent>(instance);
        }
    }

    public class StateMachineAssertions<TState, TEvent>
        : ReferenceTypeAssertions<IStateful<TState, TEvent>, StateMachineAssertions<TState, TEvent>>
            where TState : notnull
            where TEvent : notnull
    {
        public StateMachineAssertions(IStateful<TState, TEvent> subject)
            : base(subject)
        { }

        protected override string Identifier => "State Machine";

        public AndConstraint<StateMachineAssertions<TState, TEvent>> AcceptEvent(TEvent eventToAccept, string because = "", params object[] becauseArgs)
        {
            if (Subject == null)
                throw new NullReferenceException("Subject should not be null");

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.IsEventAccepted(eventToAccept, out _))
                .FailWith("Expected {context} to accept {0}{reason}, but it does not", eventToAccept);

            return new AndConstraint<StateMachineAssertions<TState, TEvent>>(this);
        }

        public AndConstraint<StateMachineAssertions<TState, TEvent>> NotAcceptEvent(TEvent eventToAccept, string because = "", params object[] becauseArgs)
        {
            if (Subject == null)
                throw new NullReferenceException("Subject should not be null");

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!Subject.IsEventAccepted(eventToAccept, out _))
                .FailWith("Expected {context} to not accept {0}{reason}, but it does", eventToAccept);

            return new AndConstraint<StateMachineAssertions<TState, TEvent>>(this);
        }

        public AndConstraint<StateMachineAssertions<TState, TEvent>> BeInState(TState state, string because = "", params object[] becauseArgs)
        {
            if (Subject == null)
                throw new NullReferenceException("Subject should not be null");

            if (Subject.State == null)
                throw new NullReferenceException("Subject.State should not be null");

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.State.Equals(state))
                .FailWith("Expected {context} to be in state {0}{reason}, but it is actually in state {1}", state, Subject.State);

            return new AndConstraint<StateMachineAssertions<TState, TEvent>>(this);
        }

        public AndConstraint<StateMachineAssertions<TState, TEvent>> NotBeInState(TState state, string because = "", params object[] becauseArgs)
        {
            if (Subject == null)
                throw new NullReferenceException("Subject should not be null");

            if (Subject.State == null)
                throw new NullReferenceException("Subject.State should not be null");

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!Subject.State.Equals(state))
                .FailWith("Expected {context} to not be in state {0}{reason}, but it is", state);

            return new AndConstraint<StateMachineAssertions<TState, TEvent>>(this);
        }
    }
}
