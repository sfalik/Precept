using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
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
                TypedField? targetFieldMeta = null;
                ImmutableArray<DeclaredQualifierMeta>? fieldQualifiers = null;
                if (ctx.FieldLookup.TryGetValue(fieldName, out var resolvedTargetFieldMeta))
                {
                    targetFieldMeta = resolvedTargetFieldMeta;
                    fieldQualifiers = resolvedTargetFieldMeta.DeclaredQualifiers;
                }

                var value = Resolve(assign.Value, ctx,
                    fieldType != TypeKind.Error ? fieldType : null,
                    fieldQualifiers);

                // B9: Post-resolution type check — verify resolved value is assignable to target field.
                if (value is not TypedErrorExpression
                    && fieldType != TypeKind.Error
                    && !IsAssignable(value.ResultType, fieldType))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.TypeMismatch, assign.Value.Span,
                            Types.GetMeta(fieldType).DisplayName, Types.GetMeta(value.ResultType).DisplayName));
                }

                if (value is not TypedErrorExpression
                    && targetFieldMeta is not null
                    && !targetFieldMeta.DeclaredQualifiers.IsDefaultOrEmpty)
                {
                    ValidateAssignmentQualifiers(
                        value,
                        fieldName,
                        targetFieldMeta.DeclaredQualifiers,
                        assign.Value.Span,
                        ctx);
                }

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
                var valueExpectedType = ctx.FieldLookup.TryGetValue(fieldName, out var fieldMeta)
                    ? fieldMeta.ElementType
                    : null;
                var value = Resolve(colVal.Value, ctx, valueExpectedType);
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
                var valueExpectedType = ctx.FieldLookup.TryGetValue(fieldName, out var fieldMeta)
                    ? fieldMeta.ElementType
                    : null;
                var keyExpectedType = ctx.FieldLookup.TryGetValue(fieldName, out var keyFieldMeta)
                    ? keyFieldMeta.KeyType
                    : null;
                var value = Resolve(colBy.Value, ctx, valueExpectedType);
                var key = Resolve(colBy.OrderingKey, ctx, keyExpectedType);
                // D5: SecondaryRole = Key, SecondaryExpression = key
                if (key is null)
                    throw new InvalidOperationException("D5: SecondaryExpression for CollectionValueBy must not be null");
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
                var valueExpectedType = ctx.FieldLookup.TryGetValue(fieldName, out var fieldMeta)
                    ? fieldMeta.ElementType
                    : null;
                var value = Resolve(insertAt.Value, ctx, valueExpectedType);
                var index = Resolve(insertAt.Index, ctx, TypeKind.Integer);
                // D5: SecondaryRole = Index, SecondaryExpression = index
                if (index is null)
                    throw new InvalidOperationException("D5: SecondaryExpression for InsertAt must not be null");
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
                var index = Resolve(removeAt.Index, ctx, TypeKind.Integer);
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
                var valueExpectedType = ctx.FieldLookup.TryGetValue(fieldName, out var fieldMeta)
                    ? fieldMeta.ElementType
                    : null;
                var keyExpectedType = ctx.FieldLookup.TryGetValue(fieldName, out var keyFieldMeta)
                    ? keyFieldMeta.KeyType
                    : null;
                var value = Resolve(put.Value, ctx, valueExpectedType);
                var key = Resolve(put.Key, ctx, keyExpectedType);
                // D5: SecondaryRole = Key, SecondaryExpression = key
                if (key is null)
                    throw new InvalidOperationException("D5: SecondaryExpression for PutKeyValue must not be null");
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
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.TypeMismatch, malformed.Span,
                        "action", "malformed"));
                return new TypedAction(
                    malformed.Kind, "", TypeKind.Error,
                    ProofRequirements: ImmutableArray<ProofRequirement>.Empty,
                    Span: malformed.Span);
            }

            default:
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.TypeMismatch, parsedAction.Span,
                        "known action", parsedAction.GetType().Name));
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
        var isCaseInsensitiveBinding = IsCaseInsensitiveCollectionElement(collection, ctx);
        ctx.QuantifierBindings.Push((expr.BindingName, elementType.Value, isCaseInsensitiveBinding));

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
            {
                ctx.EventReferences.Add(new EventReference(ev, eventId.Span));
                ctx.ArgReferences.Add(new ArgReference(arg, expr.MemberSpan));
                return new TypedArgRef(arg.ResolvedType, ev.Name, arg.Name, arg.DeclaredQualifiers, expr.Span);
            }

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
    private static TypeKind ResolveAccessorReturnType(TypeAccessor accessor, TypedExpression receiver, CheckContext ctx)
    {
        if (receiver.ResultType == TypeKind.QueueBy && string.Equals(accessor.Name, "peekby", StringComparison.Ordinal))
            return GetKeyType(receiver, ctx) ?? TypeKind.Error;

        return accessor switch
        {
            FixedReturnAccessor f     => f.Returns,
            ElementParameterAccessor  => TypeKind.Integer,
            _                         => GetElementType(receiver, ctx) ?? TypeKind.Error,
        };
    }

    /// <summary>
    /// Extract the element type from a receiver expression. For <see cref="TypedFieldRef"/>,
    /// looks up the field in <see cref="CheckContext.FieldLookup"/>.
    /// Returns null if element type cannot be determined.
    /// </summary>
    private static TypeKind? GetElementType(TypedExpression receiver, CheckContext ctx)
    {
        if (receiver is TypedListLiteral listLiteral)
            return listLiteral.ElementType;

        if (receiver is TypedFieldRef fieldRef &&
            ctx.FieldLookup.TryGetValue(fieldRef.FieldName, out var field))
            return field.ElementType;

        return null;
    }

    private static TypeKind? GetKeyType(TypedExpression receiver, CheckContext ctx)
    {
        if (receiver is TypedFieldRef fieldRef &&
            ctx.FieldLookup.TryGetValue(fieldRef.FieldName, out var field))
            return field.KeyType;

        return null;
    }

    private static bool IsCaseInsensitiveCollectionElement(TypedExpression collection, CheckContext ctx) =>
        collection is TypedFieldRef fieldRef && ctx.CIElementCollections.Contains(fieldRef.FieldName);
}
