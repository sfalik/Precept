using System;
using System.Collections.Generic;

namespace StateMachineV2
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

    public interface IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        IFiniteStateMachine<TState> Build(TState initialState);

        ITransitionStateClause<TState> WhenStateIs(TState state);
        ITransitionStateClause<TState> WhenStateIs(params TState[] states);
    }

    public delegate void StateMachineEvent();
    public delegate void StateMachineEvent<TArg>(TArg eventArgument);

    public interface ITransitionStateClause<TState> where TState : notnull
    {
        ITransitionEventClause<TState> AndEventFired(out StateMachineEvent stateMachineEvent);
        ITransitionEventClause<TState, TArg> AndEventFired<TArg>(out StateMachineEvent<TArg> stateMachineEvent);
    }

    public delegate bool StateMachineGuard();
    public interface ITransitionEventClause<TState> where TState : notnull
    {
        IFiniteStateMachineBuilder<TState> TransitionTo(TState state);
        ITransitionExecuteClause<TState> Execute(StateMachineEvent action);

        ITransitionIfClause<TState> If(StateMachineGuard guard, string reason);
    }

    public delegate bool StateMachineGuard<TArg>(TArg eventArgument);
    public interface ITransitionEventClause<TState, TArg> where TState : notnull
    {
        IFiniteStateMachineBuilder<TState> TransitionTo(TState state);
        ITransitionExecuteClause<TState> Execute(StateMachineEvent<TArg> action);

        ITransitionIfClause<TState, TArg> If(StateMachineGuard<TArg> guard, string reason);
    }

    public interface ITransitionIfClause<TState> where TState : notnull
    {
        IFiniteStateMachineBuilder<TState> TransitionTo(TState state);
        ITransitionGuardedExecuteClause<TState> Execute(StateMachineEvent action);
    }

    public interface ITransitionIfClause<TState, TArg> where TState : notnull
    {
        ITransitionGuardedExecuteClause<TState, TArg> Execute(StateMachineEvent<TArg> action);
    }

    public interface ITransitionExecuteClause<TState> where TState : notnull
    {
        IFiniteStateMachineBuilder<TState> ThenTransitionTo(TState state);
        IFiniteStateMachineBuilder<TState> AndKeepSameState();
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

    public interface IGuardedTransition<TState> : IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionEventClause<TState> Else { get; }
    }

    public interface IGuardedTransition<TState, TArg> : IFiniteStateMachineBuilder<TState> where TState : notnull
    {
        public ITransitionEventClause<TState, TArg> Else { get; }
    }

    public static class FiniteStateMachine
    {
        public static IFiniteStateMachineBuilder<TState> CreateBuilder<TState>()
            where TState : notnull


        {
            throw new NotImplementedException();
        }
    }



}
