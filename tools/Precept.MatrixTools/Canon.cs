using System.Collections.Immutable;

namespace Precept.MatrixTools;

/// <summary>
/// Normalizing factory for canonical nodes. Every normal-form rule that acts at
/// construction time lives here: flattening + operand sorting for commutative
/// operators, subtraction as added negation, double-negation cancellation,
/// numeric-literal folding, presence evaluation over substituted values.
/// Implications are never folded — vacuity semantics belongs to the discharge
/// contracts, not to normalization.
/// </summary>
public static class Canon
{
    // ── Arithmetic ───────────────────────────────────────────────────────────

    public static CanonExpr Neg(CanonExpr operand) => operand switch
    {
        CanonNumber n => new CanonNumber(-n.Value),
        CanonNeg inner => inner.Operand,
        _ => new CanonNeg(operand),
    };

    public static CanonExpr Add(params CanonExpr[] operands) =>
        Nary(CanonNaryOp.Add, operands, identity: 0m, Fold: static (a, b) => a + b);

    public static CanonExpr Mul(params CanonExpr[] operands) =>
        Nary(CanonNaryOp.Mul, operands, identity: 1m, Fold: static (a, b) => a * b);

    private static CanonExpr Nary(
        CanonNaryOp op, CanonExpr[] operands, decimal identity, Func<decimal, decimal, decimal> Fold)
    {
        var flattened = new List<CanonExpr>();
        foreach (var operand in operands)
            Flatten(op, operand, flattened);

        // Literal folding (flagged rule): combine every numeric literal into one,
        // and drop it when it is the operation's identity element.
        decimal folded = identity;
        var symbolic = new List<CanonExpr>();
        foreach (var operand in flattened)
        {
            if (operand is CanonNumber n)
                folded = Fold(folded, n.Value);
            else
                symbolic.Add(operand);
        }

        if (symbolic.Count == 0)
            return new CanonNumber(folded);
        if (folded != identity)
            symbolic.Add(new CanonNumber(folded));

        symbolic.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
        return symbolic.Count == 1 ? symbolic[0] : new CanonNary(op, [.. symbolic]);
    }

    private static void Flatten(CanonNaryOp op, CanonExpr operand, List<CanonExpr> into)
    {
        if (operand is CanonNary nary && nary.Op == op)
            into.AddRange(nary.Operands);
        else
            into.Add(operand);
    }

    // ── Logic ────────────────────────────────────────────────────────────────

    public static CanonExpr Not(CanonExpr operand) => operand switch
    {
        CanonBool b => new CanonBool(!b.Value),
        CanonNot inner => inner.Operand,
        _ => new CanonNot(operand),
    };

    public static CanonExpr And(params CanonExpr[] operands) =>
        Junction(CanonNaryOp.And, operands, absorbing: false);

    public static CanonExpr Or(params CanonExpr[] operands) =>
        Junction(CanonNaryOp.Or, operands, absorbing: true);

    private static CanonExpr Junction(CanonNaryOp op, CanonExpr[] operands, bool absorbing)
    {
        var flattened = new List<CanonExpr>();
        foreach (var operand in operands)
            Flatten(op, operand, flattened);

        var kept = new List<CanonExpr>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operand in flattened)
        {
            if (operand is CanonBool b)
            {
                if (b.Value == absorbing)
                    return new CanonBool(absorbing);   // and-with-false / or-with-true
                continue;                              // neutral literal drops
            }
            if (seen.Add(operand.Key))                 // duplicate facts collapse
                kept.Add(operand);
        }

        if (kept.Count == 0)
            return new CanonBool(!absorbing);          // empty and → true, empty or → false
        kept.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
        return kept.Count == 1 ? kept[0] : new CanonNary(op, [.. kept]);
    }

    // ── Comparison ───────────────────────────────────────────────────────────

    public static CanonExpr Compare(CanonCompareOp op, CanonExpr left, CanonExpr right)
    {
        // Literal folding (flagged rule): ground comparisons evaluate.
        if (left is CanonNumber ln && right is CanonNumber rn)
        {
            return op switch
            {
                CanonCompareOp.Lt => new CanonBool(ln.Value < rn.Value),
                CanonCompareOp.Le => new CanonBool(ln.Value <= rn.Value),
                CanonCompareOp.Eq => new CanonBool(ln.Value == rn.Value),
                CanonCompareOp.Ne => new CanonBool(ln.Value != rn.Value),
                _ => Ordered(op, left, right),
            };
        }

        if (left is CanonBool lb && right is CanonBool rb && op is CanonCompareOp.Eq or CanonCompareOp.Ne)
            return new CanonBool(op == CanonCompareOp.Eq ? lb.Value == rb.Value : lb.Value != rb.Value);

        if (left is CanonString ls && right is CanonString rs)
        {
            bool equal = op is CanonCompareOp.EqCi or CanonCompareOp.NeCi
                ? string.Equals(ls.Value, rs.Value, StringComparison.OrdinalIgnoreCase)
                : string.Equals(ls.Value, rs.Value, StringComparison.Ordinal);
            if (op is CanonCompareOp.Eq or CanonCompareOp.EqCi)
                return new CanonBool(equal);
            if (op is CanonCompareOp.Ne or CanonCompareOp.NeCi)
                return new CanonBool(!equal);
        }

        return Ordered(op, left, right);
    }

    private static CanonExpr Ordered(CanonCompareOp op, CanonExpr left, CanonExpr right)
    {
        // Equality-family operands commute; order/inequality operands do not.
        if (op is CanonCompareOp.Eq or CanonCompareOp.Ne or CanonCompareOp.EqCi or CanonCompareOp.NeCi
            && string.CompareOrdinal(left.Key, right.Key) > 0)
        {
            (left, right) = (right, left);
        }
        return new CanonCompare(op, left, right);
    }

    // ── Presence ─────────────────────────────────────────────────────────────

    public static CanonExpr IsSet(CanonExpr operand) => operand switch
    {
        // Establishment substitution can decide presence: an unset marker is not
        // set; a substituted concrete value is.
        CanonUnset => new CanonBool(false),
        CanonNumber or CanonBool or CanonString or CanonTypedConst => new CanonBool(true),
        _ => new CanonIsSet(operand),
    };
}
