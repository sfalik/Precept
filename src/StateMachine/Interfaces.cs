using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StateMachine
{
    public interface IStateful<TState> : IStateMachine<TState> where TState : notnull, System.Enum
    {

    }

    public interface IStateMachine<TState> where TState : notnull, System.Enum
    {
        public TState State { get; }

        // not needed if we assume 
        public IReadOnlyList<TState> States { get; }

        public IReadOnlyList<IEvent> Events { get; }

        /*Questions to answer:
         * 1. Which events can currently be fired, and which states will they transition to
         * 2. Wich states can be accessed, and which events / guards are required to get there
         * 3. If an event cannot be triggered, why not (not defined for the current state, or guard failing)
         * 4. If a state cannot be accessed, why not (no transition defined, or guards failing)
         * 5. All states and events, regardless of current state, as to be able to map out the full workflow
         */
    }

    public delegate void Trigger();
    public delegate void Trigger<TArg>(TArg eventArgument);
    public delegate Task AsyncTrigger();
    public delegate Task AsyncTrigger<TArg>(TArg eventArgument);

    public delegate void TransitionAction();
    public delegate void TransitionAction<TArg>(TArg eventArgument);
    public delegate Task AsyncTransitionAction();
    public delegate Task AsyncTransitionAction<TArg>(TArg eventArgument);

    public delegate bool Guard<TArg>(TArg eventArgument);


    public interface IEvent
    {
        public string Name { get; }
    }
    public interface IEvent<out TTrigger> : IEvent
        where TTrigger : Delegate
    {
        public TTrigger Trigger { get; }
    }

    public interface IEvent<TState, out TTrigger> : IEvent<TTrigger>
        where TTrigger : Delegate where TState : notnull
    {
        (bool IsAccepted, TState newStateIfAccepted, string? reasonNotAccepted) Test();
        bool Test(out TState newStateIfAccepted, out string? reasonNotAccepted);
    }

    public interface IEvent<TState, TArg, out TTrigger> : IEvent<TTrigger>
        where TTrigger : Delegate where TState : notnull
    {
        (bool IsAccepted, TState newStateIfAccepted, string? reasonNotAccepted) Test(TArg eventArgument);
        bool Test(TArg eventArgument, out TState newStateIfAccepted, out string? reasonNotAccepted);
    }

    public interface IAttribute<T>
    {
        public T Value { get; set; }
    }

    //These interfaces are used to create the fluent syntax for building a state machine.
    //The builder pattern is utilized to simplify construction of this complex object
    #region FluentInterfaces
    public interface IStateMachineBuilder<TState> where TState : notnull, System.Enum
    {
        IStateMachine<TState> Build(TState initialState);

        //IStateMachineBuilder<TState> DefineAttribute<T>(ref T attribute);

        IEventBuilder<TState, TransitionAction> DefineEvent(out IEvent<TState, Trigger> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, AsyncTransitionAction> DefineAsyncEvent(out IEvent<TState, AsyncTrigger> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, TransitionAction<TArg>, TArg> DefineEvent<TArg>(out IEvent<TState, TArg, Trigger<TArg>> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, AsyncTransitionAction<TArg>, TArg> DefineAsyncEvent<TArg>(out IEvent<TState, TArg, AsyncTrigger<TArg>> @event, [CallerArgumentExpression("event")] string? name = null);
    }




    public interface Test<TLambda>
    {

    }

    #region SimpleEvents
    public interface IEventBuilder<TState, TAction>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        IStateClause<TState, TAction> WhenStateIs(TState state);
        IStateClause<TState, TAction> WhenStateIs(params TState[] state);
        IStateClause<TState, TAction> RegardlessOfState();
    }

    public interface IStateClause<TState, TAction>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction> TransitionTo(TState state);
        ITransitionClause<TState, TAction> KeepSameState();

        IExecuteClause<TState, TAction> Execute(TAction action);
    }

    public interface ITransitionClause<TState, TAction> : IEventBuilder<TState, TAction>, IStateMachineBuilder<TState>
    where TState : notnull, System.Enum
                where TAction : System.Delegate
    {
    }

    public interface IExecuteClause<TState, TAction>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction> ThenTransitionTo(TState state);
        ITransitionClause<TState, TAction> AndKeepSameState();
    }
    #endregion

    #region ConditionalEvents
    public interface IEventBuilder<TState, TAction, TArg>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        IStateClause<TState, TAction, TArg> WhenStateIs(TState state);
        IStateClause<TState, TAction, TArg> WhenStateIs(params TState[] state);

    }

    public interface IStateClause<TState, TAction, TArg>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction, TArg> TransitionTo(TState state);
        ITransitionClause<TState, TAction, TArg> KeepSameState();

        IExecuteClause<TState, TAction, TArg> Execute(TAction action);

        IIfClause<TState, TAction, TArg> If(Guard<TArg> guard, string reason);
    }

    public interface ITransitionClause<TState, TAction, TArg> : IEventBuilder<TState, TAction, TArg>, IStateMachineBuilder<TState>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
    }

    public interface IExecuteClause<TState, TAction, TArg>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction, TArg> ThenTransitionTo(TState state);
        ITransitionClause<TState, TAction, TArg> AndKeepSameState();

        public IEvent<TState, TAction> Build();
    }

    public interface IIfClause<TState, TAction, TArg>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction, TArg> TransitionTo(TState state);
        IIfExecuteClause<TState, TAction, TArg> Execute(TAction action);

        IIfClause<TState, TAction, TArg> And(Guard<TArg> guard, string reason);
    }

    public interface IIfTransitionClause<TState, TAction, TArg> : IEventBuilder<TState, TAction, TArg>, IStateMachineBuilder<TState>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        public IStateClause<TState, TAction, TArg> Else { get; }
    }

    public interface IIfExecuteClause<TState, TAction, TArg>
        where TState : notnull, System.Enum
        where TAction : System.Delegate
    {
        IIfTransitionClause<TState, TAction, TArg> ThenTransitionTo(TState state);
        IIfTransitionClause<TState, TAction, TArg> AndKeepSameState();
    }
    #endregion
    #endregion
}
