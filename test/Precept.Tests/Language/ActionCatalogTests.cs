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
}
