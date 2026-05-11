using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class DocumentHighlightHandlerTests
{
    private const string FieldSource = """
        precept RestaurantWaitlist
        field CurrentParty as string optional
        field LastCalledParty as string optional
        state Accepting initial
        event SeatNextParty
        from Accepting on SeatNextParty when CurrentParty is set
            -> set LastCalledParty = CurrentParty
            -> clear CurrentParty
            -> no transition
        """;

    private const string ArgumentSource = """
        precept RestaurantWaitlist
        field CurrentParty as string optional
        state Accepting initial
        state Joined terminal
        event JoinWaitlist(PartyName as string notempty, PartySize as number)
        on JoinWaitlist ensure JoinWaitlist.PartyName != "" because "Party name is required"
        from Accepting on JoinWaitlist when JoinWaitlist.PartyName != ""
            -> set CurrentParty = PartyName
            -> transition Joined
        """;

    [Fact]
    public async Task DocumentHighlight_Field_ReturnsDeclarationAndReferences()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(FieldSource);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.FieldReferences.First(reference => reference.Field.Name == "CurrentParty").Site),
        };

        var highlights = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<DocumentHighlightContainer>(
            "Precept.LanguageServer.Handlers.DocumentHighlightHandler",
            request,
            compilation);

        highlights.Select(highlight => highlight.Range)
            .Should()
            .BeEquivalentTo(SymbolNavigationHandlerTestHelpers.FieldRanges(compilation, "CurrentParty"));
    }

    [Fact]
    public async Task DocumentHighlight_EventArgument_ReturnsDeclarationAndReferences()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(ArgumentSource);
        var qualifiedReference = compilation.Semantics.ArgReferences.First(reference =>
            reference.Arg.EventName == "JoinWaitlist"
            && reference.Arg.Name == "PartyName"
            && reference.Site.StartColumn > reference.Arg.Span.StartColumn);

        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(qualifiedReference.Site),
        };

        var highlights = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<DocumentHighlightContainer>(
            "Precept.LanguageServer.Handlers.DocumentHighlightHandler",
            request,
            compilation);

        highlights.Select(highlight => highlight.Range)
            .Should()
            .BeEquivalentTo(SymbolNavigationHandlerTestHelpers.ArgRanges(compilation, "JoinWaitlist", "PartyName"));
    }
}
