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

        /*Questions to answer:
         * 1. Which events can currently be fired, and which states will they transition to
         * 2. Wich states can be accessed, and which events / guards are required to get there
         * 3. If an event cannot be triggered, why not (not defined for the current state, or guard failing)
         * 4. If a state cannot be accessed, why not (no transition defined, or guards failing)
         * 5. All states and events, regardless of current state, as to be able to map out the full workflow
         */
        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger(Trigger stateMachineTrigger);
        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger<TArg>(Trigger<TArg> stateMachineTrigger, TArg argument);
        public (bool IsAccepted, TState? newState, string ReasonNotAccepted) TestTrigger<TArg>(AsyncTrigger<TArg> stateMachineTrigger, TArg argument);
    }

    public interface IEvent
    {
        public string Name { get; }
        public Delegate Trigger { get; }
    }

    public interface IEvent<TState> : IEvent
    {
        public new Trigger Trigger { get; }

        public bool IsAccepted { get; }
        public bool TryAccept(out TState? nextState, out string reason);

        public TState NextState { get; }
    }

    public interface IEvent<TState, TArg> : IEvent
    {
        public new Trigger<TArg> Trigger { get; }

        public bool IsAccepted(TArg eventArgument);
        public TState NextState(TArg eventArgument);
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


    //These interfaces are used to create the fluent syntax for building a state machine.
    //The builder pattern is utilized to simplify construction of a complex object
    #region FluentInterfaces
    public interface IStateMachineBuilder<TState> where TState : notnull
    {
        IStateMachine<TState> Build(TState initialState);

        IEventBuilder<TState, TransitionAction> DefineTrigger(out Trigger trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, TransitionAction> DefineEvent(out IEvent<TState> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, AsyncTransitionAction> DefineAsyncTrigger(out Trigger trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, AsyncTransitionAction> DefineAsyncEvent(out IEvent<TState> @event, [CallerArgumentExpression("event")] string? name = null);


        IEventBuilder<TState, TransitionAction<TArg>, TArg> DefineTrigger<TArg>(out Trigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, TransitionAction<TArg>, TArg> DefineEvent<TArg>(out IEvent<TState, TArg> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, AsyncTransitionAction<TArg>, TArg> DefineAsyncTrigger<TArg>(out AsyncTrigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, AsyncTransitionAction<TArg>, TArg> DefineAsyncEvent<TArg>(out IEvent<TState, TArg> @event, [CallerArgumentExpression("event")] string? name = null);
    }

    #region SimpleEvents
    public interface IEventBuilder<TState, TAction>
        where TState : notnull
        where TAction : System.Delegate
    {
        IStateClause<TState, TAction> WhenStateIs(TState state);
        IStateClause<TState, TAction> WhenStateIs(params TState[] state);

    }

    public interface IStateClause<TState, TAction>
        where TState : notnull
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction> TransitionTo(TState state);
        IExecuteClause<TState, TAction> Execute(TransitionAction action);
    }

    public interface ITransitionClause<TState, TAction> : IEventBuilder<TState, TAction>, IStateMachineBuilder<TState>
    where TState : notnull
                where TAction : System.Delegate
    {
    }

    public interface IExecuteClause<TState, TAction>
        where TState : notnull
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction> ThenTransitionTo(TState state);
        ITransitionClause<TState, TAction> AndKeepSameState();
    }
    #endregion

    #region ConditionalEvents
    public interface IEventBuilder<TState, TAction, TArg>
        where TState : notnull
        where TAction : System.Delegate
    {
        IStateClause<TState, TAction, TArg> WhenStateIs(TState state);
        IStateClause<TState, TAction, TArg> WhenStateIs(params TState[] state);

    }

    public interface IStateClause<TState, TAction, TArg>
        where TState : notnull
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction, TArg> TransitionTo(TState state);
        IExecuteClause<TState, TAction, TArg> Execute(TAction action);

        IIfClause<TState, TAction, TArg> If(Guard<TArg> guard, string reason);
    }

    public interface ITransitionClause<TState, TAction, TArg> : IEventBuilder<TState, TAction, TArg>, IStateMachineBuilder<TState>
        where TState : notnull
        where TAction : System.Delegate
    {
    }

    public interface IExecuteClause<TState, TAction, TArg>
        where TState : notnull
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction, TArg> ThenTransitionTo(TState state);
        ITransitionClause<TState, TAction, TArg> AndKeepSameState();
    }

    public interface IIfClause<TState, TAction, TArg>
        where TState : notnull
        where TAction : System.Delegate
    {
        ITransitionClause<TState, TAction, TArg> TransitionTo(TState state);
        IIfExecuteClause<TState, TAction, TArg> Execute(TAction action);
    }

    public interface IIfTransitionClause<TState, TAction, TArg> : IEventBuilder<TState, TAction, TArg>, IStateMachineBuilder<TState>
        where TState : notnull
        where TAction : System.Delegate
    {
        public IStateClause<TState, TAction, TArg> Else { get; }
    }

    public interface IIfExecuteClause<TState, TAction, TArg>
        where TState : notnull
        where TAction : System.Delegate
    {
        IIfTransitionClause<TState, TAction, TArg> ThenTransitionTo(TState state);
        IIfTransitionClause<TState, TAction, TArg> AndKeepSameState();
    }
    #endregion
    #endregion
}
