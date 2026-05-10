using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal static class SyntaxSelectionBuilder
{
    internal static SelectionRange? BuildSelectionRange(Compilation compilation, Position position)
    {
        if (!TryFindTokenSpan(compilation.Tokens.Tokens, position, out var tokenSpan))
        {
            return null;
        }

        var spans = new List<SourceSpan> { tokenSpan };
        var expressionSite = FindExpressionSite(compilation, position, tokenSpan);
        if (expressionSite is not null)
        {
            spans.Add(expressionSite.Value.Expression.Span);
            spans.Add(expressionSite.Value.Slot.Span);
            spans.Add(expressionSite.Value.Construct.Span);
        }
        else if (FindContainingConstruct(compilation, position) is { } construct)
        {
            spans.Add(construct.Span);
        }

        return BuildChain(spans);
    }

    private static SelectionRange BuildChain(IReadOnlyList<SourceSpan> spans)
    {
        SelectionRange? current = null;

        for (var index = spans.Count - 1; index >= 0; index--)
        {
            current = current is null
                ? new SelectionRange
                {
                    Range = DiagnosticProjector.ToRange(spans[index]),
                }
                : new SelectionRange
                {
                    Range = DiagnosticProjector.ToRange(spans[index]),
                    Parent = current,
                };
        }

        return current!;
    }

    private static (ParsedConstruct Construct, SlotValue Slot, ParsedExpression Expression)? FindExpressionSite(
        Compilation compilation,
        Position position,
        SourceSpan tokenSpan)
    {
        return EnumerateExpressionSites(compilation)
            .Where(site => site.Expression.Span != tokenSpan)
            .Where(site => Contains(site.Expression.Span, position))
            .OrderBy(site => GetSpanWidth(site.Expression.Span))
            .ThenByDescending(site => site.Expression.Span.StartLine)
            .ThenByDescending(site => site.Expression.Span.StartColumn)
            .Cast<(ParsedConstruct Construct, SlotValue Slot, ParsedExpression Expression)?>()
            .FirstOrDefault();
    }

    private static IEnumerable<(ParsedConstruct Construct, SlotValue Slot, ParsedExpression Expression)> EnumerateExpressionSites(Compilation compilation)
    {
        foreach (var construct in compilation.ConstructManifest.Constructs)
        {
            foreach (var slot in construct.Slots)
            {
                foreach (var expression in EnumerateSlotExpressions(slot))
                {
                    yield return (construct, slot, expression);
                }
            }
        }
    }

    private static IEnumerable<ParsedExpression> EnumerateSlotExpressions(SlotValue slot) => slot switch
    {
        GuardClauseSlot guard => EnumerateExpressionTree(guard.Expression),
        ComputeExpressionSlot compute => EnumerateExpressionTree(compute.Expression),
        EnsureClauseSlot ensure => EnumerateExpressionTree(ensure.Expression),
        RuleExpressionSlot rule => EnumerateExpressionTree(rule.Expression),
        ActionChainSlot actionChain => EnumerateActionExpressions(actionChain.Actions),
        _ => [],
    };

    private static IEnumerable<ParsedExpression> EnumerateActionExpressions(ImmutableArray<ParsedAction> actions)
    {
        foreach (var action in actions)
        {
            foreach (var expression in action switch
            {
                AssignAction assign => EnumerateExpressions(assign.Target, assign.Value),
                CollectionValueAction collectionValue => EnumerateExpressions(collectionValue.Target, collectionValue.Value),
                CollectionIntoAction collectionInto => EnumerateExpressions(collectionInto.Target, collectionInto.IntoTarget),
                FieldOnlyAction fieldOnly => EnumerateExpressionTree(fieldOnly.Target),
                CollectionValueByAction collectionValueBy => EnumerateExpressions(collectionValueBy.Target, collectionValueBy.Value, collectionValueBy.OrderingKey),
                InsertAtAction insertAt => EnumerateExpressions(insertAt.Target, insertAt.Value, insertAt.Index),
                RemoveAtAction removeAt => EnumerateExpressions(removeAt.Target, removeAt.Index),
                PutKeyValueAction putKeyValue => EnumerateExpressions(putKeyValue.Target, putKeyValue.Key, putKeyValue.Value),
                CollectionIntoByAction collectionIntoBy => EnumerateExpressions(collectionIntoBy.Target, collectionIntoBy.IntoTarget, collectionIntoBy.OrderingCapture),
                _ => [],
            })
            {
                yield return expression;
            }
        }
    }

    private static IEnumerable<ParsedExpression> EnumerateExpressions(params ParsedExpression?[] expressions)
    {
        foreach (var expression in expressions)
        {
            if (expression is null)
            {
                continue;
            }

            foreach (var nested in EnumerateExpressionTree(expression))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<ParsedExpression> EnumerateExpressionTree(ParsedExpression expression)
    {
        yield return expression;

        switch (expression)
        {
            case GroupedExpression grouped:
                foreach (var nested in EnumerateExpressionTree(grouped.Inner))
                {
                    yield return nested;
                }
                break;

            case BinaryOperationExpression binary:
                foreach (var nested in EnumerateExpressionTree(binary.Left))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(binary.Right))
                {
                    yield return nested;
                }
                break;

            case UnaryOperationExpression unary:
                foreach (var nested in EnumerateExpressionTree(unary.Operand))
                {
                    yield return nested;
                }
                break;

            case MemberAccessExpression memberAccess:
                foreach (var nested in EnumerateExpressionTree(memberAccess.Target))
                {
                    yield return nested;
                }
                break;

            case ConditionalExpression conditional:
                foreach (var nested in EnumerateExpressions(conditional.Condition, conditional.ThenBranch, conditional.ElseBranch))
                {
                    yield return nested;
                }
                break;

            case FunctionCallExpression functionCall:
                foreach (var argument in functionCall.Arguments)
                {
                    foreach (var nested in EnumerateExpressionTree(argument))
                    {
                        yield return nested;
                    }
                }
                break;

            case MethodCallExpression methodCall:
                foreach (var nested in EnumerateExpressionTree(methodCall.Target))
                {
                    yield return nested;
                }

                foreach (var argument in methodCall.Arguments)
                {
                    foreach (var nested in EnumerateExpressionTree(argument))
                    {
                        yield return nested;
                    }
                }
                break;

            case ListLiteralExpression listLiteral:
                foreach (var element in listLiteral.Elements)
                {
                    foreach (var nested in EnumerateExpressionTree(element))
                    {
                        yield return nested;
                    }
                }
                break;

            case PostfixOperationExpression postfix:
                foreach (var nested in EnumerateExpressionTree(postfix.Operand))
                {
                    yield return nested;
                }
                break;

            case QuantifierExpression quantifier:
                foreach (var nested in EnumerateExpressions(quantifier.Collection, quantifier.Predicate))
                {
                    yield return nested;
                }
                break;

            case CIFunctionCallExpression ciFunctionCall:
                foreach (var argument in ciFunctionCall.Arguments)
                {
                    foreach (var nested in EnumerateExpressionTree(argument))
                    {
                        yield return nested;
                    }
                }
                break;

            case InterpolatedStringExpression interpolated:
                foreach (var hole in interpolated.Segments.OfType<HoleSegment>())
                {
                    foreach (var nested in EnumerateExpressionTree(hole.Expression))
                    {
                        yield return nested;
                    }
                }
                break;
        }
    }

    private static ParsedConstruct? FindContainingConstruct(Compilation compilation, Position position)
    {
        ParsedConstruct? best = null;

        foreach (var construct in compilation.ConstructManifest.Constructs)
        {
            if (!Contains(construct.Span, position))
            {
                continue;
            }

            if (best is null || GetSpanWidth(construct.Span) < GetSpanWidth(best.Span))
            {
                best = construct;
            }
        }

        return best;
    }

    private static bool TryFindTokenSpan(ImmutableArray<Token> tokens, Position position, out SourceSpan span)
    {
        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.NewLine or TokenKind.EndOfSource)
            {
                continue;
            }

            if (Contains(token.Span, position))
            {
                span = token.Span;
                return true;
            }
        }

        span = default;
        return false;
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

        if (line == span.StartLine && character < span.StartColumn)
        {
            return false;
        }

        if (line == span.EndLine && character >= span.EndColumn)
        {
            return false;
        }

        return true;
    }
}
