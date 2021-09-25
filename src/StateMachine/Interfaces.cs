using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StateMachine
{
    public interface IStateful<TState> : IStateMachine<TState> where TState : notnull
    {

    }

    public interface IStateMachine<TState> where TState : notnull
    {
        public TState State { get; }

        public IReadOnlyList<TState> States { get; }

        public IReadOnlyList<IEvent> Events { get; }

        public interface IEvent
        {
            public string Name { get; }
            public Delegate Trigger { get; }
        }

        public interface IParameterlessEvent : IEvent
        {
            public new StateMachineTrigger Trigger { get; }

            public bool IsAccepted { get; }
            public bool TryAccept(out TState? nextState, out string reason);

            public TState NextState { get; }
        }

        public interface IParameterizedEvent<TArg> : IEvent
        {
            public new StateMachineTrigger<TArg> Trigger { get; }

            public bool IsAccepted(TArg eventArgument);
            public TState NextState(TArg eventArgument);
        }

        /*Questions to answer:
         * 1. Which events can currently be fired, and which states will they transition to
         * 2. Wich states can be accessed, and which events / guards are required to get there
         * 3. If an event cannot be triggered, why not (not defined for the current state, or guard failing)
         * 4. If a state cannot be accessed, why not (no transition defined, or guards failing)
         * 5. All states and events, regardless of current state, as to be able to map out the full workflow
         */
        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger(StateMachineTrigger stateMachineTrigger);
        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger<TArg>(StateMachineTrigger<TArg> stateMachineTrigger, TArg argument);
        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger<TArg>(StateMachineAsyncTrigger<TArg> stateMachineTrigger, TArg argument);
    }


    /// <summary>
    /// Used to initiate an event on the state machine
    /// </summary>
    public delegate void StateMachineTrigger();
    /// <summary>
    /// Used to initiate an event on the state machine
    /// </summary>
    /// <typeparam name="TArg">The type of argument to be passed to the action and associated guards</typeparam>
    /// <param name="eventArgument">Argument passed to the action and associated guards</param>
    public delegate void StateMachineTrigger<TArg>(TArg eventArgument);
    public delegate Task StateMachineAsyncTrigger<TArg>(TArg eventArgument);

    /// <summary>
    /// An action to perform during a state transition
    /// </summary>
    public delegate void StateMachineAction();
    /// <summary>
    /// An action to perform during a state transition
    /// </summary>
    /// <typeparam name="TArg">The type of argument passed from the trigger</typeparam>
    /// <param name="eventArgument">Argument passed from the trigger</param>
    public delegate void StateMachineAction<TArg>(TArg eventArgument);
    public delegate Task StateMachineAsyncAction<TArg>(TArg eventArgument);


    public delegate bool StateMachineGuard();
    public delegate bool StateMachineGuard<TArg>(TArg eventArgument);


    //These interfaces are used to create the fluent syntax for building a state machine.
    //The builder pattern is utilized to simplify construction of a complex object
    #region FluentInterfaces
    public interface IStateMachineBuilder<TState> where TState : notnull
    {
        IStateMachine<TState> Build(TState initialState);


        IEventBuilder<StateMachineTrigger, StateMachineGuard> DefineTrigger(out StateMachineTrigger trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<StateMachineTrigger, StateMachineGuard> DefineEvent(out IStateMachine<TState>.IParameterlessEvent trigger, [CallerArgumentExpression("trigger")] string? name = null);

        IEventBuilder<StateMachineTrigger<TArg>, StateMachineGuard<TArg>> DefineTrigger<TArg>(out StateMachineTrigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<StateMachineTrigger<TArg>, StateMachineGuard<TArg>> DefineEvent<TArg>(out IStateMachine<TState>.IParameterizedEvent<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);

        IEventBuilder<StateMachineAsyncTrigger<TArg>, StateMachineGuard<TArg>> DefineAsyncTrigger<TArg>(out StateMachineAsyncTrigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<StateMachineAsyncTrigger<TArg>, StateMachineGuard<TArg>> DefineAsyncEvent<TArg>(out IStateMachine<TState>.IParameterizedEvent<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);


        public interface IEventBuilder<TAction, TGuard>
            where TAction : System.Delegate
            where TGuard : System.Delegate
        {
            IStateClause WhenStateIs(TState state);
            IStateClause WhenStateIs(params TState[] state);

            public interface IStateClause
            {
                ITransitionClause TransitionTo(TState state);
                IExecuteClause Execute(TAction action);

                IIfClause If(TGuard guard, string reason);
            }

            public interface ITransitionClause : IEventBuilder<TAction, TGuard>, IStateMachineBuilder<TState>
            {
            }

            public interface IExecuteClause
            {
                ITransitionClause ThenTransitionTo(TState state);
                ITransitionClause AndKeepSameState();
            }

            public interface IIfClause
            {
                ITransitionClause TransitionTo(TState state);
                IIfExecuteClause Execute(TAction action);
            }

            public interface IIfTransitionClause : IEventBuilder<TAction, TGuard>, IStateMachineBuilder<TState>
            {
                public IStateClause Else { get; }
            }

            public interface IIfExecuteClause
            {
                IIfTransitionClause ThenTransitionTo(TState state);
                IIfTransitionClause AndKeepSameState();
            }

        }
    }
    #endregion
}
