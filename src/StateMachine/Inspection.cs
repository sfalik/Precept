using System;
using System.Collections.Generic;

namespace StateMachine
{
    // ═══════════════════════════════════════════════════════════════════
    // Inspect API — Evaluation Chain Types
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// The result of inspecting a trigger against the current machine state.
    /// Guards have already been evaluated. Use IfAccepted/IfRejected to branch,
    /// then reach Fire() via IfAccepted to commit the transition.
    /// </summary>
    public sealed class EventInspection<TState> where TState : notnull, System.Enum
    {
        private readonly Action _fireAction;

        /// <summary>Whether the event has a transition rule defined for the current state (regardless of guards).</summary>
        public bool IsDefined { get; }

        /// <summary>Whether the event was accepted — defined and all guards passed.</summary>
        public bool IsAccepted { get; }

        /// <summary>The target state if accepted; the current state if rejected.</summary>
        public TState TargetState { get; }

        /// <summary>Guard failure reasons. Empty when accepted.</summary>
        public IReadOnlyList<string> Reasons { get; }

        internal EventInspection(bool isDefined, bool isAccepted, TState targetState, IReadOnlyList<string> reasons, Action fireAction)
        {
            IsDefined = isDefined;
            IsAccepted = isAccepted;
            TargetState = targetState;
            Reasons = reasons;
            _fireAction = fireAction;
        }

        /// <summary>Register a callback to run if the event was accepted. Enables Fire() on the returned chain.</summary>
        public AcceptedChain<TState> IfAccepted(Action<TState> handler)
        {
            if (IsAccepted) handler(TargetState);
            return new AcceptedChain<TState>(this, _fireAction);
        }

        /// <summary>Register a callback to run if the event was accepted. Enables Fire() on the returned chain.</summary>
        public AcceptedChain<TState> IfAccepted(Action handler)
        {
            if (IsAccepted) handler();
            return new AcceptedChain<TState>(this, _fireAction);
        }

        /// <summary>Register a callback to run if the event was rejected.</summary>
        public RejectedChain<TState> IfRejected(Action<IReadOnlyList<string>> handler)
        {
            if (!IsAccepted) handler(Reasons);
            return new RejectedChain<TState>(this);
        }

        /// <summary>Register a callback to run if the event was rejected.</summary>
        public RejectedChain<TState> IfRejected(Action handler)
        {
            if (!IsAccepted) handler();
            return new RejectedChain<TState>(this);
        }
    }

    /// <summary>
    /// Chain produced after <see cref="EventInspection{TState}.IfAccepted"/>.
    /// Call Fire() to commit the transition, or Else() to handle the rejection case without firing.
    /// </summary>
    public sealed class AcceptedChain<TState> where TState : notnull, System.Enum
    {
        private readonly EventInspection<TState> _inspection;
        private readonly Action _fireAction;

        internal AcceptedChain(EventInspection<TState> inspection, Action fireAction)
        {
            _inspection = inspection;
            _fireAction = fireAction;
        }

        /// <summary>
        /// Execute the transition if the event was accepted.
        /// Throws <see cref="StaleStateException"/> if the machine state changed concurrently.
        /// Returns a <see cref="FiredChain{TState}"/> allowing an optional Else() for the rejection case.
        /// </summary>
        public FiredChain<TState> Fire()
        {
            if (_inspection.IsAccepted)
                _fireAction();
            return new FiredChain<TState>(_inspection);
        }

        /// <summary>Handle the rejection case without firing. Terminal — no further chaining.</summary>
        public void Else(Action<IReadOnlyList<string>> handler)
        {
            if (!_inspection.IsAccepted) handler(_inspection.Reasons);
        }

        /// <summary>Handle the rejection case without firing. Terminal — no further chaining.</summary>
        public void Else(Action handler)
        {
            if (!_inspection.IsAccepted) handler();
        }
    }

    /// <summary>
    /// Chain produced after <see cref="AcceptedChain{TState}.Fire"/>.
    /// Allows an optional Else() to handle the rejection case after the fire attempt.
    /// </summary>
    public sealed class FiredChain<TState> where TState : notnull, System.Enum
    {
        private readonly EventInspection<TState> _inspection;

