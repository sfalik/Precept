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
    public delegate Task AsyncTrigger<TArg>(TArg eventArgument);

    public delegate void Action();
    public delegate void Action<TArg>(TArg eventArgument);
    public delegate Task AsyncAction<TArg>(TArg eventArgument);


    public delegate bool Guard();
    public delegate bool Guard<TArg>(TArg eventArgument);


    //These interfaces are used to create the fluent syntax for building a state machine.
    //The builder pattern is utilized to simplify construction of a complex object
    #region FluentInterfaces
    public interface IStateMachineBuilder<TState> where TState : notnull
    {
        IStateMachine<TState> Build(TState initialState);

        IEventBuilder<TState, Trigger, Guard> DefineTrigger(out Trigger trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, Trigger, Guard> DefineEvent(out IEvent<TState> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, Trigger<TArg>, Guard<TArg>> DefineTrigger<TArg>(out Trigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, Trigger<TArg>, Guard<TArg>> DefineEvent<TArg>(out IEvent<TState, TArg> @event, [CallerArgumentExpression("event")] string? name = null);

        IEventBuilder<TState, AsyncTrigger<TArg>, Guard<TArg>> DefineAsyncTrigger<TArg>(out AsyncTrigger<TArg> trigger, [CallerArgumentExpression("trigger")] string? name = null);
        IEventBuilder<TState, AsyncTrigger<TArg>, Guard<TArg>> DefineAsyncEvent<TArg>(out IEvent<TState, TArg> @event, [CallerArgumentExpression("event")] string? name = null);
    }

    public interface IEventBuilder
    {

    }

    public interface IEventBuilder<TState, TAction, TGuard>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
        IStateClause<TState, TAction, TGuard> WhenStateIs(TState state);
        IStateClause<TState, TAction, TGuard> WhenStateIs(params TState[] state);

    }
    public interface IStateClause<TState, TAction, TGuard>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
        ITransitionClause<TState, TAction, TGuard> TransitionTo(TState state);
        IExecuteClause<TState, TAction, TGuard> Execute(TAction action);

        IIfClause<TState, TAction, TGuard> If(TGuard guard, string reason);
    }

    public interface ITransitionClause<TState, TAction, TGuard> : IEventBuilder<TState, TAction, TGuard>, IStateMachineBuilder<TState>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
    }

    public interface IExecuteClause<TState, TAction, TGuard>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
        ITransitionClause<TState, TAction, TGuard> ThenTransitionTo(TState state);
        ITransitionClause<TState, TAction, TGuard> AndKeepSameState();
    }

    public interface IIfClause<TState, TAction, TGuard>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
        ITransitionClause<TState, TAction, TGuard> TransitionTo(TState state);
        IIfExecuteClause<TState, TAction, TGuard> Execute(TAction action);
    }

    public interface IIfTransitionClause<TState, TAction, TGuard> : IEventBuilder<TState, TAction, TGuard>, IStateMachineBuilder<TState>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
        public IStateClause<TState, TAction, TGuard> Else { get; }
    }

    public interface IIfExecuteClause<TState, TAction, TGuard>
        where TState : notnull
        where TAction : System.Delegate
        where TGuard : System.Delegate
    {
        IIfTransitionClause<TState, TAction, TGuard> ThenTransitionTo(TState state);
        IIfTransitionClause<TState, TAction, TGuard> AndKeepSameState();
    }

    #endregion
}
