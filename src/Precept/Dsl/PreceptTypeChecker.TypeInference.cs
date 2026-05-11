using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

// Partial-class file for PreceptTypeChecker — expression type-inference.
// Contains the expression kind-validation and kind-inference surface:
// ValidateExpression, TryInferKind, TryInferFunctionCallKind, and TryInferBinaryKind.
// This group has the highest fan-in: callers live in Main, and the methods
// call into Narrowing (ApplyNarrowing), ProofChecks (AssessDivisorSafety,
// AssessNonnegativeArgument), and Helpers.
internal static partial class PreceptTypeChecker
{
    private static void ValidateExpression(
        PreceptExpression expression,
        string expressionText,
        int sourceLine,
        GlobalProofContext context,
        StaticValueKind expectedKind,
        string expectedLabel,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        string? stateContext = null,
        string? eventName = null,
        bool isBooleanRulePosition = false)
    {
        if (!TryInferKind(expression, context, out var actualKind, out var diagnostic))
        {
            diagnostics.Add(diagnostic! with
            {
                Line = sourceLine > 0 ? sourceLine : diagnostic.Line,
                Column = diagnostic.Column != 0 ? diagnostic.Column : expression.Position?.StartColumn ?? 0,
                EndColumn = diagnostic.EndColumn != 0 ? diagnostic.EndColumn : expression.Position?.EndColumn ?? 0,
                StateContext = stateContext
            });
            return;
        }

        // Collect any non-blocking warnings from inference (e.g., C93 unproven divisor).
        if (diagnostic is not null)
        {
            diagnostics.Add(diagnostic with
            {
                Line = sourceLine > 0 ? sourceLine : diagnostic.Line,
                Column = diagnostic.Column != 0 ? diagnostic.Column : expression.Position?.StartColumn ?? 0,
                EndColumn = diagnostic.EndColumn != 0 ? diagnostic.EndColumn : expression.Position?.EndColumn ?? 0,
                StateContext = stateContext
            });
        }

        if (IsAssignable(actualKind, expectedKind))
        {
            expressions.Add(new PreceptTypeExpressionInfo(
                sourceLine,
                expressionText,
                actualKind,
                expectedLabel,
                stateContext,
                eventName));
            return;
        }

        LanguageConstraint constraint;
        string message;
        if (isBooleanRulePosition)
        {
            constraint = DiagnosticCatalog.C46;
            message = $"{expectedLabel} must be a boolean expression, but expression produces {FormatKinds(actualKind)}.";
        }
        else
        {
            constraint = HasFlag(actualKind, StaticValueKind.Null) && !HasFlag(expectedKind, StaticValueKind.Null)
                ? DiagnosticCatalog.C42
                // SYNC:CONSTRAINT:C60
                : IsNarrowingToInteger(actualKind, expectedKind)
                    ? DiagnosticCatalog.C60
                    : DiagnosticCatalog.C39;
            message = constraint == DiagnosticCatalog.C60
                ? BuildC60Message(actualKind, expectedLabel)
                : TryBuildNumericMismatchMessage(actualKind, expectedKind, expectedLabel)
                  ?? $"{expectedLabel} type mismatch: expected {FormatKinds(expectedKind)} but expression produces {FormatKinds(actualKind)}.";
        }

        expressions.Add(new PreceptTypeExpressionInfo(
            sourceLine,
            expressionText,
            actualKind,
            expectedLabel,
            stateContext,
            eventName));

        diagnostics.Add(PreceptValidationDiagnosticFactory.FromExpression(
            constraint,
            message,
            sourceLine,
            expression,
            stateContext));
    }

