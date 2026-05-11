using System.Collections.Frozen;
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

    private static TypedErrorExpression ResolveInterpolatedTypedConstantStub(
        LiteralExpression lit,
        CheckContext ctx,
        TypeKind? expectedType)
    {
        var expected = expectedType is { } type && type != TypeKind.Error
            ? Types.GetMeta(type).DisplayName
            : "typed constant";

        ctx.Diagnostics.Add(
            Diagnostics.Create(DiagnosticCode.TypeMismatch, lit.Span,
                expected, "interpolated typed constant (not yet supported)"));
        return new TypedErrorExpression(lit.Span);
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
            TokenKind.TypedConstantStart => ResolveInterpolatedTypedConstantStub(lit, ctx, expectedType),

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
            }
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
