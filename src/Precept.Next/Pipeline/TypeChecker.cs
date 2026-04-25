using System.Collections.Immutable;
using Precept.Language;

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
                switch (decl)
                {
                    case RuleDeclaration rd:
                        CheckRuleDeclaration(rd);
                        break;
                    // Pass 2: other declarations — implemented in later slices
                }
            }
        }

        private void CheckRuleDeclaration(RuleDeclaration decl)
        {
            var condition = CheckExpression(decl.Condition, new BooleanType());

            if (!IsAssignableTo(condition.Type, new BooleanType()))
                _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, condition.Span, "boolean", TypeDisplayName(condition.Type)));

            TypedExpression? guard = null;
            if (decl.Guard is not null)
            {
                guard = CheckExpression(decl.Guard, new BooleanType());
                if (!IsAssignableTo(guard.Type, new BooleanType()))
                    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, guard.Span, "boolean", TypeDisplayName(guard.Type)));
            }

            var message = CheckExpression(decl.Message, new StringType());
            if (!IsAssignableTo(message.Type, new StringType()))
                _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, message.Span, "string", TypeDisplayName(message.Type)));

            _rules.Add(new ResolvedRule(condition, guard, message, decl.Span));
        }

        private TypedExpression CheckExpression(Expression expr, ResolvedType? expectedType)
        {
            if (expr.IsMissing)
                return new TypedExpression(expr, new ErrorType(), expr.Span);

            return expr switch
            {
                IdentifierExpression id            => CheckIdentifier(id),
                BooleanLiteralExpression b         => new TypedExpression(b, new BooleanType(), b.Span),
                StringLiteralExpression s          => new TypedExpression(s, new StringType(), s.Span),
                ParenthesizedExpression p          => CheckExpression(p.Inner, expectedType),
                InterpolatedStringExpression i     => CheckInterpolatedString(i),
                NumberLiteralExpression n          => CheckNumberLiteral(n, expectedType),
                BinaryExpression bi                => CheckBinaryExpression(bi, expectedType),
                // All other expression types → ErrorType stub for later slices
                _                                  => new TypedExpression(expr, new ErrorType(), expr.Span)
            };
        }

        private TypedExpression CheckNumberLiteral(NumberLiteralExpression expr, ResolvedType? expectedType)
        {
            var text = expr.Value.Text;

            // Classify the literal shape
            bool hasDot        = text.Contains('.');
            bool hasExponent   = text.Contains('e') || text.Contains('E');

            if (expectedType is null)
            {
                _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                    "numeric type", "cannot determine numeric type from context"));
                return new TypedExpression(expr, new ErrorType(), expr.Span);
            }

            // Not a numeric expected type — return ErrorType; caller handles the mismatch report
            if (expectedType is not (IntegerType or DecimalType or NumberType))
                return new TypedExpression(expr, new ErrorType(), expr.Span);

            if (hasExponent)
            {
                // Scientific notation → valid only for number
                if (expectedType is not NumberType)
                {
                    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                        TypeDisplayName(expectedType), "number"));
                    return new TypedExpression(expr, new ErrorType(), expr.Span);
                }
                return new TypedExpression(expr, new NumberType(), expr.Span);
            }

            if (hasDot)
            {
                // Fractional literal → valid for decimal or number, not integer
                if (expectedType is IntegerType)
                {
                    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                        "integer", "decimal"));
                    return new TypedExpression(expr, new ErrorType(), expr.Span);
                }
                return new TypedExpression(expr, expectedType, expr.Span);
            }

            // Whole number literal → valid for integer, decimal, or number
            return new TypedExpression(expr, expectedType, expr.Span);
        }

        private TypedExpression CheckBinaryExpression(BinaryExpression expr, ResolvedType? expectedType)
        {
            // TODO: propagate peer type as context per type-checker.md §4.2a
            // (e.g. IntegerField + 42 should resolve 42 as integer from the peer's type)
            var left  = CheckExpression(expr.Left,  null);
            var right = CheckExpression(expr.Right, null);

            var resultType = OperatorTable.ResolveBinary(expr.Op, left.Type, right.Type);
            if (resultType is null)
            {
                _diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                    TypeDisplayName(left.Type), BinaryOpDisplayName(expr.Op) + " on " + TypeDisplayName(right.Type)));
                return new TypedExpression(expr, new ErrorType(), expr.Span);
            }

            return new TypedExpression(expr, resultType, expr.Span);
        }

        // TODO(Slice 6): add display names for comparison, logical, and string operators
        private static string BinaryOpDisplayName(BinaryOp op) => op switch
        {
            BinaryOp.Plus    => "+",
            BinaryOp.Minus   => "-",
            BinaryOp.Star    => "*",
            BinaryOp.Slash   => "/",
            BinaryOp.Percent => "%",
            _                => op.ToString()
        };

        private TypedExpression CheckIdentifier(IdentifierExpression id)
        {
            if (_fields.TryGetValue(id.Name.Text, out var field))
                return new TypedExpression(id, field.Type, id.Span);

            _diagnostics.Add(Diagnostics.Create(DiagnosticCode.UndeclaredField, id.Span, id.Name.Text));
            return new TypedExpression(id, new ErrorType(), id.Span);
        }

        private TypedExpression CheckInterpolatedString(InterpolatedStringExpression expr)
        {
            foreach (var segment in expr.Segments)
            {
                if (segment is ExpressionSegment es)
                {
                    var typed = CheckExpression(es.Inner, null);
                    if (typed.Type is SetType or QueueType or StackType)
                        _diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidInterpolationCoercion, typed.Span, TypeDisplayName(typed.Type)));
                }
            }
            return new TypedExpression(expr, new StringType(), expr.Span);
        }

        private static bool IsAssignableTo(ResolvedType source, ResolvedType target)
        {
            if (source is ErrorType || target is ErrorType) return true;
            if (source == target) return true;
            // Numeric widening: integer → decimal, integer → number
            if (source is IntegerType && target is DecimalType) return true;
            if (source is IntegerType && target is NumberType)  return true;
            return false;
        }

        private static string TypeDisplayName(ResolvedType type) => type switch
        {
            StringType    => "string",
            BooleanType   => "boolean",
            IntegerType   => "integer",
            DecimalType   => "decimal",
            NumberType    => "number",
            ErrorType     => "error",
            SetType st    => $"set<{TypeDisplayName(st.ElementType)}>",
            QueueType qt  => $"queue<{TypeDisplayName(qt.ElementType)}>",
            StackType st  => $"stack<{TypeDisplayName(st.ElementType)}>",
            _             => type.GetType().Name
        };

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
