using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using PreceptDiagnosticCode = Precept.Language.DiagnosticCode;

namespace Precept.LanguageServer.Tests;

public class CodeActionHandlerTests
{
    private static readonly DocumentUri Uri = DocumentUri.FromFileSystemPath(@"C:\code-action-handler-test.precept");

    [Fact]
    public async Task Handle_DiagnosticWithSuggestion_ReturnsDidYouMeanAction()
    {
        const string source = """
            precept OrderItem
            field Quantity as number default 0
            state Pending initial terminal
            rule Quantty >= 0 because "must be nonnegative"
            """;

        var actions = await HandleAsync(source, nameof(PreceptDiagnosticCode.UndeclaredField));

        actions.Should().NotBeNull();
        var rename = GetCodeActions(actions!).Single(action => action.Title == "Rename 'Quantty' to 'Quantity'");
        var edit = rename.Edit!.Changes![Uri].Single();

        edit.NewText.Should().Be("Quantity");
        rename.IsPreferred.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoSuggestionsForDiagnostic_ReturnsNoAction()
    {
        const string source = """
            precept Example
            field X as number default 0
            state Draft initial terminal
            rule X == "zero" because "check"
            """;

        var actions = await HandleAsync(source, nameof(PreceptDiagnosticCode.TypeMismatch));

        actions.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoDiagnosticsInContext_ReturnsNull()
    {
        var store = new DocumentStore();
        store.GetOrAdd(Uri).Update(Precept.Compiler.Compile("precept Example"));
        var handler = new CodeActionHandler(store);

        var actions = await handler.Handle(new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier(Uri),
            Range = new LspRange(new Position(0, 0), new Position(0, 0)),
            Context = new CodeActionContext
            {
                Diagnostics = new Container<LspDiagnostic>(),
            },
        }, CancellationToken.None);

        actions.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnknownDocument_ReturnsNull()
    {
        var handler = new CodeActionHandler(new DocumentStore());
        var diagnostic = new LspDiagnostic
        {
            Range = new LspRange(new Position(0, 0), new Position(0, 1)),
            Code = nameof(PreceptDiagnosticCode.TypeMismatch),
            Source = "precept",
            Message = "Expected a number value here, but got 'zero'",
        };

        var actions = await handler.Handle(new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier(Uri),
            Range = diagnostic.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new Container<LspDiagnostic>(diagnostic),
            },
        }, CancellationToken.None);

        actions.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FixHintDiagnostic_ReturnsHintAction()
    {
        const string source = """
            precept Example
            field Name as string default "hello
            state Draft initial terminal
            """;

        var actions = await HandleAsync(source, nameof(PreceptDiagnosticCode.UnterminatedStringLiteral));

        actions.Should().NotBeNull();
        var hint = GetCodeActions(actions!).Single(action => action.Command is not null && action.Command.Name == "precept.showFixHint");

        hint.Title.Should().StartWith("ℹ ");
        hint.Command!.Title.Should().Be("Add a closing \" at the end of the text value");
        hint.Command.Arguments!.Count.Should().Be(3);
        hint.Command.Arguments[0]!.ToString().Should().Be("Add a closing \" at the end of the text value");
        hint.Command.Arguments[1]!.ToString().Should().NotBeNullOrEmpty();
        hint.Command.Arguments[2]!.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TryGetUnterminatedFix_StringLiteral_InsertsQuote()
    {
        const string source = """
            precept Example
            field Name as string default "hello
            state Draft initial terminal
            """;

        var actions = await HandleAsync(source, nameof(PreceptDiagnosticCode.UnterminatedStringLiteral));

        actions.Should().NotBeNull();
        var fix = GetCodeActions(actions!).Single(action => action.Title == "Insert closing \"");
        var edit = fix.Edit!.Changes![Uri].Single();

        edit.NewText.Should().Be("\"");
        edit.Range.Start.Should().BeEquivalentTo(edit.Range.End);
    }

    private static async Task<CommandOrCodeActionContainer?> HandleAsync(string source, string code)
    {
        var store = new DocumentStore();
        var compilation = Precept.Compiler.Compile(source);
        var (diagnostics, suggestions) = DiagnosticEnricher.Enrich(compilation);
        store.GetOrAdd(Uri).Update(compilation, suggestions);

        var handler = new CodeActionHandler(store);
        var diagnostic = diagnostics.First(d => d.Code is { } value && value.IsString && value.String == code);

        return await handler.Handle(new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier(Uri),
            Range = diagnostic.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new Container<LspDiagnostic>(diagnostic),
            },
        }, CancellationToken.None);
    }

    private static IReadOnlyList<CodeAction> GetCodeActions(CommandOrCodeActionContainer actions) =>
        actions
            .Where(action => action.IsCodeAction)
            .Select(action => action.CodeAction!)
            .ToArray();
}
