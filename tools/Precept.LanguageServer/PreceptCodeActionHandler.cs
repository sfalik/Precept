using System.Text.RegularExpressions;
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

    private static readonly Regex C93DivisorNameRegex = new(@"Divisor '([^']+)'", RegexOptions.Compiled);

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

        // ── C93: Unproven divisor safety ──
        var c93Diagnostics = (request.Context.Diagnostics ?? Enumerable.Empty<Diagnostic>())
            .Where(d => d.Code is { String: "PRECEPT093" })
            .ToArray();

        if (c93Diagnostics.Length > 0)
        {
            var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
            if (parseDiags.Count == 0 && model is not null)
            {
                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var c93 in c93Diagnostics)
                    AddC93CodeActions(actions, lines, text, request.TextDocument.Uri, model, c93);
            }
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
            .Where(ensure => string.Equals(ensure.EventName, eventName, StringComparison.Ordinal))
            .Select(ensure => Math.Max(0, ensure.SourceLine - 1)));

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

    // ═══════════════════════════════════════════════════════════════
    // C93 — Unproven divisor safety code actions
    // ═══════════════════════════════════════════════════════════════

    private static void AddC93CodeActions(
        List<CommandOrCodeAction> actions,
        string[] lines,
        string text,
        DocumentUri uri,
        PreceptDefinition model,
        Diagnostic diagnostic)
    {
        var divisorName = ExtractC93DivisorName(diagnostic, lines);
        if (divisorName is null)
            return;

        var isDotted = divisorName.Contains('.');
        string? eventName = null;
        string? argName = null;
        string? fieldName = null;

        if (isDotted)
        {
            var dotIndex = divisorName.IndexOf('.');
            eventName = divisorName[..dotIndex];
            argName = divisorName[(dotIndex + 1)..];
        }
        else
        {
            fieldName = divisorName;
        }

        // Action 1: Add `positive` constraint
        var positiveEdit = isDotted
            ? BuildAddPositiveToEventArgEdit(lines, uri, model, eventName!, argName!)
            : BuildAddPositiveToFieldEdit(lines, uri, model, fieldName!);

        if (positiveEdit is not null)
        {
            var displayName = isDotted ? argName! : fieldName!;
            actions.Add(new CodeAction
            {
                Title = $"Add `positive` constraint to field `{displayName}`",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new Container<Diagnostic>(diagnostic),
                Edit = positiveEdit
            });
        }

        // Action 2: Add ensure (event-arg only)
        if (isDotted)
        {
            var ensureEdit = BuildAddEnsureEdit(lines, text, uri, model, eventName!, argName!);
            if (ensureEdit is not null)
            {
                actions.Add(new CodeAction
                {
                    Title = $"Add `ensure {argName} > 0` to event",
                    Kind = CodeActionKind.QuickFix,
                    Diagnostics = new Container<Diagnostic>(diagnostic),
                    Edit = ensureEdit
                });
            }
        }

        // Action 3: Add `when` guard
        var guardName = isDotted ? divisorName : fieldName!;
        var guardEdit = BuildAddWhenGuardEdit(lines, uri, (int)diagnostic.Range.Start.Line, guardName);
        if (guardEdit is not null)
        {
            actions.Add(new CodeAction
            {
                Title = $"Add `when {guardName} != 0` guard",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new Container<Diagnostic>(diagnostic),
                Edit = guardEdit
            });
        }
    }

    private static readonly Regex IdentifierTokenRegex = new(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)?", RegexOptions.Compiled);

    private static string? ExtractC93DivisorName(Diagnostic diagnostic, string[] lines)
    {
        // Span-driven: read the source text at the diagnostic start position and
        // extract the first identifier token (possibly dotted like Event.Arg).
        var startLine = (int)diagnostic.Range.Start.Line;
        var startChar = (int)diagnostic.Range.Start.Character;

        if (startLine >= 0 && startLine < lines.Length && startChar > 0)
        {
            var line = lines[startLine];
            if (startChar < line.Length)
            {
                var rest = line[startChar..];
                var tokenMatch = IdentifierTokenRegex.Match(rest);
                if (tokenMatch.Success)
                    return tokenMatch.Value;
            }
        }

        // Fallback: parse from message text.
        var match = C93DivisorNameRegex.Match(diagnostic.Message);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static WorkspaceEdit? BuildAddPositiveToFieldEdit(
        string[] lines, DocumentUri uri, PreceptDefinition model, string fieldName)
    {
        var field = model.Fields.FirstOrDefault(f =>
            string.Equals(f.Name, fieldName, StringComparison.Ordinal));
        if (field is null)
            return null;

        var lineIndex = field.SourceLine - 1;
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return null;

        var line = lines[lineIndex];
        var typeMatch = Regex.Match(line, @"as\s+(?:number|integer|decimal)\??");
        if (!typeMatch.Success)
            return null;

        var insertPos = typeMatch.Index + typeMatch.Length;
        var newLine = line[..insertPos] + " positive" + line[insertPos..];

        return MakeSingleLineEdit(uri, lineIndex, line.Length, newLine);
    }

    private static WorkspaceEdit? BuildAddPositiveToEventArgEdit(
        string[] lines, DocumentUri uri, PreceptDefinition model, string eventName, string argName)
    {
        var evt = model.Events.FirstOrDefault(e =>
            string.Equals(e.Name, eventName, StringComparison.Ordinal));
        if (evt is null)
            return null;

        var lineIndex = evt.SourceLine - 1;
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return null;

        var line = lines[lineIndex];
        var pattern = Regex.Escape(argName) + @"\s+as\s+(?:number|integer|decimal)\??";
        var argMatch = Regex.Match(line, pattern);
        if (!argMatch.Success)
            return null;

        var insertPos = argMatch.Index + argMatch.Length;
        var newLine = line[..insertPos] + " positive" + line[insertPos..];

        return MakeSingleLineEdit(uri, lineIndex, line.Length, newLine);
    }

    private static WorkspaceEdit? BuildAddEnsureEdit(
        string[] lines, string text, DocumentUri uri, PreceptDefinition model, string eventName, string argName)
    {
        var evt = model.Events.FirstOrDefault(e =>
            string.Equals(e.Name, eventName, StringComparison.Ordinal));
        if (evt is null)
            return null;

        var lineIndex = evt.SourceLine - 1;
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return null;

        var eventLine = lines[lineIndex];
        var indent = eventLine.Length - eventLine.TrimStart().Length;
        var indentStr = eventLine[..indent];

        var newline = text.Contains("\r\n") ? "\r\n" : "\n";
        var ensureLine = $"{indentStr}on {eventName} ensure {argName} > 0 because \"{argName} must be positive\"";

        var insertPosition = new Position(lineIndex + 1, 0);

        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [uri] = new[]
                {
                    new TextEdit
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            insertPosition, insertPosition),
                        NewText = ensureLine + newline
                    }
                }
            }
        };
    }

    private static WorkspaceEdit? BuildAddWhenGuardEdit(
        string[] lines, DocumentUri uri, int diagnosticLineIndex, string guardName)
    {
        if (diagnosticLineIndex < 0 || diagnosticLineIndex >= lines.Length)
            return null;

        var line = lines[diagnosticLineIndex];
        var arrowIndex = line.IndexOf("->", StringComparison.Ordinal);
        if (arrowIndex < 0)
            return null;

        var prefix = line[..arrowIndex];
        var suffix = line[arrowIndex..];

        string newLine;
        if (prefix.Contains(" when ", StringComparison.Ordinal))
        {
            // Existing when clause — append with `and`
            newLine = prefix.TrimEnd() + $" and {guardName} != 0 " + suffix;
        }
        else
        {
            // No when clause — insert before ->
            newLine = prefix.TrimEnd() + $" when {guardName} != 0 " + suffix;
        }

        return MakeSingleLineEdit(uri, diagnosticLineIndex, line.Length, newLine);
    }

    private static WorkspaceEdit MakeSingleLineEdit(DocumentUri uri, int lineIndex, int lineLength, string newText)
    {
        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [uri] = new[]
                {
                    new TextEdit
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new Position(lineIndex, 0),
                            new Position(lineIndex, lineLength)),
                        NewText = newText
                    }
                }
            }
        };
    }
}
