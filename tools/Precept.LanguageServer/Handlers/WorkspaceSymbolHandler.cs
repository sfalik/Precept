using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace Precept.LanguageServer.Handlers;

internal sealed class WorkspaceSymbolHandler : IWorkspaceSymbolsHandler
{
    private readonly DocumentStore _store;

    public WorkspaceSymbolHandler(DocumentStore store)
    {
        _store = store;
    }

    public WorkspaceSymbolRegistrationOptions GetRegistrationOptions(WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities) =>
        new();

    public void SetCapability(WorkspaceSymbolCapability capability)
    {
    }

    public Task<Container<WorkspaceSymbol>?> Handle(WorkspaceSymbolParams request, CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        var symbols = _store.EnumerateOpenDocuments()
            .Where(static document => document.State.Current is not null)
            .SelectMany(document => BuildWorkspaceSymbols(document.Uri, document.State.Current!, query))
            .ToArray();

        return Task.FromResult<Container<WorkspaceSymbol>?>(new Container<WorkspaceSymbol>(symbols));
    }

    internal static WorkspaceSymbol[] BuildWorkspaceSymbols(DocumentUri uri, Precept.Pipeline.Compilation compilation, string query) =>
        OutlineSymbolProjector.Project(compilation)
            .Where(symbol => MatchesQuery(symbol.Name, query))
            .Select(symbol => new WorkspaceSymbol
            {
                Name = symbol.Name,
                Kind = symbol.Kind,
                Location = new Location
                {
                    Uri = uri,
                    Range = DiagnosticProjector.ToRange(symbol.SelectionRange),
                },
            })
            .ToArray();

    private static bool MatchesQuery(string symbolName, string query) =>
        query.Length == 0 || symbolName.Contains(query, StringComparison.OrdinalIgnoreCase);
}
