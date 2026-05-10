using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class ReferencesHandlerTests
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

    private const string StateSource = """
        precept RestaurantWaitlist
        field CurrentParty as string optional
        state Accepting initial
        state Seating
        state Closed terminal
        event SeatNextParty
        event CloseService
        in Seating ensure CurrentParty is set because "Seating requires an active party"
        from Accepting on SeatNextParty -> transition Seating
        from Seating on CloseService -> transition Closed
        """;

    private const string EventSource = """
        precept RestaurantWaitlist
        field CurrentParty as string optional
        state Accepting initial
        state Seating
        event SeatNextParty(PartyName as string notempty)
        on SeatNextParty ensure PartyName != "" because "Party name is required"
        from Accepting on SeatNextParty when CurrentParty is not set
            -> set CurrentParty = PartyName
            -> transition Seating
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
    public async Task References_Field_ReturnDeclarationAndAllSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(FieldSource);
        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.FieldReferences.First(reference => reference.Field.Name == "CurrentParty").Site),
            Context = new ReferenceContext
            {
                IncludeDeclaration = true,
            },
        };

        var locations = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<LocationContainer>(
            "Precept.LanguageServer.Handlers.ReferencesHandler",
            request,
            compilation);

        locations.Select(location => location.Uri).Should().OnlyContain(uri => uri == SymbolNavigationHandlerTestHelpers.Uri);
        locations.Select(location => location.Range)
            .Should()
            .BeEquivalentTo(SymbolNavigationHandlerTestHelpers.FieldRanges(compilation, "CurrentParty"));
    }

    [Fact]
    public async Task References_State_ReturnDeclarationAndAllSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(StateSource);
        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.StateReferences.First(reference => reference.State.Name == "Seating").Site),
            Context = new ReferenceContext
            {
                IncludeDeclaration = true,
            },
        };

        var locations = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<LocationContainer>(
            "Precept.LanguageServer.Handlers.ReferencesHandler",
            request,
            compilation);

        locations.Select(location => location.Uri).Should().OnlyContain(uri => uri == SymbolNavigationHandlerTestHelpers.Uri);
        locations.Select(location => location.Range)
            .Should()
            .BeEquivalentTo(SymbolNavigationHandlerTestHelpers.StateRanges(compilation, "Seating"));
    }

    [Fact]
    public async Task References_Event_ReturnDeclarationAndAllSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(EventSource);
        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.EventReferences.First(reference => reference.Event.Name == "SeatNextParty").Site),
            Context = new ReferenceContext
            {
                IncludeDeclaration = true,
            },
        };

        var locations = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<LocationContainer>(
            "Precept.LanguageServer.Handlers.ReferencesHandler",
            request,
            compilation);

        locations.Select(location => location.Uri).Should().OnlyContain(uri => uri == SymbolNavigationHandlerTestHelpers.Uri);
        locations.Select(location => location.Range)
            .Should()
            .BeEquivalentTo(SymbolNavigationHandlerTestHelpers.EventRanges(compilation, "SeatNextParty"));
    }

    [Fact]
    public async Task References_Argument_ReturnDeclarationAndAllSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(ArgumentSource);
        var qualifiedReference = compilation.Semantics.ArgReferences.First(reference =>
            reference.Arg.EventName == "JoinWaitlist"
            && reference.Arg.Name == "PartyName"
            && reference.Site.Length > reference.Arg.Name.Length);

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(qualifiedReference.Site),
            Context = new ReferenceContext
            {
                IncludeDeclaration = true,
            },
        };

        var locations = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<LocationContainer>(
            "Precept.LanguageServer.Handlers.ReferencesHandler",
            request,
            compilation);

        locations.Select(location => location.Uri).Should().OnlyContain(uri => uri == SymbolNavigationHandlerTestHelpers.Uri);
        locations.Select(location => location.Range)
            .Should()
            .BeEquivalentTo(SymbolNavigationHandlerTestHelpers.ArgRanges(compilation, "JoinWaitlist", "PartyName"));
    }
}
