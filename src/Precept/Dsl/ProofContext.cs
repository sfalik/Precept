using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Precept;

// ── Relational fact types ─────────────────────────────────────────────────────

/// <summary>Strict vs. non-strict ordering for relational facts stored in <see cref="GlobalProofContext"/>.</summary>
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
public enum NumericFlags
{
    None        = 0,
    Positive    = 1,   // > 0
    Nonnegative = 2,   // >= 0
    Nonzero     = 4,   // != 0
}

// ── GlobalProofContext ──────────────────────────────────────────────────────────────

/// <summary>
/// Typed proof state container for the Precept proof engine.
/// Holds typed stores for field intervals, numeric flags, relational facts,
/// and expression-level interval facts. Also retains a string-marker dictionary
/// (<see cref="Symbols"/>) used by non-proof paths (field presence, type resolution).
/// </summary>
internal sealed class GlobalProofContext
{
    private readonly IReadOnlyDictionary<string, StaticValueKind> _symbols;
    private readonly Dictionary<LinearForm, RelationalFact> _relationalFacts;
    private readonly Dictionary<string, NumericInterval> _fieldIntervals;
    private readonly Dictionary<string, NumericFlags> _flags;
    private readonly Dictionary<LinearForm, NumericInterval> _exprFacts;

    /// <summary>Constructs a context from a string-marker dictionary only.</summary>
    public GlobalProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols)
    {
        _symbols = symbols;
        _relationalFacts = new Dictionary<LinearForm, RelationalFact>();
        _fieldIntervals = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
        _flags = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
        _exprFacts = new Dictionary<LinearForm, NumericInterval>();
    }

    /// <summary>Constructs a context from a dictionary plus a typed relational fact store.</summary>
    public GlobalProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols,
                        Dictionary<LinearForm, RelationalFact> relationalFacts)
    {
        _symbols = symbols;
        _relationalFacts = relationalFacts;
        _fieldIntervals = new Dictionary<string, NumericInterval>(StringComparer.Ordinal);
        _flags = new Dictionary<string, NumericFlags>(StringComparer.Ordinal);
        _exprFacts = new Dictionary<LinearForm, NumericInterval>();
    }

    /// <summary>Constructs a context with all typed stores supplied explicitly.</summary>
    public GlobalProofContext(IReadOnlyDictionary<string, StaticValueKind> symbols,
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
    /// that have not yet been migrated to operate on <see cref="GlobalProofContext"/> directly.
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
    /// Returns the tightest <see cref="ProofResult"/> for <paramref name="expr"/> by
    /// composing interval arithmetic with relational fact lookup via <see cref="LinearForm"/>.
    /// The result pairs a <see cref="NumericInterval"/> with a <see cref="ProofAttribution"/>
    /// tracking which rules, field constraints, and assignments contributed.
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
    public ProofResult IntervalOf(PreceptExpression expr)
    {
        var arithmetic = PreceptTypeChecker.TryInferInterval(expr, this);
        var attribution = ProofAttribution.None;

        var form = LinearForm.TryNormalize(expr);
        if (form is not null)
        {
            // Pure constant: A - A = 0, etc. Return exact singleton interval directly.
            if (form.Terms.IsEmpty)
            {
                double constVal = (double)form.Constant.Numerator / form.Constant.Denominator;
                return new ProofResult(
                    new NumericInterval(constVal, true, constVal, true),
                    new ProofAttribution(new[] { "constant expression" }));
            }

            // Typed expression facts (from assignment-derived proofs).
            if (_exprFacts.TryGetValue(form, out var exprFact))
            {
                arithmetic = NumericInterval.Intersect(arithmetic, exprFact);
                attribution = ProofAttribution.Merge(attribution,
                    new ProofAttribution(new[] { "derived from assignment" }));
            }

            var relational = LookupRelationalInterval(form);
            if (!relational.IsUnknown)
            {
                arithmetic = NumericInterval.Intersect(arithmetic, relational);
                attribution = ProofAttribution.Merge(attribution,
                    BuildRelationalAttribution(form));
            }
        }

        // If arithmetic is non-unknown but no attribution yet, attribute from field constraints or literals.
        if (!arithmetic.IsUnknown && attribution.Sources.Count == 0)
            attribution = BuildExpressionAttribution(expr);

        return new ProofResult(arithmetic, attribution);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="expr"/> is provably nonzero.
    /// Delegates entirely to <see cref="IntervalOf"/>: relational facts are composed there.
    /// </summary>
    public bool KnowsNonzero(PreceptExpression expr) => IntervalOf(expr).Interval.ExcludesZero;

    /// <summary>Returns <c>true</c> when <paramref name="expr"/> is provably non-negative.</summary>
    public bool KnowsNonnegative(PreceptExpression expr) => IntervalOf(expr).Interval.IsNonnegative;

    /// <summary>Derives the sign class of <paramref name="expr"/> from its inferred interval.</summary>
    public ProofSign SignOf(PreceptExpression expr)
    {
        var ival = IntervalOf(expr).Interval;
        if (ival.IsPositive)    return ProofSign.Positive;
        if (ival.ExcludesZero)  return ProofSign.Nonzero;
        if (ival.IsNonnegative) return ProofSign.Nonneg;
        return ProofSign.Unknown;
    }

    // ── Copy-on-write mutation methods ────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="GlobalProofContext"/> that incorporates the narrowing
    /// implied by <paramref name="condition"/> being <paramref name="assumeTrue"/>.
    /// </summary>
    public GlobalProofContext WithNarrowing(PreceptExpression condition, bool assumeTrue) =>
        PreceptTypeChecker.ApplyNarrowing(condition, this, assumeTrue);

    /// <summary>
    /// Alias for <see cref="WithNarrowing"/>. Returns a new <see cref="GlobalProofContext"/>
    /// narrowed by <paramref name="condition"/> being <paramref name="branch"/>.
    /// </summary>
    public GlobalProofContext WithGuard(PreceptExpression condition, bool branch) =>
        WithNarrowing(condition, branch);

    /// <summary>
    /// Returns a new <see cref="GlobalProofContext"/> with a typed relational fact stored directly:
    /// <c><paramref name="lhs"/> <paramref name="kind"/> <paramref name="rhs"/></c>.
    /// Both sides must normalize to a <see cref="LinearForm"/>; when either is non-normalizable
    /// the method returns <c>this</c> unchanged.
    /// </summary>
    public GlobalProofContext WithRule(PreceptExpression lhs, RelationKind kind, PreceptExpression rhs)
    {
        var lf = LinearForm.TryNormalize(lhs);
        var rf = LinearForm.TryNormalize(rhs);
        if (lf is null || rf is null)
            return this;

        var relFacts = new Dictionary<LinearForm, RelationalFact>(_relationalFacts)
        {
            [GcdNormalize(lf.Subtract(rf))] = new RelationalFact(kind)
        };
        return new GlobalProofContext(
            _symbols,
            relFacts,
            new Dictionary<string, NumericInterval>(_fieldIntervals, StringComparer.Ordinal),
            new Dictionary<string, NumericFlags>(_flags, StringComparer.Ordinal),
            new Dictionary<LinearForm, NumericInterval>(_exprFacts));
    }

    /// <summary>
    /// Returns a new <see cref="GlobalProofContext"/> that incorporates proof knowledge
    /// derived from <c>set <paramref name="targetField"/> = <paramref name="rhs"/></c>.
    /// </summary>
    public GlobalProofContext WithAssignment(string targetField, PreceptExpression rhs) =>
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

        return NumericInterval.Unknown;
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

    // ── Attribution helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Builds attribution for the arithmetic tier result when no other tier contributed.
    /// Handles simple identifiers (field constraints/flags) and literal constants.
    /// </summary>
    private ProofAttribution BuildExpressionAttribution(PreceptExpression expr)
    {
        var stripped = expr;
        while (stripped is PreceptParenthesizedExpression p) stripped = p.Inner;

        if (stripped is PreceptIdentifierExpression id)
        {
            var key = id.Member is null ? id.Name : $"{id.Name}.{id.Member}";
            return BuildFieldAttribution(key);
        }

        if (stripped is PreceptLiteralExpression)
            return new ProofAttribution(new[] { "constant expression" });

        return ProofAttribution.None;
    }

    /// <summary>
    /// Builds attribution from field constraint and flag stores for the given field key.
    /// </summary>
    private ProofAttribution BuildFieldAttribution(string key)
    {
        var sources = new List<string>();

        if (_fieldIntervals.TryGetValue(key, out var ival))
        {
            bool hasPositiveFlag = _flags.TryGetValue(key, out var f) && (f & NumericFlags.Positive) != 0;

            if (ival.Lower == 0 && !ival.LowerInclusive && double.IsPositiveInfinity(ival.Upper))
            {
                sources.Add("field constraint: positive");
            }
            else
            {
                if (!double.IsNegativeInfinity(ival.Lower))
                {
                    if (ival.Lower == 0 && ival.LowerInclusive)
                        sources.Add("field constraint: nonnegative");
                    else if (ival.LowerInclusive)
                        sources.Add(string.Create(CultureInfo.InvariantCulture,
                            $"field constraint: min {ival.Lower}"));
                }

                if (!double.IsPositiveInfinity(ival.Upper) && ival.UpperInclusive)
                    sources.Add(string.Create(CultureInfo.InvariantCulture,
                        $"field constraint: max {ival.Upper}"));
            }
        }
        else if (_flags.TryGetValue(key, out var flags))
        {
            if ((flags & NumericFlags.Positive) != 0)
                sources.Add("field constraint: positive");
            else if ((flags & NumericFlags.Nonnegative) != 0)
                sources.Add("field constraint: nonnegative");
        }

        return sources.Count > 0 ? new ProofAttribution(sources) : ProofAttribution.None;
    }

    /// <summary>
    /// Builds attribution for a relational fact match by reconstructing the rule description
    /// from the <see cref="LinearForm"/> that matched in <see cref="LookupRelationalInterval"/>.
    /// Follows the same lookup tiers as <see cref="LookupRelationalInterval"/> to identify which
    /// fact matched, then describes it in DSL terms (e.g., "rule A > B").
    /// </summary>
    private ProofAttribution BuildRelationalAttribution(LinearForm form)
    {
        // 1. Direct fact lookup.
        if (_relationalFacts.TryGetValue(form, out var fact))
            return DescribeRelationalFact(form, fact);

        // 2. GCD-normalized lookup.
        var normalized = GcdNormalize(form);
        bool wasNormalized = !ReferenceEquals(normalized, form);
        if (wasNormalized && _relationalFacts.TryGetValue(normalized, out var normFact))
            return DescribeRelationalFact(normalized, normFact);

        // 3. Negated form lookup.
        var negated = GcdNormalize(form.Negate());
        if (_relationalFacts.TryGetValue(negated, out var negFact))
        {
            var flippedKind = negFact.Kind == RelationKind.GreaterThan
                ? RelationKind.GreaterThan : RelationKind.GreaterThanOrEqual;
            return DescribeRelationalFact(negated, negFact);
        }

        // 4/5. Constant-offset or transitive: generic description.
        return new ProofAttribution(new[] { "relational rule inference" });
    }

    /// <summary>
    /// Describes a relational fact as a DSL rule string (e.g., "rule A &gt; B").
    /// </summary>
    private static ProofAttribution DescribeRelationalFact(LinearForm form, RelationalFact fact)
    {
        var op = fact.Kind == RelationKind.GreaterThan ? ">" : ">=";

        // Two-term form: {A: 1, B: -1} → "rule A > B" or "rule A >= B"
        if (form.Terms.Count == 2 && Rational.IsZero(form.Constant))
        {
            string? pos = null, neg = null;
            foreach (var (key, coeff) in form.Terms)
            {
                if (coeff == Rational.One) pos = key;
                else if (coeff == Rational.NegativeOne) neg = key;
            }
            if (pos is not null && neg is not null)
                return new ProofAttribution(new[] { $"rule {pos} {op} {neg}" });
        }

        // Single-term + constant: {A: 1, c: -N} → "rule A > N" or "rule A >= N"
        if (form.Terms.Count == 1)
        {
            var (key, coeff) = form.Terms.First();
            if (coeff == Rational.One && !Rational.IsZero(form.Constant))
            {
                var constVal = -(double)form.Constant.Numerator / form.Constant.Denominator;
                return new ProofAttribution(new[] {
                    string.Create(CultureInfo.InvariantCulture,
                        $"rule {key} {op} {constVal}") });
            }
        }

        // Complex form: use generic description.
        return new ProofAttribution(new[] { $"rule ({form} {op} 0)" });
    }

    // ── Scope isolation ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates an isolated child context with deep copies of all mutable stores.
    /// Mutations to the child do not affect this context or any sibling children.
    /// </summary>
    public GlobalProofContext Child() =>
        new(CopySymbols(),
            new Dictionary<LinearForm, RelationalFact>(_relationalFacts),
            new Dictionary<string, NumericInterval>(_fieldIntervals, StringComparer.Ordinal),
            new Dictionary<string, NumericFlags>(_flags, StringComparer.Ordinal),
            new Dictionary<LinearForm, NumericInterval>(_exprFacts));

    /// <summary>
    /// Creates an isolated child with replaced <paramref name="symbols"/> and the specified
    /// <paramref name="narrowings"/> merged over this context's typed stores (last writer wins).
    /// </summary>
    internal GlobalProofContext ChildMerging(
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        params ReadOnlySpan<GlobalProofContext?> narrowings)
    {
        var relFacts = new Dictionary<LinearForm, RelationalFact>(_relationalFacts);
        var intervals = new Dictionary<string, NumericInterval>(_fieldIntervals, StringComparer.Ordinal);
        var flags = new Dictionary<string, NumericFlags>(_flags, StringComparer.Ordinal);
        var exprFacts = new Dictionary<LinearForm, NumericInterval>(_exprFacts);

        foreach (var n in narrowings)
        {
            if (n is null) continue;
            foreach (var p in n.RelationalFacts) relFacts[p.Key] = p.Value;
            foreach (var p in n.FieldIntervals) intervals[p.Key] = p.Value;
            foreach (var p in n.Flags) flags[p.Key] = p.Value;
            foreach (var p in n.ExprFacts) exprFacts[p.Key] = p.Value;
        }

        return new GlobalProofContext(symbols, relFacts, intervals, flags, exprFacts);
    }

    private Dictionary<string, StaticValueKind> CopySymbols()
    {
        var copy = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
        foreach (var pair in _symbols) copy[pair.Key] = pair.Value;
        return copy;
    }

    // ── Debug snapshot ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a structured debug snapshot of all proof stores for introspection.
    /// This is NOT the MCP-facing DTO — it's for debug/test use only.
    /// </summary>
    public ProofDump Dump()
    {
        var fields = new Dictionary<string, ProofDump.FieldEntry>(StringComparer.Ordinal);
        foreach (var (name, interval) in _fieldIntervals)
        {
            _flags.TryGetValue(name, out var flagVal);
            var sources = BuildFieldAttribution(name).Sources;
            fields[name] = new ProofDump.FieldEntry(
                FormatInterval(interval),
                FormatDisplay(interval),
                flagVal,
                interval.Lower,
                interval.LowerInclusive,
                interval.Upper,
                interval.UpperInclusive,
                sources.Count > 0 ? sources : null);
        }
        // Include fields that have flags but no interval entry
        foreach (var (name, flagVal) in _flags)
        {
            if (!fields.ContainsKey(name))
            {
                var sources = BuildFieldAttribution(name).Sources;
                fields[name] = new ProofDump.FieldEntry(null, null, flagVal,
                    Sources: sources.Count > 0 ? sources : null);
            }
        }

        var relational = new List<ProofDump.RelationalEntry>();
        foreach (var (form, fact) in _relationalFacts)
            relational.Add(new ProofDump.RelationalEntry(form.ToString(), fact.Kind.ToString()));

        var exprFacts = new List<ProofDump.ExprFactEntry>();
        foreach (var (form, interval) in _exprFacts)
            exprFacts.Add(new ProofDump.ExprFactEntry(
                form.ToString(),
                FormatInterval(interval),
                FormatDisplay(interval),
                interval.Lower, interval.LowerInclusive,
                interval.Upper, interval.UpperInclusive));

        return new ProofDump(fields, relational, exprFacts);
    }

    public static string FormatInterval(NumericInterval interval)
    {
        if (interval.IsUnknown) return "(-∞, +∞)";
        var lb = interval.LowerInclusive ? "[" : "(";
        var ub = interval.UpperInclusive ? "]" : ")";
        var lo = double.IsNegativeInfinity(interval.Lower) ? "-∞" : interval.Lower.ToString(CultureInfo.InvariantCulture);
        var hi = double.IsPositiveInfinity(interval.Upper) ? "+∞" : interval.Upper.ToString(CultureInfo.InvariantCulture);
        return $"{lb}{lo}, {hi}{ub}";
    }

    public static string FormatDisplay(NumericInterval interval)
    {
        if (interval.IsUnknown) return "unknown";

        bool loInf = double.IsNegativeInfinity(interval.Lower);
        bool hiInf = double.IsPositiveInfinity(interval.Upper);

        // (0, +∞) → "always greater than 0"
        if (interval.Lower == 0 && !interval.LowerInclusive && hiInf)
            return "always greater than 0";

        // [0, +∞) → "0 or greater"
        if (interval.Lower == 0 && interval.LowerInclusive && hiInf)
            return "0 or greater";

        // (-∞, X] or (-∞, X) → "at most X" or "less than X"
        if (loInf && !hiInf)
            return interval.UpperInclusive
                ? $"{interval.Upper.ToString(CultureInfo.InvariantCulture)} or less"
                : $"less than {interval.Upper.ToString(CultureInfo.InvariantCulture)}";

        // [X, +∞) or (X, +∞) → "X or greater" or "greater than X"
        if (!loInf && hiInf)
            return interval.LowerInclusive
                ? $"{interval.Lower.ToString(CultureInfo.InvariantCulture)} or greater"
                : $"always greater than {interval.Lower.ToString(CultureInfo.InvariantCulture)}";

        // [X, X] → "exactly X"
        if (interval.Lower == interval.Upper && interval.LowerInclusive && interval.UpperInclusive)
            return $"exactly {interval.Lower.ToString(CultureInfo.InvariantCulture)}";

        // [X, Y] → "X to Y (inclusive)"
        if (interval.LowerInclusive && interval.UpperInclusive)
            return $"{interval.Lower.ToString(CultureInfo.InvariantCulture)} to {interval.Upper.ToString(CultureInfo.InvariantCulture)} (inclusive)";

        return $"{FormatInterval(interval)}";
    }
}

/// <summary>Sign classification returned by <see cref="GlobalProofContext.SignOf"/>.</summary>
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
            if (!merged.Contains(s, StringComparer.Ordinal))
                merged.Add(s);
        }
        return new ProofAttribution(merged);
    }
}

/// <summary>Structured debug snapshot of all proof stores in a <see cref="GlobalProofContext"/>.</summary>
public sealed record ProofDump(
    IReadOnlyDictionary<string, ProofDump.FieldEntry> Fields,
    IReadOnlyList<ProofDump.RelationalEntry> RelationalFacts,
    IReadOnlyList<ProofDump.ExprFactEntry> ExpressionFacts)
{
    public sealed record FieldEntry(
        string? Interval, string? Display, NumericFlags Flags,
        double? Lower = null, bool? LowerInclusive = null,
        double? Upper = null, bool? UpperInclusive = null,
        IReadOnlyList<string>? Sources = null);
    public sealed record RelationalEntry(string Form, string Kind);
    public sealed record ExprFactEntry(
        string Form, string Interval,
        string? Display = null,
        double? Lower = null, bool? LowerInclusive = null,
        double? Upper = null, bool? UpperInclusive = null);
}
