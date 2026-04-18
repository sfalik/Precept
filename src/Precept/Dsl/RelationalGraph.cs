namespace Precept;

/// <summary>
/// Bounded transitive-closure engine over relational facts stored in
/// <see cref="ProofContext"/>. Stub — to be implemented in Commit 5.
/// </summary>
/// <remarks>
/// In Commit 5 this will perform a BFS over the relational fact store with
/// hard caps (max 64 facts, depth 4, 256 visited nodes) and the
/// strict/non-strict combination matrix: &gt;·&gt;=&gt;, &gt;=·&gt;=&gt;, &gt;=·&gt;=⇒&gt;=.
/// </remarks>
internal sealed class RelationalGraph
{
    internal RelationalGraph() { }

    /// <summary>
    /// Queries the relational graph for the tightest <see cref="NumericInterval"/>
    /// implied by transitivity for the given <see cref="LinearForm"/>.
    /// Returns <see cref="NumericInterval.Unknown"/> until Commit 5 implements the BFS.
    /// </summary>
    public NumericInterval Query(LinearForm form) => NumericInterval.Unknown;
}
