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

        private List<IEvent> _events = new();
        public IReadOnlyList<IEvent> Events => _events;

        IReadOnlyList<IEvent> IStateMachine<TState>.Events => throw new NotImplementedException();

        public StateMachine(TState initialState)
        {
            _state = State;
        }

        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger(Trigger stateMachineTrigger)
        {
            throw new NotImplementedException();
        }

        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger<TArg>(Trigger<TArg> stateMachineTrigger, TArg argument)
        {
            throw new NotImplementedException();
        }

        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger<TArg>(AsyncTrigger<TArg> stateMachineTrigger, TArg argument)
        {
            throw new NotImplementedException();
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
