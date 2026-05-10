using System;
using System.Collections.Frozen;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

/// <summary>
/// Catalog-capability tests for <see cref="Actions.GetShapeMeta"/>.
/// These tests act as regression anchors: they verify structural invariants and
/// spot-check representative separator-token sets so that future edits to
/// GetShapeMeta catch regressions immediately.
/// </summary>
public class ActionCatalogTests
{
    // ── Exhaustiveness ───────────────────────────────────────────────────────────

    [Fact]
    public void ActionShapeMeta_ExistsForEveryShape()
    {
        foreach (var shape in Enum.GetValues<ActionSyntaxShape>())
        {
            ActionShapeMeta meta;
            var act = () => { meta = Actions.GetShapeMeta(shape); };
            act.Should().NotThrow(because: $"GetShapeMeta must handle {shape}");

            var m = Actions.GetShapeMeta(shape);
            m.Shape.Should().Be(shape);
            m.Slots.Should().NotBeEmpty(because: $"{shape} must have at least the target slot");
        }
    }

    // ── Structural invariants ────────────────────────────────────────────────────

    [Fact]
    public void ActionShapeMeta_SlotsAreWellFormed()
    {
        foreach (var shape in Enum.GetValues<ActionSyntaxShape>())
        {
            var meta = Actions.GetShapeMeta(shape);

            // First slot is always the positional target — no preceding keyword.
            meta.Slots[0].PrecedingSeparator.Should().BeNull(
                because: $"{shape}: first slot (target) must never have a preceding separator");

            // Any slot with a non-null PrecedingSeparator must not be the first slot.
            for (var i = 1; i < meta.Slots.Length; i++)
            {
                var slot = meta.Slots[i];
                if (slot.PrecedingSeparator is not null)
                {
                    i.Should().BeGreaterThan(0,
                        because: $"{shape}: slot '{slot.Role}' has a separator but is at index {i}");
                }
            }

            // SeparatorTokens must exactly equal the unique set of non-null PrecedingSeparator values.
            var expectedSeparators = meta.Slots
                .Where(s => s.PrecedingSeparator.HasValue)
                .Select(s => s.PrecedingSeparator!.Value)
                .Distinct()
                .ToHashSet();

            meta.SeparatorTokens.Should().BeEquivalentTo(expectedSeparators,
                because: $"{shape}: SeparatorTokens must match unique non-null PrecedingSeparator values in Slots");
        }
    }

    // ── Spot-check separator-token sets ─────────────────────────────────────────

    [Fact]
    public void ActionShapeMeta_SeparatorsMatchExpectedTokens()
    {
        // AssignValue (set): separators = { Assign }
        Actions.GetShapeMeta(ActionSyntaxShape.AssignValue).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.Assign },
                because: "AssignValue uses '=' to introduce the value slot");

        // CollectionValue (add/remove/push/enqueue): no separator tokens
        Actions.GetShapeMeta(ActionSyntaxShape.CollectionValue).SeparatorTokens
            .Should().BeEmpty(
                because: "CollectionValue is positional — no keyword separates target from value");

        // CollectionValueBy (append-by/enqueue-by): separators = { By }
        Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.By },
                because: "CollectionValueBy uses 'by' to introduce the ordering key slot");

        // InsertAt: separators = { At }
        Actions.GetShapeMeta(ActionSyntaxShape.InsertAt).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.At },
                because: "InsertAt uses 'at' to introduce the index slot");

        // CollectionInto (dequeue/pop): separators = { Into }
        Actions.GetShapeMeta(ActionSyntaxShape.CollectionInto).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.Into },
                because: "CollectionInto uses 'into' to introduce the optional capture slot");

        // CollectionIntoBy (dequeue-by): separators = { Into, By }
        Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.Into, TokenKind.By },
                because: "CollectionIntoBy uses both 'into' and 'by' as keyword separators");

        // FieldOnly (clear): no separator tokens
        Actions.GetShapeMeta(ActionSyntaxShape.FieldOnly).SeparatorTokens
            .Should().BeEmpty(
                because: "FieldOnly has only a target slot — no separators");

        // RemoveAtIndex: separators = { At }
        Actions.GetShapeMeta(ActionSyntaxShape.RemoveAtIndex).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.At },
                because: "RemoveAtIndex uses 'at' to introduce the index slot");

        // PutKeyValue: separators = { Assign }
        Actions.GetShapeMeta(ActionSyntaxShape.PutKeyValue).SeparatorTokens
            .Should().BeEquivalentTo(new[] { TokenKind.Assign },
                because: "PutKeyValue uses '=' to separate the key from the value slot");
    }
}
