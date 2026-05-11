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

    private const string RichHoverSource = """
    precept HoverSurface
    field Tags as set of string
    field StartDate as date <- '2026-01-15'
    field Notes as string <- "vip"
    state Draft initial
    state Done terminal
    rule Notes.length >= max(0, 0) because "valid"
    event AddTag
    from Draft on AddTag
        -> add Tags "vip"
        -> transition Done
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

    [Fact]
    public void Hover_OnSetInTypePosition_UsesTypeHover()
    {
        var compilation = Precept.Compiler.Compile("""
            precept LoanApplication
            field Tags as set of string
            state Draft initial
            """);
        var setToken = compilation.Tokens.Tokens.Single(t => t.Kind == Precept.Language.TokenKind.Set);

        var hover = HoverHandler.CreateHover(compilation, new Position(setToken.Span.StartLine - 1, setToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.HasMarkupContent.Should().BeTrue();
        var markup = hover.Contents.MarkupContent!;
        markup.Value.Should().Contain("set");
        markup.Value.Should().Contain("unordered collection", because: "set in type position should show type description");
        markup.Value.Should().NotContain("Field assignment", because: "action description must not appear in type hover");
    }

    [Fact]
    public void Hover_OnTypedConstant_ShowsDeclaredTypeAndFormat()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var typedConstant = compilation.Tokens.Tokens.Single(token =>
            token.Kind == Precept.Language.TokenKind.TypedConstant
            && token.Span.StartLine == 3);

        var hover = HoverHandler.CreateHover(compilation, new Position(typedConstant.Span.StartLine - 1, typedConstant.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("date typed constant");
        hover.Contents.MarkupContent.Value.Should().Contain("ISO 8601 date (YYYY-MM-DD)");
    }

    [Fact]
    public void Hover_OnQuantityTypedConstant_ShowsResolvedUnitMetadata()
    {
        var compilation = Precept.Compiler.Compile("""
            precept HoverUnits
            field Weight as quantity <- '5 [lb_av]'
            """);
        var typedConstant = compilation.Tokens.Tokens.Single(token => token.Kind == Precept.Language.TokenKind.TypedConstant);

        var hover = HoverHandler.CreateHover(compilation, new Position(typedConstant.Span.StartLine - 1, typedConstant.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("quantity typed constant");
        hover.Contents.MarkupContent.Value.Should().Contain("Unit: `[lb_av]` (lb) — pound");
    }

    [Fact]
    public void Hover_OnFunctionCall_ShowsSignatureAndDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var maxToken = compilation.Tokens.Tokens.Single(token => token.Text == "max");

        var hover = HoverHandler.CreateHover(compilation, new Position(maxToken.Span.StartLine - 1, maxToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("function `max`");
        hover.Contents.MarkupContent.Value.Should().Contain("max(value as integer, value as integer) -> integer");
        hover.Contents.MarkupContent.Value.Should().Contain("Returns the larger of two values");
    }

    [Fact]
    public void Hover_OnOperator_UsesOperatorHoverDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var operatorToken = compilation.Tokens.Tokens.Single(token => token.Kind == Precept.Language.TokenKind.GreaterThanOrEqual);

        var hover = HoverHandler.CreateHover(compilation, new Position(operatorToken.Span.StartLine - 1, operatorToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain(">=");
        hover.Contents.MarkupContent.Value.Should().Contain("Greater-than-or-equal comparison. Requires orderable types. Cannot be chained.");
    }

    [Fact]
    public void Hover_OnCollectionType_UsesTypeHoverDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var setToken = compilation.Tokens.Tokens.Single(token =>
            token.Kind == Precept.Language.TokenKind.Set
            && token.Span.StartLine == 2);

        var hover = HoverHandler.CreateHover(compilation, new Position(setToken.Span.StartLine - 1, setToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("set");
        hover.Contents.MarkupContent.Value.Should().Contain("unordered collection of unique elements");
    }

    [Fact]
    public void Hover_OnActionVerb_UsesActionHoverDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var addToken = compilation.Tokens.Tokens.Single(token => token.Kind == Precept.Language.TokenKind.Add);

        var hover = HoverHandler.CreateHover(compilation, new Position(addToken.Span.StartLine - 1, addToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("add");
        hover.Contents.MarkupContent.Value.Should().Contain("Adds an element to a set or bag field.");
    }

    [Fact]
    public void Hover_OnAccessor_UsesAccessorDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var accessorToken = compilation.Tokens.Tokens.Single(token => token.Text == "length");

        var hover = HoverHandler.CreateHover(compilation, new Position(accessorToken.Span.StartLine - 1, accessorToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("string.length");
        hover.Contents.MarkupContent.Value.Should().Contain("Character count");
        hover.Contents.MarkupContent.Value.Should().Contain("Returns: `integer`");
    }
}
