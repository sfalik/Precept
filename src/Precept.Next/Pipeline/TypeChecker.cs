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
                switch (decl)
                {
                    case FieldDeclaration fd:
                        RegisterFieldDeclaration(fd);
                        break;
                    case StateDeclaration sd:
                        RegisterStateDeclaration(sd);
                        break;
                    case EventDeclaration ed:
                        RegisterEventDeclaration(ed);
                        break;
                }
            }

            // Post-registration: check initial state
            if (_states.Count > 0 && _initialState == null)
                _diagnostics.Add(Diagnostics.Create(DiagnosticCode.NoInitialState, _tree.Root.Span));
        }

        private void RegisterFieldDeclaration(FieldDeclaration decl)
        {
            var resolvedType = ResolveTypeRef(decl.Type);
            var isOptional   = decl.Modifiers.Any(m => m is OptionalModifier);
            var isComputed   = decl.ComputedExpression is not null;
            var emptyMods    = new ResolvedModifiers(false, false, false, false, false, null, null, null, null, null, null, null);

            foreach (var token in decl.Names)
            {
                if (token.Length == 0) continue;   // missing token from error recovery

                var span   = SpanOf(token);
                var symbol = new FieldSymbol(
                    Name:               token.Text,
                    Type:               resolvedType,
                    IsOptional:         isOptional,
                    IsComputed:         isComputed,
                    Modifiers:          emptyMods,
                    ComputedExpression: null,
                    DefaultValue:       null,
                    Span:               span
                );

                if (_fields.ContainsKey(token.Text))
                {
                    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.DuplicateFieldName, span, token.Text));
                }
                else
                {
                    _fields.Add(token.Text, symbol);
                }
            }
        }

        private void RegisterStateDeclaration(StateDeclaration decl)
        {
            foreach (var entry in decl.Entries)
            {
                if (entry.Name.Length == 0) continue;

                var span   = SpanOf(entry.Name);
                var symbol = new StateSymbol(
                    Name:      entry.Name.Text,
                    IsInitial: entry.IsInitial,
                    Modifiers: entry.Modifiers,
                    Span:      span
                );

                if (_states.ContainsKey(entry.Name.Text))
                {
                    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.DuplicateStateName, span, entry.Name.Text));
                }
                else
                {
                    _states.Add(entry.Name.Text, symbol);

                    if (entry.IsInitial)
                    {
                        if (_initialState is not null)
                            _diagnostics.Add(Diagnostics.Create(DiagnosticCode.MultipleInitialStates, span, _initialState, entry.Name.Text));
                        else
                            _initialState = entry.Name.Text;
                    }
                }
            }
        }

        private void RegisterEventDeclaration(EventDeclaration decl)
        {
            var emptyMods = new ResolvedModifiers(false, false, false, false, false, null, null, null, null, null, null, null);

            foreach (var token in decl.Names)
            {
                if (token.Length == 0) continue;

                var argsBuilder = ImmutableDictionary.CreateBuilder<string, ArgSymbol>(StringComparer.Ordinal);

                foreach (var arg in decl.Args)
                {
                    if (arg.Name.Length == 0) continue;

                    var resolvedType = ResolveTypeRef(arg.Type);
                    var isOptional   = arg.Modifiers.Any(m => m is OptionalModifier);
                    var argSymbol    = new ArgSymbol(
                        Name:         arg.Name.Text,
                        Type:         resolvedType,
                        IsOptional:   isOptional,
                        Modifiers:    emptyMods,
                        DefaultValue: null,
                        Span:         SpanOf(arg.Name)
                    );

                    if (argsBuilder.ContainsKey(arg.Name.Text))
                        _diagnostics.Add(Diagnostics.Create(DiagnosticCode.DuplicateArgName, SpanOf(arg.Name), arg.Name.Text, token.Text));
                    else
                        argsBuilder.Add(arg.Name.Text, argSymbol);
                }

                var span        = SpanOf(token);
                var eventSymbol = new EventSymbol(
                    Name:      token.Text,
                    Args:      argsBuilder.ToImmutable(),
                    IsInitial: decl.IsInitial,
                    Span:      span
                );

                if (_events.ContainsKey(token.Text))
                    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.DuplicateEventName, span, token.Text));
                else
                    _events.Add(token.Text, eventSymbol);
            }
        }

        private static SourceSpan SpanOf(Token t) =>
            new(t.Offset, t.Length, t.Line, t.Column, t.Line, t.Column + Math.Max(t.Length, 1));

        private ResolvedType ResolveTypeRef(TypeRef typeRef) => typeRef switch
        {
            { IsMissing: true }              => new ErrorType(),
            ScalarTypeRef s                  => ResolveScalarType(s.Kind, s.Qualifier, s.CaseInsensitive),
            CollectionTypeRef                => new ErrorType(),   // stub — slice 8
            ChoiceTypeRef                    => new ErrorType(),   // stub — slice 10
            _                                => new ErrorType()
        };

        private static ResolvedType ResolveScalarType(ScalarTypeKind kind, TypeQualifier? qualifier, bool caseInsensitive) => kind switch
        {
            ScalarTypeKind.String  => new StringType(caseInsensitive),
            ScalarTypeKind.Boolean => new BooleanType(),
            ScalarTypeKind.Integer => new IntegerType(),
            ScalarTypeKind.Decimal => new DecimalType(),
            ScalarTypeKind.Number  => new NumberType(),
            _                      => new ErrorType()   // temporal + business-domain stubs
        };

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
