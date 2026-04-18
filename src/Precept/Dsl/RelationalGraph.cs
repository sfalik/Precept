using System.Collections.Generic;

namespace Precept;

/// <summary>
/// Bounded transitive-closure engine over relational facts stored in
/// <see cref="ProofContext"/>. Performs a BFS over the fact graph with hard caps
/// (max 64 facts, depth 4, 256 visited nodes) and the strict/non-strict combination
/// matrix: &gt;·&gt;⇒&gt;, &gt;·&gt;=⇒&gt;, &gt;=·&gt;⇒&gt;, &gt;=·&gt;=⇒&gt;=.
/// </summary>
internal sealed class RelationalGraph
{
    private const int MaxFacts   = 64;
    private const int MaxDepth   = 4;
    private const int MaxVisited = 256;

    private readonly IReadOnlyDictionary<LinearForm, RelationalFact> _facts;

    internal RelationalGraph(IReadOnlyDictionary<LinearForm, RelationalFact> facts)
    {
        _facts = facts;
    }

    /// <summary>
    /// Queries the relational graph for the tightest <see cref="NumericInterval"/>
    /// implied by transitivity for the given <see cref="LinearForm"/>.
    /// Returns <see cref="NumericInterval.Unknown"/> when no transitive proof is found.
    /// </summary>
    /// <remarks>
    /// Only handles simple two-variable forms <c>α·A − β·B</c> (both non-zero, constant = 0).
    /// Multi-variable forms are returned as Unknown.
    /// </remarks>
    public NumericInterval Query(LinearForm form)
    {
        if (_facts.Count == 0 || _facts.Count > MaxFacts)
            return NumericInterval.Unknown;

        // Require exactly 2 variables and zero constant.
        if (form.Terms.Count != 2 || form.Constant != Rational.Zero)
            return NumericInterval.Unknown;

        // Identify the positive-coefficient variable (target) and negative-coefficient variable (source).
        string? posVar = null, negVar = null;
        foreach (var (key, coeff) in form.Terms)
        {
            if (coeff > Rational.Zero) posVar = key;
            else if (coeff < Rational.Zero) negVar = key;
        }

        if (posVar is null || negVar is null)
            return NumericInterval.Unknown;

        // BFS: each node is a variable name; each edge comes from a 2-variable fact.
        // Fact "A - B > 0" (A > B) gives directed edge B → A with kind = GreaterThan.
        // We want to reach posVar starting from negVar.
        // State: (currentNode, accumulatedStrictness, depth).
        var visited = new HashSet<string> { negVar };
        var queue   = new Queue<(string node, RelationKind kind, int depth)>();
        queue.Enqueue((negVar, RelationKind.GreaterThanOrEqual, 0));

        while (queue.Count > 0)
        {
            var (current, accumulated, depth) = queue.Dequeue();
            if (depth >= MaxDepth) continue;

            foreach (var (factForm, factFact) in _facts)
            {
                // Only process simple 2-variable, zero-constant facts.
                if (factForm.Terms.Count != 2 || factForm.Constant != Rational.Zero)
                    continue;

                // Extract the positive and negative variable from this fact.
                string? fPos = null, fNeg = null;
                foreach (var (key, coeff) in factForm.Terms)
                {
                    if (coeff > Rational.Zero) fPos = key;
                    else if (coeff < Rational.Zero) fNeg = key;
                }

                // Edge B → A: only follow when the fact's negative variable matches current node.
                if (fNeg is null || fPos is null || fNeg != current)
                    continue;

                var combined = CombineStrictness(accumulated, factFact.Kind);

                if (fPos == posVar)
                {
                    // Transitive proof found. Return the appropriate open/closed interval.
                    return combined == RelationKind.GreaterThan
                        ? new NumericInterval(0.0, false, double.PositiveInfinity, false)  // (0, +∞)
                        : new NumericInterval(0.0, true,  double.PositiveInfinity, false); // [0, +∞)
                }

                if (!visited.Contains(fPos) && visited.Count < MaxVisited)
                {
                    visited.Add(fPos);
                    queue.Enqueue((fPos, combined, depth + 1));
                }
            }
        }

        return NumericInterval.Unknown;
    }

    /// <summary>
    /// Combines two relation kinds: any strict edge makes the path strict.
    /// <c>&gt;·&gt;=⇒&gt;</c>, <c>&gt;=·&gt;⇒&gt;</c>, <c>&gt;=·&gt;=⇒&gt;=</c>.
    /// </summary>
    private static RelationKind CombineStrictness(RelationKind a, RelationKind b) =>
        (a == RelationKind.GreaterThan || b == RelationKind.GreaterThan)
            ? RelationKind.GreaterThan
            : RelationKind.GreaterThanOrEqual;
}

