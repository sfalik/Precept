using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer.Handlers;

internal sealed class RenameHandler : IRenameHandler, IPrepareRenameHandler
{
    private readonly DocumentStore _store;

    public RenameHandler(DocumentStore store)
    {
        _store = store;
    }

    public RenameRegistrationOptions GetRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            PrepareProvider = true,
        };

    public void SetCapability(RenameCapability capability)
    {
    }

    public Task<RangeOrPlaceholderRange?> Handle(PrepareRenameParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<RangeOrPlaceholderRange?>(null);
        }

        return Task.FromResult(HandlePrepareRename(state.Current, request.Position));
    }

    public Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<WorkspaceEdit?>(new WorkspaceEdit());
        }

        return Task.FromResult<WorkspaceEdit?>(HandleRename(
            request.TextDocument.Uri,
            state.Current,
            request.Position,
            request.NewName));
    }

    internal static RangeOrPlaceholderRange? HandlePrepareRename(Precept.Pipeline.Compilation compilation, Position position)
    {
        if (!TryGetRenameOccurrence(compilation, position, out _, out var span))
        {
            return null;
        }

        return new RangeOrPlaceholderRange(DiagnosticProjector.ToRange(span));
    }

    internal static WorkspaceEdit HandleRename(DocumentUri uri, Precept.Pipeline.Compilation compilation, Position position, string newName)
    {
        if (!TryGetRenameOccurrence(compilation, position, out var occurrence, out _))
        {
            return new WorkspaceEdit();
        }

        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [uri] = SymbolNavigation
                    .GetRenameSpans(compilation.Semantics, occurrence)
                    .Select(span => new TextEdit
                    {
                        Range = DiagnosticProjector.ToRange(span),
                        NewText = newName,
                    })
                    .ToArray(),
            },
        };
    }

    private static bool TryGetRenameOccurrence(
        Precept.Pipeline.Compilation compilation,
        Position position,
        out SymbolOccurrence occurrence,
        out Precept.Pipeline.SourceSpan span)
    {
        if (SymbolNavigation.TryFindOccurrence(compilation, position, out occurrence)
            && SymbolNavigation.TryGetRenameSpanAtPosition(compilation.Semantics, occurrence, position, out span))
        {
            return true;
        }

        occurrence = null!;
        span = default;
        return false;
    }
}
