using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class Parser
{
    private sealed partial class ParserState
    {
        private SlotValue ParseActionChain(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Arrow)
                return MakeSentinel(slot);

            // Peek ahead: only enter action chain if token after arrow is an action keyword
            if (!Actions.ByTokenKind.ContainsKey(Peek(1).Kind))
                return MakeSentinel(slot);

            var actions = new List<ParsedAction>();
            var startSpan = Peek().Span;
            var lastSpan = startSpan;

            while (Peek().Kind == TokenKind.Arrow && !IsAtEnd)
            {
                // Before consuming arrow, verify next token is an action keyword
                if (!Actions.ByTokenKind.ContainsKey(Peek(1).Kind))
                    break;

                Advance(); // consume '->'
                var actionToken = Peek();
                if (Actions.ByTokenKind.TryGetValue(actionToken.Kind, out var actionMeta))
                {
                    var actionStartSpan = Advance().Span; // consume action keyword
                    var action = ParseActionByShape(actionMeta, actionStartSpan);
                    actions.Add(action);
                    lastSpan = action.Span;
                }
                else
                {
                    break;
                }
            }

            if (actions.Count == 0 && !slot.IsRequired)
                return MakeSentinel(slot);
            return new ActionChainSlot(actions.ToImmutableArray(),
                SourceSpan.Covering(startSpan, lastSpan));
        }

        /// <summary>
        /// Parses action operands based on ActionSyntaxShape from catalog metadata.
        /// </summary>
        private ParsedAction ParseActionByShape(ActionMeta meta, SourceSpan actionStartSpan)
        {
            var kind = meta.Kind;

            // Terminator for action expressions: next arrow or construct boundary
            Func<bool> isAtActionBoundary = () => Peek().Kind == TokenKind.Arrow || IsAtConstructBoundary();

            // Shape-specific separator tokens from catalog — only this shape's separators terminate the target.
            var separators = Actions.GetShapeMeta(meta.SyntaxShape).SeparatorTokens;

            switch (meta.SyntaxShape)
            {
                case ActionSyntaxShape.AssignValue:
                    return ParseAssignValueAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionValue:
                    return ParseCollectionValueAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionInto:
                    return ParseCollectionIntoAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.FieldOnly:
                    return ParseFieldOnlyAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionValueBy:
                    return ParseCollectionValueByAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.InsertAt:
                    return ParseInsertAtAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.RemoveAtIndex:
                    return ParseRemoveAtIndexAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.PutKeyValue:
                    return ParsePutKeyValueAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionIntoBy:
                    return ParseCollectionIntoByAction(kind, actionStartSpan, isAtActionBoundary, separators);
                default:
                {
                    // Unknown shape — produce malformed action
                    return new MalformedAction(kind, actionStartSpan);
                }
            }
        }

        private static FrozenSet<TokenKind> GetSecondarySeparators(ActionKind primaryKind, int slotIndex)
        {
            if (!Actions.SecondaryByPrimaryActionKind.TryGetValue(primaryKind, out var secondaries))
                return Enumerable.Empty<TokenKind>().ToFrozenSet();

            return secondaries
                .Select(secondary => Actions.GetShapeMeta(secondary.SyntaxShape))
                .Where(shape => shape.Slots.Length > slotIndex && shape.Slots[slotIndex].PrecedingSeparator is not null)
                .Select(shape => shape.Slots[slotIndex].PrecedingSeparator!.Value)
                .Distinct()
                .ToFrozenSet();
        }

        private static bool TryGetSecondaryAction(ActionKind primaryKind, int slotIndex, TokenKind separator, out ActionMeta secondary)
        {
            secondary = null!;

            if (!Actions.SecondaryByPrimaryActionKind.TryGetValue(primaryKind, out var secondaries))
                return false;

            foreach (var candidate in secondaries)
            {
                var slots = Actions.GetShapeMeta(candidate.SyntaxShape).Slots;
                if (slots.Length > slotIndex && slots[slotIndex].PrecedingSeparator == separator)
                {
                    secondary = candidate;
                    return true;
                }
            }

            return false;
        }

        [HandlesCatalogMember(ActionSyntaxShape.AssignValue)]
        private ParsedAction ParseAssignValueAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field = expression
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.AssignValue).Slots;
            var target = ParseActionTarget(separators, isAtActionBoundary);
            Expect(slots[1].PrecedingSeparator!.Value); // '='
            var value = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, value.Span);
            return new AssignAction(kind, target, value, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionValue)]
        private ParsedAction ParseCollectionValueAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field expression
            var target = ParseActionTarget(separators, isAtActionBoundary);

            if (TryGetSecondaryAction(kind, 1, Peek().Kind, out var secondaryAfterTarget))
            {
                return secondaryAfterTarget.SyntaxShape switch
                {
                    ActionSyntaxShape.RemoveAtIndex => ParseRemoveAtIndexAction(
                        secondaryAfterTarget.Kind,
                        actionStartSpan,
                        target,
                        isAtActionBoundary),
                    _ => new MalformedAction(secondaryAfterTarget.Kind, actionStartSpan),
                };
            }

            var secondaryValueSeparators = GetSecondarySeparators(kind, 2);
            var value = ParseExpression(0, () => secondaryValueSeparators.Contains(Peek().Kind) || isAtActionBoundary());

            if (TryGetSecondaryAction(kind, 2, Peek().Kind, out var secondaryAfterValue))
            {
                return secondaryAfterValue.SyntaxShape switch
                {
                    ActionSyntaxShape.CollectionValueBy => ParseCollectionValueByAction(
                        secondaryAfterValue.Kind,
                        actionStartSpan,
                        target,
                        value,
                        isAtActionBoundary),
                    _ => new MalformedAction(secondaryAfterValue.Kind, actionStartSpan),
                };
            }

            var span = SourceSpan.Covering(actionStartSpan, value.Span);
            return new CollectionValueAction(kind, target, value, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionInto)]
        private ParsedAction ParseCollectionIntoAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field [into field]
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionInto).Slots;
            var intoSlot = slots[1]; // IntoTarget: optional, 'into'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var lastSpan = target.Span;
            ParsedExpression? intoTarget = null;
            if (Peek().Kind == intoSlot.PrecedingSeparator)
            {
                Advance(); // consume 'into'
                intoTarget = ParseActionTarget(separators, isAtActionBoundary);
                lastSpan = intoTarget.Span;
            }

            if (TryGetSecondaryAction(kind, 2, Peek().Kind, out var secondaryAfterInto))
            {
                return secondaryAfterInto.SyntaxShape switch
                {
                    ActionSyntaxShape.CollectionIntoBy => ParseCollectionIntoByAction(
                        secondaryAfterInto.Kind,
                        actionStartSpan,
                        target,
                        intoTarget,
                        isAtActionBoundary),
                    _ => new MalformedAction(secondaryAfterInto.Kind, actionStartSpan),
                };
            }

            var span = SourceSpan.Covering(actionStartSpan, lastSpan);
            return new CollectionIntoAction(kind, target, intoTarget, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.FieldOnly)]
        private ParsedAction ParseFieldOnlyAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, target.Span);
            return new FieldOnlyAction(kind, target, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionValueBy)]
        private ParsedAction ParseCollectionValueByAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field expr by expr
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy).Slots;
            var orderingKeySlot = slots[2]; // OrderingKey: required, 'by'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var value = ParseExpression(0, () => Peek().Kind == orderingKeySlot.PrecedingSeparator || isAtActionBoundary());
            return ParseCollectionValueByAction(kind, actionStartSpan, target, value, isAtActionBoundary);
        }

        private ParsedAction ParseCollectionValueByAction(ActionKind kind, SourceSpan actionStartSpan, ParsedExpression target, ParsedExpression value, Func<bool> isAtActionBoundary)
        {
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy).Slots;
            var orderingKeySlot = slots[2]; // OrderingKey: required, 'by'
            Expect(orderingKeySlot.PrecedingSeparator!.Value); // 'by'
            var orderingKey = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, orderingKey.Span);
            return new CollectionValueByAction(kind, target, value, orderingKey, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.InsertAt)]
        private ParsedAction ParseInsertAtAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field expr at expr
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.InsertAt).Slots;
            var indexSlot = slots[2]; // Index: required, 'at'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var value = ParseExpression(0, () => Peek().Kind == indexSlot.PrecedingSeparator || isAtActionBoundary());
            Expect(indexSlot.PrecedingSeparator!.Value); // 'at'
            var index = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, index.Span);
            return new InsertAtAction(kind, target, value, index, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.RemoveAtIndex)]
        private ParsedAction ParseRemoveAtIndexAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field at expr
            var target = ParseActionTarget(separators, isAtActionBoundary);
            return ParseRemoveAtIndexAction(kind, actionStartSpan, target, isAtActionBoundary);
        }

        private ParsedAction ParseRemoveAtIndexAction(ActionKind kind, SourceSpan actionStartSpan, ParsedExpression target, Func<bool> isAtActionBoundary)
        {
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.RemoveAtIndex).Slots;
            var indexSlot = slots[1]; // Index: required, 'at'
            Expect(indexSlot.PrecedingSeparator!.Value); // 'at'
            var index = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, index.Span);
            return new RemoveAtAction(kind, target, index, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.PutKeyValue)]
        private ParsedAction ParsePutKeyValueAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field key = value
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.PutKeyValue).Slots;
            var valueSlot = slots[2]; // Value: required, '='
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var key = ParseExpression(0, () => Peek().Kind == valueSlot.PrecedingSeparator || isAtActionBoundary());
            Expect(valueSlot.PrecedingSeparator!.Value); // '='
            var value = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, value.Span);
            return new PutKeyValueAction(kind, target, key, value, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionIntoBy)]
        private ParsedAction ParseCollectionIntoByAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field [into field] [by key]
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).Slots;
            var intoSlot = slots[1]; // IntoTarget: optional, 'into'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            ParsedExpression? intoTarget = null;

            if (Peek().Kind == intoSlot.PrecedingSeparator)
            {
                Advance(); // consume 'into'
                intoTarget = ParseActionTarget(separators, isAtActionBoundary);
            }

            return ParseCollectionIntoByAction(kind, actionStartSpan, target, intoTarget, isAtActionBoundary);
        }

        private ParsedAction ParseCollectionIntoByAction(ActionKind kind, SourceSpan actionStartSpan, ParsedExpression target, ParsedExpression? intoTarget, Func<bool> isAtActionBoundary)
        {
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).Slots;
            var orderingCaptureSlot = slots[2]; // OrderingCapture: optional, 'by'
            var lastSpan = intoTarget?.Span ?? target.Span;
            ParsedExpression? orderingCapture = null;

            if (Peek().Kind == orderingCaptureSlot.PrecedingSeparator)
            {
                Advance(); // consume 'by'
                orderingCapture = ParseActionTarget(Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).SeparatorTokens, isAtActionBoundary);
                lastSpan = orderingCapture.Span;
            }

            var span = SourceSpan.Covering(actionStartSpan, lastSpan);
            return new CollectionIntoByAction(kind, target, intoTarget, orderingCapture, span);
        }

        /// <summary>
        /// Parses the target (field reference) of an action.
        /// Terminates on the shape-specific <paramref name="separators"/> derived from
        /// <see cref="Actions.GetShapeMeta"/> — never on the union of all separator tokens.
        /// </summary>
        private ParsedExpression ParseActionTarget(FrozenSet<TokenKind> separators, Func<bool> terminates)
        {
            return ParseExpression(0, () => separators.Contains(Peek().Kind) || terminates());
        }
    }
}
