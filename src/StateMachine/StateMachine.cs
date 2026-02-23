using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StateMachine
{
    /// <summary>
    /// My interpretation of a Finite State Machine, mostly meant to be used to manage the state of a business object
    /// </summary>
    public static class StateMachine
    {
        /// <summary>
        /// Create a builder, which can be used to define a Finite State Machine
        /// </summary>
        /// <typeparam name="TState">The type used to store the state</typeparam>
        /// <returns></returns>
        public static IStateMachineBuilder<TState> CreateBuilder<TState>() where TState : notnull, System.Enum
        {
            return new StateMachineBuilder<TState>();
        }
    }

    internal class StateMachine<TState> : IStateMachine<TState> where TState : notnull, System.Enum
    {
        private readonly TState _state;
        public TState State => _state;

        private readonly List<TState> _states = new();
        public IReadOnlyList<TState> States => _states;

        private readonly List<IEvent> _events = new();

        public IReadOnlyList<IEvent> Events => _events;

        public StateMachine(TState initialState)
        {
            _state = initialState;
        }

    }

    internal class StateMachineBuilder<TState> : IStateMachineBuilder<TState>
        where TState : notnull, System.Enum
    {
        private readonly List<EventBuilder<TState, Delegate>> _events = new();

        public IStateMachine<TState> Build(TState initialState)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState, TransitionAction> DefineEvent(out IEvent<TState, Trigger> @event, [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState, AsyncTransitionAction> DefineAsyncEvent(out IEvent<TState, AsyncTrigger> @event, [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }
        public IEventBuilder<TState, TransitionAction<TArg>, TArg> DefineEvent<TArg>(out IEvent<TState, TArg, Trigger<TArg>> @event, [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState, AsyncTransitionAction<TArg>, TArg> DefineAsyncEvent<TArg>(out IEvent<TState, TArg, AsyncTrigger<TArg>> @event, [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }
    }

    internal class EventBuilder<TState, TAction> : IEventBuilder<TState, TAction>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        public IStateClause<TState, TAction> RegardlessOfState()
        {
            throw new NotImplementedException();
        }

        public IStateClause<TState, TAction> WhenStateIs(TState state)
        {
            throw new NotImplementedException();
        }

        public IStateClause<TState, TAction> WhenStateIs(params TState[] state)
        {
            throw new NotImplementedException();
        }
    }

    internal class EventBuilder<TState, TAction, TArg> : IEventBuilder<TState, TAction, TArg>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        public IStateClause<TState, TAction, TArg> WhenStateIs(TState state)
        {
            throw new NotImplementedException();
        }

        public IStateClause<TState, TAction, TArg> WhenStateIs(params TState[] state)
        {
            throw new NotImplementedException();
        }
    }
}
