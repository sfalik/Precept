using System;
using System.Collections.Generic;

namespace StateMachine.Dsl;

internal static class DslExpressionRuntimeEvaluator
{
    internal sealed record EvaluationResult(bool Success, object? Value, string? Error)
    {
        internal static EvaluationResult Ok(object? value) => new(true, value, null);
        internal static EvaluationResult Fail(string error) => new(false, null, error);
    }

    public static EvaluationResult Evaluate(DslExpression expression, IReadOnlyDictionary<string, object?> context)
    {
        return expression switch
        {
            DslLiteralExpression literal => EvaluationResult.Ok(literal.Value),
            DslIdentifierExpression identifier => EvaluateIdentifier(identifier, context),
            DslParenthesizedExpression parenthesized => Evaluate(parenthesized.Inner, context),
            DslUnaryExpression unary => EvaluateUnary(unary, context),
            DslBinaryExpression binary => EvaluateBinary(binary, context),
            _ => EvaluationResult.Fail("unsupported expression node.")
        };
    }

    private static EvaluationResult EvaluateIdentifier(DslIdentifierExpression identifier, IReadOnlyDictionary<string, object?> context)
    {
        var key = identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
        if (!context.TryGetValue(key, out var value))
            return EvaluationResult.Fail($"data key '{key}' was not provided.");

        return EvaluationResult.Ok(value);
    }

    private static EvaluationResult EvaluateUnary(DslUnaryExpression unary, IReadOnlyDictionary<string, object?> context)
    {
        var operand = Evaluate(unary.Operand, context);
        if (!operand.Success)
            return operand;

        return unary.Operator switch
        {
            "!" => operand.Value is bool b
                ? EvaluationResult.Ok(!b)
                : EvaluationResult.Fail("operator '!' requires boolean operand."),
            "-" => TryToNumber(operand.Value, out var number)
                ? EvaluationResult.Ok(-number)
                : EvaluationResult.Fail("unary '-' requires numeric operand."),
            _ => EvaluationResult.Fail($"unsupported unary operator '{unary.Operator}'.")
        };
    }

