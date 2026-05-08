using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using NodaTime;
using NodaTime.Text;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Type checker — resolves names, types, expressions, and structural constraints from the
/// parsed <see cref="ConstructManifest"/> + <see cref="SymbolTable"/> and produces a
/// <see cref="SemanticIndex"/>.
/// </summary>
/// <remarks>
/// Pipeline stages: PopulateFields → PopulateStates → PopulateEvents →
/// PopulateTransitionRows → PopulateEventHandlers → PopulateRules →
/// ValidateModifiers → ValidateStructural → ValidateCIEnforcement →
/// BuildSemanticIndex (final assembly with D26 global assert).
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

        // Pass 2: normalize transition rows and event handlers (Slice 5)
        PopulateTransitionRows(manifest, ctx);
        PopulateEventHandlers(manifest, ctx);
        PopulateRules(manifest, ctx);

        // Modifier validation (Slice 7) — depends only on Pass 1 symbols
        ValidateModifiers(ctx);

        // Structural validation (Slice 6) — runs after Pass 2; reads ComputedDeps
        // (populated during expression resolution) for cycle detection.
        ValidateStructural(ctx);

        // CI enforcement (Slice 8) — runs after all expression resolution;
        // walks resolved expression trees for ~string consistency violations.
        ValidateCIEnforcement(ctx);

        return BuildSemanticIndex(ctx);
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

            // CI tracking (Slice 8): record ~string fields and ~string-element collections
            if (declared.Type is CITypeReference)
                ctx.CIFields.Add(declared.Name);
            else if (declared.Type is CollectionTypeReference { ElementType: CITypeReference })
                ctx.CIElementCollections.Add(declared.Name);

            // Choice domain validation (Slice 6): empty domain and duplicate values
            if (declared.Type is ChoiceTypeReference choiceRef)
            {
                if (choiceRef.Domain.IsEmpty)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.EmptyChoice, declared.NameSpan));
                }
                else
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var value in choiceRef.Domain)
                    {
                        if (!seen.Add(value))
                        {
                            ctx.Diagnostics.Add(
                                Diagnostics.Create(DiagnosticCode.DuplicateChoiceValue, declared.NameSpan, value));
                        }
                    }
                }
            }

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
    internal static TypedExpression ResolveExpression(ParsedExpression expr, CheckContext ctx, TypeKind? expectedType = null) =>
        Resolve(expr, ctx, expectedType);

    // ════════════════════════════════════════════════════════════════════════
    //  Expression resolution
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a <see cref="ParsedExpression"/> node to a <see cref="TypedExpression"/>.
    /// Dispatches on expression form, resolves types via catalogs, and propagates
    /// <see cref="TypedErrorExpression"/> on failure (D13).
    /// </summary>
    private static TypedExpression Resolve(ParsedExpression expr, CheckContext ctx, TypeKind? expectedType = null) => expr switch
    {
        // ── Missing sentinel → error (no diagnostic — parser already emitted one) ──
        MissingExpression m => new TypedErrorExpression(m.Span),

        // ── Literal ──
        LiteralExpression lit => ResolveLiteral(lit, ctx, expectedType),

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

        // ── Conditionals, quantifiers, lists, postfix ──
        ConditionalExpression cond => ResolveConditional(cond, ctx),
        QuantifierExpression q    => ResolveQuantifier(q, ctx),
        ListLiteralExpression l   => ResolveListLiteral(l, ctx),
        PostfixOperationExpression postfix => ResolvePostfixOp(postfix, ctx),

        _ => new TypedErrorExpression(expr.Span),
    };

    /// <summary>
    /// Resolve a literal expression to a <see cref="TypedLiteral"/> with the appropriate
    /// <see cref="TypeKind"/> and parsed value. When <paramref name="expectedType"/> is non-null
    /// and the target type has <see cref="TypeMeta.ContentValidation"/>, numeric literals are
    /// re-interpreted as the expected type and typed constants are validated.
    /// </summary>
    private static TypedExpression ResolveLiteral(LiteralExpression lit, CheckContext ctx, TypeKind? expectedType) => lit.LiteralKind switch
    {
        TokenKind.StringLiteral => new TypedLiteral(TypeKind.String, lit.Text, lit.Span),
        TokenKind.True          => new TypedLiteral(TypeKind.Boolean, true, lit.Span),
        TokenKind.False         => new TypedLiteral(TypeKind.Boolean, false, lit.Span),
        TokenKind.NumberLiteral => ResolveNumericLiteral(lit, expectedType),

        // Typed constants: resolve with content validation from expectedType context
        TokenKind.TypedConstant      => ResolveTypedConstant(lit, ctx, expectedType),
        TokenKind.TypedConstantStart => new TypedErrorExpression(lit.Span), // Interpolated typed constants deferred

        _ => new TypedErrorExpression(lit.Span),
    };

    /// <summary>
    /// Resolve a numeric literal to integer or decimal based on the presence of a decimal point.
    /// When <paramref name="expectedType"/> is a numeric type compatible via widening, the literal
    /// is resolved as that type directly (context-sensitive numeric resolution).
    /// </summary>
    private static TypedLiteral ResolveNumericLiteral(LiteralExpression lit, TypeKind? expectedType = null)
    {
        if (lit.Text.Contains('.'))
        {
            _ = decimal.TryParse(lit.Text, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var decVal);
            return new TypedLiteral(TypeKind.Decimal, decVal, lit.Span);
        }

        _ = long.TryParse(lit.Text, System.Globalization.CultureInfo.InvariantCulture, out var intVal);

        // Context-sensitive: if expectedType is a type that integer widens to, resolve as that type
        if (expectedType is not null && expectedType != TypeKind.Integer)
        {
            if (IsAssignable(TypeKind.Integer, expectedType.Value))
                return new TypedLiteral(expectedType.Value, intVal, lit.Span);
        }

        return new TypedLiteral(TypeKind.Integer, intVal, lit.Span);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Typed constant resolution (Slice 4)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a typed constant (single-quoted string literal) to a <see cref="TypedTypedConstant"/>
    /// using the <paramref name="expectedType"/> context. If the target type has
    /// <see cref="TypeMeta.ContentValidation"/>, the content is validated against it.
    /// Without context, emits <see cref="DiagnosticCode.UnresolvedTypedConstant"/>.
    /// </summary>
    private static TypedExpression ResolveTypedConstant(LiteralExpression lit, CheckContext ctx, TypeKind? expectedType)
    {
        var rawText = lit.Text;

        // No context: we don't know which type to validate against
        if (expectedType is null || expectedType == TypeKind.Error)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UnresolvedTypedConstant, lit.Span, rawText));
            return new TypedErrorExpression(lit.Span);
        }

        var targetType = expectedType.Value;
        var meta = Types.GetMeta(targetType);
        var cv = meta.ContentValidation;

        // Type has no content validation — treat as a plain typed constant (trusted)
        if (cv is null)
            return new TypedTypedConstant(targetType, rawText, rawText, lit.Span);

        // Validate content against the ContentValidation DU
        var (isValid, parsedValue, errorMessage) = ValidateContent(cv, rawText, meta.DisplayName);

        if (isValid)
            return new TypedTypedConstant(targetType, rawText, parsedValue, lit.Span);

        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.InvalidTypedConstantContent, lit.Span,
                rawText, errorMessage ?? meta.DisplayName));
        return new TypedErrorExpression(lit.Span);
    }

    /// <summary>
    /// Validate a typed constant's raw text against a <see cref="ContentValidation"/> strategy.
    /// Returns (isValid, parsedValue, errorMessage). On success, parsedValue is the parsed
    /// NodaTime object or the raw string. On failure, errorMessage describes the issue.
    /// </summary>
    private static (bool IsValid, object? ParsedValue, string? ErrorMessage) ValidateContent(
        ContentValidation cv, string rawText, string displayName) => cv switch
    {
        NodaTimeValidation noda => ValidateNodaTime(noda, rawText, displayName),
        ClosedSetValidation closed => ValidateClosedSet(closed, rawText),
        RegexValidation regex => ValidateRegex(regex, rawText, displayName),
        _ => (true, rawText, null),
    };

    private static (bool, object?, string?) ValidateNodaTime(
        NodaTimeValidation noda, string rawText, string displayName)
    {
        // Dispatch to the appropriate NodaTime parser based on the pattern string.
        // The NodaTimePattern field identifies which parser to use.
        if (noda.NodaTimePattern == "uuuu'-'MM'-'dd")
        {
            var result = LocalDatePattern.Iso.Parse(rawText);
            return result.Success
                ? (true, result.Value, null)
                : (false, null, $"{displayName} ({noda.FormatDescription})");
        }

        if (noda.NodaTimePattern == "HH':'mm':'ss")
        {
            var result = LocalTimePattern.ExtendedIso.Parse(rawText);
            return result.Success
                ? (true, result.Value, null)
                : (false, null, $"{displayName} ({noda.FormatDescription})");
        }

        if (noda.NodaTimePattern == "uuuu'-'MM'-'dd'T'HH':'mm':'ss")
        {
            var result = LocalDateTimePattern.ExtendedIso.Parse(rawText);
            return result.Success
                ? (true, result.Value, null)
                : (false, null, $"{displayName} ({noda.FormatDescription})");
        }

        if (noda.NodaTimePattern == "NormalizingIso")
        {
            var result = PeriodPattern.NormalizingIso.Parse(rawText);
            return result.Success
                ? (true, result.Value, null)
                : (false, null, $"{displayName} ({noda.FormatDescription})");
        }

        // Unknown NodaTime pattern — accept without validation
        return (true, rawText, null);
    }

    private static (bool, object?, string?) ValidateClosedSet(ClosedSetValidation closed, string rawText)
    {
        if (closed.AllowedValues.Contains(rawText))
            return (true, rawText, null);

        return (false, null, $"{closed.SetName} value");
    }

    private static (bool, object?, string?) ValidateRegex(
        RegexValidation regex, string rawText, string displayName)
    {
        if (System.Text.RegularExpressions.Regex.IsMatch(rawText, regex.Pattern))
            return (true, rawText, null);

        return (false, null, $"{displayName} ({regex.FormatDescription})");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Context retry for binary operations (Slice 4)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempt to re-resolve a literal operand with <paramref name="expectedType"/> context
    /// when bottom-up binary operation resolution fails. This handles cases like
    /// <c>amount &gt; 100</c> where <c>amount: money</c> and <c>100</c> needs to be re-resolved as money.
    /// </summary>
    private static TypedExpression? TryContextRetryBinaryOp(
        BinaryOperationExpression bin, TypedExpression left, TypedExpression right,
        OperatorKind opKind, CheckContext ctx)
    {
        // Only retry if exactly one operand is a bare literal
        bool leftIsLiteral = bin.Left is LiteralExpression;
        bool rightIsLiteral = bin.Right is LiteralExpression;

        if (!leftIsLiteral && !rightIsLiteral)
            return null;

        // Try retrying the literal side with the other side's type as context
        if (rightIsLiteral && !leftIsLiteral)
        {
            var retried = Resolve(bin.Right, ctx, left.ResultType);
            if (retried is not TypedErrorExpression && retried.ResultType != right.ResultType)
            {
                var result = TryResolveBinaryWithWidening(opKind, left.ResultType, retried.ResultType);
                if (result is not null)
                {
                    return new TypedBinaryOp(
                        result.Result, result.Kind, left, retried,
                        ResultQualifier: MapQualifierBinding(result),
                        ProofRequirements: result.ProofRequirements.ToImmutableArray(),
                        Span: bin.Span);
                }
            }
        }

        if (leftIsLiteral && !rightIsLiteral)
        {
            var retried = Resolve(bin.Left, ctx, right.ResultType);
            if (retried is not TypedErrorExpression && retried.ResultType != left.ResultType)
            {
                var result = TryResolveBinaryWithWidening(opKind, retried.ResultType, right.ResultType);
                if (result is not null)
                {
                    return new TypedBinaryOp(
                        result.Result, result.Kind, retried, right,
                        ResultQualifier: MapQualifierBinding(result),
                        ProofRequirements: result.ProofRequirements.ToImmutableArray(),
                        Span: bin.Span);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempt context retry for function overload resolution. For each arity-matching overload,
    /// re-resolves literal arguments with the parameter's type as <c>expectedType</c>.
    /// Returns the best match or null.
    /// </summary>
    private static (FunctionKind Kind, FunctionOverload Overload, ImmutableArray<TypedExpression> Args)?
        TryContextRetryOverload(
            ReadOnlySpan<FunctionMeta> candidates,
            ImmutableArray<TypedExpression> resolvedArgs,
            ImmutableArray<ParsedExpression> parsedArgs,
            CheckContext ctx)
    {
        // Only retry if at least one arg is a bare literal
        bool hasLiteral = false;
        for (int i = 0; i < parsedArgs.Length; i++)
        {
            if (parsedArgs[i] is LiteralExpression) { hasLiteral = true; break; }
        }
        if (!hasLiteral) return null;

        FunctionKind? bestKind = null;
        FunctionOverload? bestOverload = null;
        ImmutableArray<TypedExpression> bestArgs = default;
        int bestScore = int.MaxValue;

        foreach (var meta in candidates)
        {
            foreach (var overload in meta.Overloads)
            {
                if (overload.Parameters.Count != resolvedArgs.Length) continue;

                var retriedArgs = new TypedExpression[resolvedArgs.Length];
                int score = 0;
                bool valid = true;

                for (int i = 0; i < resolvedArgs.Length; i++)
                {
                    var paramType = overload.Parameters[i].Kind;
                    var argType = resolvedArgs[i].ResultType;

                    if (argType == paramType)
                    {
                        retriedArgs[i] = resolvedArgs[i];
                        continue;
                    }

                    // If this arg is a literal, re-resolve with parameter type context
                    if (parsedArgs[i] is LiteralExpression)
                    {
                        var retried = Resolve(parsedArgs[i], ctx, paramType);
                        if (retried is not TypedErrorExpression && IsAssignable(retried.ResultType, paramType))
                        {
                            retriedArgs[i] = retried;
                            if (retried.ResultType != paramType) score++;
                            continue;
                        }
                    }

                    if (IsAssignable(argType, paramType))
                    {
                        retriedArgs[i] = resolvedArgs[i];
                        score++;
                    }
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
                    bestArgs = [..retriedArgs];
                    if (score == 0) goto found;
                }
            }
        }

    found:
        if (bestOverload is not null)
            return (bestKind!.Value, bestOverload, bestArgs);

        return null;
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

            return new TypedFieldRef(field.ResolvedType, field.Name,
                ctx.CIFields.Contains(field.Name), id.Span);
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

        // Slice 4: context retry — re-resolve literal operands with the other side's type as context
        var retried = TryContextRetryBinaryOp(bin, left, right, opKind, ctx);
        if (retried is not null)
            return retried;

        // No match at any widening level or context retry
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

    /// <summary>
    /// Resolve a postfix presence check: <c>field is set</c> / <c>field is not set</c>.
    /// Operand must resolve to an optional field; result type is always boolean.
    /// Emits <see cref="DiagnosticCode.IsSetOnNonOptional"/> if the operand field is not optional.
    /// </summary>
    private static TypedExpression ResolvePostfixOp(PostfixOperationExpression postfix, CheckContext ctx)
    {
        var operand = Resolve(postfix.Operand, ctx);

        // ErrorType propagation (D13)
        if (operand is TypedErrorExpression)
            return new TypedErrorExpression(postfix.Span);

        // Operand must be a field reference to check optionality
        if (operand is TypedFieldRef fieldRef)
        {
            if (ctx.FieldLookup.TryGetValue(fieldRef.FieldName, out var field) && !field.IsOptional)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.IsSetOnNonOptional, postfix.Span, fieldRef.FieldName));
                return new TypedErrorExpression(postfix.Span);
            }

            return new TypedPostfixOp(operand, postfix.IsNegated, postfix.Span);
        }

        // Arg refs with optional flag are also valid targets
        if (operand is TypedArgRef argRef)
        {
            if (ctx.CurrentEventArgs is not null &&
                ctx.CurrentEventArgs.TryGetValue(argRef.ArgName, out var arg) && !arg.IsOptional)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.IsSetOnNonOptional, postfix.Span, argRef.ArgName));
                return new TypedErrorExpression(postfix.Span);
            }

            return new TypedPostfixOp(operand, postfix.IsNegated, postfix.Span);
        }

        // Non-field, non-arg operand: 'is set' doesn't apply
        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.IsSetOnNonOptional, postfix.Span, "expression"));
        return new TypedErrorExpression(postfix.Span);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2 — transition row + event handler normalization (Slice 5)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Iterate all <see cref="ConstructKind.TransitionRow"/> constructs from the manifest,
    /// resolve each to a <see cref="TypedTransitionRow"/>, and accumulate into <see cref="CheckContext.TransitionRows"/>.
    /// Records <see cref="StateReference"/> and <see cref="EventReference"/> sites for LS navigation.
    /// </summary>
    private static void PopulateTransitionRows(ConstructManifest manifest, CheckContext ctx)
    {
        foreach (var construct in manifest.ByKind[ConstructKind.TransitionRow])
        {
            var row = NormalizeTransitionRow(construct, ctx);
            ctx.TransitionRows.Add(row);
        }

        // D26: if any TypedErrorExpression in transition rows → at least one Error diagnostic must exist
        Debug.Assert(
            !ctx.TransitionRows.Any(r => ContainsErrorExpression(r)) ||
            ctx.Diagnostics.Any(d => d.Severity == Severity.Error),
            "D26: TypedErrorExpression present in transition rows but no Error-severity diagnostic emitted.");
    }

    /// <summary>
    /// Iterate all <see cref="ConstructKind.EventHandler"/> constructs from the manifest,
    /// resolve each to a <see cref="TypedEventHandler"/>, and accumulate into <see cref="CheckContext.EventHandlers"/>.
    /// Records <see cref="EventReference"/> sites for LS navigation.
    /// </summary>
    private static void PopulateEventHandlers(ConstructManifest manifest, CheckContext ctx)
    {
        foreach (var construct in manifest.ByKind[ConstructKind.EventHandler])
        {
            var handler = NormalizeEventHandler(construct, ctx);
            ctx.EventHandlers.Add(handler);
        }

        // D26: if any TypedErrorExpression in event handlers → at least one Error diagnostic must exist
        Debug.Assert(
            !ctx.EventHandlers.Any(h => h.Actions.Any(a => a is TypedInputAction ia && ContainsErrorExpressionInAction(ia))) ||
            ctx.Diagnostics.Any(d => d.Severity == Severity.Error),
            "D26: TypedErrorExpression present in event handlers but no Error-severity diagnostic emitted.");
    }

    /// <summary>
    /// Iterate all <see cref="ConstructKind.RuleDeclaration"/> constructs from the manifest,
    /// resolve each to a <see cref="TypedRule"/>, and accumulate into <see cref="CheckContext.Rules"/>.
    /// </summary>
    private static void PopulateRules(ConstructManifest manifest, CheckContext ctx)
    {
        if (!manifest.ByKind.Contains(ConstructKind.RuleDeclaration))
            return;

        foreach (var construct in manifest.ByKind[ConstructKind.RuleDeclaration])
        {
            var ruleSlot = construct.GetRequiredSlot<RuleExpressionSlot>(ConstructSlotKind.RuleExpression);
            ctx.CurrentScope = FieldScopeMode.AllFields;
            var condition = Resolve(ruleSlot.Expression, ctx);

            TypedExpression? guard = null;
            var guardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
            if (guardSlot is not null)
            {
                guard = Resolve(guardSlot.Expression, ctx);
                if (guard is not TypedErrorExpression && guard.ResultType != TypeKind.Boolean)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.TypeMismatch, guardSlot.Expression.Span,
                            Types.GetMeta(TypeKind.Boolean).DisplayName,
                            Types.GetMeta(guard.ResultType).DisplayName));
                    guard = new TypedErrorExpression(guardSlot.Expression.Span);
                }
            }

            var becauseSlot = construct.GetRequiredSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause);
            var message = new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span);

            ctx.Rules.Add(new TypedRule(condition, guard, message, ImmutableArray<string>.Empty, construct));
        }
    }

    /// <summary>Normalize a transition row construct into a <see cref="TypedTransitionRow"/>.</summary>
    private static TypedTransitionRow NormalizeTransitionRow(ParsedConstruct construct, CheckContext ctx)
    {
        // —— FromState resolution ——
        var stateTargetSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
        string? fromState = null;
        if (stateTargetSlot is not null && stateTargetSlot.StateName is not null)
        {
            if (ctx.StateLookup.TryGetValue(stateTargetSlot.StateName, out var fromTypedState))
            {
                fromState = fromTypedState.Name;
                ctx.StateReferences.Add(new StateReference(fromTypedState, stateTargetSlot.Span));
            }
            else
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredState, stateTargetSlot.Span, stateTargetSlot.StateName));
            }
        }
        // StateName == null → any-state wildcard (D10): FromState stays null, no error

        // —— Event resolution ——
        var eventTargetSlot = construct.GetRequiredSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
        string eventName = "";
        TypedEvent? resolvedEvent = null;
        if (eventTargetSlot.EventName is not null)
        {
            if (ctx.EventLookup.TryGetValue(eventTargetSlot.EventName, out var evTyped))
            {
                eventName = evTyped.Name;
                resolvedEvent = evTyped;
                ctx.EventReferences.Add(new EventReference(evTyped, eventTargetSlot.Span));
            }
            else
            {
                eventName = eventTargetSlot.EventName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredEvent, eventTargetSlot.Span, eventTargetSlot.EventName));
            }
        }

        // —— Set event args scope ——
        IReadOnlyDictionary<string, TypedArg>? previousArgs = ctx.CurrentEventArgs;
        if (resolvedEvent is not null)
        {
            ctx.CurrentEventArgs = resolvedEvent.Args
                .ToFrozenDictionary(a => a.Name);
        }

        try
        {
            // —— Guard resolution ——
            TypedExpression? guard = null;
            var guardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
            if (guardSlot is not null)
            {
                ctx.CurrentScope = FieldScopeMode.AllFields;
                guard = Resolve(guardSlot.Expression, ctx);
                if (guard is not TypedErrorExpression && guard.ResultType != TypeKind.Boolean)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.TypeMismatch, guardSlot.Expression.Span,
                            Types.GetMeta(TypeKind.Boolean).DisplayName,
                            Types.GetMeta(guard.ResultType).DisplayName));
                    guard = new TypedErrorExpression(guardSlot.Expression.Span);
                }
            }

            // —— Action chain resolution ——
            var actionChainSlot = construct.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
            var actions = ImmutableArray<TypedAction>.Empty;
            if (actionChainSlot is not null)
            {
                var builder = ImmutableArray.CreateBuilder<TypedAction>(actionChainSlot.Actions.Length);
                foreach (var parsedAction in actionChainSlot.Actions)
                    builder.Add(ResolveAction(parsedAction, ctx));
                actions = builder.MoveToImmutable();
            }

            // —— Outcome resolution ——
            var outcomeSlot = construct.GetSlot<OutcomeSlot>(ConstructSlotKind.Outcome);
            TransitionRowOutcome outcome = TransitionRowOutcome.NoTransition;
            string? targetState = null;
            string? rejectReason = null;

            if (outcomeSlot is not null)
            {
                switch (outcomeSlot.Outcome)
                {
                    case TransitionOutcome trans:
                        outcome = TransitionRowOutcome.Transition;
                        if (ctx.StateLookup.TryGetValue(trans.StateName, out var toTypedState))
                        {
                            targetState = toTypedState.Name;
                            ctx.StateReferences.Add(new StateReference(toTypedState, trans.Span));
                        }
                        else
                        {
                            ctx.Diagnostics.Add(
                                Diagnostics.Create(DiagnosticCode.UndeclaredState, trans.Span, trans.StateName));
                        }
                        break;
                    case NoTransitionOutcome:
                        outcome = TransitionRowOutcome.NoTransition;
                        break;
                    case RejectOutcome reject:
                        outcome = TransitionRowOutcome.Reject;
                        rejectReason = reject.Reason;
                        break;
                    case MalformedOutcome:
                        outcome = TransitionRowOutcome.NoTransition;
                        break;
                }
            }

            return new TypedTransitionRow(
                FromState: fromState,
                EventName: eventName,
                TargetState: targetState,
                Guard: guard,
                Actions: actions,
                Outcome: outcome,
                RejectReason: rejectReason,
                ResultQualifier: null,
                Syntax: construct);
        }
        finally
        {
            ctx.CurrentEventArgs = previousArgs;
        }
    }

    /// <summary>Normalize an event handler construct into a <see cref="TypedEventHandler"/>.</summary>
    private static TypedEventHandler NormalizeEventHandler(ParsedConstruct construct, CheckContext ctx)
    {
        // —— Event resolution ——
        var eventTargetSlot = construct.GetRequiredSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
        string eventName = "";
        TypedEvent? resolvedEvent = null;
        if (eventTargetSlot.EventName is not null)
        {
            if (ctx.EventLookup.TryGetValue(eventTargetSlot.EventName, out var evTyped))
            {
                eventName = evTyped.Name;
                resolvedEvent = evTyped;
                ctx.EventReferences.Add(new EventReference(evTyped, eventTargetSlot.Span));
            }
            else
            {
                eventName = eventTargetSlot.EventName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredEvent, eventTargetSlot.Span, eventTargetSlot.EventName));
            }
        }

        // —— Set event args scope ——
        IReadOnlyDictionary<string, TypedArg>? previousArgs = ctx.CurrentEventArgs;
        if (resolvedEvent is not null)
        {
            ctx.CurrentEventArgs = resolvedEvent.Args
                .ToFrozenDictionary(a => a.Name);
        }

        try
        {
            // —— Action chain resolution ——
            var actionChainSlot = construct.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
            var actions = ImmutableArray<TypedAction>.Empty;
            if (actionChainSlot is not null)
            {
                var builder = ImmutableArray.CreateBuilder<TypedAction>(actionChainSlot.Actions.Length);
                foreach (var parsedAction in actionChainSlot.Actions)
                    builder.Add(ResolveAction(parsedAction, ctx));
                actions = builder.MoveToImmutable();
            }

            return new TypedEventHandler(
                EventName: eventName,
                Actions: actions,
                Syntax: construct);
        }
        finally
        {
            ctx.CurrentEventArgs = previousArgs;
        }
    }

    /// <summary>
    /// Resolve a <see cref="ParsedAction"/> DU node into a <see cref="TypedAction"/> DU node.
    /// Dispatches on the parsed action shape, resolves operand expressions, and applies the
    /// <see cref="ActionSecondaryRole"/> invariant (D5): <c>SecondaryRole.HasValue == (SecondaryExpression != null)</c>.
    /// </summary>
    private static TypedAction ResolveAction(ParsedAction parsedAction, CheckContext ctx)
    {
        // Resolve the target field from the identifier expression
        string fieldName = "";
        TypeKind fieldType = TypeKind.Error;
        var proofReqs = Actions.GetMeta(parsedAction.Kind).ProofRequirements;

        switch (parsedAction)
        {
            case AssignAction assign:
            {
                (fieldName, fieldType) = ResolveActionTarget(assign.Target, ctx);
                var value = Resolve(assign.Value, ctx);
                return new TypedInputAction(
                    assign.Kind, fieldName, fieldType,
                    InputExpression: value,
                    SecondaryExpression: null,
                    SecondaryRole: null,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: assign.Span);
            }

            case CollectionValueAction colVal:
            {
                (fieldName, fieldType) = ResolveActionTarget(colVal.Target, ctx);
                var value = Resolve(colVal.Value, ctx);
                return new TypedInputAction(
                    colVal.Kind, fieldName, fieldType,
                    InputExpression: value,
                    SecondaryExpression: null,
                    SecondaryRole: null,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: colVal.Span);
            }

            case CollectionIntoAction colInto:
            {
                (fieldName, fieldType) = ResolveActionTarget(colInto.Target, ctx);
                string? binding = null;
                if (colInto.IntoTarget is IdentifierExpression intoId)
                    binding = intoId.Name;
                return new TypedBindingAction(
                    colInto.Kind, fieldName, fieldType,
                    Binding: binding,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: colInto.Span);
            }

            case FieldOnlyAction fieldOnly:
            {
                (fieldName, fieldType) = ResolveActionTarget(fieldOnly.Target, ctx);
                return new TypedAction(
                    fieldOnly.Kind, fieldName, fieldType,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: fieldOnly.Span);
            }

            case CollectionValueByAction colBy:
            {
                (fieldName, fieldType) = ResolveActionTarget(colBy.Target, ctx);
                var value = Resolve(colBy.Value, ctx);
                var key = Resolve(colBy.OrderingKey, ctx);
                // D5: SecondaryRole = Key, SecondaryExpression = key
                Debug.Assert(key is not null, "D5: SecondaryExpression for CollectionValueBy must not be null");
                return new TypedInputAction(
                    colBy.Kind, fieldName, fieldType,
                    InputExpression: value,
                    SecondaryExpression: key,
                    SecondaryRole: ActionSecondaryRole.Key,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: colBy.Span);
            }

            case InsertAtAction insertAt:
            {
                (fieldName, fieldType) = ResolveActionTarget(insertAt.Target, ctx);
                var value = Resolve(insertAt.Value, ctx);
                var index = Resolve(insertAt.Index, ctx);
                // D5: SecondaryRole = Index, SecondaryExpression = index
                Debug.Assert(index is not null, "D5: SecondaryExpression for InsertAt must not be null");
                return new TypedInputAction(
                    insertAt.Kind, fieldName, fieldType,
                    InputExpression: value,
                    SecondaryExpression: index,
                    SecondaryRole: ActionSecondaryRole.Index,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: insertAt.Span);
            }

            case RemoveAtAction removeAt:
            {
                (fieldName, fieldType) = ResolveActionTarget(removeAt.Target, ctx);
                var index = Resolve(removeAt.Index, ctx);
                // RemoveAt has an index but no value — use TypedInputAction with index as primary
                return new TypedInputAction(
                    removeAt.Kind, fieldName, fieldType,
                    InputExpression: index,
                    SecondaryExpression: null,
                    SecondaryRole: null,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: removeAt.Span);
            }

            case PutKeyValueAction put:
            {
                (fieldName, fieldType) = ResolveActionTarget(put.Target, ctx);
                var value = Resolve(put.Value, ctx);
                var key = Resolve(put.Key, ctx);
                // D5: SecondaryRole = Key, SecondaryExpression = key
                Debug.Assert(key is not null, "D5: SecondaryExpression for PutKeyValue must not be null");
                return new TypedInputAction(
                    put.Kind, fieldName, fieldType,
                    InputExpression: value,
                    SecondaryExpression: key,
                    SecondaryRole: ActionSecondaryRole.Key,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: put.Span);
            }

            case CollectionIntoByAction colIntoBy:
            {
                (fieldName, fieldType) = ResolveActionTarget(colIntoBy.Target, ctx);
                string? binding = null;
                if (colIntoBy.IntoTarget is IdentifierExpression intoId)
                    binding = intoId.Name;
                return new TypedBindingAction(
                    colIntoBy.Kind, fieldName, fieldType,
                    Binding: binding,
                    ProofRequirements: proofReqs.ToImmutableArray(),
                    Span: colIntoBy.Span);
            }

            case MalformedAction malformed:
            {
                return new TypedAction(
                    malformed.Kind, "", TypeKind.Error,
                    ProofRequirements: ImmutableArray<ProofRequirement>.Empty,
                    Span: malformed.Span);
            }

            default:
                return new TypedAction(
                    parsedAction.Kind, "", TypeKind.Error,
                    ProofRequirements: ImmutableArray<ProofRequirement>.Empty,
                    Span: parsedAction.Span);
        }
    }

    /// <summary>
    /// Resolve an action target expression (the field identifier) to its name and type.
    /// Records a <see cref="FieldReference"/> if the field is found.
    /// </summary>
    private static (string FieldName, TypeKind FieldType) ResolveActionTarget(ParsedExpression target, CheckContext ctx)
    {
        if (target is IdentifierExpression id)
        {
            if (ctx.FieldLookup.TryGetValue(id.Name, out var field))
            {
                ctx.FieldReferences.Add(new FieldReference(field, id.Span));
                return (field.Name, field.ResolvedType);
            }

            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UndeclaredField, id.Span, id.Name));
            return (id.Name, TypeKind.Error);
        }

        // Non-identifier target — resolve as expression for error reporting
        var resolved = Resolve(target, ctx);
        return ("", resolved.ResultType);
    }

    /// <summary>Check whether a transition row contains any <see cref="TypedErrorExpression"/>.</summary>
    private static bool ContainsErrorExpression(TypedTransitionRow row)
    {
        if (row.Guard is TypedErrorExpression) return true;
        foreach (var action in row.Actions)
        {
            if (action is TypedInputAction ia)
            {
                if (ia.InputExpression is TypedErrorExpression) return true;
                if (ia.SecondaryExpression is TypedErrorExpression) return true;
            }
        }
        return false;
    }

    /// <summary>Check whether a typed input action contains a <see cref="TypedErrorExpression"/>.</summary>
    private static bool ContainsErrorExpressionInAction(TypedInputAction action)
    {
        if (action.InputExpression is TypedErrorExpression) return true;
        if (action.SecondaryExpression is TypedErrorExpression) return true;
        return false;
    }

    /// <summary>
    /// Resolve a quantifier expression: resolve collection, extract element type,
    /// push binding onto <see cref="CheckContext.QuantifierBindings"/>, resolve predicate
    /// (must be boolean), pop binding, return <see cref="TypedQuantifier"/>.
    /// </summary>
    private static TypedExpression ResolveQuantifier(QuantifierExpression expr, CheckContext ctx)
    {
        // 1. Resolve the collection expression
        var collection = Resolve(expr.Collection, ctx);
        if (collection is TypedErrorExpression)
            return new TypedErrorExpression(expr.Span);

        // 2. Extract element type from the collection via field lookup
        var elementType = GetElementType(collection, ctx);
        if (elementType is null)
        {
            // Not a collection type — emit InvalidQuantifierTarget
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidQuantifierTarget, expr.Collection.Span,
                    collection is TypedFieldRef fr ? fr.FieldName : collection.ResultType.ToString()));
            return new TypedErrorExpression(expr.Span);
        }

        // 3. Push binding variable into scope (shadows event args and fields)
        ctx.QuantifierBindings.Push((expr.BindingName, elementType.Value));

        // 4. Resolve predicate with binding in scope
        var predicate = Resolve(expr.Predicate, ctx);

        // 5. Pop binding
        ctx.QuantifierBindings.Pop();

        // 6. ErrorType propagation on predicate
        if (predicate is TypedErrorExpression)
            return new TypedErrorExpression(expr.Span);

        // 7. Predicate must be boolean
        if (predicate.ResultType != TypeKind.Boolean)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.QuantifierPredicateNotBoolean, expr.Predicate.Span,
                    predicate.ResultType.ToString()));
            return new TypedErrorExpression(expr.Span);
        }

        return new TypedQuantifier(
            TypeKind.Boolean,
            expr.BindingName,
            elementType.Value,
            collection,
            predicate,
            expr.Span);
    }

    /// <summary>
    /// Resolve an if/then/else conditional expression. Validates boolean condition (D13),
    /// unifies branch types via widening, and returns <see cref="TypedConditional"/>.
    /// </summary>
    private static TypedExpression ResolveConditional(ConditionalExpression expr, CheckContext ctx)
    {
        var condition = Resolve(expr.Condition, ctx);
        if (condition is TypedErrorExpression)
            return new TypedErrorExpression(expr.Span);

        if (condition.ResultType != TypeKind.Boolean)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Condition.Span,
                    Types.GetMeta(TypeKind.Boolean).DisplayName,
                    Types.GetMeta(condition.ResultType).DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        var thenBranch = Resolve(expr.ThenBranch, ctx);
        var elseBranch = Resolve(expr.ElseBranch, ctx);

        if (thenBranch is TypedErrorExpression || elseBranch is TypedErrorExpression)
            return new TypedErrorExpression(expr.Span);

        var thenType = thenBranch.ResultType;
        var elseType = elseBranch.ResultType;

        TypeKind resultType;
        if (thenType == elseType)
        {
            resultType = thenType;
        }
        else if (IsAssignable(thenType, elseType))
        {
            resultType = elseType;
        }
        else if (IsAssignable(elseType, thenType))
        {
            resultType = thenType;
        }
        else
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                    Types.GetMeta(thenType).DisplayName,
                    Types.GetMeta(elseType).DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        return new TypedConditional(resultType, condition, thenBranch, elseBranch, expr.Span);
    }

    /// <summary>
    /// Resolve a list literal expression: resolve each element, unify element types
    /// (with widening), return <see cref="TypedListLiteral"/>.
    /// </summary>
    private static TypedExpression ResolveListLiteral(ListLiteralExpression expr, CheckContext ctx)
    {
        // Empty list — can't infer element type; return Error-typed list
        if (expr.Elements.Length == 0)
            return new TypedListLiteral(TypeKind.List, TypeKind.Error, ImmutableArray<TypedExpression>.Empty, expr.Span);

        var elements = ImmutableArray.CreateBuilder<TypedExpression>(expr.Elements.Length);
        bool hasError = false;

        foreach (var elem in expr.Elements)
        {
            var resolved = Resolve(elem, ctx);
            if (resolved is TypedErrorExpression)
                hasError = true;
            elements.Add(resolved);
        }

        if (hasError)
            return new TypedErrorExpression(expr.Span);

        // Unify element types: start with first element's type, widen if needed
        var unified = elements[0].ResultType;
        for (int i = 1; i < elements.Count; i++)
        {
            var elemType = elements[i].ResultType;
            if (elemType == unified)
                continue;

            // Try widening: elemType → unified
            if (IsAssignable(elemType, unified))
                continue;

            // Try widening: unified → elemType (promote unified)
            if (IsAssignable(unified, elemType))
            {
                unified = elemType;
                continue;
            }

            // Incompatible types
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Elements[i].Span,
                    unified.ToString(), elemType.ToString()));
            return new TypedErrorExpression(expr.Span);
        }

        return new TypedListLiteral(TypeKind.List, unified, elements.ToImmutable(), expr.Span);
    }

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

        return SelectOverload(candidates, resolvedArgs, expr.Arguments, expr.FunctionName, expr.Span, ctx);
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

        return SelectOverload(candidates, resolvedArgs, expr.Arguments, ciName, expr.Span, ctx);
    }

    /// <summary>
    /// Select the best overload across all <paramref name="candidates"/> for the given resolved args.
    /// Arity filter → exact match (score 0) → widened match (score = widen count) → context retry for literals → error.
    /// </summary>
    private static TypedExpression SelectOverload(
        ReadOnlySpan<FunctionMeta> candidates,
        ImmutableArray<TypedExpression> resolvedArgs,
        ImmutableArray<ParsedExpression> parsedArgs,
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

        // Slice 4: context retry — re-resolve literal args with each candidate's parameter type
        if (parsedArgs.Length > 0)
        {
            var retryResult = TryContextRetryOverload(candidates, resolvedArgs, parsedArgs, ctx);
            if (retryResult is not null)
            {
                return new TypedFunctionCall(
                    retryResult.Value.Overload.ReturnType,
                    retryResult.Value.Kind,
                    retryResult.Value.Args,
                    retryResult.Value.Overload.ProofRequirements.ToImmutableArray(),
                    span);
            }
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
        // Qualified event arg reference: EventName.ArgName (§3.5 Event arg access)
        if (expr.Target is IdentifierExpression eventId &&
            ctx.EventLookup.TryGetValue(eventId.Name, out var ev))
        {
            var arg = ev.Args.FirstOrDefault(a =>
                string.Equals(a.Name, expr.MemberName, StringComparison.Ordinal));
            if (arg is not null)
                return new TypedArgRef(arg.ResolvedType, ev.Name, arg.Name, expr.Span);

            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UndeclaredField, expr.Span, expr.MemberName));
            return new TypedErrorExpression(expr.Span);
        }

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
    //  Modifier and structural validation
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Validate modifier applicability, conflicts, and subsumption for all fields and states.</summary>
    private static void ValidateModifiers(CheckContext ctx)
    {
        foreach (var field in ctx.Fields)
        {
            if (field.ResolvedType == TypeKind.Error) continue;
            ValidateFieldModifiers(field.Modifiers, field.ResolvedType, field.ImpliedModifiers,
                field.IsComputed, field.Syntax.Span, field.Name, isEventArg: false, ctx);
        }

        foreach (var evt in ctx.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.ResolvedType == TypeKind.Error) continue;
                ValidateFieldModifiers(arg.Modifiers, arg.ResolvedType, ImmutableArray<ModifierKind>.Empty,
                    isComputed: false, arg.Span, arg.Name, isEventArg: true, ctx);
            }
        }
    }

    /// <summary>
    /// Catalog-driven modifier validation for a single field or event arg declaration.
    /// Checks applicability, duplicates, mutual exclusivity, subsumption, redundancy with
    /// implied modifiers, and writable restrictions.
    /// </summary>
    private static void ValidateFieldModifiers(
        ImmutableArray<ModifierKind> modifiers,
        TypeKind resolvedType,
        ImmutableArray<ModifierKind> impliedModifiers,
        bool isComputed,
        SourceSpan span,
        string declarationName,
        bool isEventArg,
        CheckContext ctx)
    {
        var seen = new HashSet<ModifierKind>();

        for (int i = 0; i < modifiers.Length; i++)
        {
            var kind = modifiers[i];
            var meta = Modifiers.GetMeta(kind);

            // Duplicate check
            if (!seen.Add(kind))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.DuplicateModifier, span, meta.Token.Text));
                continue;
            }

            // Only FieldModifierMeta modifiers are valid on fields/args
            if (meta is not FieldModifierMeta fieldMeta)
                continue;

            // Applicability: empty ApplicableTo = any type; otherwise check membership
            if (fieldMeta.ApplicableTo.Length > 0 &&
                !IsTypeApplicable(fieldMeta.ApplicableTo, resolvedType, modifiers))
            {
                var typeName = Types.GetMeta(resolvedType).DisplayName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.InvalidModifierForType, span, meta.Token.Text, typeName));
            }

            // Mutual exclusivity
            foreach (var conflict in meta.MutuallyExclusiveWith)
            {
                if (seen.Contains(conflict))
                {
                    var conflictMeta = Modifiers.GetMeta(conflict);
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.InvalidModifierForType, span,
                            meta.Token.Text, $"it conflicts with '{conflictMeta.Token.Text}'"));
                }
            }

            // Subsumption: if another explicit modifier already subsumes this one
            foreach (var other in seen)
            {
                if (other == kind) continue;
                var otherMeta = Modifiers.GetMeta(other);
                if (otherMeta is FieldModifierMeta otherField && otherField.Subsumes.Contains(kind))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.RedundantModifier, span,
                            meta.Token.Text, otherMeta.Token.Text));
                }
            }

            // Redundancy with implied modifiers (type already implies this modifier)
            if (impliedModifiers.Contains(kind))
            {
                var typeName = Types.GetMeta(resolvedType).DisplayName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.RedundantModifier, span,
                        meta.Token.Text, typeName));
            }

            // Writable on event arg
            if (kind == ModifierKind.Writable && isEventArg)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.WritableOnEventArg, span, declarationName));
            }

            // Writable on computed field
            if (kind == ModifierKind.Writable && isComputed)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.ComputedFieldNotWritable, span, declarationName));
            }
        }
    }

    /// <summary>
    /// Check whether a resolved type matches any entry in a modifier's ApplicableTo array.
    /// Handles both simple <see cref="TypeTarget"/> and <see cref="ModifiedTypeTarget"/> entries.
    /// </summary>
    private static bool IsTypeApplicable(TypeTarget[] applicableTo, TypeKind resolvedType, ImmutableArray<ModifierKind> modifiers)
    {
        foreach (var target in applicableTo)
        {
            // Kind == null means "any type" within the target
            if (target.Kind is null || target.Kind == resolvedType)
            {
                if (target is ModifiedTypeTarget modified)
                {
                    // All required modifiers must be present
                    if (modified.RequiredModifiers.All(m => modifiers.Contains(m)))
                        return true;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Structural validation sub-pass: computed-field cycle detection and
    /// forward-reference belt-and-suspenders validation.
    /// Reads <see cref="CheckContext.ComputedDeps"/> (populated during computed expression
    /// resolution) and <see cref="CheckContext.Fields"/>.
    /// </summary>
    private static void ValidateStructural(CheckContext ctx)
    {
        // ── Computed field cycle detection (DFS) ──────────────────────────
        // Build adjacency list from ComputedDeps: fieldName → set of dependent field names.
        // O(n) construction, O(n) DFS traversal.
        if (ctx.ComputedDeps.Count > 0)
        {
            var adjacency = new Dictionary<string, List<string>>(ctx.ComputedDeps.Count);
            foreach (var dep in ctx.ComputedDeps)
            {
                if (!adjacency.TryGetValue(dep.FieldName, out var deps))
                {
                    deps = [];
                    adjacency[dep.FieldName] = deps;
                }
                deps.AddRange(dep.DependsOn);
            }

            // DFS with three-color marking: white (unvisited), gray (in stack), black (done)
            var white = new HashSet<string>(adjacency.Keys);
            var gray = new HashSet<string>();
            var black = new HashSet<string>();

            foreach (var startNode in adjacency.Keys)
            {
                if (!white.Contains(startNode)) continue;
                DetectCycles(startNode, adjacency, white, gray, black, [], ctx);
            }
        }

        // ── Forward-reference belt-and-suspenders ─────────────────────────
        // Verify computed field deps don't reference fields declared after the computed field.
        // This is a redundant check — ResolveIdentifier already enforces D8 at expression
        // resolution time. This pass catches any gap if expression resolution was bypassed.
        if (ctx.ComputedDeps.Count > 0)
        {
            var fieldIndex = new Dictionary<string, int>(ctx.Fields.Count);
            for (int i = 0; i < ctx.Fields.Count; i++)
                fieldIndex[ctx.Fields[i].Name] = i;

            foreach (var dep in ctx.ComputedDeps)
            {
                if (!fieldIndex.TryGetValue(dep.FieldName, out var sourceIdx)) continue;

                foreach (var target in dep.DependsOn)
                {
                    if (fieldIndex.TryGetValue(target, out var targetIdx) && targetIdx >= sourceIdx)
                    {
                        // Find the field's syntax span for the diagnostic
                        var field = ctx.FieldLookup[dep.FieldName];
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.DefaultForwardReference, field.Syntax.Span,
                                dep.FieldName, target));
                    }
                }
            }
        }
    }

    /// <summary>
    /// DFS cycle detection helper. Walks the adjacency graph using three-color marking.
    /// On back-edge detection (gray → gray), emits <see cref="DiagnosticCode.CircularComputedField"/>.
    /// </summary>
    private static void DetectCycles(
        string node,
        Dictionary<string, List<string>> adjacency,
        HashSet<string> white,
        HashSet<string> gray,
        HashSet<string> black,
        List<string> path,
        CheckContext ctx)
    {
        white.Remove(node);
        gray.Add(node);
        path.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (gray.Contains(neighbor))
                {
                    // Back edge → cycle. Build cycle description from path.
                    int cycleStart = path.IndexOf(neighbor);
                    var cycle = string.Join(" → ", path.Skip(cycleStart)) + " → " + neighbor;
                    var field = ctx.FieldLookup[neighbor];
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CircularComputedField, field.Syntax.Span,
                            neighbor, cycle));
                }
                else if (white.Contains(neighbor))
                {
                    DetectCycles(neighbor, adjacency, white, gray, black, path, ctx);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        gray.Remove(node);
        black.Add(node);
    }

    /// <summary>
    /// CI enforcement sub-pass (Slice 8): validate <c>~string</c> usage consistency.
    /// Walks all resolved expression trees and checks the 5 CI enforcement rules
    /// from the language spec §3.8. Rules 1–2 fire on <c>==</c> / <c>!=</c> with a
    /// <c>~string</c> operand. Rules 3 fires on <c>contains</c> with a <c>~string</c>
    /// value in a case-sensitive collection. Rules 4–5 fire on <c>startsWith</c> /
    /// <c>endsWith</c> with a <c>~string</c> first argument.
    /// </summary>
    private static void ValidateCIEnforcement(CheckContext ctx)
    {
        // Collect all root expression trees from the context
        foreach (var field in ctx.Fields)
        {
            if (field.DefaultExpression is not null)
                EnforceCIInExpression(field.DefaultExpression, ctx);
            if (field.ComputedExpression is not null)
                EnforceCIInExpression(field.ComputedExpression, ctx);
        }

        foreach (var row in ctx.TransitionRows)
        {
            if (row.Guard is not null)
                EnforceCIInExpression(row.Guard, ctx);
            foreach (var action in row.Actions)
                EnforceCIInAction(action, ctx);
        }

        foreach (var handler in ctx.EventHandlers)
        {
            foreach (var action in handler.Actions)
                EnforceCIInAction(action, ctx);
        }

        foreach (var rule in ctx.Rules)
        {
            EnforceCIInExpression(rule.Condition, ctx);
            if (rule.Guard is not null)
                EnforceCIInExpression(rule.Guard, ctx);
            EnforceCIInExpression(rule.Message, ctx);
        }

        foreach (var ensure in ctx.Ensures)
        {
            EnforceCIInExpression(ensure.Condition, ctx);
            if (ensure.Guard is not null)
                EnforceCIInExpression(ensure.Guard, ctx);
            EnforceCIInExpression(ensure.Message, ctx);
        }
    }

    /// <summary>Check a single action for CI violations.</summary>
    private static void EnforceCIInAction(TypedAction action, CheckContext ctx)
    {
        if (action is TypedInputAction ia)
        {
            EnforceCIInExpression(ia.InputExpression, ctx);
            if (ia.SecondaryExpression is not null)
                EnforceCIInExpression(ia.SecondaryExpression, ctx);
        }
    }

    /// <summary>
    /// Recursively walk a <see cref="TypedExpression"/> tree and emit CI enforcement
    /// diagnostics at each violation site.
    /// </summary>
    private static void EnforceCIInExpression(TypedExpression expr, CheckContext ctx)
    {
        switch (expr)
        {
            case TypedBinaryOp bin:
                // Rule 1: == with ~string operand
                if (bin.ResolvedOp == OperationKind.StringEqualsString &&
                    (IsCIExpression(bin.Left) || IsCIExpression(bin.Right)))
                {
                    var ciFieldName = GetCIFieldName(bin.Left, bin.Right);
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals, bin.Span, ciFieldName));
                }
                // Rule 2: != with ~string operand
                else if (bin.ResolvedOp == OperationKind.StringNotEqualsString &&
                         (IsCIExpression(bin.Left) || IsCIExpression(bin.Right)))
                {
                    var ciFieldName = GetCIFieldName(bin.Left, bin.Right);
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CaseInsensitiveFieldRequiresTildeNotEquals, bin.Span, ciFieldName));
                }
                // Rule 3: contains with ~string value in case-sensitive collection
                // (fires when contains OperationKind entries land — currently no-op)
                else if (IsContainsOperation(bin.ResolvedOp) &&
                         IsCIExpression(bin.Right) &&
                         bin.Left is TypedFieldRef collRef &&
                         !ctx.CIElementCollections.Contains(collRef.FieldName))
                {
                    var ciFieldName = ((TypedFieldRef)bin.Right).FieldName;
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CaseInsensitiveValueInCaseSensitiveContains, bin.Span, ciFieldName, collRef.FieldName));
                }

                EnforceCIInExpression(bin.Left, ctx);
                EnforceCIInExpression(bin.Right, ctx);
                break;

            case TypedFunctionCall func:
                // Rule 4: startsWith with ~string first arg
                if (func.ResolvedFunction == FunctionKind.StartsWith &&
                    func.Arguments.Length > 0 && IsCIExpression(func.Arguments[0]))
                {
                    var ciFieldName = ((TypedFieldRef)func.Arguments[0]).FieldName;
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CaseInsensitiveFieldRequiresTildeStartsWith, func.Span, ciFieldName));
                }
                // Rule 5: endsWith with ~string first arg
                else if (func.ResolvedFunction == FunctionKind.EndsWith &&
                         func.Arguments.Length > 0 && IsCIExpression(func.Arguments[0]))
                {
                    var ciFieldName = ((TypedFieldRef)func.Arguments[0]).FieldName;
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CaseInsensitiveFieldRequiresTildeEndsWith, func.Span, ciFieldName));
                }

                foreach (var arg in func.Arguments)
                    EnforceCIInExpression(arg, ctx);
                break;

            case TypedUnaryOp un:
                EnforceCIInExpression(un.Operand, ctx);
                break;

            case TypedConditional cond:
                EnforceCIInExpression(cond.Condition, ctx);
                EnforceCIInExpression(cond.ThenBranch, ctx);
                EnforceCIInExpression(cond.ElseBranch, ctx);
                break;

            case TypedQuantifier quant:
                EnforceCIInExpression(quant.Collection, ctx);
                EnforceCIInExpression(quant.Predicate, ctx);
                break;

            case TypedMemberAccess mem:
                EnforceCIInExpression(mem.Object, ctx);
                break;

            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                {
                    if (seg is TypedHoleSegment hole)
                        EnforceCIInExpression(hole.Expression, ctx);
                }
                break;

            case TypedListLiteral list:
                foreach (var elem in list.Elements)
                    EnforceCIInExpression(elem, ctx);
                break;

            // Leaf nodes: TypedFieldRef, TypedArgRef, TypedLiteral, TypedTypedConstant,
            // TypedPostfixOp, TypedErrorExpression — no sub-expressions to walk
        }
    }

    /// <summary>Returns true if the expression resolves to a <c>~string</c> field reference.</summary>
    private static bool IsCIExpression(TypedExpression expr) =>
        expr is TypedFieldRef { IsCaseInsensitive: true };

    /// <summary>Returns the field name from whichever operand is a <c>~string</c> field reference.</summary>
    private static string GetCIFieldName(TypedExpression left, TypedExpression right) =>
        IsCIExpression(left) ? ((TypedFieldRef)left).FieldName : ((TypedFieldRef)right).FieldName;

    /// <summary>
    /// Returns true if the operation is a <c>contains</c> membership test.
    /// Currently no <see cref="OperationKind"/> entries exist for <c>contains</c> —
    /// this predicate will match them when they land.
    /// </summary>
    private static bool IsContainsOperation(OperationKind op) =>
        false; // Placeholder: no contains OperationKind values exist yet

    // ════════════════════════════════════════════════════════════════════════
    //  Final assembly (Slice 10)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transform <see cref="CheckContext"/> mutable accumulators into the immutable <see cref="SemanticIndex"/>.
    /// Derives frozen-dictionary secondary indexes from primary arrays (D4).
    /// Enforces D26 global invariant: any <see cref="TypedErrorExpression"/> in the index
    /// requires ≥1 <see cref="Severity.Error"/> diagnostic.
    /// </summary>
    private static SemanticIndex BuildSemanticIndex(CheckContext ctx)
    {
        var fields = ctx.Fields.ToImmutableArray();
        var states = ctx.States.ToImmutableArray();
        var events = ctx.Events.ToImmutableArray();
        var ensures = ctx.Ensures.ToImmutableArray();

        var index = new SemanticIndex(
            Fields:           fields,
            States:           states,
            Events:           events,
            FieldsByName:     fields.ToFrozenDictionary(f => f.Name),
            StatesByName:     states.ToFrozenDictionary(s => s.Name),
            EventsByName:     events.ToFrozenDictionary(e => e.Name),
            TransitionRows:   ctx.TransitionRows.ToImmutableArray(),
            Rules:            ctx.Rules.ToImmutableArray(),
            Ensures:          ensures,
            AccessModes:      ctx.AccessModes.ToImmutableArray(),
            StateHooks:       ctx.StateHooks.ToImmutableArray(),
            EventHandlers:    ctx.EventHandlers.ToImmutableArray(),
            EditDeclarations: ctx.EditDeclarations.ToImmutableArray(),
            EnsuresByState:   ensures
                                  .Where(e => e.AnchorState is not null)
                                  .GroupBy(e => e.AnchorState!)
                                  .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray()),
            ComputedDeps:     ctx.ComputedDeps.ToImmutableArray(),
            ConstraintRefs:   ctx.ConstraintRefs.ToImmutableArray(),
            FieldReferences:  ctx.FieldReferences.ToImmutableArray(),
            StateReferences:  ctx.StateReferences.ToImmutableArray(),
            EventReferences:  ctx.EventReferences.ToImmutableArray(),
            Diagnostics:      ctx.Diagnostics.ToImmutableArray());

        // D26: If any TypedErrorExpression exists, at least one Error diagnostic must be present
        Debug.Assert(
            !ContainsAnyErrorExpression(index) ||
            index.Diagnostics.Any(d => d.Severity == Severity.Error),
            "D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex");

        return index;
    }

    /// <summary>
    /// Walk all expression-bearing sites in the <see cref="SemanticIndex"/> and return
    /// <c>true</c> if any <see cref="TypedErrorExpression"/> is found. Used by D26.
    /// </summary>
    private static bool ContainsAnyErrorExpression(SemanticIndex index)
    {
        // Fields: default + computed expressions
        foreach (var f in index.Fields)
        {
            if (ContainsError(f.DefaultExpression) || ContainsError(f.ComputedExpression))
                return true;
        }

        // Events: arg default expressions
        foreach (var ev in index.Events)
        {
            foreach (var arg in ev.Args)
            {
                if (ContainsError(arg.DefaultExpression))
                    return true;
            }
        }

        // Transition rows: guard + actions
        foreach (var row in index.TransitionRows)
        {
            if (ContainsError(row.Guard))
                return true;
            if (ActionsContainError(row.Actions))
                return true;
        }

        // Rules: condition + guard + message
        foreach (var rule in index.Rules)
        {
            if (ContainsError(rule.Condition) || ContainsError(rule.Guard) || ContainsError(rule.Message))
                return true;
        }

        // Ensures: condition + guard + message
        foreach (var ensure in index.Ensures)
        {
            if (ContainsError(ensure.Condition) || ContainsError(ensure.Guard) || ContainsError(ensure.Message))
                return true;
        }

        // Access modes: guard
        foreach (var am in index.AccessModes)
        {
            if (ContainsError(am.Guard))
                return true;
        }

        // State hooks: guard + actions
        foreach (var hook in index.StateHooks)
        {
            if (ContainsError(hook.Guard))
                return true;
            if (ActionsContainError(hook.Actions))
                return true;
        }

        // Event handlers: actions
        foreach (var handler in index.EventHandlers)
        {
            if (ActionsContainError(handler.Actions))
                return true;
        }

        return false;
    }

    /// <summary>Returns true if any action in the list contains a <see cref="TypedErrorExpression"/>.</summary>
    private static bool ActionsContainError(ImmutableArray<TypedAction> actions)
    {
        foreach (var action in actions)
        {
            if (action is TypedInputAction input)
            {
                if (ContainsError(input.InputExpression) || ContainsError(input.SecondaryExpression))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Recursively check whether <paramref name="expr"/> is or contains a <see cref="TypedErrorExpression"/>.
    /// Returns false for null expressions.
    /// </summary>
    private static bool ContainsError(TypedExpression? expr) => expr switch
    {
        null => false,
        TypedErrorExpression => true,
        TypedBinaryOp bin => ContainsError(bin.Left) || ContainsError(bin.Right),
        TypedUnaryOp un => ContainsError(un.Operand),
        TypedFunctionCall fn => fn.Arguments.Any(ContainsError),
        TypedMemberAccess ma => ContainsError(ma.Object),
        TypedConditional cond => ContainsError(cond.Condition) || ContainsError(cond.ThenBranch) || ContainsError(cond.ElseBranch),
        TypedQuantifier q => ContainsError(q.Collection) || ContainsError(q.Predicate),
        TypedInterpolatedString interp => interp.Segments.Any(s => s is TypedHoleSegment hole && ContainsError(hole.Expression)),
        TypedListLiteral list => list.Elements.Any(ContainsError),
        TypedPostfixOp post => ContainsError(post.Operand),
        _ => false, // TypedFieldRef, TypedArgRef, TypedLiteral, TypedTypedConstant — leaf nodes
    };
}
