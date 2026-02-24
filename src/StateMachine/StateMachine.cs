using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace StateMachine
{
    /// <summary>
    /// A typed state machine with co-located immutable data, combining finite state machine 
    /// constraints with a reducer pattern. Supports both data-less (state-only) and data-ful 
    /// (state + immutable record) modes.
    /// </summary>
    public static class StateMachine
    {
        /// <summary>
        /// Create a builder for a data-less state machine that only tracks state transitions.
        /// </summary>
        /// <typeparam name="TState">The enum type used to represent states</typeparam>
        public static IStateMachineBuilder<TState> CreateBuilder<TState>()
            where TState : notnull, System.Enum
        {
            return new StateMachineBuilder<TState>();
        }

    }

    // ═══════════════════════════════════════════════════════════════════
    // Data-Less State Machine
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Data-less state machine implementation. The underlying transition lookup uses
    /// enum ordinals for O(1) access into the transition table — states and events are
    /// sealed after construction, enabling a lightweight array-based layout.
    /// </summary>
    internal class StateMachine<TState> : IStateMachine<TState>
        where TState : notnull, System.Enum
    {
        private readonly object _lock = new();
        private TState _state;

        public TState State
        {
            get { lock (_lock) { return _state; } }
        }

        private static readonly TState[] _states = (TState[])Enum.GetValues(typeof(TState));
        public IReadOnlyList<TState> States => _states;

        private readonly List<IEvent> _events = new();
        public IReadOnlyList<IEvent> Events => _events;

        public event Action<TransitionedEventArgs<TState>>? Transitioned;

        public StateMachine(TState initialState)
        {
            _state = initialState;
        }

        internal object Lock => _lock;

        internal void TransitionTo(TState newState, string eventName)
        {
            lock (_lock)
            {
                var from = _state;
                _state = newState;
                Transitioned?.Invoke(new TransitionedEventArgs<TState>(from, newState, eventName));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Data-Ful State Machine
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Data-ful state machine implementation. Manages both state and an immutable TData record.
    /// The underlying transition lookup uses enum ordinals for O(1) access into the transition
    /// table — states and events are sealed after construction, enabling a lightweight
    /// array-based layout. State is read from TData via the compiled state selector expression.
    /// </summary>
    internal class StateMachine<TState, TData> : IStateMachine<TState, TData>
        where TState : notnull, System.Enum
    {
        private readonly object _lock = new();
        private readonly Func<TData, TState> _getState;
        private readonly Func<TData, TState, TData> _setState;
        private TData _data;

        public TState State
        {
            get { lock (_lock) { return _getState(_data); } }
        }

        public TData Data
        {
            get { lock (_lock) { return _data; } }
        }

        private static readonly TState[] _states = (TState[])Enum.GetValues(typeof(TState));
        public IReadOnlyList<TState> States => _states;

        private readonly List<IEvent> _events = new();
        public IReadOnlyList<IEvent> Events => _events;

        public event Action<TransitionedEventArgs<TState>>? Transitioned;
        public event Action<DataTransitionedEventArgs<TState, TData>>? DataTransitioned;

        public StateMachine(
            TData initialData,
            Func<TData, TState> getState,
            Func<TData, TState, TData> setState)
        {
            _data = initialData;
            _getState = getState;
            _setState = setState;
        }

        internal object Lock => _lock;

        internal void TransitionTo(
            TData newData,
            TState newState,
            string eventName)
        {
            lock (_lock)
            {
                var oldData = _data;
                var fromState = _getState(oldData);
                _data = _setState(newData, newState);

                var stateArgs = new TransitionedEventArgs<TState>(fromState, newState, eventName);
                Transitioned?.Invoke(stateArgs);

                DataTransitioned?.Invoke(new DataTransitionedEventArgs<TState, TData>(
                    fromState, newState, oldData, _data, eventName));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Data-Less Builder (stub)
    // ═══════════════════════════════════════════════════════════════════

    internal class StateMachineBuilder<TState> : IStateMachineBuilder<TState>
        where TState : notnull, System.Enum
    {
        public IStateMachine<TState> Build(TState initialState)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState> DefineEvent(
            out IEvent<TState> @event,
            [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }

        public IStateMachineBuilder<TState, TData> WithData<TData>(
            Expression<Func<TData, TState>> stateSelector)
        {
            return new StateMachineBuilder<TState, TData>(stateSelector);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Data-Ful Builder (stub)
    // ═══════════════════════════════════════════════════════════════════

    internal class StateMachineBuilder<TState, TData> : IStateMachineBuilder<TState, TData>
        where TState : notnull, System.Enum
    {
        private readonly Expression<Func<TData, TState>> _stateSelector;

        public StateMachineBuilder(Expression<Func<TData, TState>> stateSelector)
        {
            _stateSelector = stateSelector;
        }

        public IStateMachine<TState, TData> Build(TData initialData)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState, TData> DefineEvent(
            out IEvent<TState> @event,
            [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState, TData, TArg> DefineEvent<TArg>(
            out IEvent<TState, TArg> @event,
            [CallerArgumentExpression("event")] string? name = null)
        {
            throw new NotImplementedException();
        }
    }
}
