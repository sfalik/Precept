using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class HoverHandlerTests
{
    private const string Source = """
precept LoanApplication
field Amount as number
state Draft initial
""";

    [Fact]
    public void Hover_OnKeyword_ReturnsMarkdownContent()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 1));

        hover.Should().NotBeNull();
        hover!.Contents.HasMarkupContent.Should().BeTrue();

        var markup = hover.Contents.MarkupContent;
        markup.Should().NotBeNull();
        markup!.Kind.Should().Be(MarkupKind.Markdown);
        markup.Value.Should().Contain("**field**");
        markup.Value.Should().Contain("Field declaration");
    }

    [Fact]
    public void Hover_OnDeclaredField_ReturnsIdentifierDoc()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 7));

        hover.Should().NotBeNull();
        hover!.Contents.HasMarkupContent.Should().BeTrue();

        var markup = hover.Contents.MarkupContent;
        markup.Should().NotBeNull();
        markup!.Value.Should().Contain("field `Amount`");
        markup.Value.Should().Contain("number");
    }

    [Fact]
    public void Hover_OnUnknownPosition_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 5));

        hover.Should().BeNull();
    }

    [Fact]
    public async Task Hover_NoDocument_ReturnsNull()
    {
        var store = new DocumentStore();
        var handler = new HoverHandler(store);
        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(@"C:\hover-test.precept")),
            Position = new Position(0, 0),
        };

        var hover = await handler.Handle(request, CancellationToken.None);

        hover.Should().BeNull();
    }
}
