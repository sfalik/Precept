using System.Collections.Generic;

namespace Precept;

/// <summary>
/// Typed proof state container for the Precept proof engine.
/// Wraps an <see cref="IReadOnlyDictionary{TKey,TValue}"/> of proof markers
/// and exposes the proof query API.
/// </summary>
/// <remarks>
/// In Commit 2 this is a thin seam: all logic remains in
/// <see cref="PreceptTypeChecker"/>. Subsequent commits will gradually
/// migrate logic into this class as the unified engine is built out.
/// <para>
/// The six marker conventions stored in <see cref="Symbols"/>:
/// <list type="bullet">
///   <item><c>$positive:{key}</c> — key &gt; 0</item>
///   <item><c>$nonneg:{key}</c> — key &gt;= 0</item>
///   <item><c>$nonzero:{key}</c> — key != 0</item>
///   <item><c>$ival:{key}:{lower}:{lowerInc}:{upper}:{upperInc}</c> — interval</item>
///   <item><c>$gt:{a}:{b}</c> — a &gt; b (strict)</item>
///   <item><c>$gte:{a}:{b}</c> — a &gt;= b (non-strict)</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class ProofContext
{
    private readonly IReadOnlyDictionary<string, StaticValueKind> _symbols;

    public ProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols)
    {
        _symbols = symbols;
    }

    /// <summary>
    /// The raw marker dictionary. Used by <see cref="PreceptTypeChecker"/> methods
    /// that have not yet been migrated to operate on <see cref="ProofContext"/> directly.
    /// </summary>
    internal IReadOnlyDictionary<string, StaticValueKind> Symbols => _symbols;

    // ── Primary query methods ─────────────────────────────────────────────────

    /// <summary>
    /// Infers the tightest <see cref="NumericInterval"/> for <paramref name="expr"/>
    /// using interval arithmetic and relational marker lookup.
    /// Delegates to <see cref="PreceptTypeChecker.TryInferInterval"/>.
    /// </summary>
    public NumericInterval IntervalOf(PreceptExpression expr) =>
        PreceptTypeChecker.TryInferInterval(expr, this);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="expr"/> is provably nonzero.
    /// Combines interval zero-exclusion with relational pattern matching.
    /// </summary>
    public bool KnowsNonzero(PreceptExpression expr) =>
        IntervalOf(expr).ExcludesZero || PreceptTypeChecker.TryInferRelationalNonzero(expr, this);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="expr"/> is provably nonnegative.
    /// </summary>
    public bool KnowsNonnegative(PreceptExpression expr) =>
        IntervalOf(expr).IsNonnegative;

    /// <summary>
    /// Derives the sign class of <paramref name="expr"/> from its inferred interval.
    /// </summary>
    public ProofSign SignOf(PreceptExpression expr)
    {
        var ival = IntervalOf(expr);
        if (ival.IsPositive) return ProofSign.Positive;
        if (ival.ExcludesZero) return ProofSign.Nonzero;
        if (ival.IsNonnegative) return ProofSign.Nonneg;
        return ProofSign.Unknown;
    }

    // ── Copy-on-write mutation methods ────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="ProofContext"/> that incorporates the narrowing
    /// implied by <paramref name="condition"/> being <paramref name="assumeTrue"/>.
    /// Delegates to <see cref="PreceptTypeChecker.ApplyNarrowing"/>.
    /// </summary>
    public ProofContext WithNarrowing(PreceptExpression condition, bool assumeTrue) =>
        PreceptTypeChecker.ApplyNarrowing(condition, this, assumeTrue);

    /// <summary>
    /// Returns a new <see cref="ProofContext"/> that incorporates proof knowledge
    /// derived from <c>set <paramref name="targetField"/> = <paramref name="rhs"/></c>.
    /// Delegates to <see cref="PreceptTypeChecker.ApplyAssignmentNarrowing"/>.
    /// </summary>
    public ProofContext WithAssignment(string targetField, PreceptExpression rhs) =>
        PreceptTypeChecker.ApplyAssignmentNarrowing(targetField, rhs, this);
}

/// <summary>Sign classification returned by <see cref="ProofContext.SignOf"/>.</summary>
internal enum ProofSign
{
    Unknown,
    Nonneg,
    Nonzero,
    Positive,
}
