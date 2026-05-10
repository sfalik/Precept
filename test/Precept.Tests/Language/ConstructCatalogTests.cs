using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class ConstructCatalogTests
{
    private static readonly ConstructKind[] TopLevelCompletionConstructs =
    [
        ConstructKind.PreceptHeader,
        ConstructKind.FieldDeclaration,
        ConstructKind.StateDeclaration,
        ConstructKind.EventDeclaration,
        ConstructKind.RuleDeclaration,
    ];

    [Fact]
    public void SnippetTemplate_PresentForTopLevelCompletionConstructs()
    {
        foreach (var kind in TopLevelCompletionConstructs)
        {
            var meta = Constructs.GetMeta(kind);
            meta.SnippetTemplate.Should().NotBeNullOrWhiteSpace(
                because: $"{kind} is a top-level completion construct and must carry a snippet template");
        }
    }

    [Fact]
    public void SnippetTemplate_ContainsLeadingKeyword()
    {
        Constructs.GetMeta(ConstructKind.PreceptHeader).SnippetTemplate.Should().StartWith("precept ");
        Constructs.GetMeta(ConstructKind.FieldDeclaration).SnippetTemplate.Should().StartWith("field ");
        Constructs.GetMeta(ConstructKind.StateDeclaration).SnippetTemplate.Should().StartWith("state ");
        Constructs.GetMeta(ConstructKind.EventDeclaration).SnippetTemplate.Should().StartWith("event ");
        Constructs.GetMeta(ConstructKind.RuleDeclaration).SnippetTemplate.Should().StartWith("rule ");
    }

    [Fact]
    public void SnippetTemplate_ContainsAtLeastOneTabStop()
    {
        foreach (var kind in TopLevelCompletionConstructs)
        {
            var meta = Constructs.GetMeta(kind);
            meta.SnippetTemplate.Should().Contain("${",
                because: $"{kind} snippet template must contain at least one VS Code tab stop");
        }
    }
}
