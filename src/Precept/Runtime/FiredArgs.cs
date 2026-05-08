namespace Precept.Runtime;

/// <summary>
/// Event arg egress — carries the arg values that were submitted to an event call.
/// Appears on <see cref="EventOutcome.Transitioned.Args"/>, <see cref="EventOutcome.Applied.Args"/>, and
/// <see cref="EventOutcome.Rejected.Args"/>.
/// </summary>
/// <remarks>
/// The raw indexer returns <see cref="PreceptValue"/>; <see cref="Get{T}"/> resolves
/// via the registered <c>TypeRuntime&lt;T&gt;.ToClr</c> delegate for zero-boxing egress.
/// </remarks>
public sealed class FiredArgs
{
    private FiredArgs() { }

    /// <summary>Returns the raw <see cref="PreceptValue"/> for the named arg.</summary>
    public PreceptValue this[string name]
        => throw new NotImplementedException();

    /// <summary>Returns the arg value converted to <typeparamref name="T"/> via the registered <c>TypeRuntime&lt;T&gt;</c>.</summary>
    public T Get<T>(string name)
        => throw new NotImplementedException();
}
