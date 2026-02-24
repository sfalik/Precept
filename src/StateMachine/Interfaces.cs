using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace StateMachine
{
    // ═══════════════════════════════════════════════════════════════════
    // State Machine Interfaces (the built result)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// A data-less state machine that manages state transitions only.
    /// Thread-safe after construction.
    /// </summary>
    public interface IStateMachine<TState> where TState : notnull, System.Enum
    {
        /// <summary>Current state of the machine</summary>
        TState State { get; }

        /// <summary>All valid states for this machine, derived from the TState enum values</summary>
        IReadOnlyList<TState> States { get; }

        /// <summary>All defined events on this machine</summary>
        IReadOnlyList<IEvent> Events { get; }

        /// <summary>
        /// Evaluate a trigger against the current state — definition check and guard evaluation —
        /// without committing a transition.
        /// Returns an <see cref="EventInspection{TState}"/> whose fluent chain leads to Fire().
        /// </summary>
        EventInspection<TState> Inspect(Action trigger);

        /// <summary>
        /// Evaluate a parameterized trigger with a known argument against the current state.
        /// Returns an <see cref="EventInspection{TState}"/> whose fluent chain leads to Fire().
        /// </summary>
        EventInspection<TState> Inspect<TArg>(Action<TArg> trigger, TArg arg);

        /// <summary>
        /// Inspect a parameterized trigger without providing an argument — definition check only,
        /// no guard evaluation. Call <see cref="PartialEventInspection{TState, TArg}.WithArg"/> to
        /// progress to full guard evaluation.
        /// </summary>
        PartialEventInspection<TState, TArg> Inspect<TArg>(Action<TArg> trigger);

        /// <summary>
        /// Raised after every successful state transition.
        /// Fires inside the transition lock — handlers should be fast and non-blocking.
        /// </summary>
        event Action<TransitionedEventArgs<TState>>? Transitioned;
    }

    /// <summary>
    /// A data-ful state machine that manages both state and immutable data records.
    /// State is stored as a property on the TData record, identified via the state selector expression.
    /// Transitions produce new TData records — the original is never mutated.
    /// Thread-safe after construction.
    /// </summary>
    public interface IStateMachine<TState, TData> : IStateMachine<TState>
        where TState : notnull, System.Enum
    {
        /// <summary>Current data record (immutable — includes the state property)</summary>
        TData Data { get; }

        /// <summary>
        /// Raised after every successful transition, providing both old and new data records.
        /// Fires inside the transition lock — handlers should be fast and non-blocking.
        /// </summary>
        event Action<DataTransitionedEventArgs<TState, TData>>? DataTransitioned;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Transition Observation
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Information about a completed state transition</summary>
    public record TransitionedEventArgs<TState>(
        TState FromState,
        TState ToState,
        string EventName
    ) where TState : notnull, System.Enum;

    /// <summary>Information about a completed transition including before/after data</summary>
    public record DataTransitionedEventArgs<TState, TData>(
        TState FromState,
        TState ToState,
        TData OldData,
        TData NewData,
        string EventName
    ) : TransitionedEventArgs<TState>(FromState, ToState, EventName)
        where TState : notnull, System.Enum;

    // ═══════════════════════════════════════════════════════════════════
    // Event Interfaces
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Base event interface — for introspection via <see cref="IStateMachine{TState}.Events"/></summary>
    public interface IEvent
    {
        /// <summary>
        /// Name of the event, auto-captured from the variable name via CallerArgumentExpression
        /// </summary>
        string Name { get; }
    }

    /// <summary>Synchronous event with no arguments (internal — consumers use Action delegates)</summary>
    internal interface IEvent<TState> : IEvent where TState : notnull, System.Enum
    {
        void Trigger();
        bool Evaluate(out TState state, out IReadOnlyList<string> reasons);
    }

    /// <summary>Synchronous event with a typed argument (internal — consumers use Action&lt;TArg&gt; delegates)</summary>
    internal interface IEvent<TState, TArg> : IEvent where TState : notnull, System.Enum
    {
        void Trigger(TArg arg);
        bool Evaluate(TArg arg, out TState state, out IReadOnlyList<string> reasons);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Exceptions
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Thrown when an event is triggered in a state where it is not defined</summary>
    public class InvalidTransitionException : Exception
    {
        public InvalidTransitionException() { }
        public InvalidTransitionException(string message) : base(message) { }
        public InvalidTransitionException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Thrown when all guards on a conditional event fail</summary>
    public class GuardFailedException : Exception
    {
        /// <summary>The individual reason strings from each failed guard</summary>
        public IReadOnlyList<string> Reasons { get; }

        public GuardFailedException(IReadOnlyList<string> reasons)
            : base("All guards failed: " + string.Join("; ", reasons))
        {
            Reasons = reasons;
        }

        public GuardFailedException(string message) : base(message)
        {
            Reasons = new[] { message };
        }

        public GuardFailedException(string message, Exception inner) : base(message, inner)
        {
            Reasons = new[] { message };
        }
    }

    /// <summary>Thrown when all conditions on a conditional transition fail (legacy)</summary>
    public class ConditionFailedException : Exception
    {
        public ConditionFailedException() { }
        public ConditionFailedException(string message) : base(message) { }
        public ConditionFailedException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Thrown by <see cref="AcceptedChain{TState}.Fire"/> when the machine's state changed
    /// between <c>Inspect()</c> and <c>Fire()</c>, indicating a concurrent modification
    /// invalidated the pre-evaluated inspection result.
    /// </summary>
    public class StaleStateException : Exception
    {
        public StaleStateException() { }
        public StaleStateException(string message) : base(message) { }
        public StaleStateException(string message, Exception inner) : base(message, inner) { }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Fluent Builder Interfaces
    // ═══════════════════════════════════════════════════════════════════

    #region Data-Less Builder

    /// <summary>Builder for a data-less state machine that only tracks state transitions</summary>
    public interface IStateMachineBuilder<TState> where TState : notnull, System.Enum
    {
        /// <summary>Build the state machine with the given initial state</summary>
        IStateMachine<TState> Build(TState initialState);

        /// <summary>Define a new event with no arguments. The out Action delegate can be invoked directly to fire the event.</summary>
        IEventBuilder<TState> On(
            out Action trigger,
            [CallerArgumentExpression("trigger")] string? name = null);

        /// <summary>
        /// Attach an immutable data record to this builder, producing a data-ful state machine builder.
        /// The expression identifies which property on TData holds the state.
        /// </summary>
        IStateMachineBuilder<TState, TData> WithData<TData>(
            Expression<Func<TData, TState>> stateSelector);
    }

    /// <summary>Choose which state(s) to associate with the event</summary>
    public interface IEventBuilder<TState>
        where TState : notnull, System.Enum
    {
        IStateClause<TState> WhenStateIs(TState state);
        IStateClause<TState> WhenStateIs(params TState[] states);
        IStateClause<TState> RegardlessOfState();
    }

    /// <summary>Choose the transition target for the current state clause</summary>
    public interface IStateClause<TState>
        where TState : notnull, System.Enum
    {
        ITransitionClause<TState> TransitionTo(TState state);
        ITransitionClause<TState> KeepSameState();
    }

    /// <summary>
    /// Transition is defined — continue defining more state clauses for this event,
    /// define new events, or build the machine.
    /// </summary>
    public interface ITransitionClause<TState> : IEventBuilder<TState>, IStateMachineBuilder<TState>
        where TState : notnull, System.Enum
    {
    }

    #endregion

    #region Data-Ful Builder — Simple Events (no TArg)

    /// <summary>Builder for a data-ful state machine that manages both state and immutable data</summary>
    public interface IStateMachineBuilder<TState, TData> where TState : notnull, System.Enum
    {
        /// <summary>Build the state machine with the given initial data (which includes the initial state)</summary>
        IStateMachine<TState, TData> Build(TData initialData);

        /// <summary>Define a synchronous event with no arguments. The out Action delegate can be invoked directly to fire the event.</summary>
        IEventBuilder<TState, TData> On(
            out Action trigger,
            [CallerArgumentExpression("trigger")] string? name = null);

        /// <summary>Define a synchronous event with a typed argument. The out Action&lt;TArg&gt; delegate can be invoked directly to fire the event.</summary>
        IEventBuilder<TState, TData, TArg> On<TArg>(
            out Action<TArg> trigger,
            [CallerArgumentExpression("trigger")] string? name = null);
    }

    /// <summary>Choose which state(s) to associate with a data-ful event (no arguments)</summary>
    public interface IEventBuilder<TState, TData>
        where TState : notnull, System.Enum
    {
        IStateClause<TState, TData> WhenStateIs(TState state);
        IStateClause<TState, TData> WhenStateIs(params TState[] states);
        IStateClause<TState, TData> RegardlessOfState();
    }

    /// <summary>Choose the action or transition for the current state clause (no arguments)</summary>
    public interface IStateClause<TState, TData>
        where TState : notnull, System.Enum
    {
        ITransitionClause<TState, TData> TransitionTo(TState state);
        ITransitionClause<TState, TData> KeepSameState();

        /// <summary>Execute a pure data transform as part of this transition</summary>
        IExecuteClause<TState, TData> Transform(Func<TData, TData> transform);

        /// <summary>Add a guard condition — transition only proceeds if the guard returns true</summary>
        IIfClause<TState, TData> If(Func<TData, bool> guard, string reason);
    }

    /// <summary>
    /// Transition defined for a data-ful event — continue with more state clauses,
    /// define new events, or build.
    /// </summary>
    public interface ITransitionClause<TState, TData> : IEventBuilder<TState, TData>, IStateMachineBuilder<TState, TData>
        where TState : notnull, System.Enum
    {
    }

    /// <summary>A data transform has been specified — choose the transition target</summary>
    public interface IExecuteClause<TState, TData>
        where TState : notnull, System.Enum
    {
        ITransitionClause<TState, TData> ThenTransitionTo(TState state);
        ITransitionClause<TState, TData> AndKeepSameState();
    }

    /// <summary>A guard condition is active — choose the guarded action</summary>
    public interface IIfClause<TState, TData>
        where TState : notnull, System.Enum
    {
        IIfTransitionClause<TState, TData> TransitionTo(TState state);
        IIfExecuteClause<TState, TData> Transform(Func<TData, TData> transform);
    }

    /// <summary>
    /// Guarded transition defined — continue with Else branch, more state clauses,
    /// new events, or build.
    /// </summary>
    public interface IIfTransitionClause<TState, TData> : IEventBuilder<TState, TData>, IStateMachineBuilder<TState, TData>
        where TState : notnull, System.Enum
    {
        /// <summary>Define a fallback or another guarded branch for when the previous guard fails</summary>
        IStateClause<TState, TData> Else { get; }
    }

    /// <summary>Guarded transform specified — choose the transition target</summary>
    public interface IIfExecuteClause<TState, TData>
        where TState : notnull, System.Enum
    {
        IIfTransitionClause<TState, TData> ThenTransitionTo(TState state);
        IIfTransitionClause<TState, TData> AndKeepSameState();
    }

    #endregion

    #region Data-Ful Builder — Parameterized Events (with TArg)

    /// <summary>Choose which state(s) to associate with a parameterized event</summary>
    public interface IEventBuilder<TState, TData, TArg>
        where TState : notnull, System.Enum
    {
        IStateClause<TState, TData, TArg> WhenStateIs(TState state);
        IStateClause<TState, TData, TArg> WhenStateIs(params TState[] states);
    }

    /// <summary>Choose the action or transition for a parameterized event</summary>
    public interface IStateClause<TState, TData, TArg>
        where TState : notnull, System.Enum
    {
        ITransitionClause<TState, TData, TArg> TransitionTo(TState state);
        ITransitionClause<TState, TData, TArg> KeepSameState();

        /// <summary>Execute a pure data transform that receives the event argument</summary>
        IExecuteClause<TState, TData, TArg> Transform(Func<TData, TArg, TData> transform);

        /// <summary>Add a guard condition that evaluates both data and the event argument</summary>
        IIfClause<TState, TData, TArg> If(Func<TData, TArg, bool> guard, string reason);
    }

    /// <summary>
    /// Transition defined for a parameterized event — continue with more state clauses,
    /// define new events, or build.
    /// </summary>
    public interface ITransitionClause<TState, TData, TArg> : IEventBuilder<TState, TData, TArg>, IStateMachineBuilder<TState, TData>
        where TState : notnull, System.Enum
    {
    }

    /// <summary>A parameterized transform specified — choose the transition target</summary>
    public interface IExecuteClause<TState, TData, TArg>
        where TState : notnull, System.Enum
    {
        ITransitionClause<TState, TData, TArg> ThenTransitionTo(TState state);
        ITransitionClause<TState, TData, TArg> AndKeepSameState();
    }

    /// <summary>A guard is active on a parameterized event — choose action</summary>
    public interface IIfClause<TState, TData, TArg>
        where TState : notnull, System.Enum
    {
        IIfTransitionClause<TState, TData, TArg> TransitionTo(TState state);
        IIfExecuteClause<TState, TData, TArg> Transform(Func<TData, TArg, TData> transform);
    }

    /// <summary>
    /// Guarded transition defined for a parameterized event — continue with Else,
    /// more state clauses, new events, or build.
    /// </summary>
    public interface IIfTransitionClause<TState, TData, TArg> : IEventBuilder<TState, TData, TArg>, IStateMachineBuilder<TState, TData>
        where TState : notnull, System.Enum
    {
        /// <summary>Define a fallback or another guarded branch</summary>
        IStateClause<TState, TData, TArg> Else { get; }
    }

    /// <summary>Guarded parameterized transform specified — choose the transition target</summary>
    public interface IIfExecuteClause<TState, TData, TArg>
        where TState : notnull, System.Enum
    {
        IIfTransitionClause<TState, TData, TArg> ThenTransitionTo(TState state);
        IIfTransitionClause<TState, TData, TArg> AndKeepSameState();
    }

    #endregion
}
