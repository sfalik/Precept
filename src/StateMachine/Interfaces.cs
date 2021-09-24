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

        ITransitionEventClause<TState> DefineTrigger(out StateMachineTrigger trigger, [CallerArgumentExpression("trigger")] string? name = null);
        ITransitionEventClause<TState> DefineEvent(out IStateful<TState>.IEvent2 trigger, [CallerArgumentExpression("trigger")] string? name = null);

        ITransitionEventClause<TState, TArg> DefineTrigger<TArg>(out StateMachineTrigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        ITransitionEventClause<TState, TArg> DefineEvent<TArg>(out IStateful<TState>.IEvent2<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
    }

    public interface ITransitionEventClause<TState> where TState : notnull
    {
        ITransitionStateClause<TState> IfStateIs(TState state);
        ITransitionStateClause<TState> IfStateIs(params TState[] state);
    }

    public interface ITransitionEventClause<TState, TArg> where TState : notnull
    {
        ITransitionStateClause<TState, TArg> IfStateIs(TState state);
        ITransitionStateClause<TState, TArg> IfStateIs(params TState[] state);
    }

    public delegate bool StateMachineGuard();
    public delegate bool StateMachineGuard<TArg>(TArg eventArgument);

    public interface ITransitionStateClause<TState> where TState : notnull
    {
        ITransition<TState> TransitionTo(TState state);
        ITransitionExecuteClause<TState> Execute(StateMachineAction action);

        ITransitionIfClause<TState> If(StateMachineGuard guard, string reason);
    }
    public interface ITransitionStateClause<TState, TArg> where TState : notnull
    {
        ITransition<TState> TransitionTo(TState state);
        ITransitionExecuteClause<TState> Execute(StateMachineAction<TArg> action);

        ITransitionIfClause<TState, TArg> If(StateMachineGuard<TArg> guard, string reason);
    }

    public interface ITransitionIfClause<TState> where TState : notnull
    {
        ITransition<TState> TransitionTo(TState state);
        ITransitionGuardedExecuteClause<TState> Execute(StateMachineAction action);
    }
    public interface ITransitionIfClause<TState, TArg> where TState : notnull
    {
        ITransitionGuardedExecuteClause<TState, TArg> Execute(StateMachineAction<TArg> action);
    }

    public interface ITransitionExecuteClause<TState> where TState : notnull
    {
        ITransition<TState> ThenTransitionTo(TState state);
        ITransition<TState> AndKeepSameState();
    }

    public interface ITransitionGuardedExecuteClause<TState> where TState : notnull
    {
        IGuardedTransition<TState> ThenTransitionTo(TState state);
        IGuardedTransition<TState> AndKeepSameState();
    }
    public interface ITransitionGuardedExecuteClause<TState, TArg> where TState : notnull
    {
        IGuardedTransition<TState, TArg> ThenTransitionTo(TState state);
        IGuardedTransition<TState, TArg> AndKeepSameState();
    }

    public interface ITransition<TState> : IStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionEventClause<TState> Or { get; }
    }
    public interface ITransition<TState, TArg> : IStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionEventClause<TState, TArg> Or { get; }
    }

    public interface IGuardedTransition<TState> : IStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionStateClause<TState> Else { get; }
        public ITransitionEventClause<TState> Or { get; }
    }
    public interface IGuardedTransition<TState, TArg> : IStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionStateClause<TState, TArg> Else { get; }
        public ITransitionEventClause<TState, TArg> Or { get; }
    }
    #endregion
}
