using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class ActionCatalogTests
{
    private static readonly ActionKind[] PrimaryAuthorFacingActions =
    [
        ActionKind.Set,
        ActionKind.Add,
        ActionKind.Remove,
        ActionKind.Enqueue,
        ActionKind.Dequeue,
        ActionKind.Push,
        ActionKind.Pop,
        ActionKind.Clear,
        ActionKind.Append,
        ActionKind.Insert,
        ActionKind.Put,
    ];

    [Fact]
    public void SnippetTemplate_PresentForPrimaryActionVerbs()
    {
        foreach (var kind in PrimaryAuthorFacingActions)
        {
            var meta = Actions.GetMeta(kind);
            meta.SnippetTemplate.Should().NotBeNullOrWhiteSpace(
                because: $"{kind} is a primary author-facing action verb and must carry a snippet template");
        }
    }

    [Fact]
    public void SnippetTemplate_ContainsLeadingVerb()
    {
        Actions.GetMeta(ActionKind.Set).SnippetTemplate.Should().StartWith("set ");
        Actions.GetMeta(ActionKind.Add).SnippetTemplate.Should().StartWith("add ");
        Actions.GetMeta(ActionKind.Remove).SnippetTemplate.Should().StartWith("remove ");
        Actions.GetMeta(ActionKind.Enqueue).SnippetTemplate.Should().StartWith("enqueue ");
        Actions.GetMeta(ActionKind.Dequeue).SnippetTemplate.Should().StartWith("dequeue ");
        Actions.GetMeta(ActionKind.Push).SnippetTemplate.Should().StartWith("push ");
        Actions.GetMeta(ActionKind.Pop).SnippetTemplate.Should().StartWith("pop ");
        Actions.GetMeta(ActionKind.Clear).SnippetTemplate.Should().StartWith("clear ");
        Actions.GetMeta(ActionKind.Append).SnippetTemplate.Should().StartWith("append ");
        Actions.GetMeta(ActionKind.Insert).SnippetTemplate.Should().StartWith("insert ");
        Actions.GetMeta(ActionKind.Put).SnippetTemplate.Should().StartWith("put ");
    }

    [Fact]
    public void SnippetTemplate_ContainsAtLeastOneTabStop()
    {
        foreach (var kind in PrimaryAuthorFacingActions)
        {
            var meta = Actions.GetMeta(kind);
            meta.SnippetTemplate.Should().Contain("${",
                because: $"{kind} snippet template must contain at least one VS Code tab stop");
        }
    }

    [Fact]
    public void WriteSemantics_PopulatedForEveryActionKind()
    {
        // F-LANG-GRAPH-04 Decision 6: WriteSemantics classification is catalog-driven.
        // Every action in today's catalog is either EstablishesValue or ClearsContents —
        // the `None` and `MutatesContents` variants exist for future actions
        // (e.g. reject/transition outcomes; structural mutations).
        foreach (var kind in Enum.GetValues<ActionKind>())
        {
            var meta = Actions.GetMeta(kind);
            meta.WriteSemantics.Should().Match(
                ws => ws == ActionWriteSemantics.EstablishesValue
                   || ws == ActionWriteSemantics.ClearsContents,
                because: $"{kind} must classify its write semantics; current catalog has no None/MutatesContents members");
        }
    }

    [Fact]
    public void WriteSemantics_ValueEstablishingActionsClassifiedCorrectly()
    {
        // Per F-LANG-GRAPH-04 design § Inventory — the EstablishesValue classification.
        ActionWriteSemantics[] establishing =
        [
            Actions.GetMeta(ActionKind.Set).WriteSemantics,
            Actions.GetMeta(ActionKind.Put).WriteSemantics,
            Actions.GetMeta(ActionKind.Add).WriteSemantics,
            Actions.GetMeta(ActionKind.Enqueue).WriteSemantics,
            Actions.GetMeta(ActionKind.Push).WriteSemantics,
            Actions.GetMeta(ActionKind.Append).WriteSemantics,
            Actions.GetMeta(ActionKind.AppendBy).WriteSemantics,
            Actions.GetMeta(ActionKind.Insert).WriteSemantics,
            Actions.GetMeta(ActionKind.EnqueueBy).WriteSemantics,
        ];
        establishing.Should().AllSatisfy(ws => ws.Should().Be(ActionWriteSemantics.EstablishesValue));
    }

    [Fact]
    public void WriteSemantics_ClearingActionsClassifiedCorrectly()
    {
        // Per F-LANG-GRAPH-04 design § Inventory — the ClearsContents classification.
        // These actions remove or clear; they do NOT establish a value at the target
        // and therefore must NOT suppress FieldNeverSet.
        ActionWriteSemantics[] clearing =
        [
            Actions.GetMeta(ActionKind.Clear).WriteSemantics,
            Actions.GetMeta(ActionKind.Remove).WriteSemantics,
            Actions.GetMeta(ActionKind.RemoveAt).WriteSemantics,
            Actions.GetMeta(ActionKind.Dequeue).WriteSemantics,
            Actions.GetMeta(ActionKind.DequeueBy).WriteSemantics,
            Actions.GetMeta(ActionKind.Pop).WriteSemantics,
        ];
        clearing.Should().AllSatisfy(ws => ws.Should().Be(ActionWriteSemantics.ClearsContents));
    }
}
