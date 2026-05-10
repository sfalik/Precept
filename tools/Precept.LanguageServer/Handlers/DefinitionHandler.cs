using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class DefinitionHandler : IDefinitionHandler
{
    private readonly DocumentStore _store;

    public DefinitionHandler(DocumentStore store)
    {
        _store = store;
    }

    public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(DefinitionCapability capability)
    {
    }

    public Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        return Task.FromResult<LocationOrLocationLinks?>(HandleDefinition(request.TextDocument.Uri, state.Current, request.Position));
    }

    internal static LocationOrLocationLinks HandleDefinition(DocumentUri uri, Compilation compilation, Position position)
    {
        var semantics = compilation.Semantics;

        foreach (var fieldReference in semantics.FieldReferences)
        {
            if (Covers(fieldReference.Site, position))
            {
                return ToLocationLinks(uri, fieldReference.Field.NameSpan);
            }
        }

        foreach (var stateReference in semantics.StateReferences)
        {
            if (Covers(stateReference.Site, position))
            {
                return ToLocationLinks(uri, stateReference.State.NameSpan);
            }
        }

        foreach (var eventReference in semantics.EventReferences)
        {
            if (Covers(eventReference.Site, position))
            {
                return ToLocationLinks(uri, eventReference.Event.NameSpan);
            }
        }

        foreach (var argReference in semantics.ArgReferences)
        {
            if (Covers(argReference.Site, position))
            {
                return ToLocationLinks(uri, argReference.Arg.Span);
            }
        }

        return new LocationOrLocationLinks();
    }

    private static bool Covers(SourceSpan span, Position pos)
    {
        var startLine = span.StartLine - 1;
        var startChar = span.StartColumn - 1;
        var endLine = span.EndLine - 1;
        var endChar = span.EndColumn - 1;

        if (pos.Line < startLine || pos.Line > endLine)
        {
            return false;
        }

        if (pos.Line == startLine && pos.Character < startChar)
        {
            return false;
        }

        if (pos.Line == endLine && pos.Character >= endChar)
        {
            return false;
        }

        return true;
    }

    private static LocationOrLocationLinks ToLocationLinks(DocumentUri uri, SourceSpan span) =>
        new(new LocationOrLocationLink(new Location
        {
            Uri = uri,
            Range = DiagnosticProjector.ToRange(span),
        }));
}
