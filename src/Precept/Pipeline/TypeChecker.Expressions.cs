using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    /// <summary>
    /// Recursively collect all <see cref="TypedFieldRef.FieldName"/> values from an expression tree.
    /// Used by <see cref="ResolveFieldExpressions"/> to build <see cref="ComputedFieldDep"/> entries.
    /// </summary>
    private static void CollectFieldRefs(TypedExpression expr, List<string> refs)
    {
        switch (expr)
        {
            case TypedFieldRef fr:
                refs.Add(fr.FieldName);
                break;
            case TypedBinaryOp bin:
                CollectFieldRefs(bin.Left, refs);
                CollectFieldRefs(bin.Right, refs);
                break;
            case TypedUnaryOp un:
                CollectFieldRefs(un.Operand, refs);
                break;
            case TypedFunctionCall fn:
                foreach (var arg in fn.Arguments) CollectFieldRefs(arg, refs);
                break;
            case TypedMemberAccess ma:
                CollectFieldRefs(ma.Object, refs);
                break;
            case TypedConditional cond:
                CollectFieldRefs(cond.Condition, refs);
                CollectFieldRefs(cond.ThenBranch, refs);
                CollectFieldRefs(cond.ElseBranch, refs);
                break;
            case TypedQuantifier q:
                CollectFieldRefs(q.Collection, refs);
                CollectFieldRefs(q.Predicate, refs);
                break;
            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                    if (seg is TypedHoleSegment hole) CollectFieldRefs(hole.Expression, refs);
                break;
            case TypedInterpolatedTypedConstant itc:
                foreach (var slot in itc.Slots)
                    CollectFieldRefs(slot.Expression, refs);
                break;
            case TypedListLiteral list:
                foreach (var elem in list.Elements) CollectFieldRefs(elem, refs);
                break;
            case TypedPostfixOp post:
                CollectFieldRefs(post.Operand, refs);
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Expression resolution
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a <see cref="ParsedExpression"/> node to a <see cref="TypedExpression"/>.
    /// Dispatches on expression form, resolves types via catalogs, and propagates
    /// <see cref="TypedErrorExpression"/> on failure (D13).
    /// </summary>
    private static TypedExpression Resolve(
        ParsedExpression expr,
        CheckContext ctx,
        TypeKind? expectedType = null,
        ImmutableArray<DeclaredQualifierMeta>? qualifiers = null) => expr switch
    {
        // ── Missing sentinel → error + lightweight TC diagnostic to satisfy D26 ──
        MissingExpression m => ResolveMissing(m, ctx),

        // ── Literal ──
        LiteralExpression lit => ResolveLiteral(lit, ctx, expectedType, qualifiers),

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

        // Interpolated typed constant — full type-grammar matching (Slice 2)
        InterpolatedTypedConstantExpression interpTyped =>
            ResolveInterpolatedTypedConstant(interpTyped, ctx, expectedType, qualifiers),

        // ── Conditionals, quantifiers, lists, postfix ──
        ConditionalExpression cond => ResolveConditional(cond, ctx),
        QuantifierExpression q    => ResolveQuantifier(q, ctx),
        ListLiteralExpression l   => ResolveListLiteral(l, ctx),
        PostfixOperationExpression postfix => ResolvePostfixOp(postfix, ctx),

        _ => ResolveUnknownExpression(expr, ctx),
    };

    /// <summary>
    /// Resolve a <see cref="MissingExpression"/> sentinel: emit a lightweight TC-level diagnostic
    /// to satisfy D26 self-containment (the parser already emitted a detailed diagnostic, but
    /// that lives in <see cref="ConstructManifest.Diagnostics"/>, not <see cref="CheckContext.Diagnostics"/>).
    /// Uses <see cref="DiagnosticCode.TypeMismatch"/> as the nearest existing Error-severity TC code.
    /// </summary>
    private static TypedErrorExpression ResolveMissing(MissingExpression m, CheckContext ctx)
    {
        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.TypeMismatch, m.Span, "expression", "missing"));
        return new TypedErrorExpression(m.Span);
    }

    private static TypedErrorExpression ResolveUnknownExpression(ParsedExpression expr, CheckContext ctx)
    {
        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.TypeMismatch, expr.Span,
                "known expression", expr.GetType().Name));
        return new TypedErrorExpression(expr.Span);
    }

    /// <summary>
    /// Resolve a literal expression to a <see cref="TypedLiteral"/> with the appropriate
    /// <see cref="TypeKind"/> and parsed value. When <paramref name="expectedType"/> is non-null
    /// and the target type has <see cref="TypeMeta.ContentValidation"/>, numeric literals are
    /// re-interpreted as the expected type and typed constants are validated.
    /// </summary>
    private static TypedExpression ResolveLiteral(
        LiteralExpression lit,
        CheckContext ctx,
        TypeKind? expectedType,
        ImmutableArray<DeclaredQualifierMeta>? qualifiers)
    {
        if (expectedType == TypeKind.Choice && IsChoiceLiteralToken(lit.LiteralKind))
            return ResolveChoiceLiteral(lit);

        return lit.LiteralKind switch
        {
            TokenKind.StringLiteral => new TypedLiteral(TypeKind.String, lit.Text, lit.Span),
            TokenKind.True          => new TypedLiteral(TypeKind.Boolean, true, lit.Span),
            TokenKind.False         => new TypedLiteral(TypeKind.Boolean, false, lit.Span),
            TokenKind.NumberLiteral => ResolveNumericLiteral(lit, expectedType),

            // Typed constants: resolve with content validation from expectedType context
            TokenKind.TypedConstant      => ResolveTypedConstant(lit, ctx, expectedType, qualifiers),

            // TypedConstantStart is now handled by InterpolatedTypedConstantExpression dispatch;
            // if it still appears as a bare LiteralExpression, treat as error
            TokenKind.TypedConstantStart => new TypedErrorExpression(lit.Span),

            _ => new TypedErrorExpression(lit.Span),
        };
    }

    private static bool IsChoiceLiteralToken(TokenKind kind) =>
        Types.All.Any(meta => meta.ChoiceLiteralTokens?.Contains(kind) == true);

    private static TypedExpression ResolveChoiceLiteral(LiteralExpression lit) => lit.LiteralKind switch
    {
        TokenKind.StringLiteral => new TypedLiteral(TypeKind.Choice, lit.Text, lit.Span),
        TokenKind.True          => new TypedLiteral(TypeKind.Choice, true, lit.Span),
        TokenKind.False         => new TypedLiteral(TypeKind.Choice, false, lit.Span),
        TokenKind.NumberLiteral when lit.Text.Contains('.')
            && decimal.TryParse(lit.Text, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var decVal)
            => new TypedLiteral(TypeKind.Choice, decVal, lit.Span),
        TokenKind.NumberLiteral when long.TryParse(lit.Text, System.Globalization.CultureInfo.InvariantCulture, out var intVal)
            => new TypedLiteral(TypeKind.Choice, intVal, lit.Span),
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

            if (expectedType == TypeKind.Number)
                return new TypedLiteral(TypeKind.Number, decVal, lit.Span);

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
    private static TypedExpression ResolveTypedConstant(
        LiteralExpression lit,
        CheckContext ctx,
        TypeKind? expectedType,
        ImmutableArray<DeclaredQualifierMeta>? qualifiers)
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

        var typedConstantContext = qualifiers is not null
            ? new TypedConstantContext(DeclaredQualifiers: qualifiers)
            : null;
        var result = TypedConstantValidation.Validate(cv, rawText, targetType, typedConstantContext);

        if (result.IsValid)
            return new TypedTypedConstant(targetType, rawText, result.Value, lit.Span);

        foreach (var diagnostic in result.Diagnostics)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidTypedConstantContent, lit.Span,
                    rawText, diagnostic.Message));
        }

        return new TypedErrorExpression(lit.Span);
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
        BinaryOperationExpression bin,
        TypedExpression left,
        TypedExpression right,
        OperatorMeta opMeta,
        CheckContext ctx)
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
                var result = TryResolveBinaryWithWidening(opMeta.Kind, left.ResultType, retried.ResultType);
                if (result is not null)
                {
                    return CreateResolvedBinaryOp(bin.Span, opMeta, result, left, retried, ctx);
                }
            }
        }

        if (leftIsLiteral && !rightIsLiteral)
        {
            var retried = Resolve(bin.Left, ctx, right.ResultType);
            if (retried is not TypedErrorExpression && retried.ResultType != left.ResultType)
            {
                var result = TryResolveBinaryWithWidening(opMeta.Kind, retried.ResultType, right.ResultType);
                if (result is not null)
                {
                    return CreateResolvedBinaryOp(bin.Span, opMeta, result, retried, right, ctx);
                }
            }
        }

        return null;
    }

    private static TypedBinaryOp CreateResolvedBinaryOp(
        SourceSpan span,
        OperatorMeta opMeta,
        BinaryOperationMeta resolvedOperation,
        TypedExpression left,
        TypedExpression right,
        CheckContext ctx) =>
        new(
            ResolveBinaryResultType(opMeta, resolvedOperation, left, ctx),
            resolvedOperation.Kind,
            left,
            right,
            ResultQualifier: MapQualifierBinding(resolvedOperation),
            ProofRequirements: resolvedOperation.ProofRequirements.ToImmutableArray(),
            Span: span);

    private static TypedBinaryOp CreateSyntheticBinaryOp(
        SourceSpan span,
        OperatorMeta opMeta,
        OperationKind operationKind,
        TypedExpression left,
        TypedExpression right,
        CheckContext ctx,
        ImmutableArray<ProofRequirement>? proofRequirements = null) =>
        new(
            ResolveBinaryResultType(opMeta, resolvedOperation: null, left, ctx),
            operationKind,
            left,
            right,
            ResultQualifier: null,
            ProofRequirements: proofRequirements ?? ImmutableArray<ProofRequirement>.Empty,
            Span: span);

    private static TypeKind ResolveBinaryResultType(
        OperatorMeta opMeta,
        BinaryOperationMeta? resolvedOperation,
        TypedExpression left,
        CheckContext ctx) =>
        opMeta.ResultTypePolicy switch
        {
            ResultTypePolicy.Fixed => opMeta.ResultType ?? resolvedOperation?.Result ?? TypeKind.Error,
            ResultTypePolicy.BothOperands => opMeta.ResultType ?? resolvedOperation?.Result ?? left.ResultType,
            ResultTypePolicy.LhsType => left.ResultType,
            ResultTypePolicy.ElementType => GetElementType(left, ctx) ?? TypeKind.Error,
            ResultTypePolicy.OperationResult => resolvedOperation?.Result ?? TypeKind.Error,
            _ => TypeKind.Error,
        };

    private static TypeKind ResolveUnaryResultType(
        OperatorMeta opMeta,
        UnaryOperationMeta? resolvedOperation,
        TypedExpression operand) =>
        opMeta.ResultTypePolicy switch
        {
            ResultTypePolicy.Fixed => opMeta.ResultType ?? resolvedOperation?.Result ?? TypeKind.Error,
            ResultTypePolicy.LhsType => operand.ResultType,
            ResultTypePolicy.OperationResult => resolvedOperation?.Result ?? TypeKind.Error,
            _ => resolvedOperation?.Result ?? TypeKind.Error,
        };

    private static (TypedExpression Left, TypedExpression Right) RetryChoiceComparisonLiterals(
        BinaryOperationExpression bin,
        OperatorMeta opMeta,
        TypedExpression left,
        TypedExpression right,
        CheckContext ctx)
    {
        if (opMeta.Family != OperatorFamily.Comparison)
            return (left, right);

        if (left.ResultType == TypeKind.Choice && bin.Right is LiteralExpression)
            right = Resolve(bin.Right, ctx, TypeKind.Choice);

        if (right.ResultType == TypeKind.Choice && bin.Left is LiteralExpression)
            left = Resolve(bin.Left, ctx, TypeKind.Choice);

        return (left, right);
    }

    private static TypedExpression? TryResolveCatalogBinaryWithoutOperation(
        BinaryOperationExpression bin,
        OperatorMeta opMeta,
        TypedExpression left,
        TypedExpression right,
        CheckContext ctx)
    {
        if (opMeta is { Family: OperatorFamily.Membership, ResultTypePolicy: ResultTypePolicy.Fixed } &&
            TryResolveContainsOperandTypes(left, right, ctx))
        {
            return CreateSyntheticBinaryOp(bin.Span, opMeta, OperationKind.CollectionContains, left, right, ctx);
        }

        if (opMeta is { Family: OperatorFamily.Membership, ResultTypePolicy: ResultTypePolicy.ElementType } &&
            TryResolveLookupOperandTypes(left, right, ctx))
        {
            return CreateSyntheticBinaryOp(bin.Span, opMeta, OperationKind.LookupAccess, left, right, ctx);
        }

        return null;
    }

    private static bool TryResolveContainsOperandTypes(TypedExpression collection, TypedExpression candidate, CheckContext ctx)
    {
        if (!TryGetContainsCandidateTypes(collection, ctx, out var primaryType, out var alternateType))
            return false;

        return IsAssignable(candidate.ResultType, primaryType)
            || (alternateType is { } alt && IsAssignable(candidate.ResultType, alt));
    }

    private static bool TryResolveLookupOperandTypes(TypedExpression lookup, TypedExpression key, CheckContext ctx)
    {
        var keyType = GetKeyType(lookup, ctx);
        return lookup.ResultType == TypeKind.Lookup
            && keyType is { } resolvedKeyType
            && IsAssignable(key.ResultType, resolvedKeyType);
    }

    private static bool TryGetContainsCandidateTypes(
        TypedExpression collection,
        CheckContext ctx,
        out TypeKind primaryType,
        out TypeKind? alternateType)
    {
        primaryType = TypeKind.Error;
        alternateType = null;

        if (collection is TypedListLiteral list)
        {
            primaryType = list.ElementType;
            return true;
        }

        if (collection is not TypedFieldRef fieldRef || !ctx.FieldLookup.TryGetValue(fieldRef.FieldName, out var field))
            return false;

        switch (field.ResolvedType)
        {
            case TypeKind.Set:
            case TypeKind.Queue:
            case TypeKind.Stack:
            case TypeKind.Log:
            case TypeKind.Bag:
            case TypeKind.List:
            case TypeKind.QueueBy:
                primaryType = field.ElementType ?? TypeKind.Error;
                return field.ElementType is not null;
            case TypeKind.LogBy:
                primaryType = field.ElementType ?? TypeKind.Error;
                alternateType = field.KeyType;
                return field.ElementType is not null || field.KeyType is not null;
            case TypeKind.Lookup:
                primaryType = field.KeyType ?? TypeKind.Error;
                return field.KeyType is not null;
            default:
                return false;
        }
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
                return new TypedFieldRef(binding.Type, name, binding.IsCaseInsensitive, null, id.Span);
        }

        // 2. Event args (second priority)
        if (ctx.CurrentEventArgs is not null &&
            ctx.CurrentEventArgs.TryGetValue(name, out var arg))
        {
            ctx.ArgReferences.Add(new ArgReference(arg, id.Span));
            return new TypedArgRef(arg.ResolvedType, arg.EventName, arg.Name, arg.DeclaredQualifiers, id.Span);
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
                ctx.CIFields.Contains(field.Name), field.DeclaredQualifiers, id.Span);
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
        // Map TokenKind → OperatorKind via the Operators catalog
        if (!Operators.ByToken.TryGetValue((bin.Operator, Arity.Binary), out var opMeta))
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.TypeMismatch, bin.Span,
                    "binary operator", bin.Operator.ToString()));
            return new TypedErrorExpression(bin.Span);
        }

        var leftDiagStart = ctx.Diagnostics.Count;
        var left = Resolve(bin.Left, ctx);
        var leftDiagEnd = ctx.Diagnostics.Count;

        // Proactive context propagation: when the right operand is a typed constant,
        // use the left operand's resolved type as expectedType context.
        // Typed constants require type context to resolve — without it they emit PRE0052.
        var right = (bin.Right is LiteralExpression { LiteralKind: TokenKind.TypedConstant } && left is not TypedErrorExpression)
            ? Resolve(bin.Right, ctx, left.ResultType)
            : Resolve(bin.Right, ctx);

        // Symmetric: retry left-side typed constants with right's type as context.
        if (left is TypedErrorExpression
            && bin.Left is LiteralExpression { LiteralKind: TokenKind.TypedConstant }
            && right is not TypedErrorExpression)
        {
            // Remove the stale PRE0052 diagnostic from the failed initial resolution
            if (leftDiagEnd > leftDiagStart)
                ctx.Diagnostics.RemoveRange(leftDiagStart, leftDiagEnd - leftDiagStart);
            left = Resolve(bin.Left, ctx, right.ResultType);
        }

        // D13: ErrorType propagation — if either operand is error, propagate
        if (left is TypedErrorExpression || right is TypedErrorExpression)
            return new TypedErrorExpression(bin.Span);

        (left, right) = RetryChoiceComparisonLiterals(bin, opMeta, left, right, ctx);
        if (left is TypedErrorExpression || right is TypedErrorExpression)
            return new TypedErrorExpression(bin.Span);

        // Attempt resolution: exact → left widen → right widen → both widen
        var result = TryResolveBinaryWithWidening(opMeta.Kind, left.ResultType, right.ResultType);
        if (result is not null)
            return CreateResolvedBinaryOp(bin.Span, opMeta, result, left, right, ctx);

        var catalogResolved = TryResolveCatalogBinaryWithoutOperation(bin, opMeta, left, right, ctx);
        if (catalogResolved is not null)
            return catalogResolved;

        // Slice 4: context retry — re-resolve literal operands with the other side's type as context
        var retried = TryContextRetryBinaryOp(bin, left, right, opMeta, ctx);
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
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.TypeMismatch, un.Span,
                    "unary operator", un.Operator.ToString()));
            return new TypedErrorExpression(un.Span);
        }

        var resolved = Operations.FindUnary(opMeta.Kind, operand.ResultType);
        if (resolved is not null)
        {
            return new TypedUnaryOp(
                ResolveUnaryResultType(opMeta, resolved, operand),
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

    // ════════════════════════════════════════════════════════════════════════
    //  Expression resolution — Slice 3: Functions, Accessors, Interpolated Strings
    // ════════════════════════════════════════════════════════════════════════

    private static void ValidateAssignmentQualifiers(
        TypedExpression value,
        string fieldName,
        ImmutableArray<DeclaredQualifierMeta> targetQualifiers,
        SourceSpan valueSpan,
        CheckContext ctx)
    {
        if (targetQualifiers.IsDefaultOrEmpty || value is TypedErrorExpression)
            return;

        if (value is TypedTypedConstant { ResultType: TypeKind.Quantity })
            return;

        // Binary/unary expressions: validate each qualified leaf operand against the target.
        // Scalar operands (no qualifiers) are skipped — e.g. `set usdField = usdField * 2` is valid.
        if (value is TypedBinaryOp or TypedUnaryOp)
        {
            foreach (var leaf in ExtractLeafOperands(value))
            {
                ValidateAssignmentQualifiers(leaf, fieldName, targetQualifiers, valueSpan, ctx);
            }
            return;
        }

        ImmutableArray<DeclaredQualifierMeta>? sourceQualifiers = value switch
        {
            TypedFieldRef { DeclaredQualifiers: { } fieldQualifiers } => fieldQualifiers,
            TypedArgRef { DeclaredQualifiers: { } argQualifiers } => argQualifiers,
            TypedTypedConstant
            {
                ResultType: TypeKind.Money,
                ParsedValue: ValueTuple<decimal, object?> (_, CurrencyEntry currency)
            } => [new DeclaredQualifierMeta.Currency(currency.AlphaCode)],
            _ => null,
        };

        if (sourceQualifiers is not { } qualifiers || qualifiers.IsDefaultOrEmpty)
            return;

        foreach (var targetQualifier in targetQualifiers)
        {
            switch (targetQualifier)
            {
                case DeclaredQualifierMeta.Dimension { DimensionName: var targetDimension }:
                {
                    string? sourceDimension = qualifiers
                        .OfType<DeclaredQualifierMeta.Dimension>()
                        .Select(q => q.DimensionName)
                        .FirstOrDefault()
                        ?? qualifiers
                            .OfType<DeclaredQualifierMeta.Unit>()
                            .Select(q => q.DimensionName)
                            .FirstOrDefault();

                    if (sourceDimension is not null
                        && !string.Equals(sourceDimension, targetDimension, StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.DimensionCategoryMismatch,
                                valueSpan,
                                sourceDimension,
                                targetDimension,
                                fieldName));
                    }

                    break;
                }

                case DeclaredQualifierMeta.Unit { UnitCode: var targetUnit }:
                {
                    var sourceUnit = qualifiers
                        .OfType<DeclaredQualifierMeta.Unit>()
                        .Select(q => q.UnitCode)
                        .FirstOrDefault();

                    if (sourceUnit is not null
                        && !string.Equals(sourceUnit, targetUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.QualifierMismatch,
                                valueSpan,
                                targetUnit,
                                fieldName));
                    }

                    break;
                }

                case DeclaredQualifierMeta.Currency { CurrencyCode: var targetCurrency }:
                {
                    var sourceCurrency = qualifiers
                        .OfType<DeclaredQualifierMeta.Currency>()
                        .Select(q => q.CurrencyCode)
                        .FirstOrDefault();

                    if (sourceCurrency is not null
                        && !string.Equals(sourceCurrency, targetCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.QualifierMismatch,
                                valueSpan,
                                targetCurrency,
                                fieldName));
                    }

                    break;
                }

                case DeclaredQualifierMeta.FromCurrency { CurrencyCode: var targetFromCurrency }:
                {
                    var sourceFromCurrency = qualifiers
                        .OfType<DeclaredQualifierMeta.FromCurrency>()
                        .Select(q => q.CurrencyCode)
                        .FirstOrDefault();

                    if (sourceFromCurrency is not null
                        && !string.Equals(sourceFromCurrency, targetFromCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.QualifierMismatch,
                                valueSpan,
                                targetFromCurrency,
                                fieldName));
                    }

                    break;
                }

                case DeclaredQualifierMeta.ToCurrency { CurrencyCode: var targetToCurrency }:
                {
                    var sourceToCurrency = qualifiers
                        .OfType<DeclaredQualifierMeta.ToCurrency>()
                        .Select(q => q.CurrencyCode)
                        .FirstOrDefault();

                    if (sourceToCurrency is not null
                        && !string.Equals(sourceToCurrency, targetToCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.QualifierMismatch,
                                valueSpan,
                                targetToCurrency,
                                fieldName));
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// Recursively extracts non-expression leaf operands from a binary/unary expression tree.
    /// For <see cref="TypedBinaryOp"/>, yields leaves from both sides.
    /// For <see cref="TypedUnaryOp"/>, yields leaves from the operand.
    /// All other typed expressions are yielded as leaves directly.
    /// </summary>
    private static IEnumerable<TypedExpression> ExtractLeafOperands(TypedExpression expression)
    {
        switch (expression)
        {
            case TypedBinaryOp binary:
                foreach (var leaf in ExtractLeafOperands(binary.Left))
                    yield return leaf;
                foreach (var leaf in ExtractLeafOperands(binary.Right))
                    yield return leaf;
                break;

            case TypedUnaryOp unary:
                foreach (var leaf in ExtractLeafOperands(unary.Operand))
                    yield return leaf;
                break;

            default:
                yield return expression;
                break;
        }
    }

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

    // ════════════════════════════════════════════════════════════════════════
    //  Interpolated typed constant resolution (Slice 2)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Types that do not support interpolation.</summary>
    private static readonly FrozenSet<TypeKind> InterpolationUnsupportedTypes = new[]
    {
        TypeKind.Date, TypeKind.Time, TypeKind.Instant,
        TypeKind.DateTime, TypeKind.ZonedDateTime, TypeKind.Timezone,
    }.ToFrozenSet();

    private static readonly IReadOnlyDictionary<TypeKind, string> InterpolationUnsupportedGuidance =
        new Dictionary<TypeKind, string>
        {
            [TypeKind.Date]          = "Date values like '2026-04-15' must be written as complete literals. To compute dates dynamically, use arithmetic: StartDate + '{n} days'.",
            [TypeKind.Time]          = "Time values like '14:30:00' must be written as complete literals. To compute times dynamically, use arithmetic: StartTime + '{n} hours'.",
            [TypeKind.Instant]       = "Instant values must be written as complete literals.",
            [TypeKind.DateTime]      = "DateTime values must be written as complete literals.",
            [TypeKind.ZonedDateTime] = "ZonedDateTime values must be written as complete literals.",
            [TypeKind.Timezone]      = "Timezone values like 'America/New_York' must be written as complete literals.",
        };

    /// <summary>
    /// Classifies text content for grammar matching.
    /// </summary>
    private enum TextClass { Numeric = 1, CurrencyCode, UnitName, Empty, Other }

    private static bool IsNumericLiteral(string s) =>
        s.Length > 0 && decimal.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture, out _);

    private static bool IsIntegerLiteral(string s) =>
        s.Length > 0 && long.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out _);

    private static bool IsCurrencyCode(string s) =>
        s.Length > 0 && CurrencyCatalog.All.ContainsKey(s.ToUpperInvariant());

    private static bool IsUnitName(string s) =>
        s.Length > 0 && (UcumCatalog.IsValid(s) || TemporalUnits.TryGet(s, out _));

    /// <summary>
    /// A segment-aware form pattern. For N holes, the pattern describes:
    /// text₀, slot₁, text₁, slot₂, ..., textₙ where each textᵢ is a classifier
    /// for the text between holes. The slots array has N entries, the texts array has N+1 entries.
    /// </summary>
    private sealed record SegmentForm(TextMatch[] TextChecks, InterpolationSlotKind[] Slots);

    /// <summary>Checker for text content between holes.</summary>
    private delegate bool TextMatch(string text);

    // ── Text matchers ──────────────────────────────────────────────────────

    private static bool MatchEmpty(string text) => text.Length == 0 || string.IsNullOrWhiteSpace(text);
    private static bool MatchSpaceCurrency(string text) => text.StartsWith(" ", StringComparison.Ordinal) && IsCurrencyCode(text.TrimStart());
    private static bool MatchSpaceUnit(string text) => text.StartsWith(" ", StringComparison.Ordinal) && IsUnitName(text.TrimStart());
    private static bool MatchNumericSpace(string text) => text.EndsWith(" ", StringComparison.Ordinal) && IsNumericLiteral(text.TrimEnd());
    private static bool MatchIntegerSpace(string text) => text.EndsWith(" ", StringComparison.Ordinal) && IsIntegerLiteral(text.TrimEnd());
    private static bool MatchSpaceCurrencySlash(string text)
    {
        if (!text.StartsWith(" ", StringComparison.Ordinal)) return false;
        var rest = text.TrimStart();
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && rest.Length == slashIdx + 1;
    }
    private static bool MatchSpaceCurrencySlashUnit(string text)
    {
        if (!text.StartsWith(" ", StringComparison.Ordinal)) return false;
        var rest = text.TrimStart();
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && IsUnitName(rest[(slashIdx + 1)..]);
    }
    private static bool MatchSlashCurrency(string text) => text.StartsWith("/", StringComparison.Ordinal) && IsCurrencyCode(text[1..]);
    private static bool MatchSlashUnit(string text) => text.StartsWith("/", StringComparison.Ordinal) && IsUnitName(text[1..]);
    private static bool MatchSlash(string text) => text == "/";
    private static bool MatchNumericSpaceCurrencySlash(string text)
    {
        if (!text.EndsWith("/", StringComparison.Ordinal)) return false;
        var content = text[..^1];
        var spaceIdx = content.IndexOf(' ');
        return spaceIdx > 0 && IsNumericLiteral(content[..spaceIdx]) && IsCurrencyCode(content[(spaceIdx + 1)..]);
    }
    private static bool MatchNumericSpaceCurrencySlashUnit(string text)
    {
        var spaceIdx = text.IndexOf(' ');
        if (spaceIdx <= 0) return false;
        if (!IsNumericLiteral(text[..spaceIdx])) return false;
        var rest = text[(spaceIdx + 1)..];
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && IsUnitName(rest[(slashIdx + 1)..]);
    }
    private static bool MatchNumericSpaceUnitSlash(string text)
    {
        if (!text.EndsWith("/", StringComparison.Ordinal)) return false;
        var content = text[..^1];
        var spaceIdx = content.IndexOf(' ');
        return spaceIdx > 0 && IsNumericLiteral(content[..spaceIdx]) && IsUnitName(content[(spaceIdx + 1)..]);
    }

    // ── Per-type valid form tables (segment-aware) ────────────────────────

    private static readonly SegmentForm[] MoneyForms =
    [
        // M1: H[whole-value]  — text: ("", "")
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        // M2: H[magnitude] " USD"  — text: ("", " USD")
        new([MatchEmpty, MatchSpaceCurrency], [InterpolationSlotKind.Magnitude]),
        // M3: "100 " H[currency]  — text: ("100 ", "")
        new([MatchNumericSpace, MatchEmpty], [InterpolationSlotKind.Currency]),
        // M4: H[magnitude] " " H[currency]  — text: ("", " ", "")
        new([MatchEmpty, (string s) => s == " ", MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Currency]),
    ];

    private static readonly SegmentForm[] QuantityForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        new([MatchEmpty, MatchSpaceUnit], [InterpolationSlotKind.Magnitude]),
        new([MatchNumericSpace, MatchEmpty], [InterpolationSlotKind.Unit]),
        new([MatchEmpty, (string s) => s == " ", MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Unit]),
        new([MatchEmpty, (string s) => s == " ", MatchSlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.NumeratorUnit, InterpolationSlotKind.DenominatorUnit]),
        new([MatchNumericSpace, MatchSlash, MatchEmpty], [InterpolationSlotKind.NumeratorUnit, InterpolationSlotKind.DenominatorUnit]),
        new([MatchNumericSpace, MatchSlashUnit], [InterpolationSlotKind.NumeratorUnit]),
        new([MatchNumericSpaceUnitSlash, MatchEmpty], [InterpolationSlotKind.DenominatorUnit]),
    ];

    private static readonly SegmentForm[] PriceForms =
    [
        // P1: H[whole-value]
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        // P2: H[magnitude] " USD/each"
        new([MatchEmpty, MatchSpaceCurrencySlashUnit], [InterpolationSlotKind.Magnitude]),
        // P3: "4.17 " H[currency] "/each"
        new([MatchNumericSpace, MatchSlashUnit], [InterpolationSlotKind.Currency]),
        // P4: "4.17 USD/" H[unit]
        new([MatchNumericSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Unit]),
        // P5: "4.17 " H[currency] "/" H[unit]
        new([MatchNumericSpace, MatchSlash, MatchEmpty], [InterpolationSlotKind.Currency, InterpolationSlotKind.Unit]),
        // P6: H[magnitude] " " H[currency] "/each"
        new([MatchEmpty, (string s) => s == " ", MatchSlashUnit], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Currency]),
        // P7: H[magnitude] " USD/" H[unit]
        new([MatchEmpty, MatchSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Unit]),
        // P8: H[magnitude] " " H[currency] "/" H[unit]
        new([MatchEmpty, (string s) => s == " ", MatchSlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Currency, InterpolationSlotKind.Unit]),
    ];

    private static readonly SegmentForm[] ExchangeRateForms =
    [
        // X1: H[whole-value]
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        // X2: H[magnitude] " USD/EUR"
        new([MatchEmpty, MatchSpaceCurrencySlashCurrency], [InterpolationSlotKind.Magnitude]),
        // X3: "1.08 " H[from-currency] "/EUR"
        new([MatchNumericSpace, MatchSlashCurrency], [InterpolationSlotKind.FromCurrency]),
        // X4: "1.08 USD/" H[to-currency]
        new([MatchNumericSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.ToCurrency]),
        // X5: "1.08 " H[from-currency] "/" H[to-currency]
        new([MatchNumericSpace, MatchSlash, MatchEmpty], [InterpolationSlotKind.FromCurrency, InterpolationSlotKind.ToCurrency]),
        // X6: H[magnitude] " " H[from-currency] "/EUR"
        new([MatchEmpty, (string s) => s == " ", MatchSlashCurrency], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.FromCurrency]),
        // X7: H[magnitude] " USD/" H[to-currency]
        new([MatchEmpty, MatchSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.ToCurrency]),
        // X8: H[magnitude] " " H[from-currency] "/" H[to-currency]
        new([MatchEmpty, (string s) => s == " ", MatchSlash, MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.FromCurrency, InterpolationSlotKind.ToCurrency]),
    ];

    private static bool MatchSpaceCurrencySlashCurrency(string text)
    {
        if (!text.StartsWith(" ", StringComparison.Ordinal)) return false;
        var rest = text.TrimStart();
        var slashIdx = rest.IndexOf('/');
        return slashIdx > 0 && IsCurrencyCode(rest[..slashIdx]) && IsCurrencyCode(rest[(slashIdx + 1)..]);
    }

    private static readonly SegmentForm[] SingleComponentForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
    ];

    private static readonly SegmentForm[] UnitOfMeasureForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        new([MatchEmpty, MatchSlash, MatchEmpty], [InterpolationSlotKind.NumeratorUnit, InterpolationSlotKind.DenominatorUnit]),
    ];

    private static readonly SegmentForm[] TemporalSingleForms =
    [
        new([MatchEmpty, MatchEmpty], [InterpolationSlotKind.WholeValue]),
        new([MatchEmpty, MatchSpaceUnit], [InterpolationSlotKind.Magnitude]),
        new([MatchIntegerSpace, MatchEmpty], [InterpolationSlotKind.Unit]),
        new([MatchEmpty, (string s) => s == " ", MatchEmpty], [InterpolationSlotKind.Magnitude, InterpolationSlotKind.Unit]),
    ];

    /// <summary>
    /// Gets the applicable form patterns for a target type.
    /// Returns null if the type doesn't support interpolation at all.
    /// </summary>
    private static SegmentForm[]? GetFormsForType(TypeKind type) => type switch
    {
        TypeKind.Money         => MoneyForms,
        TypeKind.Quantity      => QuantityForms,
        TypeKind.Price         => PriceForms,
        TypeKind.ExchangeRate  => ExchangeRateForms,
        TypeKind.Currency      => SingleComponentForms,
        TypeKind.UnitOfMeasure => UnitOfMeasureForms,
        TypeKind.Dimension     => SingleComponentForms,
        TypeKind.Duration      => TemporalSingleForms,
        TypeKind.Period        => TemporalSingleForms,
        _ => null,
    };

    /// <summary>
    /// Returns the set of types compatible with a given slot for a given target type.
    /// </summary>
    private static bool IsSlotCompatible(InterpolationSlotKind slot, TypeKind holeType, TypeKind targetType, bool isTemporal)
    {
        if (holeType == TypeKind.String) return false;
        if (holeType == TypeKind.Error) return true; // suppress cascading

        return slot switch
        {
            InterpolationSlotKind.Magnitude when isTemporal
                => holeType == TypeKind.Integer,
            InterpolationSlotKind.Magnitude
                => holeType is TypeKind.Integer or TypeKind.Decimal or TypeKind.Number,
            InterpolationSlotKind.Currency or InterpolationSlotKind.FromCurrency or InterpolationSlotKind.ToCurrency
                => holeType == TypeKind.Currency,
            InterpolationSlotKind.Unit or InterpolationSlotKind.NumeratorUnit or InterpolationSlotKind.DenominatorUnit
                => holeType == TypeKind.UnitOfMeasure,
            InterpolationSlotKind.WholeValue
                => holeType == targetType,
            _ => false,
        };
    }

    private static string SlotCompatibleTypesDescription(InterpolationSlotKind slot, bool isTemporal) => slot switch
    {
        InterpolationSlotKind.Magnitude when isTemporal => "integer",
        InterpolationSlotKind.Magnitude                 => "integer, decimal, or number",
        InterpolationSlotKind.Currency or InterpolationSlotKind.FromCurrency or InterpolationSlotKind.ToCurrency
                                                        => "currency",
        InterpolationSlotKind.Unit or InterpolationSlotKind.NumeratorUnit or InterpolationSlotKind.DenominatorUnit
                                                        => "unitofmeasure",
        InterpolationSlotKind.WholeValue                => "the target type",
        _ => "unknown",
    };

    /// <summary>
    /// Try to match a segment sequence against a segment-aware form pattern.
    /// The segment sequence is 2N+1 elements for N holes (alternating text, hole, text, ...).
    /// The form has N+1 text checks and N slot assignments.
    /// Returns slot assignments on success, null on failure.
    /// </summary>
    private static List<(int HoleIndex, InterpolationSlotKind Slot)>? TryMatchForm(
        SegmentForm form,
        ImmutableArray<InterpolationSegment> segments)
    {
        int expectedHoles = form.Slots.Length;
        int expectedSegments = 2 * expectedHoles + 1;
        if (segments.Length != expectedSegments) return null;

        // Validate text segments
        for (int i = 0; i < form.TextChecks.Length; i++)
        {
            int segIdx = i * 2;
            if (i > expectedHoles) segIdx = segments.Length - 1; // shouldn't happen
            else segIdx = i <= expectedHoles ? i * 2 : segments.Length - 1;

            // For N holes: text segments are at indices 0, 2, 4, ..., 2N
            // TextChecks[0] → segment[0], TextChecks[1] → segment[2], ..., TextChecks[N] → segment[2N]
            int textSegIdx = i * 2;
            if (textSegIdx >= segments.Length) return null;
            if (segments[textSegIdx] is not TextSegment text) return null;
            if (!form.TextChecks[i](text.Text)) return null;
        }

        // Validate hole segments exist and collect slots
        var slots = new List<(int, InterpolationSlotKind)>(expectedHoles);
        for (int h = 0; h < expectedHoles; h++)
        {
            int holeSegIdx = h * 2 + 1;
            if (segments[holeSegIdx] is not HoleSegment) return null;
            slots.Add((h, form.Slots[h]));
        }

        return slots;
    }

    /// <summary>
    /// Attempts to match temporal compound forms (D5–D7 / Pe5–Pe7) for duration/period.
    /// Pattern: '{h} hours + {m} minutes' → TextSegment(""), HoleSegment(h), TextSegment(" hours + "), HoleSegment(m), TextSegment(" minutes")
    /// </summary>
    private static List<(int HoleIndex, InterpolationSlotKind Slot)>? TryMatchTemporalCompound(
        ImmutableArray<InterpolationSegment> segments)
    {
        // Must have at least 2 holes → 5 segments
        if (segments.Length < 5) return null;

        // Must have odd count (2N+1)
        if (segments.Length % 2 == 0) return null;

        int holeCount = segments.Length / 2;
        var slots = new List<(int, InterpolationSlotKind)>();

        for (int i = 0; i < segments.Length; i++)
        {
            if (i % 2 == 0)
            {
                // Text segment
                if (segments[i] is not TextSegment text) return null;
                string t = text.Text;

                if (i == 0)
                {
                    // First text: empty (hole follows) or integer followed by space
                    if (t.Length == 0 || string.IsNullOrWhiteSpace(t))
                    {
                        // Next is a hole
                    }
                    else if (t.EndsWith(" ", StringComparison.Ordinal) && IsIntegerLiteral(t.TrimEnd()))
                    {
                        // Static first component — no hole for this
                    }
                    else return null;
                }
                else if (i == segments.Length - 1)
                {
                    // Last text: must end with a temporal unit name
                    var trimmed = t.Trim();
                    if (trimmed.Length == 0 || !(TemporalUnits.TryGet(trimmed, out _) || UcumCatalog.IsValid(trimmed)))
                        return null;
                }
                else
                {
                    // Middle text between holes: must contain " <unit> + " pattern
                    if (!t.Contains(" + ", StringComparison.Ordinal)) return null;
                    // The text is like " hours + " — unit before the +, space after
                    var plusIdx = t.IndexOf(" + ", StringComparison.Ordinal);
                    var unitPart = t[..plusIdx].Trim();
                    if (unitPart.Length == 0 || !(TemporalUnits.TryGet(unitPart, out _) || UcumCatalog.IsValid(unitPart)))
                        return null;
                }
            }
            else
            {
                // Hole segment
                if (segments[i] is not HoleSegment) return null;
                slots.Add((i / 2, InterpolationSlotKind.Magnitude));
            }
        }

        // Check first text: if it was numeric (static), need to remove
        // the first hole index mapping since there's no hole for that component
        if (segments[0] is TextSegment ft && ft.Text.Length > 0 && !string.IsNullOrWhiteSpace(ft.Text))
        {
            // First component is static — all holes are for subsequent components
            // Already handled correctly since we only add holes at odd indices
        }

        return slots.Count >= 1 ? slots : null;
    }

    /// <summary>
    /// Full type-grammar matching resolution for interpolated typed constants.
    /// Implements the algorithm from the plan §Type-Grammar Matching Algorithm.
    /// </summary>
    private static TypedExpression ResolveInterpolatedTypedConstant(
        InterpolatedTypedConstantExpression expr,
        CheckContext ctx,
        TypeKind? expectedType,
        ImmutableArray<DeclaredQualifierMeta>? qualifiers)
    {
        // Step 1–2: Target type from context
        if (expectedType is not { } targetType || targetType == TypeKind.Error)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UnresolvedTypedConstant, expr.Span, "(interpolated)"));
            return new TypedErrorExpression(expr.Span);
        }

        // Step 3: Unsupported types
        if (InterpolationUnsupportedTypes.Contains(targetType))
        {
            var guidance = InterpolationUnsupportedGuidance.GetValueOrDefault(targetType, "");
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InterpolationNotSupportedForType, expr.Span,
                    Types.GetMeta(targetType).DisplayName, guidance));
            return new TypedErrorExpression(expr.Span);
        }

        // Step 4: Extract segments
        var segments = expr.Segments;

        // Step 5–6: Match against form grammars
        var forms = GetFormsForType(targetType);
        if (forms is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidInterpolatedTypedConstantForm, expr.Span,
                    Types.GetMeta(targetType).DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        List<(int HoleIndex, InterpolationSlotKind Slot)>? matchedSlots = null;

        foreach (var form in forms)
        {
            matchedSlots = TryMatchForm(form, segments);
            if (matchedSlots is not null) break;
        }

        // For duration/period, also try compound forms if single-component didn't match
        if (matchedSlots is null && targetType is TypeKind.Duration or TypeKind.Period)
        {
            matchedSlots = TryMatchTemporalCompound(segments);
        }

        // Step 7: No match → structural error
        if (matchedSlots is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidInterpolatedTypedConstantForm, expr.Span,
                    Types.GetMeta(targetType).DisplayName));
            return new TypedErrorExpression(expr.Span);
        }

        // Step 8: Resolve each hole expression and validate slot compatibility
        bool isTemporal = targetType is TypeKind.Duration or TypeKind.Period;
        var holes = segments.OfType<HoleSegment>().ToArray();
        var typedSlots = ImmutableArray.CreateBuilder<TypedInterpolationSlot>(matchedSlots.Count);
        bool hasError = false;

        foreach (var (holeIndex, slotKind) in matchedSlots)
        {
            if (holeIndex >= holes.Length)
            {
                hasError = true;
                continue;
            }

            var hole = holes[holeIndex];

            // Determine advisory expected type for the hole
            TypeKind? slotExpectedType = slotKind switch
            {
                InterpolationSlotKind.Magnitude when isTemporal => TypeKind.Integer,
                InterpolationSlotKind.Magnitude                 => TypeKind.Decimal,
                InterpolationSlotKind.Currency                  => TypeKind.Currency,
                InterpolationSlotKind.FromCurrency              => TypeKind.Currency,
                InterpolationSlotKind.ToCurrency                => TypeKind.Currency,
                InterpolationSlotKind.Unit                      => TypeKind.UnitOfMeasure,
                InterpolationSlotKind.NumeratorUnit             => TypeKind.UnitOfMeasure,
                InterpolationSlotKind.DenominatorUnit           => TypeKind.UnitOfMeasure,
                InterpolationSlotKind.WholeValue                => targetType,
                _ => null,
            };

            var resolved = Resolve(hole.Expression, ctx, slotExpectedType);

            // Check slot compatibility
            if (resolved is not TypedErrorExpression && !IsSlotCompatible(slotKind, resolved.ResultType, targetType, isTemporal))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch, hole.Span,
                        Types.GetMeta(resolved.ResultType).DisplayName,
                        slotKind.ToString().ToLowerInvariant(),
                        SlotCompatibleTypesDescription(slotKind, isTemporal)));
                hasError = true;
            }

            typedSlots.Add(new TypedInterpolationSlot(resolved, slotKind));
        }

        if (hasError)
            return new TypedErrorExpression(expr.Span);

        // Step 9: Dimension-unit consistency for unit-slot holes
        foreach (var slot in typedSlots)
        {
            if (slot.SlotKind != InterpolationSlotKind.Unit) continue;
            if (slot.Expression.ResultType != TypeKind.UnitOfMeasure) continue;

            ValidateUnitSlotDimensionConsistency(slot.Expression, qualifiers, expr.Span, ctx);
        }

        return new TypedInterpolatedTypedConstant(typedSlots.ToImmutable(), targetType, expr.Span);
    }

    /// <summary>
    /// Dimension-unit consistency check for unit-slot holes in interpolated typed constants.
    /// Structural AST pattern match per plan §Dimension-Unit Consistency Validation.
    /// </summary>
    private static void ValidateUnitSlotDimensionConsistency(
        TypedExpression holeExpr,
        ImmutableArray<DeclaredQualifierMeta>? targetQualifiers,
        SourceSpan span,
        CheckContext ctx)
    {
        if (holeExpr is not TypedMemberAccess
            {
                ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: QualifierAxis.Unit }
            } memberAccess)
        {
            return;
        }

        ImmutableArray<DeclaredQualifierMeta>? sourceQualifiers;
        string sourceName;

        if (memberAccess.Object is TypedFieldRef { DeclaredQualifiers: { } fq, FieldName: var fn })
        {
            sourceQualifiers = fq;
            sourceName = fn;
        }
        else if (memberAccess.Object is TypedArgRef { DeclaredQualifiers: { } aq })
        {
            sourceQualifiers = aq;
            sourceName = "(arg)";
        }
        else
        {
            return;
        }

        string? sourceDimension = sourceQualifiers.Value
            .OfType<DeclaredQualifierMeta.Dimension>().Select(q => q.DimensionName).FirstOrDefault()
            ?? sourceQualifiers.Value
            .OfType<DeclaredQualifierMeta.Unit>().Select(q => q.DimensionName).FirstOrDefault();

        if (sourceDimension is null || targetQualifiers is not { } tq || tq.IsDefaultOrEmpty)
            return;

        string? targetDimension = tq
            .OfType<DeclaredQualifierMeta.Dimension>().Select(q => q.DimensionName).FirstOrDefault()
            ?? tq
            .OfType<DeclaredQualifierMeta.Unit>().Select(q => q.DimensionName).FirstOrDefault();

        if (targetDimension is null) return;

        if (!string.Equals(sourceDimension, targetDimension, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.DimensionMismatchInUnitSlot, span,
                    sourceName, sourceDimension, targetDimension));
        }
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
}
