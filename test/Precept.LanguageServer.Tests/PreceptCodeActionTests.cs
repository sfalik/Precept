using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptCodeActionTests
{
    [Fact]
    public async Task CodeActions_RejectOnlyPair_OffersRemoveRowsQuickFix()
    {
        const string text = """
            precept M
            state Pending initial
            state Active
            event Open
            event Deposit
            from Pending on Open -> transition Active

            from Pending on Deposit -> reject "not open"

            from Active on Deposit -> no transition
            """;

        var analyzer = PreceptTextDocumentSyncHandler.SharedAnalyzer;
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        var diagnostics = analyzer.GetDiagnostics(uri).ToArray();
        var targetDiagnostic = diagnostics.First(d => d.Message.Contains("ends in reject", StringComparison.Ordinal));

        var handler = new PreceptCodeActionHandler();
        var actions = await handler.Handle(
            new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Range = targetDiagnostic.Range,
                Context = new CodeActionContext
                {
                    Diagnostics = new Container<Diagnostic>(targetDiagnostic)
                }
            },
            CancellationToken.None);

        actions.Should().NotBeNull();
        actions!.Any(action => action.IsCodeAction).Should().BeTrue();
        var codeAction = actions!
            .Select(action => action.CodeAction)
            .FirstOrDefault(candidate =>
                candidate is not null &&
                candidate.Title.Contains("Remove reject-only rows", StringComparison.Ordinal) &&
                candidate.Edit is not null);

        codeAction.Should().NotBeNull();
        var updatedText = ApplyWorkspaceEdit(text, uri, codeAction!.Edit!);
        updatedText.Should().Be(
            """
            precept M
            state Pending initial
            state Active
            event Open
            event Deposit
            from Pending on Open -> transition Active

            from Active on Deposit -> no transition
            """);
    }

    [Fact]
    public async Task CodeActions_OrphanedEvent_OffersRemoveEventQuickFix()
    {
        const string text = """
            precept M
            state A initial
            event Go

            event Unused with Amount as number
            on Unused ensure Amount > 0 because "Amount must be positive"

            from A on Go -> no transition
            """;

        var analyzer = PreceptTextDocumentSyncHandler.SharedAnalyzer;
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        var diagnostics = analyzer.GetDiagnostics(uri).ToArray();
        var targetDiagnostic = diagnostics.First(d => d.Message.Contains("never referenced", StringComparison.OrdinalIgnoreCase));

        var handler = new PreceptCodeActionHandler();
        var actions = await handler.Handle(
            new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Range = targetDiagnostic.Range,
                Context = new CodeActionContext
                {
                    Diagnostics = new Container<Diagnostic>(targetDiagnostic)
                }
            },
            CancellationToken.None);

        actions.Should().NotBeNull();
        actions!.Any(action => action.IsCodeAction).Should().BeTrue();
        var codeAction = actions!
            .Select(action => action.CodeAction)
            .FirstOrDefault(candidate =>
                candidate is not null &&
                candidate.Title.Contains("Remove orphaned event", StringComparison.Ordinal) &&
                candidate.Edit is not null);

        codeAction.Should().NotBeNull();
        var updatedText = ApplyWorkspaceEdit(text, uri, codeAction!.Edit!);
        updatedText.Should().Be(
            """
            precept M
            state A initial
            event Go

            from A on Go -> no transition
            """);
    }

    private static string ApplyWorkspaceEdit(string text, DocumentUri uri, WorkspaceEdit edit)
    {
        edit.Changes.Should().NotBeNull();
        edit.Changes!.TryGetValue(uri, out var textEdits).Should().BeTrue();
        textEdits.Should().NotBeNull();

        var buffer = new StringBuilder(text);
        foreach (var textEdit in textEdits!
                     .OrderByDescending(e => GetOffset(text, e.Range.Start))
                     .ThenByDescending(e => GetOffset(text, e.Range.End)))
        {
            var start = GetOffset(buffer.ToString(), textEdit.Range.Start);
            var end = GetOffset(buffer.ToString(), textEdit.Range.End);
            buffer.Remove(start, end - start);
            buffer.Insert(start, textEdit.NewText);
        }

        return buffer.ToString();
    }

    private static int GetOffset(string text, Position position)
    {
        var lineStart = 0;
        for (var line = 0; line < position.Line; line++)
        {
            var nextNewLine = text.IndexOf('\n', lineStart);
            if (nextNewLine < 0)
                return text.Length;

            lineStart = nextNewLine + 1;
        }

        return Math.Min(lineStart + (int)position.Character, text.Length);
    }

    // ═══════════════════════════════════════════════════════════════
    // C93 — Unproven divisor safety code actions
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodeActions_C93_FieldDivisor_OffersAddPositiveConstraint()
    {
        const string text = """
            precept P
            field Rate as number default 1
            field Result as number default 0
            state A initial
            event Calculate
            from A on Calculate -> set Result = 100 / Rate -> no transition
            """;

        var (actions, uri, _) = await GetC93CodeActionsAsync(text);

        var codeAction = actions
            .Select(a => a.CodeAction)
            .FirstOrDefault(a => a is not null && a.Title.Contains("Add `positive` constraint", StringComparison.Ordinal));

        codeAction.Should().NotBeNull();
        var updatedText = ApplyWorkspaceEdit(text, uri, codeAction!.Edit!);
        updatedText.Should().Contain("field Rate as number positive default 1");
    }

    [Fact]
    public async Task CodeActions_C93_EventArgDivisor_OffersAddPositiveConstraint()
    {
        const string text = """
            precept P
            field Result as number default 0
            state A initial
            event Calculate with Divisor as number
            from A on Calculate -> set Result = 100 / Calculate.Divisor -> no transition
            """;

        var (actions, uri, _) = await GetC93CodeActionsAsync(text);

        var codeAction = actions
            .Select(a => a.CodeAction)
            .FirstOrDefault(a => a is not null && a.Title.Contains("Add `positive` constraint", StringComparison.Ordinal));

        codeAction.Should().NotBeNull();
        var updatedText = ApplyWorkspaceEdit(text, uri, codeAction!.Edit!);
        updatedText.Should().Contain("event Calculate with Divisor as number positive");
    }

    [Fact]
    public async Task CodeActions_C93_EventArgDivisor_OffersAddEnsure()
    {
        const string text = """
            precept P
            field Result as number default 0
            state A initial
            event Calculate with Divisor as number
            from A on Calculate -> set Result = 100 / Calculate.Divisor -> no transition
            """;

        var (actions, uri, _) = await GetC93CodeActionsAsync(text);

        var codeAction = actions
            .Select(a => a.CodeAction)
            .FirstOrDefault(a => a is not null && a.Title.Contains("ensure Divisor > 0", StringComparison.Ordinal));

        codeAction.Should().NotBeNull();
        var updatedText = ApplyWorkspaceEdit(text, uri, codeAction!.Edit!);
        updatedText.Should().Contain("on Calculate ensure Divisor > 0 because \"Divisor must be nonzero\"");
    }

    [Fact]
    public async Task CodeActions_C93_WhenGuard_AppendsToExistingGuard()
    {
        const string text = """
            precept P
            field Rate as number default 1
            field Amount as number default 0
            field Result as number default 0
            state A initial
            event Calculate
            from A on Calculate when Amount > 0 -> set Result = Amount / Rate -> no transition
            """;

        var (actions, uri, _) = await GetC93CodeActionsAsync(text);

        var codeAction = actions
            .Select(a => a.CodeAction)
            .FirstOrDefault(a => a is not null && a.Title.Contains("when Rate != 0", StringComparison.Ordinal));

        codeAction.Should().NotBeNull();
        var updatedText = ApplyWorkspaceEdit(text, uri, codeAction!.Edit!);
        updatedText.Should().Contain("when Amount > 0 and Rate != 0 ->");
    }

    private static async Task<(CommandOrCodeActionContainer Actions, DocumentUri Uri, string Text)> GetC93CodeActionsAsync(string text)
    {
        var analyzer = PreceptTextDocumentSyncHandler.SharedAnalyzer;
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        var diagnostics = analyzer.GetDiagnostics(uri).ToArray();
        var c93Diagnostic = diagnostics.FirstOrDefault(d => d.Code is { String: "PRECEPT093" });
        c93Diagnostic.Should().NotBeNull("Expected a C93 diagnostic but none was found");

        var handler = new PreceptCodeActionHandler();
        var actions = await handler.Handle(
            new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Range = c93Diagnostic!.Range,
                Context = new CodeActionContext
                {
                    Diagnostics = new Container<Diagnostic>(c93Diagnostic)
                }
            },
            CancellationToken.None);

        actions.Should().NotBeNull();
        return (actions!, uri, text);
    }
}
