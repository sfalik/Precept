using System.Globalization;
using System.Text;

namespace Precept.MatrixTools;

/// <summary>
/// Renders canonical expressions: <see cref="Key"/> is the deterministic
/// S-expression identity string (normal-form equality and operand-sort key);
/// <see cref="Display"/> is the human-readable infix form for CLI output.
/// </summary>
public static class CanonRender
{
    public static string Key(CanonExpr e) => e switch
    {
        CanonNumber n => "#" + n.Value.ToString("G29", CultureInfo.InvariantCulture),
        CanonBool b => b.Value ? "true" : "false",
        CanonString s => "\"" + s.Value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
        CanonUnset => "unset",
        CanonFieldPre f => $"pre({f.Name})",
        CanonArg a => $"arg({a.EventName}.{a.ArgName})",
        CanonBinding b => "$" + b.Depth.ToString(CultureInfo.InvariantCulture),
        CanonCompare c => $"({CompareTag(c.Op)} {Key(c.Left)} {Key(c.Right)})",
        CanonNary n => $"({NaryTag(n.Op)} {string.Join(' ', n.Operands.Select(Key))})",
        CanonBinary b => $"({BinaryTag(b.Op)} {Key(b.Left)} {Key(b.Right)})",
        CanonNeg n => $"(neg {Key(n.Operand)})",
        CanonNot n => $"(not {Key(n.Operand)})",
        CanonIsSet s => $"(isset {Key(s.Operand)})",
        CanonImplies i => $"(=> {Key(i.Antecedent)} {Key(i.Consequent)})",
        CanonConditional c => $"(if {Key(c.Condition)} {Key(c.Then)} {Key(c.Else)})",
        CanonMember m => $"(acc {m.Accessor} {Key(m.Receiver)}{Args(m.Arguments)})",
        CanonCall c => $"(call {c.Function}{Args(c.Arguments)})",
        CanonQuantifier q => $"(quant {Key(q.Collection)} {Key(q.Predicate)})",
        CanonTypedConst t => $"(const {t.TypeTag} {t.ValueKey})",
        CanonList l => $"(list{Args(l.Elements)})",
        CanonInterp i => $"(interp{Args(i.Segments)})",
        CanonError => "(error)",
        _ => throw new NotSupportedException(e.GetType().Name),
    };

    private static string Args(IEnumerable<CanonExpr> items)
    {
        var joined = string.Join(' ', items.Select(Key));
        return joined.Length == 0 ? "" : " " + joined;
    }

    private static string CompareTag(CanonCompareOp op) => op switch
    {
        CanonCompareOp.Lt => "lt",
        CanonCompareOp.Le => "le",
        CanonCompareOp.Eq => "eq",
        CanonCompareOp.Ne => "ne",
        CanonCompareOp.EqCi => "eqci",
        CanonCompareOp.NeCi => "neci",
        _ => throw new NotSupportedException(op.ToString()),
    };

    private static string NaryTag(CanonNaryOp op) => op switch
    {
        CanonNaryOp.Add => "+",
        CanonNaryOp.Mul => "*",
        CanonNaryOp.And => "and",
        CanonNaryOp.Or => "or",
        _ => throw new NotSupportedException(op.ToString()),
    };

    private static string BinaryTag(CanonBinaryOp op) => op switch
    {
        CanonBinaryOp.Divide => "div",
        CanonBinaryOp.Modulo => "mod",
        CanonBinaryOp.Contains => "contains",
        CanonBinaryOp.LookupAccess => "for",
        _ => throw new NotSupportedException(op.ToString()),
    };

    // ── Display (infix, canonical operand order) ─────────────────────────────

