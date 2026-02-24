using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace StateMachine
{
    // ═══════════════════════════════════════════════════════════════════
    // Enum Ordinal Helper
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Provides O(1) enum-to-index conversion by assuming the enum values form a
    /// contiguous range starting at 0.  Validated once at type-initialization time;
    /// if the enum is sparse or starts at a non-zero value, an
    /// <see cref="InvalidOperationException"/> is thrown immediately.
    /// </summary>
    internal static class EnumOrdinal<TState> where TState : notnull, System.Enum
    {
        /// <summary>All enum values in declaration order</summary>
        internal static readonly TState[] Values = (TState[])Enum.GetValues(typeof(TState));

        /// <summary>Number of enum members</summary>
        internal static readonly int Count = Values.Length;

        static EnumOrdinal()
        {
            // Validate that the enum forms a contiguous 0-based sequence
            for (int i = 0; i < Values.Length; i++)
            {
                int v = Convert.ToInt32(Values[i]);
                if (v != i)
                {
                    throw new InvalidOperationException(
                        $"Enum '{typeof(TState).Name}' is not a contiguous zero-based sequence. " +
                        $"Value '{Values[i]}' has underlying value {v} but expected {i}. " +
                        $"StateMachine requires sequential enums for O(1) array indexing.");
                }
            }
        }

        /// <summary>
        /// Convert an enum value to its array index.  O(1) — no boxing, no lookup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ToIndex(TState state) => Unsafe.As<TState, int>(ref state);
    }

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

        public IReadOnlyList<TState> States => EnumOrdinal<TState>.Values;

        private readonly List<IEvent> _events = new();
        public IReadOnlyList<IEvent> Events => _events;

        public event Action<TransitionedEventArgs<TState>>? Transitioned;

        public bool CanHandle(Action trigger, out TState state, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle<TArg>(Action<TArg> trigger, TArg arg, out TState state, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public bool TryHandle(Action trigger, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public bool TryHandle<TArg>(Action<TArg> trigger, TArg arg, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public StateMachine(TState initialState)
        {
            // Touching EnumOrdinal<TState>.Count forces the static constructor
            // to run, validating the enum is contiguous and zero-based.
            _ = EnumOrdinal<TState>.Count;
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

        public IReadOnlyList<TState> States => EnumOrdinal<TState>.Values;

        private readonly List<IEvent> _events = new();
        public IReadOnlyList<IEvent> Events => _events;

        public event Action<TransitionedEventArgs<TState>>? Transitioned;
        public event Action<DataTransitionedEventArgs<TState, TData>>? DataTransitioned;

        public bool CanHandle(Action trigger, out TState state, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public bool CanHandle<TArg>(Action<TArg> trigger, TArg arg, out TState state, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public bool TryHandle(Action trigger, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public bool TryHandle<TArg>(Action<TArg> trigger, TArg arg, out IReadOnlyList<string> reasons)
        {
            throw new NotImplementedException();
        }

        public StateMachine(
            TData initialData,
            Func<TData, TState> getState,
            Func<TData, TState, TData> setState)
        {
            _ = EnumOrdinal<TState>.Count;
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

        public IEventBuilder<TState> On(
            out Action trigger,
            [CallerArgumentExpression("trigger")] string? name = null)
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

        public IEventBuilder<TState, TData> On(
            out Action trigger,
            [CallerArgumentExpression("trigger")] string? name = null)
        {
            throw new NotImplementedException();
        }

        public IEventBuilder<TState, TData, TArg> On<TArg>(
            out Action<TArg> trigger,
            [CallerArgumentExpression("trigger")] string? name = null)
        {
            throw new NotImplementedException();
        }
    }
}
