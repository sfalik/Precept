using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Type checker — resolves names, types, expressions, and structural constraints from the
/// parsed <see cref="ConstructManifest"/> + <see cref="SymbolTable"/> and produces a
/// <see cref="SemanticIndex"/>.
/// </summary>
/// <remarks>
/// Implementation is staged across Slices 1–10. Slice 1 (typed symbol population) is live.
/// Remaining private methods throw <see cref="NotImplementedException"/> until their owning slice lands.
/// </remarks>
internal static class TypeChecker
{
    /// <summary>
    /// Entry point: type-check <paramref name="manifest"/> using pre-resolved
    /// <paramref name="symbols"/> and return a <see cref="SemanticIndex"/>.
    /// </summary>
    internal static SemanticIndex Check(ConstructManifest manifest, SymbolTable symbols)
    {
        var ctx = new CheckContext();

        // Pass 1: populate typed symbols from SymbolTable declarations
        PopulateFields(symbols, ctx);
        PopulateStates(symbols, ctx);
        PopulateEvents(symbols, ctx);

        // Pass 2 + final assembly deferred to Slices 2–10.
        // Return a partial SemanticIndex with the symbol tables populated.
        return BuildPartialSemanticIndex(ctx);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 1 — typed symbol population (Slice 1)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a <see cref="ParsedTypeReference"/> to a <see cref="TypeKind"/> using the
    /// Types catalog. For collections, also resolves the inner element type and key type.
    /// Returns <see cref="TypeKind.Error"/> for <see cref="MissingTypeReference"/>.
    /// </summary>
    private static (TypeKind Type, TypeKind? ElementType, TypeKind? KeyType) ResolveTypeKind(ParsedTypeReference typeRef) =>
        typeRef switch
        {
            SimpleTypeReference simple => (simple.Type.Kind, null, null),
            CollectionTypeReference coll => (
                coll.CollectionType.Kind,
                ResolveTypeKind(coll.ElementType).Type,
                coll.KeyType is not null ? ResolveTypeKind(coll.KeyType).Type : null),
            ChoiceTypeReference choice => (choice.Type.Kind, null, null),
            CITypeReference ci => (ci.Type.Kind, null, null),
            MissingTypeReference => (TypeKind.Error, null, null),
            _ => (TypeKind.Error, null, null),
        };

    /// <summary>
    /// Populate <see cref="CheckContext.Fields"/> from <see cref="SymbolTable.Fields"/>.
    /// Resolves <see cref="TypeKind"/>, extracts declared modifiers, computes implied modifiers
    /// from the Types catalog (D3), and builds <see cref="TypedField"/> records.
    /// </summary>
    private static void PopulateFields(SymbolTable symbols, CheckContext ctx)
    {
        foreach (var declared in symbols.Fields)
        {
            var (resolvedType, elementType, keyType) = ResolveTypeKind(declared.Type);

            // Declared modifiers: extract ModifierKind values from ParsedModifier list
            var modifiers = declared.Modifiers
                .Select(m => m.Kind)
                .ToImmutableArray();

            // Implied modifiers from the Types catalog (D3: catalog-driven, no inline logic)
            var impliedModifiers = resolvedType != TypeKind.Error
                ? Types.GetMeta(resolvedType).ImpliedModifiers.ToImmutableArray()
                : ImmutableArray<ModifierKind>.Empty;

            bool isOptional = modifiers.Contains(ModifierKind.Optional);
            bool isWritable = modifiers.Contains(ModifierKind.Writable);

            var typedField = new TypedField(
                Name: declared.Name,
                ResolvedType: resolvedType,
                ElementType: elementType,
                KeyType: keyType,
                Modifiers: modifiers,
                ImpliedModifiers: impliedModifiers,
                DefaultExpression: null,   // Slice 2+
                ComputedExpression: null,  // Slice 2+
                Qualifier: null,           // Slice 2+
                IsComputed: declared.IsComputed,
                IsOptional: isOptional,
                IsWritable: isWritable,
                Syntax: declared.Syntax);

            ctx.Fields.Add(typedField);
            ctx.FieldLookup[declared.Name] = typedField;

            // Diagnostic for unknown type (MissingTypeReference → TypeKind.Error).
            // The parser already emits a diagnostic for the missing type token;
            // the checker emits TypeMismatch to surface the field-level impact.
            if (resolvedType == TypeKind.Error)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.TypeMismatch, declared.NameSpan, "type", declared.Name));
            }
        }
    }

    /// <summary>
    /// Populate <see cref="CheckContext.States"/> from <see cref="SymbolTable.States"/>.
    /// Resolves modifier flags and validates initial/terminal state counts (D7).
    /// </summary>
    private static void PopulateStates(SymbolTable symbols, CheckContext ctx)
    {
        string? firstInitialStateName = null;
        SourceSpan firstInitialSpan = default;

        foreach (var declared in symbols.States)
        {
            var typedState = new TypedState(
                Name: declared.Name,
                Modifiers: declared.Modifiers,
                Syntax: declared.Syntax);

            ctx.States.Add(typedState);
            ctx.StateLookup[declared.Name] = typedState;

            // Track initial state for count validation
            if (declared.Modifiers.Contains(ModifierKind.InitialState))
            {
                if (firstInitialStateName is null)
                {
                    firstInitialStateName = declared.Name;
                    firstInitialSpan = declared.NameSpan;
                }
                else
                {
                    // D7: Multiple initial states → diagnostic
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.MultipleInitialStates, declared.NameSpan,
                            firstInitialStateName, declared.Name));
                }
            }
        }

        // D7: Zero initial states on a stateful precept → diagnostic
        // A stateless precept (no states at all) is valid; only fire when states exist but none is initial.
        if (symbols.States.Length > 0 && firstInitialStateName is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.NoInitialState, symbols.States[0].NameSpan));
        }
    }

    /// <summary>
    /// Populate <see cref="CheckContext.Events"/> from <see cref="SymbolTable.Events"/>.
    /// Resolves event argument types via the Types catalog.
    /// </summary>
    private static void PopulateEvents(SymbolTable symbols, CheckContext ctx)
    {
        foreach (var declared in symbols.Events)
        {
            var typedArgs = declared.Args
                .Select(arg => new TypedArg(
                    Name: arg.Name,
                    EventName: arg.EventName,
                    ResolvedType: arg.Type.Kind,
                    ElementType: null, // event arg element types deferred until arg type parsing is richer
                    Modifiers: arg.Modifiers,
                    DefaultExpression: null, // Slice 2+
                    IsOptional: arg.Modifiers.Contains(ModifierKind.Optional),
                    Span: arg.NameSpan))
                .ToImmutableArray();

            var typedEvent = new TypedEvent(
                Name: declared.Name,
                Args: typedArgs,
                IsInitial: declared.IsInitial,
                Syntax: declared.Syntax);

            ctx.Events.Add(typedEvent);
            ctx.EventLookup[declared.Name] = typedEvent;
        }
    }

    /// <summary>
    /// Build a partial <see cref="SemanticIndex"/> from the populated <see cref="CheckContext"/>.
    /// Only symbol tables (Fields, States, Events) and diagnostics are populated;
    /// normalized declarations and dependency facts are empty pending Slices 2–10.
    /// </summary>
    private static SemanticIndex BuildPartialSemanticIndex(CheckContext ctx)
    {
        var fields = ctx.Fields.ToImmutableArray();
        var states = ctx.States.ToImmutableArray();
        var events = ctx.Events.ToImmutableArray();

        return new SemanticIndex(
            Fields: fields,
            States: states,
            Events: events,
            FieldsByName: fields.ToFrozenDictionary(f => f.Name),
            StatesByName: states.ToFrozenDictionary(s => s.Name),
            EventsByName: events.ToFrozenDictionary(e => e.Name),
            TransitionRows: ImmutableArray<TypedTransitionRow>.Empty,
            Rules: ImmutableArray<TypedRule>.Empty,
            Ensures: ImmutableArray<TypedEnsure>.Empty,
            AccessModes: ImmutableArray<TypedAccessMode>.Empty,
            StateHooks: ImmutableArray<TypedStateHook>.Empty,
            EventHandlers: ImmutableArray<TypedEventHandler>.Empty,
            EditDeclarations: ImmutableArray<TypedEditDeclaration>.Empty,
            EnsuresByState: FrozenDictionary<string, ImmutableArray<TypedEnsure>>.Empty,
            ComputedDeps: ImmutableArray<ComputedFieldDep>.Empty,
            ConstraintRefs: ImmutableArray<ConstraintFieldRefs>.Empty,
            FieldReferences: ImmutableArray<FieldReference>.Empty,
            StateReferences: ImmutableArray<StateReference>.Empty,
            EventReferences: ImmutableArray<EventReference>.Empty,
            Diagnostics: ctx.Diagnostics.ToImmutableArray());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Test entry points (InternalsVisibleTo — Precept.Tests)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a <see cref="CheckContext"/> with Pass 1 symbols populated.
    /// Used by tests to build a resolution context without going through Check().
    /// </summary>
    internal static CheckContext CreateContext(ConstructManifest manifest, SymbolTable symbols)
    {
        var ctx = new CheckContext();
        PopulateFields(symbols, ctx);
        PopulateStates(symbols, ctx);
        PopulateEvents(symbols, ctx);
        return ctx;
    }

    /// <summary>
    /// Resolves a single <see cref="ParsedExpression"/> in the given context.
    /// Thin wrapper over the private <see cref="Resolve"/> for test access.
    /// </summary>
    internal static TypedExpression ResolveExpression(ParsedExpression expr, CheckContext ctx) =>
        Resolve(expr, ctx);

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2 stubs — declaration normalization (Slices 2–9)
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    //  Expression resolution (Slice 2)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a <see cref="ParsedExpression"/> node to a <see cref="TypedExpression"/>.
    /// Dispatches on expression form, resolves types via catalogs, and propagates
    /// <see cref="TypedErrorExpression"/> on failure (D13).
    /// </summary>
    private static TypedExpression Resolve(ParsedExpression expr, CheckContext ctx) => expr switch
    {
        // ── Missing sentinel → error (no diagnostic — parser already emitted one) ──
        MissingExpression m => new TypedErrorExpression(m.Span),

        // ── Literal ──
        LiteralExpression lit => ResolveLiteral(lit),

        // ── Identifier (field, arg, or quantifier binding) ──
        IdentifierExpression id => ResolveIdentifier(id, ctx),

        // ── Grouped (parenthesized) — unwrap and resolve inner ──
        GroupedExpression grp => Resolve(grp.Inner, ctx),

        // ── Binary operation ──
        BinaryOperationExpression bin => ResolveBinaryOp(bin, ctx),

        // ── Unary operation ──
        UnaryOperationExpression un => ResolveUnaryOp(un, ctx),

        // ── Slice 3: functions, accessors, method calls, interpolated strings ──
        FunctionCallExpression func         => ResolveFunctionCall(func, ctx),
        CIFunctionCallExpression ciFunc     => ResolveCIFunctionCall(ciFunc, ctx),
        MemberAccessExpression mem          => ResolveMemberAccess(mem, ctx),
        MethodCallExpression meth           => ResolveMethodCall(meth, ctx),
        InterpolatedStringExpression interp => ResolveInterpolatedString(interp, ctx),

        // ── Stub arms: return TypedErrorExpression with no diagnostic (Slices 4–9) ──
        ConditionalExpression     => new TypedErrorExpression(expr.Span),
        QuantifierExpression      => new TypedErrorExpression(expr.Span),
        ListLiteralExpression     => new TypedErrorExpression(expr.Span),
        PostfixOperationExpression => new TypedErrorExpression(expr.Span),

        _ => new TypedErrorExpression(expr.Span),
    };

    /// <summary>
    /// Resolve a literal expression to a <see cref="TypedLiteral"/> with the appropriate
    /// <see cref="TypeKind"/> and parsed value.
    /// </summary>
    private static TypedExpression ResolveLiteral(LiteralExpression lit) => lit.LiteralKind switch
    {
        TokenKind.StringLiteral => new TypedLiteral(TypeKind.String, lit.Text, lit.Span),
        TokenKind.True          => new TypedLiteral(TypeKind.Boolean, true, lit.Span),
        TokenKind.False         => new TypedLiteral(TypeKind.Boolean, false, lit.Span),
        TokenKind.NumberLiteral => ResolveNumericLiteral(lit),

        // Typed constants are Slice 4 stubs
        TokenKind.TypedConstant      => new TypedErrorExpression(lit.Span),
        TokenKind.TypedConstantStart => new TypedErrorExpression(lit.Span),

        _ => new TypedErrorExpression(lit.Span),
    };

    /// <summary>
    /// Resolve a numeric literal to integer or decimal based on the presence of a decimal point.
    /// Bottom-up resolution only — context retry for widening is Slice 4.
    /// </summary>
    private static TypedLiteral ResolveNumericLiteral(LiteralExpression lit)
    {
        if (lit.Text.Contains('.'))
        {
            _ = decimal.TryParse(lit.Text, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var decVal);
            return new TypedLiteral(TypeKind.Decimal, decVal, lit.Span);
        }

        _ = long.TryParse(lit.Text, System.Globalization.CultureInfo.InvariantCulture, out var intVal);
        return new TypedLiteral(TypeKind.Integer, intVal, lit.Span);
    }

    /// <summary>
    /// Resolve an identifier to a field reference, event arg reference, or quantifier binding.
    /// Priority (D20): quantifier bindings > event args > fields.
    /// Forward-reference prohibition (D8) applies when <see cref="CheckContext.CurrentScope"/>
    /// is <see cref="FieldScopeMode.PriorFieldsOnly"/>.
    /// </summary>
    private static TypedExpression ResolveIdentifier(IdentifierExpression id, CheckContext ctx)
    {
        var name = id.Name;

        // 1. Quantifier bindings (innermost scope, highest priority)
        foreach (var binding in ctx.QuantifierBindings)
        {
            if (string.Equals(binding.Name, name, StringComparison.Ordinal))
                return new TypedFieldRef(binding.Type, name, false, id.Span);
        }

        // 2. Event args (second priority)
        if (ctx.CurrentEventArgs is not null &&
            ctx.CurrentEventArgs.TryGetValue(name, out var arg))
        {
            return new TypedArgRef(arg.ResolvedType, arg.EventName, arg.Name, id.Span);
        }

        // 3. Fields (lowest priority)
        if (ctx.FieldLookup.TryGetValue(name, out var field))
        {
            // D8: Forward-reference prohibition in PriorFieldsOnly scope
            if (ctx.CurrentScope == FieldScopeMode.PriorFieldsOnly)
            {
                int fieldIndex = ctx.Fields.IndexOf(field);
                if (fieldIndex >= ctx.CurrentFieldIndex)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.DefaultForwardReference, id.Span,
                            ctx.Fields[ctx.CurrentFieldIndex].Name, name));
                    return new TypedErrorExpression(id.Span);
                }
            }

            // Record field reference site for LS navigation
            ctx.FieldReferences.Add(new FieldReference(field, id.Span));

            return new TypedFieldRef(field.ResolvedType, field.Name, false, id.Span);
        }

        // Unknown identifier
        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.UndeclaredField, id.Span, name));
        return new TypedErrorExpression(id.Span);
    }

    /// <summary>
    /// Resolve a binary operation expression. Resolves both operands, propagates ErrorType (D13),
    /// then performs catalog lookup via <see cref="Operations.FindCandidates"/> with widening
    /// fallback (D16) and qualifier disambiguation (D9).
    /// </summary>
    private static TypedExpression ResolveBinaryOp(BinaryOperationExpression bin, CheckContext ctx)
    {
        var left = Resolve(bin.Left, ctx);
        var right = Resolve(bin.Right, ctx);

        // D13: ErrorType propagation — if either operand is error, propagate
        if (left is TypedErrorExpression || right is TypedErrorExpression)
            return new TypedErrorExpression(bin.Span);

        // Map TokenKind → OperatorKind via the Operators catalog
        if (!Operators.ByToken.TryGetValue((bin.Operator, Arity.Binary), out var opMeta))
            return new TypedErrorExpression(bin.Span);

        var opKind = opMeta.Kind;

        // Attempt resolution: exact → left widen → right widen → both widen
        var result = TryResolveBinaryWithWidening(opKind, left.ResultType, right.ResultType);

        if (result is not null)
        {
            return new TypedBinaryOp(
                result.Result,
                result.Kind,
                left, right,
                ResultQualifier: MapQualifierBinding(result),
                ProofRequirements: result.ProofRequirements.ToImmutableArray(),
                Span: bin.Span);
        }

        // No match at any widening level
        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.TypeMismatch, bin.Span,
                Types.GetMeta(left.ResultType).DisplayName,
                Types.GetMeta(right.ResultType).DisplayName));
        return new TypedErrorExpression(bin.Span);
    }

    /// <summary>
    /// Try to resolve a binary operation with the 4-level widening fallback algorithm (D16/§7.3).
    /// Returns the first matching <see cref="BinaryOperationMeta"/>, or null if no match at any level.
    /// </summary>
    private static BinaryOperationMeta? TryResolveBinaryWithWidening(
        OperatorKind op, TypeKind lhsType, TypeKind rhsType)
    {
        // Level 1: Exact match (no widening)
        var exact = DisambiguateCandidates(Operations.FindCandidates(op, lhsType, rhsType));
        if (exact is not null) return exact;

        // Level 2: Left widening only
        foreach (var lwt in Types.GetMeta(lhsType).WidensTo)
        {
            var match = DisambiguateCandidates(Operations.FindCandidates(op, lwt, rhsType));
            if (match is not null) return match;
        }

        // Level 3: Right widening only
        foreach (var rwt in Types.GetMeta(rhsType).WidensTo)
        {
            var match = DisambiguateCandidates(Operations.FindCandidates(op, lhsType, rwt));
            if (match is not null) return match;
        }

        // Level 4: Both widening
        foreach (var lwt in Types.GetMeta(lhsType).WidensTo)
        {
            foreach (var rwt in Types.GetMeta(rhsType).WidensTo)
            {
                var match = DisambiguateCandidates(Operations.FindCandidates(op, lwt, rwt));
                if (match is not null) return match;
            }
        }

        return null;
    }

    /// <summary>
    /// Disambiguate binary operation candidates using qualifier matching (D9/§7.3).
    /// Returns a single <see cref="BinaryOperationMeta"/> if unambiguous, or null if no candidates.
    /// For multi-candidate results (qualifier-disambiguated operations), selects the
    /// <see cref="QualifierMatch.Same"/> entry by default — the checker assumes same-qualifier
    /// until runtime qualifier values prove otherwise. The ProofEngine adds obligations to verify.
    /// </summary>
    private static BinaryOperationMeta? DisambiguateCandidates(ReadOnlySpan<BinaryOperationMeta> candidates)
    {
        if (candidates.Length == 0) return null;
        if (candidates.Length == 1) return candidates[0];

        // Multi-candidate: qualifier disambiguation.
        // Default to QualifierMatch.Same — the structurally safe assumption.
        // ProofEngine will verify qualifier compatibility at deeper analysis.
        foreach (var c in candidates)
        {
            if (c.Match == QualifierMatch.Same) return c;
        }

        // Fallback: return first candidate if no Same entry exists
        return candidates[0];
    }

    /// <summary>
    /// Map a <see cref="BinaryOperationMeta"/>'s qualifier match to the corresponding
    /// <see cref="QualifierBinding"/> for the typed expression result.
    /// </summary>
    private static QualifierBinding? MapQualifierBinding(BinaryOperationMeta meta) => meta.Match switch
    {
        QualifierMatch.Same      => new SameQualifierRequired(),
        QualifierMatch.Different => null, // different-qualifier operations produce unqualified results
        _                        => null, // QualifierMatch.Any — no qualifier constraint
    };

    /// <summary>
    /// Resolve a unary operation expression. Resolves the operand, propagates ErrorType (D13),
    /// then performs catalog lookup via <see cref="Operations.FindUnary"/>.
    /// </summary>
    private static TypedExpression ResolveUnaryOp(UnaryOperationExpression un, CheckContext ctx)
    {
        var operand = Resolve(un.Operand, ctx);

        // D13: ErrorType propagation
        if (operand is TypedErrorExpression)
            return new TypedErrorExpression(un.Span);

        // Map TokenKind → OperatorKind via the Operators catalog
        if (!Operators.ByToken.TryGetValue((un.Operator, Arity.Unary), out var opMeta))
            return new TypedErrorExpression(un.Span);

        var resolved = Operations.FindUnary(opMeta.Kind, operand.ResultType);
        if (resolved is not null)
        {
            return new TypedUnaryOp(
                resolved.Result,
                resolved.Kind,
                operand,
                un.Span);
        }

        // No matching unary operation
        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.TypeMismatch, un.Span,
                Types.GetMeta(operand.ResultType).DisplayName, opMeta.Kind.ToString()));
        return new TypedErrorExpression(un.Span);
    }

    /// <summary>Normalize a transition row construct into a <see cref="TypedTransitionRow"/>.</summary>
    private static TypedTransitionRow NormalizeTransitionRow(ParsedConstruct construct, CheckContext ctx) =>
        throw new NotImplementedException("Slice 5");

    /// <summary>Normalize an event handler construct into a <see cref="TypedEventHandler"/>.</summary>
    private static TypedEventHandler NormalizeEventHandler(ParsedConstruct construct, CheckContext ctx) =>
        throw new NotImplementedException("Slice 5");

    /// <summary>Resolve a quantifier expression arm (push/pop binding stack).</summary>
    private static TypedExpression ResolveQuantifier(QuantifierExpression expr, CheckContext ctx) =>
        throw new NotImplementedException("Slice 9");

    // ════════════════════════════════════════════════════════════════════════
    //  Expression resolution — Slice 3: Functions, Accessors, Interpolated Strings
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check whether <paramref name="source"/> is assignable to <paramref name="target"/>
    /// via identity or single-hop widening from <see cref="TypeMeta.WidensTo"/>.
    /// ErrorType is assignable to anything (suppresses cascading diagnostics).
    /// </summary>
    private static bool IsAssignable(TypeKind source, TypeKind target)
    {
        if (source == target) return true;
        if (source == TypeKind.Error || target == TypeKind.Error) return true;
        return Types.GetMeta(source).WidensTo.Contains(target);
    }

    /// <summary>
    /// Resolve a function call expression using the Functions catalog overload resolution algorithm.
    /// Looks up <see cref="Functions.FindByName"/>, resolves args, selects best overload via
    /// arity filter → exact → widened scoring.
    /// </summary>
    private static TypedExpression ResolveFunctionCall(FunctionCallExpression expr, CheckContext ctx)
    {
        var candidates = Functions.FindByName(expr.FunctionName);
        if (candidates.Length == 0)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UndeclaredFunction, expr.Span, expr.FunctionName));
            return new TypedErrorExpression(expr.Span);
        }

        var resolvedArgs = expr.Arguments.Select(a => Resolve(a, ctx)).ToImmutableArray();
        if (resolvedArgs.Any(a => a is TypedErrorExpression))
            return new TypedErrorExpression(expr.Span);

        return SelectOverload(candidates, resolvedArgs, expr.FunctionName, expr.Span, ctx);
    }

    /// <summary>
    /// Resolve a case-insensitive function call expression. The parser produces
    /// <see cref="CIFunctionCallExpression"/> with the name sans tilde prefix;
    /// the CI variant is looked up via <c>"~" + name</c> in <see cref="Functions.ByName"/>.
    /// </summary>
    private static TypedExpression ResolveCIFunctionCall(CIFunctionCallExpression expr, CheckContext ctx)
    {
        var ciName = "~" + expr.FunctionName;
        var candidates = Functions.FindByName(ciName);
        if (candidates.Length == 0)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UndeclaredFunction, expr.Span, ciName));
            return new TypedErrorExpression(expr.Span);
        }

        var resolvedArgs = expr.Arguments.Select(a => Resolve(a, ctx)).ToImmutableArray();
        if (resolvedArgs.Any(a => a is TypedErrorExpression))
            return new TypedErrorExpression(expr.Span);

        return SelectOverload(candidates, resolvedArgs, ciName, expr.Span, ctx);
    }

    /// <summary>
    /// Select the best overload across all <paramref name="candidates"/> for the given resolved args.
    /// Arity filter → exact match (score 0) → widened match (score = widen count) → error.
    /// </summary>
    private static TypedExpression SelectOverload(
        ReadOnlySpan<FunctionMeta> candidates,
        ImmutableArray<TypedExpression> resolvedArgs,
        string functionName,
        SourceSpan span,
        CheckContext ctx)
    {
        FunctionKind? bestKind = null;
        FunctionOverload? bestOverload = null;
        int bestScore = int.MaxValue;

        foreach (var meta in candidates)
        {
            foreach (var overload in meta.Overloads)
            {
                if (overload.Parameters.Count != resolvedArgs.Length) continue;

                int score = 0;
                bool valid = true;
                for (int i = 0; i < resolvedArgs.Length; i++)
                {
                    var argType = resolvedArgs[i].ResultType;
                    var paramType = overload.Parameters[i].Kind;
                    if (argType == paramType) continue;
                    if (IsAssignable(argType, paramType))
                        score++;
                    else
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid && score < bestScore)
                {
                    bestScore = score;
                    bestKind = meta.Kind;
                    bestOverload = overload;
                    if (score == 0) goto selected;
                }
            }
            if (bestScore == 0) goto selected;
        }

    selected:
        if (bestOverload is not null)
        {
            return new TypedFunctionCall(
                bestOverload.ReturnType,
                bestKind!.Value,
                resolvedArgs,
                bestOverload.ProofRequirements.ToImmutableArray(),
                span);
        }

        // No matching overload — determine arity vs type mismatch for diagnostic
        bool anyArityMatch = false;
        foreach (var meta in candidates)
            foreach (var overload in meta.Overloads)
                if (overload.Parameters.Count == resolvedArgs.Length)
                    anyArityMatch = true;

        if (!anyArityMatch)
        {
            var arities = new HashSet<int>();
            foreach (var meta in candidates)
                foreach (var overload in meta.Overloads)
                    arities.Add(overload.Parameters.Count);
            var expected = string.Join(" or ", arities.OrderBy(x => x));
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.FunctionArityMismatch, span,
                    functionName, expected, resolvedArgs.Length.ToString()));
        }
        else
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.TypeMismatch, span,
                    functionName,
                    string.Join(", ", resolvedArgs.Select(a => Types.GetMeta(a.ResultType).DisplayName))));
        }

        return new TypedErrorExpression(span);
    }

    /// <summary>
    /// Resolve a member access expression (property-style dot access).
    /// Looks up the accessor in <see cref="TypeMeta.Accessors"/> for the receiver's type.
    /// </summary>
    private static TypedExpression ResolveMemberAccess(MemberAccessExpression expr, CheckContext ctx)
    {
        var receiver = Resolve(expr.Target, ctx);
        if (receiver is TypedErrorExpression)
            return new TypedErrorExpression(expr.Span);

        var typeMeta = Types.GetMeta(receiver.ResultType);
        var accessor = typeMeta.Accessors.FirstOrDefault(a =>
            string.Equals(a.Name, expr.MemberName, StringComparison.Ordinal));

        if (accessor is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidMemberAccess, expr.Span,
                    expr.MemberName, typeMeta.DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        var returnType = ResolveAccessorReturnType(accessor, receiver, ctx);
        if (returnType == TypeKind.Error)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidMemberAccess, expr.Span,
                    expr.MemberName, typeMeta.DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        return new TypedMemberAccess(
            returnType,
            receiver,
            accessor,
            accessor.ProofRequirements.ToImmutableArray(),
            expr.Span);
    }

    /// <summary>
    /// Resolve a method call expression (dot access with arguments).
    /// Same accessor lookup as <see cref="ResolveMemberAccess"/> plus argument validation.
    /// </summary>
    private static TypedExpression ResolveMethodCall(MethodCallExpression expr, CheckContext ctx)
    {
        var receiver = Resolve(expr.Target, ctx);
        if (receiver is TypedErrorExpression)
            return new TypedErrorExpression(expr.Span);

        var typeMeta = Types.GetMeta(receiver.ResultType);
        var accessor = typeMeta.Accessors.FirstOrDefault(a =>
            string.Equals(a.Name, expr.MethodName, StringComparison.Ordinal));

        if (accessor is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidMemberAccess, expr.Span,
                    expr.MethodName, typeMeta.DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        // Resolve arguments and propagate errors
        var resolvedArgs = expr.Arguments.Select(a => Resolve(a, ctx)).ToImmutableArray();
        if (resolvedArgs.Any(a => a is TypedErrorExpression))
            return new TypedErrorExpression(expr.Span);

        // Determine expected parameter type from accessor DU subtype
        TypeKind? expectedParamType = accessor switch
        {
            FixedReturnAccessor f     => f.ParameterType,
            ElementParameterAccessor  => GetElementType(receiver, ctx),
            _                         => accessor.ParameterType,
        };

        // Validate argument count and type
        if (expectedParamType is not null)
        {
            if (resolvedArgs.Length != 1)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.FunctionArityMismatch, expr.Span,
                        expr.MethodName, "1", resolvedArgs.Length.ToString()));
                return new TypedErrorExpression(expr.Span);
            }
            if (!IsAssignable(resolvedArgs[0].ResultType, expectedParamType.Value))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                        Types.GetMeta(expectedParamType.Value).DisplayName,
                        Types.GetMeta(resolvedArgs[0].ResultType).DisplayName));
                return new TypedErrorExpression(expr.Span);
            }
        }

        var returnType = ResolveAccessorReturnType(accessor, receiver, ctx);
        if (returnType == TypeKind.Error)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidMemberAccess, expr.Span,
                    expr.MethodName, typeMeta.DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        return new TypedMemberAccess(
            returnType,
            receiver,
            accessor,
            accessor.ProofRequirements.ToImmutableArray(),
            expr.Span);
    }

    /// <summary>
    /// Determine the return type of an accessor based on its DU subtype.
    /// Base <see cref="TypeAccessor"/>: returns element type of owning collection.
    /// <see cref="FixedReturnAccessor"/>: returns <see cref="FixedReturnAccessor.Returns"/>.
    /// <see cref="ElementParameterAccessor"/>: returns <see cref="TypeKind.Integer"/>.
    /// </summary>
    private static TypeKind ResolveAccessorReturnType(TypeAccessor accessor, TypedExpression receiver, CheckContext ctx) =>
        accessor switch
        {
            FixedReturnAccessor f     => f.Returns,
            ElementParameterAccessor  => TypeKind.Integer,
            _                         => GetElementType(receiver, ctx) ?? TypeKind.Error,
        };

    /// <summary>
    /// Extract the element type from a receiver expression. For <see cref="TypedFieldRef"/>,
    /// looks up the field in <see cref="CheckContext.FieldLookup"/>.
    /// Returns null if element type cannot be determined.
    /// </summary>
    private static TypeKind? GetElementType(TypedExpression receiver, CheckContext ctx)
    {
        if (receiver is TypedFieldRef fieldRef &&
            ctx.FieldLookup.TryGetValue(fieldRef.FieldName, out var field))
            return field.ElementType;
        return null;
    }

    /// <summary>
    /// Resolve an interpolated string expression. Resolves each hole segment's expression.
    /// ErrorType propagation: if any hole is error, the entire string is <see cref="TypedErrorExpression"/>.
    /// Result type is always <see cref="TypeKind.String"/>.
    /// </summary>
    private static TypedExpression ResolveInterpolatedString(InterpolatedStringExpression expr, CheckContext ctx)
    {
        var segments = ImmutableArray.CreateBuilder<TypedInterpolationSegment>(expr.Segments.Length);
        bool hasError = false;

        foreach (var segment in expr.Segments)
        {
            switch (segment)
            {
                case TextSegment text:
                    segments.Add(new TypedTextSegment(text.Text, text.Span));
                    break;
                case HoleSegment hole:
                    var resolved = Resolve(hole.Expression, ctx);
                    if (resolved is TypedErrorExpression)
                        hasError = true;
                    segments.Add(new TypedHoleSegment(resolved, hole.Span));
                    break;
            }
        }

        if (hasError)
            return new TypedErrorExpression(expr.Span);

        return new TypedInterpolatedString(segments.ToImmutable(), expr.Span);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2 stubs — structural validation (Slices 6–8)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Validate modifier applicability, conflicts, and subsumption for all fields and states.</summary>
    private static void ValidateModifiers(CheckContext ctx) =>
        throw new NotImplementedException("Slice 7");

    /// <summary>
    /// Structural validation sub-pass: computed-field cycle detection, choice validation,
    /// forward-reference prohibition, stateless/stateful cross-validation.
    /// </summary>
    private static void ValidateStructural(CheckContext ctx) =>
        throw new NotImplementedException("Slice 6");

    /// <summary>CI enforcement sub-pass: validate ~string usage on CI functions and operators.</summary>
    private static void ValidateCIEnforcement(CheckContext ctx) =>
        throw new NotImplementedException("Slice 8");

    // ════════════════════════════════════════════════════════════════════════
    //  Final assembly stub (Slice 10)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transform <see cref="CheckContext"/> mutable accumulators into the immutable <see cref="SemanticIndex"/>.
    /// Derives frozen-dictionary secondary indexes from primary arrays.
    /// </summary>
    private static SemanticIndex BuildSemanticIndex(CheckContext ctx) =>
        throw new NotImplementedException("Slice 10");
}
