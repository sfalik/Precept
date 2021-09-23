using System.Collections.Generic;

namespace StateMachine
{
    public interface IStateful<TState> where TState : notnull
    {
        public TState State { get; }

        public IReadOnlyList<TState> States { get; }

        public bool IsEventAccepted(StateMachineEvent stateMachineEvent);
        public bool IsEventAccepted(StateMachineEvent stateMachineEvent, out TState? newState, out string reason);
        public bool IsEventAccepted<TArg>(StateMachineEvent<TArg> stateMachineEvent, TArg arg);
        public bool IsEventAccepted<TArg>(StateMachineEvent<TArg> stateMachineEvent, TArg arg, out TState? newState, out string reason);
    }

    public interface IFiniteStateMachine<TState> : IStateful<TState> where TState : notnull
    {

    }

    public delegate void StateMachineEvent();
    public delegate void StateMachineEvent<TArg>(TArg eventArgument);

    public interface IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        IFiniteStateMachine<TState> Build(TState initialState);

        ITransitionEventClause<TState> WhenEventFired(out StateMachineEvent stateMachineEvent);
        ITransitionEventClause<TState, TArg> WhenEventFired<TArg>(out StateMachineEvent<TArg> stateMachineEvent);
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
        ITransitionExecuteClause<TState> Execute(StateMachineEvent action);

        ITransitionIfClause<TState> If(StateMachineGuard guard, string reason);
    }
    public interface ITransitionStateClause<TState, TArg> where TState : notnull
    {
        ITransition<TState> TransitionTo(TState state);
        ITransitionExecuteClause<TState> Execute(StateMachineEvent<TArg> action);

        ITransitionIfClause<TState, TArg> If(StateMachineGuard<TArg> guard, string reason);
    }

    public interface ITransitionIfClause<TState> where TState : notnull
    {
        ITransition<TState> TransitionTo(TState state);
        ITransitionGuardedExecuteClause<TState> Execute(StateMachineEvent action);
    }
    public interface ITransitionIfClause<TState, TArg> where TState : notnull
    {
        ITransitionGuardedExecuteClause<TState, TArg> Execute(StateMachineEvent<TArg> action);
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

    public interface ITransition<TState> : IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionEventClause<TState> Or { get; }
    }
    public interface ITransition<TState, TArg> : IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionEventClause<TState, TArg> Or { get; }
    }

    public interface IGuardedTransition<TState> : IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionStateClause<TState> Else { get; }
        public ITransitionEventClause<TState> Or { get; }
    }
    public interface IGuardedTransition<TState, TArg> : IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionStateClause<TState, TArg> Else { get; }
        public ITransitionEventClause<TState, TArg> Or { get; }
    }
}
