using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal static class TypedConstantCollector
{
    internal static ImmutableArray<string> CollectByType(SemanticIndex index, TypeKind type) =>
        EnumerateTypedExpressions(index)
            .OfType<TypedTypedConstant>()
            .Where(constant => constant.ResultType == type)
            .Select(constant => constant.RawText)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToImmutableArray();

    internal static TypedTypedConstant? FindAtPosition(SemanticIndex index, Position position) =>
        EnumerateTypedExpressions(index)
            .OfType<TypedTypedConstant>()
            .Where(constant => Contains(constant.Span, position))
            .OrderBy(constant => GetSpanWidth(constant.Span))
            .ThenByDescending(constant => constant.Span.StartLine)
            .ThenByDescending(constant => constant.Span.StartColumn)
            .FirstOrDefault();

    /// <summary>
    /// Finds the innermost <see cref="InterpolatedTypedConstant"/> whose span contains
    /// <paramref name="position"/>. Used to look up the slot kind for a cursor inside a hole.
    /// </summary>
    internal static InterpolatedTypedConstant? FindInterpolatedAtPosition(SemanticIndex index, Position position) =>
        EnumerateTypedExpressions(index)
            .OfType<InterpolatedTypedConstant>()
            .Where(itc => Contains(itc.Span, position))
            .OrderBy(itc => GetSpanWidth(itc.Span))
            .ThenByDescending(itc => itc.Span.StartLine)
            .ThenByDescending(itc => itc.Span.StartColumn)
            .FirstOrDefault();

    private static IEnumerable<TypedExpression> EnumerateTypedExpressions(SemanticIndex semantics)
    {
        foreach (var field in semantics.Fields)
        {
            if (field.DefaultExpression is not null)
            {
                foreach (var expression in EnumerateExpressionTree(field.DefaultExpression))
                {
                    yield return expression;
                }
            }

            if (field.ComputedExpression is not null)
            {
                foreach (var expression in EnumerateExpressionTree(field.ComputedExpression))
                {
                    yield return expression;
                }
            }
        }

        foreach (var evt in semantics.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.DefaultExpression is null)
                {
                    continue;
                }

                foreach (var expression in EnumerateExpressionTree(arg.DefaultExpression))
                {
                    yield return expression;
                }
            }
        }

        foreach (var row in semantics.TransitionRows)
        {
            if (row.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(row.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateActionExpressions(row.Actions))
            {
                yield return expression;
            }
        }

        foreach (var rule in semantics.Rules)
        {
            foreach (var expression in EnumerateExpressionTree(rule.Condition))
            {
                yield return expression;
            }

            if (rule.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(rule.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateExpressionTree(rule.Message))
            {
                yield return expression;
            }
        }

        foreach (var ensure in semantics.Ensures)
        {
            foreach (var expression in EnumerateExpressionTree(ensure.Condition))
            {
                yield return expression;
            }

            if (ensure.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(ensure.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateExpressionTree(ensure.Message))
            {
                yield return expression;
            }
        }

        foreach (var accessMode in semantics.AccessModes)
        {
            if (accessMode.Guard is null)
            {
                continue;
            }

            foreach (var expression in EnumerateExpressionTree(accessMode.Guard))
            {
                yield return expression;
            }
        }

        foreach (var hook in semantics.StateHooks)
        {
            if (hook.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(hook.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateActionExpressions(hook.Actions))
            {
                yield return expression;
            }
        }

        foreach (var handler in semantics.EventHandlers)
        {
            foreach (var expression in EnumerateActionExpressions(handler.Actions))
            {
                yield return expression;
            }
        }
    }

    private static IEnumerable<TypedExpression> EnumerateActionExpressions(ImmutableArray<TypedAction> actions)
    {
        foreach (var action in actions)
        {
            if (action is not TypedInputAction input)
            {
                continue;
            }

            foreach (var expression in EnumerateExpressionTree(input.InputExpression))
            {
                yield return expression;
            }

            if (input.SecondaryExpression is null)
            {
                continue;
            }

            foreach (var expression in EnumerateExpressionTree(input.SecondaryExpression))
            {
                yield return expression;
            }
        }
    }

    private static IEnumerable<TypedExpression> EnumerateExpressionTree(TypedExpression expression)
    {
        yield return expression;

        switch (expression)
        {
            case TypedBinaryOp binary:
                foreach (var nested in EnumerateExpressionTree(binary.Left))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(binary.Right))
                {
                    yield return nested;
                }
                break;

            case TypedUnaryOp unary:
                foreach (var nested in EnumerateExpressionTree(unary.Operand))
                {
                    yield return nested;
                }
                break;

            case TypedFunctionCall functionCall:
                foreach (var argument in functionCall.Arguments)
                {
                    foreach (var nested in EnumerateExpressionTree(argument))
                    {
                        yield return nested;
                    }
                }
                break;

            case TypedMemberAccess memberAccess:
                foreach (var nested in EnumerateExpressionTree(memberAccess.Object))
                {
                    yield return nested;
                }
                break;

            case TypedConditional conditional:
                foreach (var nested in EnumerateExpressionTree(conditional.Condition))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(conditional.ThenBranch))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(conditional.ElseBranch))
                {
                    yield return nested;
                }
                break;

            case TypedQuantifier quantifier:
                foreach (var nested in EnumerateExpressionTree(quantifier.Collection))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(quantifier.Predicate))
                {
                    yield return nested;
                }
                break;

            case TypedInterpolatedString interpolatedString:
                foreach (var hole in interpolatedString.Segments.OfType<TypedHoleSegment>())
                {
                    foreach (var nested in EnumerateExpressionTree(hole.Expression))
                    {
                        yield return nested;
                    }
                }
                break;

            case TypedListLiteral listLiteral:
                foreach (var element in listLiteral.Elements)
                {
                    foreach (var nested in EnumerateExpressionTree(element))
                    {
                        yield return nested;
                    }
                }
                break;

            case TypedPostfixOp postfix:
                foreach (var nested in EnumerateExpressionTree(postfix.Operand))
                {
                    yield return nested;
                }
                break;
        }
    }

    private static int GetSpanWidth(SourceSpan span) =>
        (span.EndLine - span.StartLine) * 10000 + (span.EndColumn - span.StartColumn);

    private static bool Contains(SourceSpan span, Position position)
    {
        var line = position.Line + 1;
        var character = position.Character + 1;

        if (line < span.StartLine || line > span.EndLine)
        {
            return false;
        }

        if (span.StartLine == span.EndLine)
        {
            return character >= span.StartColumn && character < span.EndColumn;
        }

        if (line == span.StartLine)
        {
            return character >= span.StartColumn;
        }

        if (line == span.EndLine)
        {
            return character < span.EndColumn;
        }

        return true;
    }
}

