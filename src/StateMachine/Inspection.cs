using System;
using System.Collections.Generic;

namespace StateMachine
{
    // ═══════════════════════════════════════════════════════════════════
    // Inspect API — Staged Fluent Types
    // ═══════════════════════════════════════════════════════════════════

    public readonly struct Inspection<TState> where TState : notnull, System.Enum
    {
        internal readonly Action? FireAction;

        public bool IsDefined { get; }
        public bool IsAccepted { get; }
        public TState TargetState { get; }
        public IReadOnlyList<string> Reasons { get; }

        internal Inspection(
            bool isDefined,
            bool isAccepted,
            TState targetState,
            IReadOnlyList<string> reasons,
            Action? fireAction)
        {
            IsDefined = isDefined;
            IsAccepted = isAccepted;
            TargetState = targetState;
            Reasons = reasons ?? Array.Empty<string>();
            FireAction = fireAction;
        }

        public Defined<TState> IfDefined() => new Defined<TState>(this);

        public Defined<TState> IfDefined(Action handler)
        {
            if (IsDefined) handler();
            return new Defined<TState>(this);
        }

        public void IfNotDefined(Action handler)
        {
            if (!IsDefined) handler();
        }
    }

    public readonly struct Inspection<TState, TArg>
        where TState : notnull, System.Enum
    {
        private readonly Func<TArg, Inspection<TState>>? _withArg;

        public bool IsDefined { get; }

        internal Inspection(bool isDefined, Func<TArg, Inspection<TState>>? withArg)
        {
            IsDefined = isDefined;
            _withArg = withArg;
        }

        public Defined<TState, TArg> IfDefined() => new Defined<TState, TArg>(IsDefined, _withArg);

        public Defined<TState, TArg> IfDefined(Action handler)
        {
            if (IsDefined) handler();
            return new Defined<TState, TArg>(IsDefined, _withArg);
        }

        public void IfNotDefined(Action handler)
        {
            if (!IsDefined) handler();
        }
    }

    public readonly struct Defined<TState> where TState : notnull, System.Enum
    {
        private readonly Inspection<TState> _inspection;

        internal Defined(Inspection<TState> inspection)
        {
            _inspection = inspection;
        }

        public Accepted<TState> IfAccepted() => new Accepted<TState>(_inspection);

        public Accepted<TState> IfAccepted(Action<TState> handler)
        {
            if (_inspection.IsAccepted) handler(_inspection.TargetState);
            return new Accepted<TState>(_inspection);
        }

        public Accepted<TState> IfAccepted(Action handler)
        {
            if (_inspection.IsAccepted) handler();
            return new Accepted<TState>(_inspection);
        }

        public void IfNotDefined(Action handler)
        {
            if (!_inspection.IsDefined) handler();
        }
    }

    public readonly struct Defined<TState, TArg> where TState : notnull, System.Enum
    {
        private readonly bool _isDefined;
        private readonly Func<TArg, Inspection<TState>>? _withArg;

        internal Defined(bool isDefined, Func<TArg, Inspection<TState>>? withArg)
        {
            _isDefined = isDefined;
            _withArg = withArg;
        }

        public Evaluated<TState> WithArg(TArg arg)
        {
            var inspection = _withArg?.Invoke(arg)
                ?? new Inspection<TState>(false, false, default!, Array.Empty<string>(), null);
            return new Evaluated<TState>(inspection);
        }

        public void IfNotDefined(Action handler)
        {
            if (!_isDefined) handler();
        }
    }

    public readonly struct Evaluated<TState> where TState : notnull, System.Enum
    {
        private readonly Inspection<TState> _inspection;

        internal Evaluated(Inspection<TState> inspection)
        {
            _inspection = inspection;
        }

        public bool IsDefined => _inspection.IsDefined;
        public bool IsAccepted => _inspection.IsAccepted;
        public TState TargetState => _inspection.TargetState;
        public IReadOnlyList<string> Reasons => _inspection.Reasons;

        public Accepted<TState> IfAccepted() => new Accepted<TState>(_inspection);

        public Accepted<TState> IfAccepted(Action<TState> handler)
        {
            if (_inspection.IsAccepted) handler(_inspection.TargetState);
            return new Accepted<TState>(_inspection);
        }

        public Accepted<TState> IfAccepted(Action handler)
        {
            if (_inspection.IsAccepted) handler();
            return new Accepted<TState>(_inspection);
        }
    }

    public readonly struct Accepted<TState> where TState : notnull, System.Enum
    {
        private readonly Inspection<TState> _inspection;

        internal Accepted(Inspection<TState> inspection)
        {
            _inspection = inspection;
        }

        public bool IsAccepted => _inspection.IsAccepted;

        public Rejected<TState> Fire()
        {
            if (_inspection.IsAccepted)
                _inspection.FireAction?.Invoke();
            return new Rejected<TState>(_inspection);
        }

        public Accepted<TState> IfRejected(Action<IReadOnlyList<string>> handler)
        {
            if (!_inspection.IsAccepted && _inspection.IsDefined) handler(_inspection.Reasons);
            return this;
        }

        public Accepted<TState> IfRejected(Action handler)
        {
            if (!_inspection.IsAccepted && _inspection.IsDefined) handler();
            return this;
        }

        public void IfNotDefined(Action handler)
        {
            if (!_inspection.IsDefined) handler();
        }
    }

    public readonly struct Rejected<TState> where TState : notnull, System.Enum
    {
        private readonly Inspection<TState> _inspection;

        internal Rejected(Inspection<TState> inspection)
        {
            _inspection = inspection;
        }

        public Undefined<TState> IfRejected(Action<IReadOnlyList<string>> handler)
        {
            if (!_inspection.IsAccepted && _inspection.IsDefined) handler(_inspection.Reasons);
            return new Undefined<TState>(_inspection);
        }

        public Undefined<TState> IfRejected(Action handler)
        {
            if (!_inspection.IsAccepted && _inspection.IsDefined) handler();
            return new Undefined<TState>(_inspection);
        }
    }

    public readonly struct Undefined<TState> where TState : notnull, System.Enum
    {
        private readonly Inspection<TState> _inspection;

        internal Undefined(Inspection<TState> inspection)
        {
            _inspection = inspection;
        }

        public void IfNotDefined(Action handler)
        {
            if (!_inspection.IsDefined) handler();
        }
    }
}
