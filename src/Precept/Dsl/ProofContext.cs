using System;
using System.Collections.Generic;

namespace Precept;

// ── Relational fact types ─────────────────────────────────────────────────────

/// <summary>Strict vs. non-strict ordering for relational facts stored in <see cref="ProofContext"/>.</summary>
internal enum RelationKind { GreaterThan, GreaterThanOrEqual }

/// <summary>
/// A relational fact asserted about a <see cref="LinearForm"/>: the form is either strictly
/// positive (<c>&gt; 0</c>) or non-negative (<c>&gt;= 0</c>).
/// </summary>
internal sealed record RelationalFact(RelationKind Kind)
{
    /// <summary>
    /// Converts this fact to the tightest <see cref="NumericInterval"/> it implies.
    /// <c>&gt; 0</c> → <c>(0, +∞)</c>; <c>&gt;= 0</c> → <c>[0, +∞)</c>.
    /// </summary>
    public NumericInterval ToInterval() => Kind switch
    {
        RelationKind.GreaterThan        => NumericInterval.Positive,   // > 0
        RelationKind.GreaterThanOrEqual => NumericInterval.Nonneg,     // >= 0
        _                               => NumericInterval.Unknown,
    };
}

// ── Numeric flags ─────────────────────────────────────────────────────────────

/// <summary>Bitwise numeric sign/zero flags for proof stores.</summary>
[Flags]
internal enum NumericFlags
{
    None        = 0,
    Positive    = 1,   // > 0
    Nonnegative = 2,   // >= 0
    Nonzero     = 4,   // != 0
}

// ── ProofContext ──────────────────────────────────────────────────────────────

/// <summary>
/// Typed proof state container for the Precept proof engine.
/// Holds a string-marker dictionary (legacy path) and a typed
/// <see cref="LinearForm"/>-keyed relational fact store (new path).
/// </summary>
/// <remarks>
/// The six string-marker conventions stored in <see cref="Symbols"/>:
/// <list type="bullet">
///   <item><c>$positive:{key}</c> — key &gt; 0</item>
///   <item><c>$nonneg:{key}</c> — key &gt;= 0</item>
///   <item><c>$nonzero:{key}</c> — key != 0</item>
///   <item><c>$ival:{key}:{lower}:{lowerInc}:{upper}:{upperInc}</c> — interval</item>
///   <item><c>$gt:{a}:{b}</c> — a &gt; b (strict)</item>
///   <item><c>$gte:{a}:{b}</c> — a &gt;= b (non-strict)</item>
/// </list>
/// </remarks>
internal sealed class ProofContext
{
    private readonly IReadOnlyDictionary<string, StaticValueKind> _symbols;
    private readonly Dictionary<LinearForm, RelationalFact> _relationalFacts;
    private readonly Dictionary<string, NumericInterval> _fieldIntervals;
    private readonly Dictionary<string, NumericFlags> _flags;
    private readonly Dictionary<LinearForm, NumericInterval> _exprFacts;

