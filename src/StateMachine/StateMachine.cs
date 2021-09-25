using System;
using System.Collections.Generic;

namespace StateMachine
{
    /// <summary>
    /// My interpretation of a Finite State Machine, mostly meant to be used to managing the state of a business object
    /// </summary>
    public static class StateMachine
    {
        /// <summary>
        /// Create a builder, which can be used to define a Finite State Machine
        /// </summary>
        /// <typeparam name="TState">The type used to store the state</typeparam>
        /// <returns></returns>
        public static IStateMachineBuilder<TState> CreateBuilder<TState>() where TState : notnull
        {
            throw new NotImplementedException();
        }
    }

    internal class StateMachine<TState> : IStateMachine<TState> where TState : notnull
    {
        private TState _state;
        public TState State => _state;

        private List<TState> _states = new();
        public IReadOnlyList<TState> States => _states;

        private List<IEvent<TState, Delegate>> _events = new();

        public IReadOnlyList<IEvent<Delegate>> Events => _events;

        public StateMachine(TState initialState)
        {
            _state = State;
        }

    }



    internal class Transition
    {
        public void Fire()
        {

        }
    }

    internal class TransitionWithAction
    {
        public void Fire()
        {

        }
    }

    internal class TransitionWithAction<TArg>
    {
        public void Fire(TArg arg)
        {

        }
    }


    internal class TransitionBuilder<TState>
        where TState : notnull
    {
        public IList<TState> States { get; } = new List<TState>();

        public Transition Transition { get; init; } = new();
    }


}
