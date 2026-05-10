using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using PreceptDiagnosticCode = Precept.Language.DiagnosticCode;

namespace Precept.LanguageServer.Handlers;

internal sealed class CodeActionHandler : ICodeActionHandler
{
    private readonly DocumentStore _store;

    public CodeActionHandler(DocumentStore store)
    {
        _store = store;
    }

    public CodeActionRegistrationOptions GetRegistrationOptions(
        CodeActionCapability capability,
        ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            CodeActionKinds = new Container<CodeActionKind>(CodeActionKind.QuickFix),
            ResolveProvider = false,
        };

    public void SetCapability(CodeActionCapability capability)
    {
    }

    public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        if (!request.Context.Diagnostics.Any() ||
            !_store.TryGet(request.TextDocument.Uri, out var state) ||
            state.Current is null)
        {
            return Task.FromResult<CommandOrCodeActionContainer?>(null);
        }

        List<CommandOrCodeAction> actions = [];

        foreach (var diagnostic in request.Context.Diagnostics)
        {
            if (!TryParseCode(diagnostic, out var code))
            {
                continue;
            }

            if (state.Suggestions is not null &&
                TryFindSuggestion(state.Suggestions, code, diagnostic.Range, out var suggestion))
            {
                actions.Add(new CodeAction
                {
                    Title = $"Rename '{suggestion.OriginalName}' to '{suggestion.SuggestedName}'",
                    Kind = CodeActionKind.QuickFix,
                    Diagnostics = new Container<LspDiagnostic>(diagnostic),
                    Edit = new WorkspaceEdit
                    {
                        Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                        {
                            [request.TextDocument.Uri] = new[]
                            {
                                new TextEdit
                                {
                                    Range = diagnostic.Range,
                                    NewText = suggestion.SuggestedName,
                                },
                            },
                        },
                    },
                    IsPreferred = true,
                });
            }

            if (TryGetUnterminatedFix(request.TextDocument.Uri, code, diagnostic, out var fixAction))
            {
                actions.Add(fixAction);
            }

            if (TryGetFixHintAction(code, diagnostic, out var hintAction))
            {
                actions.Add(hintAction);
            }
        }

        return Task.FromResult<CommandOrCodeActionContainer?>(actions.Count == 0 ? null : new CommandOrCodeActionContainer(actions));
    }

    private static bool TryParseCode(LspDiagnostic diagnostic, out PreceptDiagnosticCode code)
    {
        if (diagnostic.Code is { } rawCode &&
            rawCode.IsString &&
            Enum.TryParse(rawCode.String, out code))
        {
            return true;
        }

        code = default;
        return false;
    }

    private static bool TryFindSuggestion(
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions,
        PreceptDiagnosticCode code,
        LspRange range,
        out SuggestionInfo suggestion)
    {
        foreach (var entry in suggestions)
        {
            if (entry.Key.Code == code && RangeEquals(DiagnosticProjector.ToRange(entry.Key.Span), range))
            {
                suggestion = entry.Value;
                return true;
            }
        }

        suggestion = null!;
        return false;
    }

    private static bool RangeEquals(LspRange left, LspRange right) =>
        left.Start.Line == right.Start.Line &&
        left.Start.Character == right.Start.Character &&
        left.End.Line == right.End.Line &&
        left.End.Character == right.End.Character;

    private static bool TryGetUnterminatedFix(
        DocumentUri uri,
        PreceptDiagnosticCode code,
        LspDiagnostic diagnostic,
        out CommandOrCodeAction action)
    {
        var insertion = code switch
        {
            PreceptDiagnosticCode.UnterminatedStringLiteral => "\"",
            PreceptDiagnosticCode.UnterminatedTypedConstant => "'",
            _ => null,
        };

        if (insertion is null)
        {
            action = default!;
            return false;
        }

        action = new CodeAction
        {
            Title = $"Insert closing {insertion}",
            Kind = CodeActionKind.QuickFix,
            Diagnostics = new Container<LspDiagnostic>(diagnostic),
            Edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    [uri] = new[]
                    {
                        new TextEdit
                        {
                            Range = new LspRange(diagnostic.Range.End, diagnostic.Range.End),
                            NewText = insertion,
                        },
                    },
                },
            },
            IsPreferred = true,
        };

        return true;
    }

    private static bool TryGetFixHintAction(PreceptDiagnosticCode code, LspDiagnostic diagnostic, out CommandOrCodeAction action)
    {
        var meta = Diagnostics.GetMeta(code);
        if (meta.FixHint is null)
        {
            action = default!;
            return false;
        }

        action = new CodeAction
        {
            Title = $"ℹ {meta.FixHint}",
            Kind = CodeActionKind.QuickFix,
            Diagnostics = new Container<LspDiagnostic>(diagnostic),
            Command = new Command
            {
                Name = "precept.showFixHint",
                Title = meta.FixHint,
                Arguments = new JArray(
                    meta.FixHint,
                    meta.ExampleBefore ?? string.Empty,
                    meta.ExampleAfter ?? string.Empty),
            },
        };

        return true;
    }
}