    /// <summary>Constructs a context from the legacy string-marker dictionary only.</summary>
    public ProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols)
    {
        _symbols = symbols;
        _relationalFacts = new Dictionary<LinearForm, RelationalFact>();
        _fieldIntervals = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
        _flags = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
        _exprFacts = new Dictionary<LinearForm, NumericInterval>();
    }

    /// <summary>Constructs a context from the legacy dictionary plus a typed relational fact store.</summary>
    public ProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols,
                        Dictionary<LinearForm, RelationalFact> relationalFacts)
    {
        _symbols = symbols;
        _relationalFacts = relationalFacts;
        _fieldIntervals = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
        _flags = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
        _exprFacts = new Dictionary<LinearForm, NumericInterval>();
    }

    /// <summary>Constructs a context with all typed stores supplied explicitly.</summary>
    public ProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols,
                        Dictionary<LinearForm, RelationalFact> relationalFacts,
                        Dictionary<string, NumericInterval> fieldIntervals,
                        Dictionary<string, NumericFlags> flags,
                        Dictionary<LinearForm, NumericInterval> exprFacts)
    {
        _symbols = symbols;
        _relationalFacts = relationalFacts;
        _fieldIntervals = fieldIntervals;
        _flags = flags;
        _exprFacts = exprFacts;
    }

    /// <summary>
    /// The raw string-marker dictionary. Used by <see cref="PreceptTypeChecker"/> methods
    /// that have not yet been migrated to operate on <see cref="ProofContext"/> directly.
    /// </summary>
    internal IReadOnlyDictionary<string, StaticValueKind> Symbols => _symbols;

    /// <summary>
    /// The typed relational fact store, keyed by the <see cref="LinearForm"/> of
    /// <c>LHS - RHS</c> for each stored <c>LHS op RHS</c> fact.
    /// </summary>
    internal IReadOnlyDictionary<LinearForm, RelationalFact> RelationalFacts => _relationalFacts;

    /// <summary>Typed field-level interval store.</summary>
    internal IReadOnlyDictionary<string, NumericInterval> FieldIntervals => _fieldIntervals;

    /// <summary>Typed field-level numeric sign/zero flags.</summary>
    internal IReadOnlyDictionary<string, NumericFlags> Flags => _flags;

    /// <summary>Typed expression-level interval store keyed by <see cref="LinearForm"/>.</summary>
    internal IReadOnlyDictionary<LinearForm, NumericInterval> ExprFacts => _exprFacts;

    // ── Primary query methods ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the tightest <see cref="NumericInterval"/> for <paramref name="expr"/> by
    /// composing interval arithmetic with relational fact lookup via <see cref="LinearForm"/>.
    /// </summary>
    /// <remarks>
    /// Step 1: Interval arithmetic via <see cref="PreceptTypeChecker.TryInferInterval"/>.
    /// Step 2: If the expression normalizes to a <see cref="LinearForm"/>, tighten via
    ///   (a) pure-constant form: return exact singleton interval,
    ///   (b) direct lookup in <see cref="_relationalFacts"/>,
    ///   (c) GCD-normalized scalar-multiple lookup (e.g., 3A−3B → A−B; 0.5A−0.5B → A−B),
    ///   (d) negated form lookup (e.g., B−A matched against A−B fact),
    ///   (e) constant-offset scan (e.g., (A+1)−B matched against A−B fact),
    ///   (f) legacy string-marker fallback for simple A−B forms.
    /// The result is the intersection of all available information.
    /// </remarks>
    public NumericInterval IntervalOf(PreceptExpression expr)
    {
        var arithmetic = PreceptTypeChecker.TryInferInterval(expr, this);

        var form = LinearForm.TryNormalize(expr);
        if (form is not null)
        {
            // Pure constant: A - A = 0, etc. Return exact singleton interval directly.
            if (form.Terms.IsEmpty)
            {
                double constVal = (double)form.Constant.Numerator / form.Constant.Denominator;
                return new NumericInterval(constVal, true, constVal, true);
            }

            var relational = LookupRelationalInterval(form);
            arithmetic = NumericInterval.Intersect(arithmetic, relational);
        }

        return arithmetic;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="expr"/> is provably nonzero.
    /// Delegates entirely to <see cref="IntervalOf"/>: relational facts are composed there.
    /// </summary>
    public bool KnowsNonzero(PreceptExpression expr) => IntervalOf(expr).ExcludesZero;

    /// <summary>Returns <c>true</c> when <paramref name="expr"/> is provably non-negative.</summary>
    public bool KnowsNonnegative(PreceptExpression expr) => IntervalOf(expr).IsNonnegative;

    /// <summary>Derives the sign class of <paramref name="expr"/> from its inferred interval.</summary>
    public ProofSign SignOf(PreceptExpression expr)
    {
        var ival = IntervalOf(expr);
        if (ival.IsPositive)    return ProofSign.Positive;
        if (ival.ExcludesZero)  return ProofSign.Nonzero;
        if (ival.IsNonnegative) return ProofSign.Nonneg;
        return ProofSign.Unknown;
    }

    // ── Copy-on-write mutation methods ────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="ProofContext"/> that incorporates the narrowing
    /// implied by <paramref name="condition"/> being <paramref name="assumeTrue"/>.
    /// </summary>
    public ProofContext WithNarrowing(PreceptExpression condition, bool assumeTrue) =>
        PreceptTypeChecker.ApplyNarrowing(condition, this, assumeTrue);

    /// <summary>
    /// Alias for <see cref="WithNarrowing"/>. Returns a new <see cref="ProofContext"/>
    /// narrowed by <paramref name="condition"/> being <paramref name="branch"/>.
    /// </summary>
    public ProofContext WithGuard(PreceptExpression condition, bool branch) =>
        WithNarrowing(condition, branch);

    /// <summary>
    /// Returns a new <see cref="ProofContext"/> with a typed relational fact stored directly:
    /// <c><paramref name="lhs"/> <paramref name="kind"/> <paramref name="rhs"/></c>.
    /// Both sides must normalize to a <see cref="LinearForm"/>; when either is non-normalizable
    /// the method returns <c>this</c> unchanged.
    /// </summary>
    public ProofContext WithRule(PreceptExpression lhs, RelationKind kind, PreceptExpression rhs)
    {
        var lf = LinearForm.TryNormalize(lhs);
        var rf = LinearForm.TryNormalize(rhs);
        if (lf is null || rf is null)
            return this;

        var relFacts = new Dictionary<LinearForm, RelationalFact>(_relationalFacts)
        {
            [GcdNormalize(lf.Subtract(rf))] = new RelationalFact(kind)
        };
        return new ProofContext(_symbols, relFacts, _fieldIntervals, _flags, _exprFacts);
    }

    /// <summary>
    /// Returns a new <see cref="ProofContext"/> that incorporates proof knowledge
    /// derived from <c>set <paramref name="targetField"/> = <paramref name="rhs"/></c>.
    /// </summary>
    public ProofContext WithAssignment(string targetField, PreceptExpression rhs) =>
        PreceptTypeChecker.ApplyAssignmentNarrowing(targetField, rhs, this);

    // ── Relational tightening ─────────────────────────────────────────────────

    private NumericInterval LookupRelationalInterval(LinearForm form)
    {
        // 1. Direct fact lookup.
        if (_relationalFacts.TryGetValue(form, out var fact))
            return fact.ToInterval();

        // 2. GCD-normalized scalar-multiple lookup: 3A−3B and 0.5A−0.5B both reduce to A−B.
        var normalized = GcdNormalize(form);
        bool wasNormalized = !ReferenceEquals(normalized, form);
        if (wasNormalized && _relationalFacts.TryGetValue(normalized, out var normFact))
            return normFact.ToInterval();

        // 3. Negated form lookup: B−A matched against stored A−B fact (negates the resulting interval).
        var negated = GcdNormalize(form.Negate());
        if (_relationalFacts.TryGetValue(negated, out var negFact))
            return NumericInterval.Negate(negFact.ToInterval());

        // 4. Constant-offset scan on GCD-normalized (or original) form.
        //    (A+1)−B with rule A>B: stored key {A:1,B:-1}, query {A:1,B:-1,c=1}, c=+1>0 → (1,+∞).
        var scanForm = wasNormalized ? normalized : form;
        var offsetResult = ConstantOffsetScan(scanForm);
        if (!offsetResult.IsUnknown) return offsetResult;

        // 5. Transitive closure: BFS over the relational fact graph (depth ≤ 4, nodes ≤ 256).
        var transitiveResult = new RelationalGraph(_relationalFacts).Query(form);
        if (!transitiveResult.IsUnknown) return transitiveResult;

        // 6. Legacy string-marker fallback for simple A − B identifier differences.
        return LookupLegacyRelationalInterval(form);
    }

    /// <summary>
    /// GCD-normalizes all variable-term coefficients using the rational GCD:
    /// <c>gcd(p1/q1, …) = gcd(|p_i|) / lcm(q_i)</c>.
    /// <c>3A − 3B</c> → <c>A − B</c>; <c>½A − ½B</c> → <c>A − B</c>.
    /// Returns the same instance when the rational GCD equals 1 (no-op).
    /// </summary>
    internal static LinearForm GcdNormalize(LinearForm form)
    {
        if (form.Terms.IsEmpty) return form;

        long numGcd = 0;
        long denLcm = 1;
        foreach (var (_, coeff) in form.Terms)
        {
            numGcd = Gcd(numGcd, Math.Abs(coeff.Numerator));
            denLcm = Lcm(denLcm, coeff.Denominator);
        }

        if (numGcd == 0) return form;
        var gcdRat = new Rational(numGcd, denLcm);
        if (gcdRat == Rational.One) return form; // already normalized — same reference

        return form.ScaleByConstant(Rational.One / gcdRat);
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0) { long t = b; b = a % b; a = t; }
        return a;
    }

    private static long Lcm(long a, long b)
    {
        if (a == 0) return b;
        if (b == 0) return a;
        return checked(a / Gcd(a, b) * b);
    }

    /// <summary>
    /// Scans <see cref="_relationalFacts"/> for an entry whose variable terms match
    /// <paramref name="form"/> exactly but whose constant differs by a useful offset.
    /// When the offset <c>c = form.Constant − stored.Constant</c> satisfies:
    /// <list type="bullet">
    ///   <item><c>c &gt; 0</c> and fact is <c>&gt;</c>: returns <c>(c, +∞)</c></item>
    ///   <item><c>c &gt;= 0</c> and fact is <c>&gt;=</c>: returns <c>[c, +∞)</c></item>
    /// </list>
    /// </summary>
    private NumericInterval ConstantOffsetScan(LinearForm form)
    {
        foreach (var (storedKey, storedFact) in _relationalFacts)
        {
            if (!TermsMatch(form, storedKey)) continue;

            var c = form.Constant - storedKey.Constant;
            var cDouble = (double)c.Numerator / c.Denominator;

            if (storedFact.Kind == RelationKind.GreaterThan && c > Rational.Zero)
                return new NumericInterval(cDouble, false, double.PositiveInfinity, false);

            if (storedFact.Kind == RelationKind.GreaterThanOrEqual && c >= Rational.Zero)
                return new NumericInterval(cDouble, true, double.PositiveInfinity, false);
        }
        return NumericInterval.Unknown;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> and <paramref name="b"/> have
    /// identical variable terms (same keys and same coefficients), ignoring the constant.
    /// </summary>
    private static bool TermsMatch(LinearForm a, LinearForm b)
    {
        if (a.Terms.Count != b.Terms.Count) return false;
        foreach (var (key, coeff) in a.Terms)
        {
            if (!b.Terms.TryGetValue(key, out var otherCoeff) || coeff != otherCoeff)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Fallback relational interval lookup using legacy <c>$gt:</c>/<c>$gte:</c> string markers.
    /// Only applies to exactly two-term forms <c>{A → 1, B → −1}</c> with constant 0 (i.e., A − B).
    /// </summary>
    private NumericInterval LookupLegacyRelationalInterval(LinearForm form)
    {
        if (form.Terms.Count != 2 || !Rational.IsZero(form.Constant))
            return NumericInterval.Unknown;

        string? posKey = null, negKey = null;
        foreach (var (key, coeff) in form.Terms)
        {
            if (coeff == Rational.One)         posKey = key;
            else if (coeff == Rational.NegativeOne) negKey = key;
            else return NumericInterval.Unknown;
        }

        if (posKey is null || negKey is null) return NumericInterval.Unknown;

        // $gt:A:B → A > B → A − B ∈ (0, +∞)
        if (_symbols.ContainsKey($"$gt:{posKey}:{negKey}"))
            return NumericInterval.Positive;
        // $gt:B:A → B > A → A − B ∈ (−∞, 0) — still ExcludesZero
        if (_symbols.ContainsKey($"$gt:{negKey}:{posKey}"))
            return new NumericInterval(double.NegativeInfinity, false, 0, false);
        // $gte:A:B → A >= B → A − B ∈ [0, +∞)
        if (_symbols.ContainsKey($"$gte:{posKey}:{negKey}"))
            return NumericInterval.Nonneg;
        // $gte:B:A → B >= A → A − B ∈ (−∞, 0]
        if (_symbols.ContainsKey($"$gte:{negKey}:{posKey}"))
            return new NumericInterval(double.NegativeInfinity, false, 0, true);

        return NumericInterval.Unknown;
    }
}

/// <summary>Sign classification returned by <see cref="ProofContext.SignOf"/>.</summary>
internal enum ProofSign
{
    Unknown,
    Nonneg,
    Nonzero,
    Positive,
}

/// <summary>An interval result paired with attribution indicating which proof sources contributed.</summary>
internal readonly record struct ProofResult(NumericInterval Interval, ProofAttribution Attribution)
{
    public static ProofResult Unknown { get; } = new(NumericInterval.Unknown, ProofAttribution.None);
    public bool IsUnknown => Interval.IsUnknown;
}

/// <summary>Tracks which proof sources contributed to an interval result.</summary>
internal sealed class ProofAttribution
{
    public static readonly ProofAttribution None = new(Array.Empty<string>());

    public IReadOnlyList<string> Sources { get; }

    public ProofAttribution(IReadOnlyList<string> sources) => Sources = sources;

    public static ProofAttribution Merge(ProofAttribution a, ProofAttribution b)
    {
        if (a.Sources.Count == 0) return b;
        if (b.Sources.Count == 0) return a;
        var merged = new List<string>(a.Sources);
        foreach (var s in b.Sources)
        {
            bool found = false;
            foreach (var m in merged)
            {
                if (string.Equals(m, s, StringComparison.Ordinal)) { found = true; break; }
            }
            if (!found)
                merged.Add(s);
        }
        return new ProofAttribution(merged);
    }
}
