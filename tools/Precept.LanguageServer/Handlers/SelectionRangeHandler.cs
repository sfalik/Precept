using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer.Handlers;

internal sealed class SelectionRangeHandler : ISelectionRangeHandler
{
    private readonly DocumentStore _store;

    public SelectionRangeHandler(DocumentStore store)
    {
        _store = store;
    }

    public SelectionRangeRegistrationOptions GetRegistrationOptions(SelectionRangeCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(SelectionRangeCapability capability)
    {
    }

    public Task<Container<SelectionRange>?> Handle(SelectionRangeParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<Container<SelectionRange>?>(new Container<SelectionRange>());
        }

        return Task.FromResult<Container<SelectionRange>?>(
            new Container<SelectionRange>(request.Positions
                .Select(position => SyntaxSelectionBuilder.BuildSelectionRange(state.Current, position))
                .Where(static selectionRange => selectionRange is not null)
                .Select(static selectionRange => selectionRange!)));
    }
}
