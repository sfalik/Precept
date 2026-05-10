using System.Threading;
using Precept.Pipeline;

namespace Precept.LanguageServer;

/// <summary>
/// Holds the latest compiled state for a single open document.
/// Thread-safe via atomic swap using <see cref="Interlocked.Exchange(ref Compilation?, Compilation?)"/>.
/// </summary>
internal sealed class DocumentState
{
    private volatile Compilation? _current;

    /// <summary>The most recently compiled artifact, or null if the document has not been compiled yet.</summary>
    public Compilation? Current => _current;

    /// <summary>Atomically replaces the current compilation with a new one.</summary>
    public void Update(Compilation compilation) =>
        Interlocked.Exchange(ref _current, compilation);
}