    private static EvaluationResult EvaluateBinary(DslBinaryExpression binary, IReadOnlyDictionary<string, object?> context)
    {
        if (binary.Operator == "&&")
        {
            var left = Evaluate(binary.Left, context);
            if (!left.Success)
                return left;

            if (left.Value is not bool leftBool)
                return EvaluationResult.Fail("operator '&&' requires boolean operands.");

            if (!leftBool)
                return EvaluationResult.Ok(false);

            var right = Evaluate(binary.Right, context);
            if (!right.Success)
                return right;

            return right.Value is bool rightBool
                ? EvaluationResult.Ok(rightBool)
                : EvaluationResult.Fail("operator '&&' requires boolean operands.");
        }

        if (binary.Operator == "||")
        {
            var left = Evaluate(binary.Left, context);
            if (!left.Success)
                return left;

            if (left.Value is not bool leftBool)
                return EvaluationResult.Fail("operator '||' requires boolean operands.");

            if (leftBool)
                return EvaluationResult.Ok(true);

            var right = Evaluate(binary.Right, context);
            if (!right.Success)
                return right;

            return right.Value is bool rightBool
                ? EvaluationResult.Ok(rightBool)
                : EvaluationResult.Fail("operator '||' requires boolean operands.");
        }

        var leftValue = Evaluate(binary.Left, context);
        if (!leftValue.Success)
            return leftValue;

        var rightValue = Evaluate(binary.Right, context);
        if (!rightValue.Success)
            return rightValue;

        var leftOperand = leftValue.Value;
        var rightOperand = rightValue.Value;

        switch (binary.Operator)
        {
            case "+":
                if (leftOperand is string leftString && rightOperand is string rightString)
                    return EvaluationResult.Ok(leftString + rightString);

                if (TryToNumber(leftOperand, out var leftNumberForAdd) && TryToNumber(rightOperand, out var rightNumberForAdd))
                    return EvaluationResult.Ok(leftNumberForAdd + rightNumberForAdd);

                return EvaluationResult.Fail("operator '+' requires number+number or string+string.");

            case "-":
                if (TryToNumber(leftOperand, out var leftNumberForSub) && TryToNumber(rightOperand, out var rightNumberForSub))
                    return EvaluationResult.Ok(leftNumberForSub - rightNumberForSub);

                return EvaluationResult.Fail("operator '-' requires numeric operands.");

            case "*":
                if (TryToNumber(leftOperand, out var leftNumberForMul) && TryToNumber(rightOperand, out var rightNumberForMul))
                    return EvaluationResult.Ok(leftNumberForMul * rightNumberForMul);

                return EvaluationResult.Fail("operator '*' requires numeric operands.");

            case "/":
                if (TryToNumber(leftOperand, out var leftNumberForDiv) && TryToNumber(rightOperand, out var rightNumberForDiv))
                    return EvaluationResult.Ok(leftNumberForDiv / rightNumberForDiv);

                return EvaluationResult.Fail("operator '/' requires numeric operands.");

            case "%":
                if (TryToNumber(leftOperand, out var leftNumberForMod) && TryToNumber(rightOperand, out var rightNumberForMod))
                    return EvaluationResult.Ok(leftNumberForMod % rightNumberForMod);

                return EvaluationResult.Fail("operator '%' requires numeric operands.");

            case "==":
                if (TryToNumber(leftOperand, out var leftNumberForEq) && TryToNumber(rightOperand, out var rightNumberForEq))
                    return EvaluationResult.Ok(leftNumberForEq == rightNumberForEq);

                return EvaluationResult.Ok(Equals(leftOperand, rightOperand));

            case "!=":
                if (TryToNumber(leftOperand, out var leftNumberForNeq) && TryToNumber(rightOperand, out var rightNumberForNeq))
                    return EvaluationResult.Ok(leftNumberForNeq != rightNumberForNeq);

                return EvaluationResult.Ok(!Equals(leftOperand, rightOperand));

            case ">":
                if (TryToNumber(leftOperand, out var leftNumberForGt) && TryToNumber(rightOperand, out var rightNumberForGt))
                    return EvaluationResult.Ok(leftNumberForGt > rightNumberForGt);

                return EvaluationResult.Fail("operator '>' requires numeric operands.");

            case ">=":
                if (TryToNumber(leftOperand, out var leftNumberForGte) && TryToNumber(rightOperand, out var rightNumberForGte))
                    return EvaluationResult.Ok(leftNumberForGte >= rightNumberForGte);

                return EvaluationResult.Fail("operator '>=' requires numeric operands.");

            case "<":
                if (TryToNumber(leftOperand, out var leftNumberForLt) && TryToNumber(rightOperand, out var rightNumberForLt))
                    return EvaluationResult.Ok(leftNumberForLt < rightNumberForLt);

                return EvaluationResult.Fail("operator '<' requires numeric operands.");

            case "<=":
                if (TryToNumber(leftOperand, out var leftNumberForLte) && TryToNumber(rightOperand, out var rightNumberForLte))
                    return EvaluationResult.Ok(leftNumberForLte <= rightNumberForLte);

                return EvaluationResult.Fail("operator '<=' requires numeric operands.");

            default:
                return EvaluationResult.Fail($"unsupported binary operator '{binary.Operator}'.");
        }
    }

    private static bool TryToNumber(object? value, out double number)
    {
        switch (value)
        {
            case byte b: number = b; return true;
            case sbyte sb: number = sb; return true;
            case short s: number = s; return true;
            case ushort us: number = us; return true;
            case int i: number = i; return true;
            case uint ui: number = ui; return true;
            case long l: number = l; return true;
            case ulong ul: number = ul; return true;
            case float f: number = f; return true;
            case double d: number = d; return true;
            case decimal dec: number = (double)dec; return true;
            default:
                number = default;
                return false;
        }
    }
}