    private static bool TryInferKind(
        PreceptExpression expression,
        GlobalProofContext context,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;
        var symbols = context.Symbols;

        switch (expression)
        {
            case PreceptLiteralExpression literal:
                kind = MapLiteralKind(literal.Value);
                return true;

            case PreceptIdentifierExpression identifier:
            {
                var key = identifier.SubMember is not null
                    ? $"{identifier.Name}.{identifier.Member}.{identifier.SubMember}"
                    : identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
                if (!symbols.TryGetValue(key, out kind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C38,
                        $"unknown identifier '{key}'.",
                        0,
                        Column: identifier.Position?.StartColumn ?? 0);
                    return false;
                }

                // C56: .length on a nullable string requires an explicit null guard before access.
                // SYNC:CONSTRAINT:C56
                if (identifier.Member == "length" &&
                    symbols.TryGetValue(identifier.Name, out var baseKind) &&
                    HasFlag(baseKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C56,
                        DiagnosticCatalog.C56.FormatMessage(("field", identifier.Name)),
                        0,
                        Column: identifier.Position?.StartColumn ?? 0);
                    return false;
                }

                // C56 three-level form: EventName.ArgName.length — nullable arg requires null guard.
                // SYNC:CONSTRAINT:C56
                if (identifier.SubMember == "length")
                {
                    var argKey = $"{identifier.Name}.{identifier.Member}";
                    if (symbols.TryGetValue(argKey, out var argKind) && HasFlag(argKind, StaticValueKind.Null))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C56,
                            DiagnosticCatalog.C56.FormatMessage(("field", argKey)),
                            0,
                            Column: identifier.Position?.StartColumn ?? 0);
                        return false;
                    }
                }

                return true;
            }

            case PreceptParenthesizedExpression parenthesized:
                return TryInferKind(parenthesized.Inner, context, out kind, out diagnostic);