    public static string Display(CanonExpr e) => e switch
    {
        CanonNumber n => n.Value.ToString("G29", CultureInfo.InvariantCulture),
        CanonBool b => b.Value ? "true" : "false",
        CanonString s => "\"" + s.Value + "\"",
        CanonUnset => "⟨unset⟩",
        CanonFieldPre f => f.Name,
        CanonArg a => $"{a.EventName}.{a.ArgName}",
        CanonBinding b => $"_x{b.Depth}",
        CanonCompare c => $"{Operand(c.Left)} {CompareSymbol(c.Op)} {Operand(c.Right)}",
        CanonNary n => DisplayNary(n),
        CanonBinary b => $"{Operand(b.Left)} {BinarySymbol(b.Op)} {Operand(b.Right)}",
        CanonNeg n => $"-{Operand(n.Operand)}",
        CanonNot n => $"not {Operand(n.Operand)}",
        CanonIsSet s => $"{Operand(s.Operand)} is set",
        CanonImplies i => $"{Operand(i.Antecedent)} implies {Operand(i.Consequent)}",
        CanonConditional c => $"if {Operand(c.Condition)} then {Operand(c.Then)} else {Operand(c.Else)}",
        CanonMember m => $"{Operand(m.Receiver)}.{m.Accessor}"
            + (m.Arguments.IsEmpty ? "" : "(" + string.Join(", ", m.Arguments.Select(Display)) + ")"),
        CanonCall c => $"{c.Function}(" + string.Join(", ", c.Arguments.Select(Display)) + ")",
        CanonQuantifier q => $"no _x in {Operand(q.Collection)} ({Display(q.Predicate)})",
        CanonTypedConst t => $"'{t.ValueKey}'",
        CanonList l => "[" + string.Join(", ", l.Elements.Select(Display)) + "]",
        CanonInterp i => "interp{" + string.Join(", ", i.Segments.Select(Display)) + "}",
        CanonError => "⟨error⟩",
        _ => throw new NotSupportedException(e.GetType().Name),
    };

    private static string DisplayNary(CanonNary n)
    {
        if (n.Op is CanonNaryOp.And or CanonNaryOp.Or)
        {
            var word = n.Op == CanonNaryOp.And ? " and " : " or ";
            return string.Join(word, n.Operands.Select(Operand));
        }

        // Arithmetic: render "+ (neg X)" as "- X" for readability.
        var sb = new StringBuilder();
        for (int i = 0; i < n.Operands.Length; i++)
        {
            var operand = n.Operands[i];
            if (i == 0)
            {
                sb.Append(Operand(operand));
                continue;
            }
            if (n.Op == CanonNaryOp.Add && operand is CanonNeg neg)
                sb.Append(" - ").Append(Operand(neg.Operand));
            else if (n.Op == CanonNaryOp.Add && operand is CanonNumber { Value: < 0 } num)
                sb.Append(" - ").Append((-num.Value).ToString("G29", CultureInfo.InvariantCulture));
            else
                sb.Append(n.Op == CanonNaryOp.Add ? " + " : " * ").Append(Operand(operand));
        }
        return sb.ToString();
    }

    /// <summary>Display with parentheses around composite operands.</summary>
    private static string Operand(CanonExpr e) => e switch
    {
        CanonNumber or CanonBool or CanonString or CanonUnset or CanonFieldPre
            or CanonArg or CanonBinding or CanonTypedConst or CanonMember or CanonCall
            or CanonList or CanonNeg => Display(e),
        _ => "(" + Display(e) + ")",
    };

    private static string CompareSymbol(CanonCompareOp op) => op switch
    {
        CanonCompareOp.Lt => "<",
        CanonCompareOp.Le => "<=",
        CanonCompareOp.Eq => "==",
        CanonCompareOp.Ne => "!=",
        CanonCompareOp.EqCi => "=~",
        CanonCompareOp.NeCi => "!~",
        _ => throw new NotSupportedException(op.ToString()),
    };

    private static string BinarySymbol(CanonBinaryOp op) => op switch
    {
        CanonBinaryOp.Divide => "/",
        CanonBinaryOp.Modulo => "%",
        CanonBinaryOp.Contains => "contains",
        CanonBinaryOp.LookupAccess => "for",
        _ => throw new NotSupportedException(op.ToString()),
    };
}
