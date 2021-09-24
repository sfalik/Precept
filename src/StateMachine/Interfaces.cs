using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StateMachine
{
    public interface IStateful<TState> where TState : notnull
    {
        public TState State { get; }

        public IReadOnlyList<TState> States { get; }

        public IReadOnlyList<IEvent> Events { get; }

        public interface IEvent
        {
            public string Name { get; }
            public Delegate Trigger { get; }
        }

        public interface IEvent2 : IEvent
        {
            public new StateMachineTrigger Trigger { get; }
        }

        public interface IEvent2<TArg> : IEvent
        {
            public new StateMachineTrigger<TArg> Trigger { get; }
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
    }

    public interface IStateMachine<TState> : IStateful<TState> where TState : notnull
    {
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

    //These interfaces are used to create the fluent syntax for building a state machine.
    #region FluentInterfaces
    public interface IStateMachineBuilder<TState> where TState : notnull
    {
        IStateMachine<TState> Build(TState initialState);

        IEventBuilder DefineTrigger(out StateMachineTrigger trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder DefineEvent(out IStateful<TState>.IEvent2 trigger, [CallerArgumentExpression("trigger")] string? name = null);

        IEventBuilder<TArg> DefineTrigger<TArg>(out StateMachineTrigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TArg> DefineEvent<TArg>(out IStateful<TState>.IEvent2<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);

        public interface IEventBuilder
        {
            IStateClause IfStateIs(TState state);
            IStateClause IfStateIs(params TState[] state);

            public delegate bool StateMachineGuard();

            public interface IStateClause
            {
                ITransitionClause TransitionTo(TState state);
                IExecuteClause Execute(StateMachineAction action);

                IIfClause If(StateMachineGuard guard, string reason);
            }

            public interface IIfClause
            {
                ITransitionClause TransitionTo(TState state);
                IGuardedExecuteClause Execute(StateMachineAction action);
            }

            public interface IExecuteClause
            {
                ITransitionClause ThenTransitionTo(TState state);
                ITransitionClause AndKeepSameState();
            }

            public interface IGuardedExecuteClause
            {
                IGuardedTransitionClause ThenTransitionTo(TState state);
                IGuardedTransitionClause AndKeepSameState();
            }

            public interface ITransitionClause : IStateMachineBuilder<TState>
            {
                public IEventBuilder Or { get; }
            }

            public interface IGuardedTransitionClause : IStateMachineBuilder<TState>
            {
                public IStateClause Else { get; }
                public IEventBuilder Or { get; }
            }
        }


        public interface IEventBuilder<TArg>
        {
            IStateClause IfStateIs(TState state);
            IStateClause IfStateIs(params TState[] state);

            public delegate bool StateMachineGuard(TArg eventArgument);

            public interface IStateClause
            {
                ITransitionClause TransitionTo(TState state);
                IExecuteClause Execute(StateMachineAction<TArg> action);

                IIfClause If(StateMachineGuard guard, string reason);
            }

            public interface IIfClause
            {
                IGuardedExecuteClause Execute(StateMachineAction<TArg> action);
            }

            public interface IExecuteClause
            {
                ITransitionClause ThenTransitionTo(TState state);
                ITransitionClause AndKeepSameState();
            }

            public interface IGuardedExecuteClause
            {
                IGuardedTransitionClause ThenTransitionTo(TState state);
                IGuardedTransitionClause AndKeepSameState();
            }
            public interface ITransitionClause : IStateMachineBuilder<TState>
            {
                public IEventBuilder Or { get; }
            }

            public interface IGuardedTransitionClause : IStateMachineBuilder<TState>
            {
                public IStateClause Else { get; }
                public IEventBuilder Or { get; }
            }
        }
    }
    #endregion
}
