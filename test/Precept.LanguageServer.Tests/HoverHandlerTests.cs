using System.Linq;
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

    private const string SourceWithEventArgs = """
precept LoanApplication
field Amount as number
state Draft initial
state Approved terminal
event Approve(Note as string optional notempty)
on Approve ensure Approve.Note is set because "note required"
from Draft on Approve -> transition Approved
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
    public void Hover_OnWhitespace_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 5));

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnNewLineToken_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var newLineSpan = compilation.Tokens.Tokens.First(token => token.Kind == Precept.Language.TokenKind.NewLine).Span;

        var hover = HoverHandler.CreateHover(compilation, new Position(newLineSpan.StartLine - 1, newLineSpan.StartColumn - 1));

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnEndOfSourceToken_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var endOfSourceSpan = compilation.Tokens.Tokens.First(token => token.Kind == Precept.Language.TokenKind.EndOfSource).Span;

        var hover = HoverHandler.CreateHover(compilation, new Position(endOfSourceSpan.StartLine - 1, endOfSourceSpan.StartColumn - 1));

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnEventArgumentDeclaration_ReturnsIdentifierDoc()
    {
        var compilation = Precept.Compiler.Compile(SourceWithEventArgs);
        var noteToken = compilation.Tokens.Tokens.Single(token =>
            token.Kind == Precept.Language.TokenKind.Identifier
            && token.Text == "Note"
            && token.Span.StartLine == 5);

        var hover = HoverHandler.CreateHover(
            compilation,
            new Position(noteToken.Span.StartLine - 1, noteToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("argument `Note`");
        hover.Contents.MarkupContent.Value.Should().Contain("Event: `Approve`");
    }

    [Fact]
    public void Hover_OnDeclaredState_ReturnsIdentifierDoc()
    {
        var compilation = Precept.Compiler.Compile(Source);
        // "state Draft initial" is line 3 (1-based) → Position(2, 6) = "Draft" (0-based col 6 = 1-based col 7)
        var hover = HoverHandler.CreateHover(compilation, new Position(2, 6));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("state `Draft`");
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