        internal FiredChain(EventInspection<TState> inspection)
        {
            _inspection = inspection;
        }

        /// <summary>Handle the rejection case after Fire() was called.</summary>
        public void Else(Action<IReadOnlyList<string>> handler)
        {
            if (!_inspection.IsAccepted) handler(_inspection.Reasons);
        }

        /// <summary>Handle the rejection case after Fire() was called.</summary>
        public void Else(Action handler)
        {
            if (!_inspection.IsAccepted) handler();
        }
    }

    /// <summary>
    /// Chain produced after <see cref="EventInspection{TState}.IfRejected"/>.
    /// Else() provides the accepted handler. No Fire() available — accepted path not registered.
    /// </summary>
    public sealed class RejectedChain<TState> where TState : notnull, System.Enum
    {
        private readonly EventInspection<TState> _inspection;

        internal RejectedChain(EventInspection<TState> inspection)
        {
            _inspection = inspection;
        }

        /// <summary>Handle the accepted case. Terminal — no further chaining.</summary>
        public void Else(Action<TState> handler)
        {
            if (_inspection.IsAccepted) handler(_inspection.TargetState);
        }

        /// <summary>Handle the accepted case. Terminal — no further chaining.</summary>
        public void Else(Action handler)
        {
            if (_inspection.IsAccepted) handler();
        }
    }

    /// <summary>
    /// Result of inspecting a parameterized trigger without providing an argument.
    /// Only definition can be checked — guards require the argument.
    /// Call IfDefined/IfNotDefined to branch on definition, then WithArg to evaluate guards.
    /// </summary>
    public sealed class PartialEventInspection<TState, TArg>
        where TState : notnull, System.Enum
    {
        private readonly Func<TArg, EventInspection<TState>> _withArg;

        /// <summary>Whether the event has a transition rule defined for the current state.</summary>
        public bool IsDefined { get; }

        internal PartialEventInspection(bool isDefined, Func<TArg, EventInspection<TState>> withArg)
        {
            IsDefined = isDefined;
            _withArg = withArg;
        }

        /// <summary>Register a callback to run if the event is defined. Returns a chain with Else() and WithArg().</summary>
        public PartialChain<TState, TArg> IfDefined(Action handler)
        {
            if (IsDefined) handler();
            return new PartialChain<TState, TArg>(runElse: !IsDefined, _withArg);
        }

        /// <summary>Register a callback to run if the event is not defined. Returns a chain with Else() and WithArg().</summary>
        public PartialChain<TState, TArg> IfNotDefined(Action handler)
        {
            if (!IsDefined) handler();
            return new PartialChain<TState, TArg>(runElse: IsDefined, _withArg);
        }

        /// <summary>Provide the argument and evaluate guards, returning a full <see cref="EventInspection{TState}"/>.</summary>
        public EventInspection<TState> WithArg(TArg arg) => _withArg(arg);
    }

    /// <summary>
    /// Chain produced after <see cref="PartialEventInspection{TState,TArg}.IfDefined"/> or IfNotDefined.
    /// Else() handles the opposite branch; WithArg() progresses to full guard evaluation.
    /// </summary>
    public sealed class PartialChain<TState, TArg>
        where TState : notnull, System.Enum
    {
        private readonly bool _runElse;
        private readonly Func<TArg, EventInspection<TState>> _withArg;

        internal PartialChain(bool runElse, Func<TArg, EventInspection<TState>> withArg)
        {
            _runElse = runElse;
            _withArg = withArg;
        }

        /// <summary>Handle the opposite branch (not-defined after IfDefined, or defined after IfNotDefined).</summary>
        public PartialChain<TState, TArg> Else(Action handler)
        {
            if (_runElse) handler();
            return new PartialChain<TState, TArg>(runElse: false, _withArg);
        }

        /// <summary>Provide the argument and evaluate guards, returning a full <see cref="EventInspection{TState}"/>.</summary>
        public EventInspection<TState> WithArg(TArg arg) => _withArg(arg);
    }
}