            case PreceptUnaryExpression unary:
            {
                if (!TryInferKind(unary.Operand, context, out var operandKind, out diagnostic))
                    return false;

                if (unary.Operator == "not")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Boolean))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C40,
                            "operator 'not' requires boolean operand.",
                            0,
                            Column: unary.Operand.Position?.StartColumn ?? 0);
                        return false;
                    }

                    kind = StaticValueKind.Boolean;
                    return true;
                }

                if (unary.Operator == "-")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Number) &&
                        !IsExactly(operandKind, StaticValueKind.Integer))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C40,
                            "unary '-' requires numeric operand.",
                            0,
                            Column: unary.Operand.Position?.StartColumn ?? 0);
                        return false;
                    }

                    kind = operandKind; // preserve Integer or Number
                    return true;
                }

                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C40,
                    $"unsupported unary operator '{unary.Operator}'.",
                    0,
                    Column: unary.Position?.StartColumn ?? 0);
                return false;
            }

            case PreceptBinaryExpression binary:
                return TryInferBinaryKind(binary, context, out kind, out diagnostic);

            case PreceptFunctionCallExpression fn:
                return TryInferFunctionCallKind(fn, context, out kind, out diagnostic);

            // SYNC:CONSTRAINT:C78
            // SYNC:CONSTRAINT:C79
            case PreceptConditionalExpression cond:
            {
                // Infer condition type
                if (!TryInferKind(cond.Condition, context, out var condKind, out diagnostic))
                    return false;

                // C78: condition must be non-nullable boolean
                if (!IsExactly(condKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C78,
                        DiagnosticCatalog.C78.FormatMessage(("actual", FormatKinds(condKind))),
                        0,
                        Column: cond.Condition.Position?.StartColumn ?? 0);
                    return false;
                }

                // Null-narrow symbols for then-branch (condition assumed true)
                var thenContext = ApplyNarrowing(cond.Condition, context, assumeTrue: true);
                if (!TryInferKind(cond.ThenBranch, thenContext, out var thenKind, out diagnostic))
                    return false;

                // Else branch uses original symbols (no reverse narrowing)
                if (!TryInferKind(cond.ElseBranch, context, out var elseKind, out diagnostic))
                    return false;

                // C79: branches must produce compatible scalar types (with integer widening)
                var thenBase = thenKind & ~StaticValueKind.Null;
                var elseBase = elseKind & ~StaticValueKind.Null;
                StaticValueKind resultBase;
                if (thenBase == elseBase)
                {
                    resultBase = thenBase;
                }
                else if (IsAssignable(thenBase, elseBase))
                {
                    // then-branch widens to else-branch's type (e.g. integer → number)
                    resultBase = elseBase;
                }
                else if (IsAssignable(elseBase, thenBase))
                {
                    // else-branch widens to then-branch's type (e.g. integer → number)
                    resultBase = thenBase;
                }
                else
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C79,
                        BuildC79Message(thenKind, elseKind),
                        0,
                        Column: cond.ElseBranch.Position?.StartColumn ?? 0);
                    return false;
                }

                // Result type is the wider base + Null if either branch is nullable
                kind = resultBase | ((thenKind | elseKind) & StaticValueKind.Null);
                return true;
            }

            default:
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C39,
                    "unsupported expression node.",
                    0,
                    Column: expression.Position?.StartColumn ?? 0);
                return false;
        }
    }

    private static bool TryInferFunctionCallKind(
        PreceptFunctionCallExpression fn,
        GlobalProofContext context,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;
        var symbols = context.Symbols;

        // SYNC:CONSTRAINT:C71 — unknown function name
        if (!FunctionRegistry.TryGetFunction(fn.Name, out var funcDef))
        {
            diagnostic = new PreceptValidationDiagnostic(
                DiagnosticCatalog.C71,
                $"Unknown function '{fn.Name}'.",
                0,
                Column: fn.Position?.StartColumn ?? 0);
            return false;
        }

        // Infer all argument kinds before overload resolution.
        var argKinds = new StaticValueKind[fn.Arguments.Length];
        for (int i = 0; i < fn.Arguments.Length; i++)
        {
            if (!TryInferKind(fn.Arguments[i], context, out argKinds[i], out diagnostic))
                return false;

            // SYNC:CONSTRAINT:C77 — nullable arguments
            if ((argKinds[i] & StaticValueKind.Null) != 0)
            {
                var argName = fn.Arguments[i] is PreceptIdentifierExpression id ? id.Name : $"argument {i + 1}";
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C77,
                    $"Function '{fn.Name}' does not accept nullable arguments. '{argName}' may be null. Add a null check.",
                    0,
                    Column: fn.Arguments[i].Position?.StartColumn ?? 0);
                return false;
            }
        }

        // Find the best matching overload (arity + argument types).
        FunctionOverload? matched = null;
        foreach (var overload in funcDef.Overloads)
        {
            bool arityOk = overload.MinArity.HasValue
                ? fn.Arguments.Length >= overload.MinArity.Value
                : fn.Arguments.Length == overload.Parameters.Length;

            if (!arityOk)
                continue;

            bool typesOk = true;
            if (overload.MinArity.HasValue)
            {
                // Variadic: single parameter type applies to all arguments.
                var paramType = overload.Parameters[0].AcceptedTypes;
                for (int i = 0; i < fn.Arguments.Length; i++)
                {
                    if ((argKinds[i] & paramType) == 0)
                    {
                        typesOk = false;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < overload.Parameters.Length; i++)
                {
                    if ((argKinds[i] & overload.Parameters[i].AcceptedTypes) == 0)
                    {
                        typesOk = false;
                        break;
                    }
                }
            }

            if (typesOk)
            {
                matched = overload;
                break;
            }
        }

        if (matched is null)
        {
            // SYNC:CONSTRAINT:C75 — pow exponent must be integer
            if (fn.Name == "pow" && fn.Arguments.Length == 2 &&
                IsNumericKind(argKinds[1]) && !IsExactly(argKinds[1], StaticValueKind.Integer))
            {
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C75,
                    $"pow() exponent must be integer type, but got {KindLabel(argKinds[1])}.",
                    0,
                    Column: fn.Arguments[1].Position?.StartColumn ?? 0);
                return false;
            }

            // Try to pinpoint a specific parameter type mismatch (C73).
            var arityMatch = funcDef.Overloads.LastOrDefault(o =>
                o.MinArity.HasValue
                    ? fn.Arguments.Length >= o.MinArity.Value
                    : fn.Arguments.Length == o.Parameters.Length);

            if (arityMatch is not null)
            {
                // SYNC:CONSTRAINT:C73 — argument type mismatch
                if (arityMatch.MinArity.HasValue)
                {
                    var param = arityMatch.Parameters[0];
                    for (int i = 0; i < fn.Arguments.Length; i++)
                    {
                        if ((argKinds[i] & param.AcceptedTypes) == 0)
                        {
                            diagnostic = new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C73,
                                $"{fn.Name}() no matching overload: {param.Name} argument expects {KindLabel(param.AcceptedTypes)} but got {KindLabel(argKinds[i])}.",
                                0,
                                Column: fn.Arguments[i].Position?.StartColumn ?? 0);
                            return false;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < arityMatch.Parameters.Length; i++)
                    {
                        var param = arityMatch.Parameters[i];
                        if ((argKinds[i] & param.AcceptedTypes) == 0)
                        {
                            diagnostic = new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C73,
                                $"{fn.Name}() no matching overload: {param.Name} argument expects {KindLabel(param.AcceptedTypes)} but got {KindLabel(argKinds[i])}.",
                                0,
                                Column: fn.Arguments[i].Position?.StartColumn ?? 0);
                            return false;
                        }
                    }
                }
            }

            // SYNC:CONSTRAINT:C72 — no matching overload
            diagnostic = new PreceptValidationDiagnostic(
                DiagnosticCatalog.C72,
                $"{fn.Name}() called with {fn.Arguments.Length} argument(s), but no matching overload found.",
                0,
                Column: fn.Position?.StartColumn ?? 0);
            return false;
        }

        // Validate special parameter constraints on the matched overload.
        var paramLoop = matched.MinArity.HasValue
            ? Enumerable.Range(0, fn.Arguments.Length).Select(i => (ParamIndex: 0, ArgIndex: i))
            : Enumerable.Range(0, matched.Parameters.Length).Select(i => (ParamIndex: i, ArgIndex: i));

        foreach (var (paramIndex, argIndex) in paramLoop)
        {
            var param = matched.Parameters[paramIndex];

            // SYNC:CONSTRAINT:C74 — round precision must be non-negative integer literal
            if (param.Constraint == FunctionArgConstraint.MustBeIntegerLiteral)
            {
                if (fn.Arguments[argIndex] is not PreceptLiteralExpression { Value: long lv } || lv < 0)
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C74,
                        "round() precision argument must be a non-negative integer literal.",
                        0,
                        Column: fn.Arguments[argIndex].Position?.StartColumn ?? 0);
                    return false;
                }
            }

            // SYNC:CONSTRAINT:C76 — sqrt requires non-negative proof
            if (param.Constraint == FunctionArgConstraint.RequiresNonNegativeProof)
            {
                var arg = fn.Arguments[argIndex];
                var assessment = AssessNonnegativeArgument(arg, context);
                if (assessment is not null && assessment.Outcome != ProofOutcome.Satisfied)
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        assessment.DiagnosticCode,
                        ProofDiagnosticRenderer.Render(assessment),
                        0,
                        Column: arg.Position?.StartColumn ?? 0,
                        EndColumn: arg.Position?.EndColumn ?? 0,
                        Assessment: assessment);
                    return false;
                }
            }
        }

        kind = matched.ReturnType;
        return true;
    }

    private static bool TryInferBinaryKind(
        PreceptBinaryExpression binary,
        GlobalProofContext context,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;
        var symbols = context.Symbols;

        switch (binary.Operator)
        {
            case "and":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'and' requires boolean operands.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                var rightContext = ApplyNarrowing(binary.Left, context, assumeTrue: true);
                if (!TryInferKind(binary.Right, rightContext, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'and' requires boolean operands.", 0, Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "or":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'or' requires boolean operands.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                var rightContext = ApplyNarrowing(binary.Left, context, assumeTrue: false);
                if (!TryInferKind(binary.Right, rightContext, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'or' requires boolean operands.", 0, Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "+":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic) ||
                    !TryInferKind(binary.Right, context, out var rightKind, out diagnostic))
                    return false;

                var stringCandidate = IsExactly(leftKind, StaticValueKind.String) && IsExactly(rightKind, StaticValueKind.String);

                if (stringCandidate)
                {
                    kind = StaticValueKind.String;
                    return true;
                }

                if (IsNumericKind(leftKind) && IsNumericKind(rightKind))
                {
                    kind = ResolveNumericResultKind(leftKind, rightKind);
                    return true;
                }

                diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator '+' requires number+number or string+string.", 0, Column: binary.Position?.StartColumn ?? 0);
                return false;
            }

            case "-":
            case "*":
            case "/":
            case "%":
            case ">":
            case ">=":
            case "<":
            case "<=":
            {
                if (!TryInferKind(binary.Left, context, out var leftKind, out diagnostic) ||
                    !TryInferKind(binary.Right, context, out var rightKind, out diagnostic))
                    return false;

                // Choice-type ordinal checks apply only to the comparison operators, not arithmetic.
                if (binary.Operator is ">" or ">=" or "<" or "<=")
                {
                    var leftBase = leftKind & ~StaticValueKind.Null;
                    var rightBase = rightKind & ~StaticValueKind.Null;

                    // SYNC:CONSTRAINT:C65: ordinal operator on a choice field that lacks 'ordered'
                    if (leftBase == StaticValueKind.UnorderedChoice)
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C65,
                            DiagnosticCatalog.C65.FormatMessage(("operator", binary.Operator)),
                            0,
                            Column: binary.Left.Position?.StartColumn ?? 0);
                        return false;
                    }

                    // SYNC:CONSTRAINT:C67: ordinal comparison between two choice fields — rank is field-local
                    if (leftBase == StaticValueKind.OrderedChoice && rightBase == StaticValueKind.OrderedChoice)
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C67,
                            DiagnosticCatalog.C67.FormatMessage(("operator", binary.Operator)),
                            0,
                            Column: binary.Position?.StartColumn ?? 0);
                        return false;
                    }

                    // Valid ordinal comparison: ordered choice field vs string literal (or plain string)
                    if (leftBase == StaticValueKind.OrderedChoice
                        && (rightBase == StaticValueKind.String || rightBase == StaticValueKind.UnorderedChoice))
                    {
                        kind = StaticValueKind.Boolean;
                        return true;
                    }
                }

                if (!IsNumericKind(leftKind) || !IsNumericKind(rightKind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' requires numeric operands.",
                        0,
                        Column: binary.Position?.StartColumn ?? 0);
                    return false;
                }

                // SYNC:CONSTRAINT:C92 — provably-zero divisor
                // SYNC:CONSTRAINT:C93 — unproven divisor safety
                // Principle #8 ("never guess"): C92 is error (provably zero — contradiction).
                // C93 is error (unproven divisor — risks IEEE 754 Infinity/NaN at runtime).
                // Unified assessment: C92 = proven contradiction, C93 = unresolved obligation.
                if (binary.Operator is "/" or "%")
                {
                    var assessment = AssessDivisorSafety(binary.Right, context);
                    if (assessment is not null && assessment.Outcome != ProofOutcome.Satisfied)
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            assessment.DiagnosticCode,
                            ProofDiagnosticRenderer.Render(assessment),
                            0,
                            Column: binary.Right.Position?.StartColumn ?? 0,
                            EndColumn: binary.Right.Position?.EndColumn ?? 0,
                            Assessment: assessment);
                        if (assessment.Outcome == ProofOutcome.Contradiction)
                            return false;
                    }
                }

                kind = binary.Operator is ">" or ">=" or "<" or "<="
                    ? StaticValueKind.Boolean
                    : ResolveNumericResultKind(leftKind, rightKind);
                return true;
            }

            case "==":
            case "!=":
            {
                if (!TryInferKind(binary.Left, context, out var leftEqKind, out diagnostic) ||
                    !TryInferKind(binary.Right, context, out var rightEqKind, out diagnostic))
                    return false;

                // Normalize choice kinds to String for equality compatibility:
                // 'Status == "Active"' is valid whether Status is ordered or unordered choice.
                var leftEqFamily = NormalizeChoiceKind(leftEqKind & ~StaticValueKind.Null);
                var rightEqFamily = NormalizeChoiceKind(rightEqKind & ~StaticValueKind.Null);
                var leftIsNull = leftEqKind == StaticValueKind.Null;
                var rightIsNull = rightEqKind == StaticValueKind.Null;

                // null literal compared to a non-nullable operand is always wrong
                if (leftIsNull && !HasFlag(rightEqKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' cannot compare non-nullable {FormatKinds(rightEqKind)} with null.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                if (rightIsNull && !HasFlag(leftEqKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' cannot compare non-nullable {FormatKinds(leftEqKind)} with null.", 0, Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                // Cross-type equality: both sides have non-null scalar families that differ
                // Allow Integer <=> Number mix (widening)
                var isIntNumberMix =
                    (leftEqFamily == StaticValueKind.Integer && rightEqFamily == StaticValueKind.Number) ||
                    (leftEqFamily == StaticValueKind.Number  && rightEqFamily == StaticValueKind.Integer);
                // Allow Decimal <=> Integer mix (widening) and Decimal <=> Decimal
                var isDecimalMix =
                    (leftEqFamily == StaticValueKind.Decimal && rightEqFamily == StaticValueKind.Integer) ||
                    (leftEqFamily == StaticValueKind.Integer && rightEqFamily == StaticValueKind.Decimal) ||
                    (leftEqFamily == StaticValueKind.Decimal && rightEqFamily == StaticValueKind.Decimal);
                if (!isIntNumberMix && !isDecimalMix && leftEqFamily != StaticValueKind.None && rightEqFamily != StaticValueKind.None && leftEqFamily != rightEqFamily)
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' requires operands of the same type, but found {FormatKinds(leftEqKind)} and {FormatKinds(rightEqKind)}.", 0, Column: binary.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "contains":
            {
                if (binary.Left is not PreceptIdentifierExpression { Member: null } collectionIdentifier)
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "'contains' requires a collection field on the left side.", 0, Column: binary.Left.Position?.StartColumn ?? 0);
                    return false;
                }

                if (!TryInferKind(binary.Right, context, out var rightKind, out diagnostic))
                    return false;

                var collectionKey = $"{collectionIdentifier.Name}.count";
                if (!symbols.ContainsKey(collectionKey))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C38, $"unknown identifier '{collectionIdentifier.Name}'.", 0, Column: collectionIdentifier.Position?.StartColumn ?? 0);
                    return false;
                }

                var innerKeyCandidates = new[]
                {
                    $"{collectionIdentifier.Name}.min",
                    $"{collectionIdentifier.Name}.peek",
                    $"{collectionIdentifier.Name}.max"
                };

                var innerKind = innerKeyCandidates
                    .Where(symbols.ContainsKey)
                    .Select(key => symbols[key])
                    .DefaultIfEmpty(StaticValueKind.None)
                    .First();

                if (innerKind != StaticValueKind.None && !IsAssignable(rightKind, innerKind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C41,
                        $"operator 'contains' requires RHS of type {FormatKinds(innerKind)} but expression produces {FormatKinds(rightKind)}.",
                        0,
                        Column: binary.Right.Position?.StartColumn ?? 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            default:
                diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, $"unsupported binary operator '{binary.Operator}'.", 0, Column: binary.Position?.StartColumn ?? 0);
                return false;
        }
    }
}
