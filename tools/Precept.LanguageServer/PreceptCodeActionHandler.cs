using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;

namespace Precept.LanguageServer;

internal sealed class PreceptCodeActionHandler : ICodeActionHandler
{
    private static readonly TextDocumentSelector Selector = new(new TextDocumentFilter
    {
        Pattern = "**/*.precept",
        Language = "precept"
    });

    public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        if (!PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.TextDocument.Uri, out var text))
            return Task.FromResult<CommandOrCodeActionContainer?>(new CommandOrCodeActionContainer());

        var actions = new List<CommandOrCodeAction>();

        var issues = PreceptTextDocumentSyncHandler.SharedAnalyzer.GetRejectOnlyStateEventIssues(request.TextDocument.Uri)
            .Where(issue => issue.FirstLineIndex == request.Range.Start.Line)
            .ToArray();
        foreach (var issue in issues)
        {
            var edit = BuildRemoveRejectOnlyPairEdit(text, request.TextDocument.Uri, issue.StateName, issue.EventName);
            if (edit is null)
                continue;

            var relatedDiagnostics = request.Context.Diagnostics?
                .Where(diagnostic => diagnostic.Range.Start.Line == issue.FirstLineIndex)
                .ToArray();

            actions.Add(new CodeAction
            {
                Title = $"Remove reject-only rows for '{issue.EventName}' in state '{issue.StateName}'",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = relatedDiagnostics is { Length: > 0 } ? new Container<Diagnostic>(relatedDiagnostics) : null,
                Edit = edit
            });
        }

        var orphanedEvents = PreceptTextDocumentSyncHandler.SharedAnalyzer.GetOrphanedEventIssues(request.TextDocument.Uri)
            .Where(issue => issue.LineIndex == request.Range.Start.Line)
            .ToArray();

        foreach (var issue in orphanedEvents)
        {
            var edit = BuildRemoveOrphanedEventEdit(text, request.TextDocument.Uri, issue.EventName);
            if (edit is null)
                continue;

            var relatedDiagnostics = request.Context.Diagnostics?
                .Where(diagnostic => diagnostic.Range.Start.Line == issue.LineIndex)
                .ToArray();

            actions.Add(new CodeAction
            {
                Title = $"Remove orphaned event '{issue.EventName}'",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = relatedDiagnostics is { Length: > 0 } ? new Container<Diagnostic>(relatedDiagnostics) : null,
                Edit = edit
            });
        }

        return Task.FromResult<CommandOrCodeActionContainer?>(new CommandOrCodeActionContainer(actions));
    }

    public CodeActionRegistrationOptions GetRegistrationOptions(CodeActionCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector,
            ResolveProvider = false,
            CodeActionKinds = new Container<CodeActionKind>(CodeActionKind.QuickFix)
        };

    private static WorkspaceEdit? BuildRemoveRejectOnlyPairEdit(string text, DocumentUri uri, string stateName, string eventName)
    {
        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
        if (parseDiags.Count > 0 || model is null)
            return null;

        try
        {
            PreceptCompiler.Compile(model);
        }
        catch (Exception)
        {
            return null;
        }

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var lineIndexes = FindTransitionPairLines(lines, stateName, eventName).ToArray();

        if (lineIndexes.Length == 0)
            return null;

        var ranges = BuildContiguousLineDeletionRanges(lines, lineIndexes);
        var edits = ranges
            .Select(range => new TextEdit { Range = range, NewText = string.Empty })
            .ToArray();

        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [uri] = edits
            }
        };
    }

    private static WorkspaceEdit? BuildRemoveOrphanedEventEdit(string text, DocumentUri uri, string eventName)
    {
        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
        if (parseDiags.Count > 0 || model is null)
            return null;

        try
        {
            PreceptCompiler.Compile(model);
        }
        catch (Exception)
        {
            return null;
        }

        var evt = model.Events.FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
        if (evt is null)
            return null;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var eventLine = FindEventLine(lines, eventName);
        var lineIndexes = new List<int> { eventLine };
        lineIndexes.AddRange((model.EventEnsures ?? Array.Empty<EventEnsure>())
            .Where(assertion => string.Equals(assertion.EventName, eventName, StringComparison.Ordinal))
            .Select(assertion => Math.Max(0, assertion.SourceLine - 1)));

        var ranges = BuildContiguousLineDeletionRanges(lines, lineIndexes);
        var edits = ranges
            .Select(range => new TextEdit { Range = range, NewText = string.Empty })
            .ToArray();

        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [uri] = edits
            }
        };
    }

    private static IReadOnlyList<OmniSharp.Extensions.LanguageServer.Protocol.Models.Range> BuildContiguousLineDeletionRanges(string[] lines, IEnumerable<int> lineIndexes)
    {
        var sorted = lineIndexes.Distinct().OrderBy(index => index).ToArray();
        if (sorted.Length == 0)
            return Array.Empty<OmniSharp.Extensions.LanguageServer.Protocol.Models.Range>();

        var ranges = new List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Range>();
        var blockStart = sorted[0];
        var previous = sorted[0];

        for (var i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] == previous + 1)
            {
                previous = sorted[i];
                continue;
            }

            ranges.Add(CreateNormalizedLineDeletionRange(lines, blockStart, previous));
            blockStart = sorted[i];
            previous = sorted[i];
        }

        ranges.Add(CreateNormalizedLineDeletionRange(lines, blockStart, previous));
        ranges.Reverse();
        return ranges;
    }

    private static OmniSharp.Extensions.LanguageServer.Protocol.Models.Range CreateNormalizedLineDeletionRange(string[] lines, int startLine, int endLine)
    {
        var normalizedStart = startLine;
        var normalizedEnd = endLine;

        // If the deleted block is surrounded by blank lines, absorb one trailing blank
        // line so the file keeps a single visual separator instead of two.
        if (normalizedStart > 0 &&
            normalizedEnd < lines.Length - 1 &&
            IsBlankLine(lines[normalizedStart - 1]) &&
            IsBlankLine(lines[normalizedEnd + 1]))
        {
            normalizedEnd++;
        }
        else if (normalizedEnd == lines.Length - 1 &&
                 normalizedStart > 0 &&
                 IsBlankLine(lines[normalizedStart - 1]))
        {
            // Avoid leaving a dangling separator line at EOF when the last logical block is removed.
            normalizedStart--;
        }

        if (normalizedEnd < lines.Length - 1)
        {
            return new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(normalizedStart, 0),
                new Position(normalizedEnd + 1, 0));
        }

        var lastLineLength = normalizedEnd >= 0 && normalizedEnd < lines.Length ? lines[normalizedEnd].Length : 0;
        return new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
            new Position(normalizedStart, 0),
            new Position(normalizedEnd, lastLineLength));
    }

    private static bool IsBlankLine(string line)
        => string.IsNullOrWhiteSpace(line);

    private static int FindEventLine(string[] lines, string eventName)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith($"event {eventName}", StringComparison.Ordinal))
                return i;
        }

        return 0;
    }

    private static IEnumerable<int> FindTransitionPairLines(string[] lines, string stateName, string eventName)
    {
        var prefix = $"from {stateName} on {eventName}";
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                yield return i;
        }
    }
}