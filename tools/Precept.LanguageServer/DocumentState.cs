using System.Collections.Generic;
using System.Threading;
using Precept.Pipeline;

namespace Precept.LanguageServer;

/// <summary>
/// Holds the latest compiled state for a single open document.
/// Thread-safe via atomic snapshot swaps.
/// </summary>
internal sealed class DocumentState
{
    private static readonly IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> EmptySuggestions = new Dictionary<DiagnosticKey, SuggestionInfo>();

    private Snapshot _snapshot = Snapshot.Empty;

    /// <summary>The most recently compiled artifact, or null if the document has not been compiled yet.</summary>
    public Compilation? Current => Volatile.Read(ref _snapshot).Current;

    public IReadOnlyDictionary<DiagnosticKey, SuggestionInfo>? Suggestions => Volatile.Read(ref _snapshot).Suggestions;

    public string SourceText => Volatile.Read(ref _snapshot).SourceText;

    public int Version => Volatile.Read(ref _snapshot).Version;

    /// <summary>Atomically replaces the current compilation with a new one.</summary>
    public void Update(Compilation compilation, IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions)
    {
        while (true)
        {
            var current = Volatile.Read(ref _snapshot);
            var updated = new Snapshot(current.Version + 1, compilation, suggestions, current.SourceText);

            if (ReferenceEquals(Interlocked.CompareExchange(ref _snapshot, updated, current), current))
            {
                return;
            }
        }
    }

    public void Update(
        Compilation compilation,
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions,
        string sourceText)
    {
        while (true)
        {
            var current = Volatile.Read(ref _snapshot);
            var updated = new Snapshot(current.Version + 1, compilation, suggestions, sourceText);

            if (ReferenceEquals(Interlocked.CompareExchange(ref _snapshot, updated, current), current))
            {
                return;
            }
        }
    }

    public void Update(Compilation compilation) =>
        Update(compilation, EmptySuggestions);

    public void Update(Compilation compilation, string sourceText) =>
        Update(compilation, EmptySuggestions, sourceText);

    public bool TryUpdate(
        int version,
        Compilation compilation,
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions)
    {
        while (true)
        {
            var current = Volatile.Read(ref _snapshot);
            if (version <= current.Version)
            {
                return false;
            }

            var updated = new Snapshot(version, compilation, suggestions, current.SourceText);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _snapshot, updated, current), current))
            {
                return true;
            }
        }
    }

    public bool TryUpdate(
        int version,
        Compilation compilation,
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions,
        string sourceText)
    {
        while (true)
        {
            var current = Volatile.Read(ref _snapshot);
            if (version <= current.Version)
            {
                return false;
            }

            var updated = new Snapshot(version, compilation, suggestions, sourceText);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _snapshot, updated, current), current))
            {
                return true;
            }
        }
    }

    private sealed record Snapshot(
        int Version,
        Compilation? Current,
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> Suggestions,
        string SourceText)
    {
        public static Snapshot Empty { get; } = new(0, null, EmptySuggestions, string.Empty);
    }
}
