using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
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

    private static readonly HashSet<string> ProofDiagnosticCodes = new(StringComparer.Ordinal)
    {
        "PRECEPT092", "PRECEPT093", "PRECEPT076",
        "PRECEPT095", "PRECEPT096", "PRECEPT097", "PRECEPT098"
    };

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

        // ── Proof-backed diagnostics: C92, C93, C76, C95-C98 ──
        var proofDiagnostics = (request.Context.Diagnostics ?? Enumerable.Empty<Diagnostic>())
            .Where(d => d.Code is not null && d.Code.Value.String is not null && ProofDiagnosticCodes.Contains(d.Code.Value.String))
            .ToArray();

        if (proofDiagnostics.Length > 0)
        {
            var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
            if (parseDiags.Count == 0 && model is not null)
            {
                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var diag in proofDiagnostics)
                {
                    var code = diag.Code!.Value.String;
                    switch (code)
                    {
                        case "PRECEPT092":
                            AddDivisorCodeActions(actions, lines, text, request.TextDocument.Uri, model, diag, isContradiction: true);
                            break;
                        case "PRECEPT093":
                            AddDivisorCodeActions(actions, lines, text, request.TextDocument.Uri, model, diag, isContradiction: false);
                            break;
                        case "PRECEPT076":
                            AddSqrtCodeActions(actions, lines, text, request.TextDocument.Uri, model, diag);
                            break;
                        case "PRECEPT095":
                        case "PRECEPT096":
                        case "PRECEPT097":
                        case "PRECEPT098":
                            AddRemoveLineCodeAction(actions, lines, request.TextDocument.Uri, diag);
                            break;
                    }
                }
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
    // Proof-backed code actions (C92, C93, C76, C95-C98)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Extracts the subject name from the diagnostic's structured Data field.</summary>
    private static string? ExtractSubjectFromData(Diagnostic diagnostic)
    {
        if (diagnostic.Data is JObject data && data.TryGetValue("subject", out var subjectToken))
            return subjectToken.Value<string>();
        return null;
    }

    private static readonly Regex IdentifierTokenRegex = new(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)?", RegexOptions.Compiled);

    /// <summary>
    /// Extracts the subject name from a proof diagnostic. Prefers structured Data;
    /// falls back to span-based token extraction at the diagnostic position.
    /// </summary>
    private static string? ExtractSubjectName(Diagnostic diagnostic, string[] lines)
    {
        // Prefer structured metadata from Diagnostic.Data
        var subject = ExtractSubjectFromData(diagnostic);
        if (subject is not null)
            return subject;

        // Fallback: span-driven token extraction at diagnostic position
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

        return null;
    }

    private static (bool IsDotted, string? EventName, string? ArgName, string? FieldName) ParseSubjectName(string subjectName)
    {
        if (subjectName.Contains('.'))
        {
            var dotIndex = subjectName.IndexOf('.');
            return (true, subjectName[..dotIndex], subjectName[(dotIndex + 1)..], null);
        }
        return (false, null, null, subjectName);
    }

    // ── C92/C93: Division safety code actions ──

    private static void AddDivisorCodeActions(
        List<CommandOrCodeAction> actions,
        string[] lines,
        string text,
        DocumentUri uri,
        PreceptDefinition model,
        Diagnostic diagnostic,
        bool isContradiction)
    {
        var subjectName = ExtractSubjectName(diagnostic, lines);
        if (subjectName is null)
            return;

        var (isDotted, eventName, argName, fieldName) = ParseSubjectName(subjectName);

        // For C92 (contradiction) the divisor is provably zero. Adding a `positive` constraint
        // may still be meaningful if the field lacks one — it would turn the contradiction into
        // a constraint+rule mismatch the user can debug. But the main useful action is a guard.
        if (!isContradiction)
        {
            // Action: Add `positive` constraint
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

            // Action: Add ensure (event-arg only)
            if (isDotted)
            {
                var ensureEdit = BuildAddEnsureEdit(lines, text, uri, model, eventName!, argName!, "> 0", "must be positive");
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
        }

        // Action: Add `when` guard (useful for both C92 and C93)
        var guardName = isDotted ? subjectName : fieldName!;
        var guardEdit = BuildAddWhenGuardEdit(lines, uri, (int)diagnostic.Range.Start.Line, guardName, "!= 0");
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

    // ── C76: sqrt non-negative safety code actions ──

    private static void AddSqrtCodeActions(
        List<CommandOrCodeAction> actions,
        string[] lines,
        string text,
        DocumentUri uri,
        PreceptDefinition model,
        Diagnostic diagnostic)
    {
        var subjectName = ExtractSubjectName(diagnostic, lines);
        if (subjectName is null)
            return;

        var (isDotted, eventName, argName, fieldName) = ParseSubjectName(subjectName);

        // Action: Add `nonnegative` constraint
        var constraintEdit = isDotted
            ? BuildAddConstraintToEventArgEdit(lines, uri, model, eventName!, argName!, "nonnegative")
            : BuildAddConstraintToFieldEdit(lines, uri, model, fieldName!, "nonnegative");

        if (constraintEdit is not null)
        {
            var displayName = isDotted ? argName! : fieldName!;
            actions.Add(new CodeAction
            {
                Title = $"Add `nonnegative` constraint to field `{displayName}`",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new Container<Diagnostic>(diagnostic),
                Edit = constraintEdit
            });
        }

        // Action: Add ensure (event-arg only)
        if (isDotted)
        {
            var ensureEdit = BuildAddEnsureEdit(lines, text, uri, model, eventName!, argName!, ">= 0", "must be non-negative");
            if (ensureEdit is not null)
            {
                actions.Add(new CodeAction
                {
                    Title = $"Add `ensure {argName} >= 0` to event",
                    Kind = CodeActionKind.QuickFix,
                    Diagnostics = new Container<Diagnostic>(diagnostic),
                    Edit = ensureEdit
                });
            }
        }

        // Action: Add `when` guard
        var guardName = isDotted ? subjectName : fieldName!;
        var guardEdit = BuildAddWhenGuardEdit(lines, uri, (int)diagnostic.Range.Start.Line, guardName, ">= 0");
        if (guardEdit is not null)
        {
            actions.Add(new CodeAction
            {
                Title = $"Add `when {guardName} >= 0` guard",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new Container<Diagnostic>(diagnostic),
                Edit = guardEdit
            });
        }
    }

    // ── C95-C98: Remove problematic line code actions ──

    private static void AddRemoveLineCodeAction(
        List<CommandOrCodeAction> actions,
        string[] lines,
        DocumentUri uri,
        Diagnostic diagnostic)
    {
        var lineIndex = (int)diagnostic.Range.Start.Line;
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return;

        var code = diagnostic.Code!.Value.String;
        var title = code switch
        {
            "PRECEPT095" => "Remove contradictory rule",
            "PRECEPT096" => "Remove vacuous rule",
            "PRECEPT097" => "Remove unreachable transition",
            "PRECEPT098" => "Remove tautological guard",
            _ => "Remove line"
        };

        var ranges = BuildContiguousLineDeletionRanges(lines, new[] { lineIndex });
        var edits = ranges
            .Select(range => new TextEdit { Range = range, NewText = string.Empty })
            .ToArray();

        if (edits.Length == 0)
            return;

        actions.Add(new CodeAction
        {
            Title = title,
            Kind = CodeActionKind.QuickFix,
            Diagnostics = new Container<Diagnostic>(diagnostic),
            Edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    [uri] = edits
                }
            }
        });
    }

    private static WorkspaceEdit? BuildAddPositiveToFieldEdit(
        string[] lines, DocumentUri uri, PreceptDefinition model, string fieldName)
        => BuildAddConstraintToFieldEdit(lines, uri, model, fieldName, "positive");

    private static WorkspaceEdit? BuildAddConstraintToFieldEdit(
        string[] lines, DocumentUri uri, PreceptDefinition model, string fieldName, string constraint)
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
        var newLine = line[..insertPos] + " " + constraint + line[insertPos..];

        return MakeSingleLineEdit(uri, lineIndex, line.Length, newLine);
    }

    private static WorkspaceEdit? BuildAddPositiveToEventArgEdit(
        string[] lines, DocumentUri uri, PreceptDefinition model, string eventName, string argName)
        => BuildAddConstraintToEventArgEdit(lines, uri, model, eventName, argName, "positive");

    private static WorkspaceEdit? BuildAddConstraintToEventArgEdit(
        string[] lines, DocumentUri uri, PreceptDefinition model, string eventName, string argName, string constraint)
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
        var newLine = line[..insertPos] + " " + constraint + line[insertPos..];

        return MakeSingleLineEdit(uri, lineIndex, line.Length, newLine);
    }

    private static WorkspaceEdit? BuildAddEnsureEdit(
        string[] lines, string text, DocumentUri uri, PreceptDefinition model, string eventName, string argName,
        string comparison = "> 0", string reason = "must be positive")
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
        var ensureLine = $"{indentStr}on {eventName} ensure {argName} {comparison} because \"{argName} {reason}\"";

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
        string[] lines, DocumentUri uri, int diagnosticLineIndex, string guardName, string comparison = "!= 0")
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
            newLine = prefix.TrimEnd() + $" and {guardName} {comparison} " + suffix;
        }
        else
        {
            // No when clause — insert before ->
            newLine = prefix.TrimEnd() + $" when {guardName} {comparison} " + suffix;
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
