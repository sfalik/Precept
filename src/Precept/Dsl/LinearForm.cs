using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Precept;

/// <summary>
/// Canonical linear form for <see cref="PreceptExpression"/>s: a sum of field references
/// weighted by exact <see cref="Rational"/> coefficients plus an additive constant.
/// </summary>
/// <remarks>
/// <para>Used as a dictionary key in <c>ProofContext._relationalFacts</c>.  Equality is
/// content-based: same terms (key → coefficient) and same constant.</para>
/// <para>Zero-coefficient terms are eliminated on construction.  The <see cref="Terms"/>
/// dictionary is sorted by field key, ensuring deterministic iteration and hash order.</para>
/// </remarks>
internal sealed class LinearForm : IEquatable<LinearForm>
{
    /// <summary>Field name (possibly dotted) → rational coefficient.  Zero-coefficient entries are absent.</summary>
    public ImmutableSortedDictionary<string, Rational> Terms { get; }

    /// <summary>Additive constant.</summary>
    public Rational Constant { get; }

    private LinearForm(ImmutableSortedDictionary<string, Rational> terms, Rational constant)
    {
        Terms = terms;
        Constant = constant;
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>Single-field form: <c>{field → 1}</c>, constant = 0.</summary>
    public static LinearForm FromField(string key) =>
        new(ImmutableSortedDictionary<string, Rational>.Empty.Add(key, Rational.One), Rational.Zero);

    /// <summary>Constant-only form: empty terms, constant = <paramref name="constant"/>.</summary>
    public static LinearForm FromConstant(Rational constant) =>
        new(ImmutableSortedDictionary<string, Rational>.Empty, constant);

    // ── Algebra ───────────────────────────────────────────────────────────────

    /// <summary>Returns <c>this + other</c>, cancelling zero-coefficient terms.</summary>
    public LinearForm Add(LinearForm other)
    {
        var builder = Terms.ToBuilder();
        foreach (var (key, coeff) in other.Terms)
        {
            if (builder.TryGetValue(key, out var existing))
            {
                var sum = existing + coeff;
                if (Rational.IsZero(sum))
                    builder.Remove(key);
                else
                    builder[key] = sum;
            }
            else
            {
                builder[key] = coeff;
            }
        }
        return new(builder.ToImmutable(), Constant + other.Constant);
    }

    /// <summary>Returns <c>this - other</c>.</summary>
    public LinearForm Subtract(LinearForm other) => Add(other.Negate());

    /// <summary>Returns <c>-this</c> (negates all coefficients and the constant).</summary>
    public LinearForm Negate()
    {
        var builder = Terms.ToBuilder();
        foreach (var key in builder.Keys.ToArray())
            builder[key] = -builder[key];
        return new(builder.ToImmutable(), -Constant);
    }

    /// <summary>
    /// Returns <c>this * scalar</c>.  Zero scalar produces a constant-only zero form.
    /// </summary>
    public LinearForm ScaleByConstant(Rational scalar)
    {
        if (Rational.IsZero(scalar))
            return FromConstant(Rational.Zero);
        var builder = Terms.ToBuilder();
        foreach (var key in builder.Keys.ToArray())
            builder[key] = builder[key] * scalar;
        return new(builder.ToImmutable(), Constant * scalar);
    }

    // ── TryNormalize ──────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to normalize <paramref name="expr"/> into a <see cref="LinearForm"/>.
    /// Returns <c>null</c> when the expression is non-linear, too deep, or arithmetic overflows.
    /// Never throws.
    /// </summary>
    /// <param name="expr">The expression to normalize.</param>
    /// <param name="depth">
    /// Remaining recursion budget (default 8).  Decremented on binary ops and unary ops.
    /// Parenthesized expressions do NOT consume depth budget.
    /// </param>
    public static LinearForm? TryNormalize(PreceptExpression expr, int depth = 8)
    {
        try
        {
            return TryNormalizeCore(expr, depth);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static LinearForm? TryNormalizeCore(PreceptExpression expr, int depth)
    {
        if (depth < 0) return null;

        switch (expr)
        {
            // ── Numeric literals ─────────────────────────────────────────────
            case PreceptLiteralExpression { Value: long l }:
                return FromConstant(new Rational(l, 1));

            case PreceptLiteralExpression { Value: int i }:
                return FromConstant(new Rational(i, 1));

            case PreceptLiteralExpression { Value: double d }:
                if (!double.IsFinite(d)) return null;
                return FromConstant(Rational.FromDouble(d));

            case PreceptLiteralExpression { Value: float f }:
                if (!float.IsFinite(f)) return null;
                return FromConstant(Rational.FromDouble(f));

            // ── Field references ─────────────────────────────────────────────
            case PreceptIdentifierExpression id:
            {
                var key = id.Member is null
                    ? id.Name
                    : id.SubMember is null
                        ? $"{id.Name}.{id.Member}"
                        : $"{id.Name}.{id.Member}.{id.SubMember}";
                return FromField(key);
            }

            // ── Parenthesized (no depth decrement) ───────────────────────────
            case PreceptParenthesizedExpression { Inner: var inner }:
                return TryNormalizeCore(inner, depth);

            // ── Unary minus / plus ────────────────────────────────────────────
            case PreceptUnaryExpression { Operator: "-", Operand: var operand }:
            {
                if (depth <= 0) return null;
                var form = TryNormalizeCore(operand, depth - 1);
                return form?.Negate();
            }

            case PreceptUnaryExpression { Operator: "+", Operand: var operand }:
            {
                if (depth <= 0) return null;
                return TryNormalizeCore(operand, depth - 1);
            }

            // ── Binary addition ──────────────────────────────────────────────
            case PreceptBinaryExpression { Operator: "+", Left: var left, Right: var right }:
            {
                if (depth <= 0) return null;
                var lf = TryNormalizeCore(left, depth - 1);
                if (lf is null) return null;
                var rf = TryNormalizeCore(right, depth - 1);
                if (rf is null) return null;
                return lf.Add(rf);
            }

            // ── Binary subtraction ───────────────────────────────────────────
            case PreceptBinaryExpression { Operator: "-", Left: var left, Right: var right }:
            {
                if (depth <= 0) return null;
                var lf = TryNormalizeCore(left, depth - 1);
                if (lf is null) return null;
                var rf = TryNormalizeCore(right, depth - 1);
                if (rf is null) return null;
                return lf.Subtract(rf);
            }

            // ── Binary multiplication (linear when one side is a constant) ───
            case PreceptBinaryExpression { Operator: "*", Left: var left, Right: var right }:
            {
                if (depth <= 0) return null;
                var lf = TryNormalizeCore(left, depth - 1);
                if (lf is null) return null;
                var rf = TryNormalizeCore(right, depth - 1);
                if (rf is null) return null;
                // Linear only when one side has no variable terms.
                if (lf.Terms.IsEmpty) return rf.ScaleByConstant(lf.Constant);
                if (rf.Terms.IsEmpty) return lf.ScaleByConstant(rf.Constant);
                return null; // non-linear: both sides have field terms
            }

            // ── Binary division (linear when divisor is a non-zero constant) ─
            case PreceptBinaryExpression { Operator: "/", Left: var left, Right: var right }:
            {
                if (depth <= 0) return null;
                var lf = TryNormalizeCore(left, depth - 1);
                if (lf is null) return null;
                var rf = TryNormalizeCore(right, depth - 1);
                if (rf is null) return null;
                if (!rf.Terms.IsEmpty) return null;  // dividing by a variable expression
                if (Rational.IsZero(rf.Constant)) return null;  // division by zero constant
                return lf.ScaleByConstant(Rational.One / rf.Constant);
            }

            // ── Everything else is non-normalizable ──────────────────────────
            // PreceptFunctionCallExpression, PreceptConditionalExpression, etc.
            default:
                return null;
        }
    }

    // ── Content-based equality ────────────────────────────────────────────────

    public bool Equals(LinearForm? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Constant != other.Constant) return false;
        if (Terms.Count != other.Terms.Count) return false;
        foreach (var (key, coeff) in Terms)
        {
            if (!other.Terms.TryGetValue(key, out var otherCoeff) || coeff != otherCoeff)
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as LinearForm);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Constant);
        // Terms is ImmutableSortedDictionary — iteration is deterministic (sorted by key).
        foreach (var (key, coeff) in Terms)
        {
            hash.Add(key);
            hash.Add(coeff);
        }
        return hash.ToHashCode();
    }

    // ── ToString (for diagnostics / debugging) ────────────────────────────────

    public override string ToString()
    {
        if (Terms.IsEmpty) return Constant.ToString();
        var sb = new StringBuilder();
        bool first = true;
        foreach (var (key, coeff) in Terms)
        {
            if (!first) sb.Append(" + ");
            if (coeff == Rational.One)
                sb.Append(key);
            else if (coeff == Rational.NegativeOne)
                sb.Append('-').Append(key);
            else
                sb.Append(coeff).Append('·').Append(key);
            first = false;
        }
        if (!Rational.IsZero(Constant))
            sb.Append(" + ").Append(Constant);
        return sb.ToString();
    }
}
