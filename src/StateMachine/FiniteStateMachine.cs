using System;
using System.Diagnostics;
using System.Reflection;

namespace StateMachine
{
    /// <summary>
    /// An simple implemenation of a Finite State Machine (though made more complex with the addition of a Fluent API, the 
    /// underlying implementation is pretty simple and traditional.  The state machine is deterministic, in that the outcome
    /// of an event can be determined ahead of time.  States and Events cannot be added after the state machine is
    /// instantiated -- this allows a light-weight two-dimensinal array to be used for the state transition table
    /// </summary>
    /// <typeparam name="TState">Type of State</typeparam>
    /// <typeparam name="TEvent">Type of Event Triggers</typeparam>
    [DebuggerDisplay("{State}")]
    public class FiniteStateMachine<TState, TEvent> : IStateful<TState, TEvent>
        where TState : notnull
        where TEvent : notnull
    {
        public TState[] States { get; }
        public TState State { get; private set; }
        public TEvent[] Events { get; }
        private Transition[,] TransitionTable { get; }

        public FiniteStateMachine(TState[] states, TEvent[] events, TState initialState)
        {
            States = states;
            Events = events;
            TransitionTable = new Transition[States.Length, Events.Length];

            int s = Array.IndexOf(States, initialState);
            if (s == -1)
                throw new ArgumentException($"State '{initialState}' is not a valid state", nameof(initialState));
            State = initialState;
        }

        public FiniteStateMachine(TState initialState)
        {
            if (!typeof(TState).GetTypeInfo().IsEnum)
                throw new InvalidOperationException($"{nameof(TState)} must be an emum type)");
            if (!typeof(TEvent).GetTypeInfo().IsEnum)
                throw new InvalidOperationException($"{nameof(TEvent)} must be an emum type)");

            States = (TState[])Enum.GetValues(typeof(TState));
            Events = (TEvent[])Enum.GetValues(typeof(TEvent));
            TransitionTable = new Transition[States.Length, Events.Length];

            State = initialState;
        }

        /// <summary>
        /// Initiate the creation of a state transition by indicating the state to transition from
        /// </summary>
        /// <param name="state">state to transition from</param>
        public WhenClause WhenStateIs(TState state)
        {
            return new WhenClause(state, this);
        }

        /// <summary>
        /// Determine if the Event defines a valid state transition at the current point in time.  The state machine is 
        /// deterministic in that all state transitions can be predetermined before they are initiated -- however there
        /// is a slight chance that any conditional transitions might not evaluate consistently between this call and a 
        /// call to <see cref="TriggerEvent(TEvent)"/>
        /// </summary>
        /// <param name="trigger">The event to check</param>
        public bool IsEventAccepted(TEvent trigger, out TState newState)
        {

            int s = Array.IndexOf(States, State);
            int e = Array.IndexOf(Events, trigger);
            if (e == -1)
                throw new ArgumentException($"Event {trigger} is not a valid trigger", nameof(trigger));

            var transition = TransitionTable[s, e];
            if (transition == null)
            {
                newState = State;
                return false;
            }

            return transition.Validate(out newState);
        }

        public TState TriggerEvent(TEvent trigger)
        {
            int s = Array.IndexOf(States, State);
            int e = Array.IndexOf(Events, trigger);
            if (e == -1)
                throw new ArgumentException($"Event {trigger} is not a valid trigger", nameof(trigger));

            var transition = TransitionTable[s, e];
            if (transition == null)
                throw new InvalidTransitionException($"Transition for Event '{trigger}' while in State '{State}' is not defined");

            transition.Perform();

            return State;
        }

        public class WhenClause
        {
            protected TState _state;
            protected FiniteStateMachine<TState, TEvent> _stateMachine;
            public WhenClause(TState state, FiniteStateMachine<TState, TEvent> stateMachine)
            {
                int s = Array.IndexOf(stateMachine.States, state);
                if (s == -1) throw new ArgumentException($"State '{state}' is not a valid state", nameof(state));

                _state = state;
                _stateMachine = stateMachine;
            }

            /// <summary>
            /// Define an event to initiate this state transition
            /// </summary>
            /// <param name="trigger">The event that triggers the state transition</param>
            public WhenEventClause AndEventIs(TEvent trigger)
            {
                return new WhenEventClause(_state, trigger, _stateMachine);
            }
        }

        public class WhenEventClause
        {
            protected TEvent _trigger;
            protected TState _state;
            protected FiniteStateMachine<TState, TEvent> _stateMachine;
            public WhenEventClause(TState state, TEvent trigger, FiniteStateMachine<TState, TEvent> stateMachine)
            {
                int s = Array.IndexOf(stateMachine.States, state);
                if (s == -1)
                    throw new ArgumentException($"State '{state}' is not a valid state", nameof(state));

                int e = Array.IndexOf(stateMachine.Events, trigger);
                if (e == -1)
                    throw new ArgumentException($"Event '{trigger}' is not a valid trigger", nameof(trigger));

                _state = state;
                _trigger = trigger;
                _stateMachine = stateMachine;
            }

            /// <summary>
            /// Define the state to transition to.  All state transitions must be deterministic and declared ahead of time
            /// </summary>
            /// <param name="state">The new state set at the end of this state transtion</param>
            public Transition TransitionTo(TState state)
            {
                int s = Array.IndexOf(_stateMachine.States, state);
                if (s == -1)
                    throw new ArgumentException($"State '{state}' is not a valid state", nameof(state));

                return new Transition(_state, _trigger, state, _stateMachine);
            }

            /// <summary>
            /// Allow the event without changing the state
            /// </summary>
            public Transition KeepCurrentState()
            {
                return new Transition(_state, _trigger, _state, _stateMachine);
            }

            /// <summary>
            /// Make this state transition conditional
            /// </summary>
            /// <param name="condition">Callback to evaluate if the condition is satisifed</param>
            /// <returns></returns>
            public WhenEventIfClause If(Func<bool> condition)
            {
                return new WhenEventIfClause(_state, _trigger, condition, _stateMachine);
            }
        }

        public class WhenEventIfClause
        {
            protected TEvent _trigger;
            protected TState _state;
            protected Func<bool> _condition;
            protected FiniteStateMachine<TState, TEvent> _stateMachine;
            public WhenEventIfClause(TState state, TEvent trigger, Func<bool> condition, FiniteStateMachine<TState, TEvent> stateMachine)
            {
                int s = Array.IndexOf(stateMachine.States, state);
                if (s == -1)
                    throw new ArgumentException($"State '{state}' is not a valid state", nameof(state));

                int e = Array.IndexOf(stateMachine.Events, trigger);
                if (e == -1)
                    throw new ArgumentException($"Event '{trigger}' is not a valid trigger", nameof(trigger));

                _state = state;
                _trigger = trigger;
                _condition = condition;
                _stateMachine = stateMachine;
            }

            /// <summary>
            /// Define the state to transition to.  All state transitions must be deterministic and declared ahead of time
            /// </summary>
            /// <param name="state">The new state set at the end of this state transtion</param>
            public ConditionalTransition TransitionTo(TState state)
            {
                int s = Array.IndexOf(_stateMachine.States, state);
                if (s == -1)
                    throw new ArgumentException($"State '{state}' is not a valid state", nameof(state));

                return new ConditionalTransition(_state, _trigger, _condition, state, _stateMachine);
            }

            /// <summary>
            /// Allow the event without changing the state
            /// </summary>
            public ConditionalTransition KeepCurrentState()
            {
                return new ConditionalTransition(_state, _trigger, _condition, _state, _stateMachine);
            }

        }

        /// <summary>
        /// Represents a state transition and it's associated logic
        /// </summary>
        public class Transition
        {
            public TEvent Trigger { get; private set; }
            public TState FromState { get; private set; }
            public TState ToState { get; private set; }
            public Action? Action { get; protected set; }
            public TState ActionErrorState { get; protected set; }
            public FiniteStateMachine<TState, TEvent> StateMachine { get; private set; }

            protected Transition? _nextTransition;

            public Transition(TState fromState, TEvent trigger, TState toState, FiniteStateMachine<TState, TEvent> stateMachine)
            {
                int s = Array.IndexOf(stateMachine.States, fromState);
                if (s == -1)
                    throw new ArgumentException($"State '{fromState}' is not a valid state", nameof(fromState));

                int e = Array.IndexOf(stateMachine.Events, trigger);
                if (e == -1)
                    throw new ArgumentException($"Event '{trigger}' is not a valid trigger", nameof(trigger));

                int s2 = Array.IndexOf(stateMachine.States, toState);
                if (s2 == -1)
                    throw new ArgumentException($"State '{toState}' is not a valid state", nameof(toState));

                FromState = fromState;
                ActionErrorState = fromState; //Default is to keep the same state on transition errors
                Trigger = trigger;
                ToState = toState;
                StateMachine = stateMachine;

                if (StateMachine.TransitionTable[s, e] == null)
                {
                    StateMachine.TransitionTable[s, e] = this;
                }
                else if (StateMachine.TransitionTable[s, e] is ConditionalTransition existing)
                {
                    //add this instance to the end of the linked list
                    while (existing._nextTransition != null)
                        existing = existing._nextTransition as ConditionalTransition;

                    existing._nextTransition = this;
                }
                else
                {
                    throw new InvalidOperationException($"Transition from State '{fromState}' for Event '{trigger}' already defined");
                }
            }

            public virtual bool Validate(out TState newState)
            {
                //Not much to do here, but this is overridden in the condition version with more complex logic to walk the chain of 'if' statements
                newState = ToState;
                return true;
            }

            /// <summary>
            /// Initiate the state transition and it's associated logic
            /// </summary>
            public virtual void Perform()
            {
                if (Action != null)
                {
                    try
                    {
                        //attempt to perform the transition
                        Action();
                    }
                    catch
                    {
                        //if there is an exception, set the state accordingly and rethrow
                        StateMachine.State = ActionErrorState;
                        throw;
                    }
                }
                StateMachine.State = ToState;
            }

            /// <summary>
            /// Add an action to be performed along with the state transition.  
            /// <para>It is important to consider what to do if the
            /// Action encounters and exception.  Handle the exception within the action if possible, otherwise the exception
            /// is propogated back to the caller that initates the state transition.</para>
            /// <para>The state machine can either remain in the current state by passing it to <paramref name="errorState"/> 
            /// otherwise, it might make sense to define a specific error state for the state machine with specific transitions
            /// to recover from the error</para>
            /// </summary>
            /// <param name="action">Action to be performed</param>
            /// <param name="errorState">State to transition to in the event of an exception executing <paramref name="action"/></param>
            public void WithAction(Action action, TState errorState)
            {
                Action = action;
                ActionErrorState = errorState;
            }

            /// <summary>
            /// Add an action to be performed along with the state transition.  
            /// <para>It is important to consider what to do if the
            /// Action encounters and exception.  Handle the exception within the action if possible, otherwise the exception
            /// is propogated back to the caller that initates the state transition.</para>
            /// </summary>
            /// <param name="action">Action to be performed</param>
            /// <param name="errorState">State to transition to in the event of an exception executing <paramref name="action"/></param>
            public void WithAction(Action action)
            {
                Action = action;
            }
        }

        /// <summary>
        /// Represents a conditional state transition and it's associated logic
        /// </summary>
        public class ConditionalTransition : Transition
        {
            public Transition? NextTransition => _nextTransition;
            public Func<bool> Condition { get; private set; }
            public ConditionalTransition(TState state, TEvent trigger, Func<bool> condition, TState newState, FiniteStateMachine<TState, TEvent> stateMachine)
                : base(state, trigger, newState, stateMachine)
            {
                Condition = condition;
            }

            public override bool Validate(out TState newState)
            {
                //Evaluate the condition -- if satisifed, return true
                if (Condition())
                {
                    newState = ToState;
                    return true;
                }
                //otherwise, walk out to the end of the linked list -- first condition to be satisifed returns true
                else if (NextTransition != null)
                {
                    return NextTransition.Validate(out newState);
                }
                //we are at the end of the chain and no conditions were satisifed, return false
                else
                {
                    newState = FromState;
                    return false;
                }
            }

            /// <summary>
            /// Initiate the state transition and it's associated logic
            /// </summary>
            public override void Perform()
            {
                if (Condition())
                {
                    base.Perform();
                }
                else if (NextTransition != null)
                {
                    NextTransition.Perform();
                }
                else
                {
                    throw new ConditionFailedException();
                }
            }

            /// <summary>
            /// Add an action to be performed along with the state transition.  
            /// <para>It is important to consider what to do if the
            /// Action encounters and exception.  Handle the exception within the action if possible, otherwise the exception
            /// is propogated back to the caller that initates the state transition.</para>
            /// <para>The state machine can either remain in the current state by passing it to <paramref name="errorState"/> 
            /// otherwise, it might make sense to define a specific error state for the state machine with specific transitions
            /// to recover from the error</para>
            /// </summary>
            /// <param name="action">Action to be performed</param>
            /// <param name="errorState">State to transition to in the event of an exception executing <paramref name="action"/></param>
            public new ConditionalTransitionWithAction WithAction(Action action, TState errorState)
            {
                Action = action;
                ActionErrorState = errorState;
                return new ConditionalTransitionWithAction(this);
            }

            /// <summary>
            /// Add an action to be performed along with the state transition.  
            /// <para>It is important to consider what to do if the
            /// Action encounters and exception.  Handle the exception within the action if possible, otherwise the exception
            /// is propogated back to the caller that initates the state transition.</para>
            /// </summary>
            /// <param name="action">Action to be performed</param>
            /// <param name="errorState">State to transition to in the event of an exception executing <paramref name="action"/></param>
            public new ConditionalTransitionWithAction WithAction(Action action)
            {
                Action = action;
                return new ConditionalTransitionWithAction(this);
            }

            /// <summary>
            /// Define another state transition, which can alse be made conditional by calling <see cref="Else"/>.If()
            /// </summary>
            public WhenEventClause Else
            { get { return new WhenEventClause(FromState, Trigger, StateMachine); } }
        }

        public class ConditionalTransitionWithAction
        {
            private ConditionalTransition _transition;

            public ConditionalTransitionWithAction(ConditionalTransition transition)
            {
                _transition = transition;
            }

            /// <summary>
            /// Define another state transition, which can alse be made conditional by calling <see cref="Else"/>.If()
            /// </summary>
            public WhenEventClause Else
            { get { return new WhenEventClause(_transition.FromState, _transition.Trigger, _transition.StateMachine); } }
        }
    }
}
