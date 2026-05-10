using System.Collections.Concurrent;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Precept.LanguageServer;

/// <summary>
/// Thread-safe registry of open documents keyed by <see cref="DocumentUri"/>.
/// </summary>
internal sealed class DocumentStore
{
    private readonly ConcurrentDictionary<DocumentUri, DocumentState> _states = new();

    public DocumentState GetOrAdd(DocumentUri uri) =>
        _states.GetOrAdd(uri, static _ => new DocumentState());

    public bool TryGet(DocumentUri uri, out DocumentState state) =>
        _states.TryGetValue(uri, out state!);

    public KeyValuePair<DocumentUri, DocumentState>[] Snapshot() => _states.ToArray();

    public IEnumerable<(DocumentUri Uri, DocumentState State)> EnumerateOpenDocuments()
    {
        foreach (var pair in Snapshot())
        {
            yield return (pair.Key, pair.Value);
        }
    }

    public void Remove(DocumentUri uri) =>
        _states.TryRemove(uri, out _);
}
