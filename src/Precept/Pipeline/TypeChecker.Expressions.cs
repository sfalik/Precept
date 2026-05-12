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
            return new TypedTypedConstant(targetType, rawText, rawText, null, lit.Span);

        var typedConstantContext = qualifiers is not null
            ? new TypedConstantContext(DeclaredQualifiers: qualifiers)
            : null;
        var result = TypedConstantValidation.Validate(cv, rawText, targetType, typedConstantContext);

        if (result.IsValid)
        {
            var declaredQualifiers = ExtractQualifiersFromParsedValue(targetType, result.Value);
            return new TypedTypedConstant(targetType, rawText, result.Value, declaredQualifiers, lit.Span);
        }

        foreach (var _ in result.Diagnostics)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.InvalidTypedConstantContent, lit.Span,
                    rawText, meta.DisplayName));
        }

        return new TypedErrorExpression(lit.Span);
    }

    /// <summary>
    /// Extract qualifier metadata from the validated parsed value of a typed constant.
    /// Returns qualifiers for Money (currency), Quantity (unit), and Price (currency + unit);
    /// returns null for types that carry no structured qualifier identity.
    /// </summary>
    private static ImmutableArray<DeclaredQualifierMeta>? ExtractQualifiersFromParsedValue(
        TypeKind type, object? parsedValue)
    {
        switch (type)
        {
            case TypeKind.Money:
                if (parsedValue is ValueTuple<decimal, object?>(_, CurrencyEntry moneyCurrency))
                    return [new DeclaredQualifierMeta.Currency(moneyCurrency.AlphaCode)];
                break;
            case TypeKind.Quantity:
                if (parsedValue is ValueTuple<decimal, UcumParsedUnit?>(_, UcumParsedUnit quantityUnit))
                    return [new DeclaredQualifierMeta.Unit(
                        quantityUnit.CanonicalCode,
                        UnitDimensionHelper.DeriveUnitDimensionName(quantityUnit))];
                break;
            case TypeKind.Price:
                if (parsedValue is ValueTuple<decimal, object?, UcumParsedUnit?>(_, CurrencyEntry priceCurrency, UcumParsedUnit priceUnit))
                    return
                    [
                        new DeclaredQualifierMeta.Currency(priceCurrency.AlphaCode),
                        new DeclaredQualifierMeta.Unit(
                            priceUnit.CanonicalCode,
                            UnitDimensionHelper.DeriveUnitDimensionName(priceUnit)),
                    ];
                break;
        }
        return null;
    }


    //  Context retry for binary operations (Slice 4)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempt to re-resolve a literal-like operand with <paramref name="expectedType"/> context
    /// when bottom-up binary operation resolution fails. This handles cases like
    /// <c>amount &gt; 100</c> where <c>amount: money</c> and <c>100</c> needs to be re-resolved as money,
    /// plus interpolated typed constants whose target type is only known from the other operand.
    /// </summary>
    private static TypedExpression? TryContextRetryBinaryOp(
        BinaryOperationExpression bin,
        TypedExpression left,
        TypedExpression right,
        OperatorMeta opMeta,
        CheckContext ctx)
    {
        bool leftNeedsContext = NeedsContextRetry(bin.Left);
        bool rightNeedsContext = NeedsContextRetry(bin.Right);

        if (!leftNeedsContext && !rightNeedsContext)
            return null;

        if (rightNeedsContext && !leftNeedsContext)
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

        if (leftNeedsContext && !rightNeedsContext)
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

    private static bool NeedsContextRetry(ParsedExpression expression) => expression is LiteralExpression { LiteralKind: TokenKind.TypedConstant }
        or InterpolatedTypedConstantExpression;

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

        // Proactive context propagation: when the right operand is a typed or interpolated typed constant,
        // use the left operand's resolved type as expectedType context.
        var right = (NeedsContextRetry(bin.Right) && left is not TypedErrorExpression)
            ? Resolve(bin.Right, ctx, left.ResultType)
            : Resolve(bin.Right, ctx);

        // Symmetric: retry left-side typed/interpolated typed constants with right's type as context.
        if (left is TypedErrorExpression
            && NeedsContextRetry(bin.Left)
            && right is not TypedErrorExpression)
        {
            // Remove the stale unresolved-type diagnostic from the failed initial resolution.
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
    private static QualifierBinding? MapQualifierBinding(BinaryOperationMeta meta)
    {
        if (meta.ResultQualifierPolicy == ResultQualifierPolicy.CompoundUnitCancellation)
            return new CompoundUnitCancellationRequired();

        if (meta.ResultQualifierPolicy == ResultQualifierPolicy.InheritFromQualifiedOperand)
            return new QualifiedOperandInherited();

        if (meta.ResultQualifierPolicy == ResultQualifierPolicy.CurrencyConversion)
            return new CurrencyConversionRequired();

        return meta.Match switch
        {
            QualifierMatch.Same      => new SameQualifierRequired(),
            QualifierMatch.Different => null, // different-qualifier operations produce unqualified results
            _                        => null, // QualifierMatch.Any — no qualifier constraint
        };
    }

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
}
