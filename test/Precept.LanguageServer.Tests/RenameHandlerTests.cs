using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Precept.LanguageServer.Tests;

public class RenameHandlerTests
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
    public async Task PrepareRename_FieldReference_ReturnsIdentifierRange()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(FieldSource);
        var request = new PrepareRenameParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.FieldReferences.First(reference => reference.Field.Name == "CurrentParty").Site),
        };

        var rename = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<RangeOrPlaceholderRange?>(
            "Precept.LanguageServer.Handlers.RenameHandler",
            request,
            compilation);

        rename.Should().NotBeNull();
        rename!.IsRange.Should().BeTrue();
        rename.Range.Should().BeEquivalentTo(SymbolNavigationHandlerTestHelpers.FieldRanges(compilation, "CurrentParty")[1]);
    }

    [Fact]
    public async Task Rename_Field_UpdatesDeclarationAndAllReferences()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(FieldSource);
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.FieldReferences.First(reference => reference.Field.Name == "CurrentParty").Site),
            NewName = "ActiveParty",
        };

        var edit = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<WorkspaceEdit>(
            "Precept.LanguageServer.Handlers.RenameHandler",
            request,
            compilation);

        AssertSingleDocumentRename(edit, "ActiveParty", SymbolNavigationHandlerTestHelpers.FieldRanges(compilation, "CurrentParty"));
    }

    [Fact]
    public async Task Rename_State_UpdatesDeclarationAndTransitionSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(StateSource);
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.StateReferences.First(reference => reference.State.Name == "Seating").Site),
            NewName = "Serving",
        };

        var edit = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<WorkspaceEdit>(
            "Precept.LanguageServer.Handlers.RenameHandler",
            request,
            compilation);

        AssertSingleDocumentRename(edit, "Serving", SymbolNavigationHandlerTestHelpers.StateRanges(compilation, "Seating"));
    }

    [Fact]
    public async Task Rename_Event_UpdatesDeclarationAndEventSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(EventSource);
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = SymbolNavigationHandlerTestHelpers.PositionAt(
                compilation.Semantics.EventReferences.First(reference => reference.Event.Name == "SeatNextParty").Site),
            NewName = "SeatNextWaitingParty",
        };

        var edit = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<WorkspaceEdit>(
            "Precept.LanguageServer.Handlers.RenameHandler",
            request,
            compilation);

        AssertSingleDocumentRename(edit, "SeatNextWaitingParty", SymbolNavigationHandlerTestHelpers.EventRanges(compilation, "SeatNextParty"));
    }

    [Fact]
    public async Task Rename_Argument_UpdatesDeclarationAndQualifiedArgumentSites()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(ArgumentSource);
        var qualifiedReference = compilation.Semantics.ArgReferences.First(reference =>
            reference.Arg.EventName == "JoinWaitlist"
            && reference.Arg.Name == "PartyName"
            && reference.Site.StartColumn > reference.Arg.Span.StartColumn);

        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = IdentifierPositionInQualifiedArgReference(qualifiedReference),
            NewName = "GuestName",
        };

        var edit = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<WorkspaceEdit>(
            "Precept.LanguageServer.Handlers.RenameHandler",
            request,
            compilation);

        AssertSingleDocumentRename(edit, "GuestName", SymbolNavigationHandlerTestHelpers.ArgIdentifierRanges(compilation, "JoinWaitlist", "PartyName"));
    }

    [Fact]
    public async Task PrepareRename_OnKeyword_ReturnsNull()
    {
        var compilation = SymbolNavigationHandlerTestHelpers.Compile(FieldSource);
        var request = new PrepareRenameParams
        {
            TextDocument = new TextDocumentIdentifier(SymbolNavigationHandlerTestHelpers.Uri),
            Position = new Position(0, 0),
        };

        var rename = await SymbolNavigationHandlerTestHelpers.InvokeHandlerAsync<RangeOrPlaceholderRange?>(
            "Precept.LanguageServer.Handlers.RenameHandler",
            request,
            compilation);

        rename.Should().BeNull();
    }

    private static void AssertSingleDocumentRename(WorkspaceEdit edit, string newName, LspRange[] expectedRanges)
    {
        edit.Changes.Should().NotBeNull();
        edit.DocumentChanges.Should().BeNull();

        var changes = edit.Changes!;
        changes.Keys.Should().ContainSingle().Which.Should().Be(SymbolNavigationHandlerTestHelpers.Uri);
        var textEdits = changes[SymbolNavigationHandlerTestHelpers.Uri].ToArray();

        textEdits.Select(textEdit => textEdit.NewText)
            .Should()
            .OnlyContain(text => text == newName);
        textEdits.Select(textEdit => textEdit.Range)
            .Should()
            .BeEquivalentTo(expectedRanges);
    }

    private static Position IdentifierPositionInQualifiedArgReference(Precept.Pipeline.ArgReference reference) =>
        new(reference.Site.StartLine - 1, reference.Site.StartColumn - 1);
}
