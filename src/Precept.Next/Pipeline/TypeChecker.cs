using System.Collections.Immutable;

namespace Precept.Pipeline;

public static class TypeChecker
{
    public static TypedModel Check(SyntaxTree tree)
    {
        var session = new CheckSession(tree);
        session.RegisterDeclarations();
        session.CheckDeclarations();
        return session.BuildModel();
    }

    private struct CheckSession
    {
        private readonly SyntaxTree _tree;
        private readonly ImmutableDictionary<string, FieldSymbol>.Builder _fields;
        private readonly ImmutableDictionary<string, StateSymbol>.Builder _states;
        private readonly ImmutableDictionary<string, EventSymbol>.Builder _events;
        private readonly ImmutableArray<ResolvedRule>.Builder _rules;
        private readonly ImmutableArray<ResolvedEnsure>.Builder _ensures;
        private readonly ImmutableArray<ResolvedTransitionRow>.Builder _transitionRows;
        private readonly ImmutableArray<ResolvedAccessMode>.Builder _accessModes;
        private readonly ImmutableArray<ResolvedStateAction>.Builder _stateActions;
        private readonly ImmutableArray<ResolvedStatelessHook>.Builder _statelessHooks;
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;
        private string? _initialState;

        public CheckSession(SyntaxTree tree)
        {
            _tree = tree;
            _fields = ImmutableDictionary.CreateBuilder<string, FieldSymbol>(StringComparer.Ordinal);
            _states = ImmutableDictionary.CreateBuilder<string, StateSymbol>(StringComparer.Ordinal);
            _events = ImmutableDictionary.CreateBuilder<string, EventSymbol>(StringComparer.Ordinal);
            _rules = ImmutableArray.CreateBuilder<ResolvedRule>();
            _ensures = ImmutableArray.CreateBuilder<ResolvedEnsure>();
            _transitionRows = ImmutableArray.CreateBuilder<ResolvedTransitionRow>();
            _accessModes = ImmutableArray.CreateBuilder<ResolvedAccessMode>();
            _stateActions = ImmutableArray.CreateBuilder<ResolvedStateAction>();
            _statelessHooks = ImmutableArray.CreateBuilder<ResolvedStatelessHook>();
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            _initialState = null;
        }

        public void RegisterDeclarations()
        {
            foreach (var decl in _tree.Root.Body)
            {
                // Pass 1: build symbol tables — implemented in slice 2/3
            }
        }

        public void CheckDeclarations()
        {
            foreach (var decl in _tree.Root.Body)
            {
                // Pass 2: type-check declarations — implemented in slice 4+
            }
        }

        public TypedModel BuildModel()
        {
            // Merge parser diagnostics with type-checker diagnostics
            var allDiagnostics = _tree.Diagnostics.AddRange(_diagnostics.ToImmutable());

            return new TypedModel(
                PreceptName: _tree.Root.Name.Text,
                Fields: _fields.ToImmutable(),
                States: _states.ToImmutable(),
                Events: _events.ToImmutable(),
                Rules: _rules.ToImmutable(),
                Ensures: _ensures.ToImmutable(),
                TransitionRows: _transitionRows.ToImmutable(),
                AccessModes: _accessModes.ToImmutable(),
                StateActions: _stateActions.ToImmutable(),
                StatelessHooks: _statelessHooks.ToImmutable(),
                InitialState: _initialState,
                Diagnostics: allDiagnostics
            );
        }
    }
}
