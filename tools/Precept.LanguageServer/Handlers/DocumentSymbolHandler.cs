using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class DocumentSymbolHandler : IDocumentSymbolHandler
{
    private readonly DocumentStore _store;

    public DocumentSymbolHandler(DocumentStore store)
    {
        _store = store;
    }

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(DocumentSymbolCapability capability)
    {
    }

    public Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(new SymbolInformationOrDocumentSymbolContainer());
        }

        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(BuildDocumentSymbols(state.Current));
    }

    internal static SymbolInformationOrDocumentSymbolContainer BuildDocumentSymbols(Compilation compilation) =>
        new(compilation.ConstructManifest.Constructs
            .Where(construct => construct.Meta.IsOutlineNode && construct.Meta.OutlineSymbolTag is not null)
            .Select(construct => new SymbolInformationOrDocumentSymbol(new DocumentSymbol
            {
                Name = ExtractName(construct),
                Kind = Enum.Parse<SymbolKind>(construct.Meta.OutlineSymbolTag!),
                Range = DiagnosticProjector.ToRange(construct.Span),
                SelectionRange = DiagnosticProjector.ToRange(construct.Span),
            })));

    private static string ExtractName(ParsedConstruct construct)
    {
        if (construct.Slots.OfType<IdentifierListSlot>().FirstOrDefault() is { Names.Length: > 0 } identifiers)
        {
            return string.Join(", ", identifiers.Names);
        }

        if (construct.Slots.OfType<StateEntryListSlot>().FirstOrDefault() is { Entries.Length: > 0 } states)
        {
            return string.Join(", ", states.Entries.Select(static entry => entry.Name));
        }

        return construct.Meta.Name;
    }
}
