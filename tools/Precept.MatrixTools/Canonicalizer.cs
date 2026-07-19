using System.Collections.Immutable;
using System.Globalization;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

/// <summary>
/// A substitution environment: field name → canonical replacement. Fields
/// absent from the map canonicalize to pre-state reads (<see cref="CanonFieldPre"/>).
/// </summary>
public sealed class Substitution
{
    private readonly ImmutableDictionary<string, CanonExpr> _map;

    private Substitution(ImmutableDictionary<string, CanonExpr> map) => _map = map;

    public static readonly Substitution Empty =
        new(ImmutableDictionary<string, CanonExpr>.Empty.WithComparers(StringComparer.Ordinal));

    public Substitution With(string fieldName, CanonExpr replacement) =>
        new(_map.SetItem(fieldName, replacement));

    public CanonExpr? Lookup(string fieldName) =>
        _map.TryGetValue(fieldName, out var value) ? value : null;
}

/// <summary>
/// Translates the pipeline's <see cref="TypedExpression"/> DU into the canonical
/// (normal-form) tree, applying a substitution on field reads. All normalization
/// rules live in <see cref="Canon"/>'s factory methods; this class only walks the
/// typed AST and dispatches on the catalog-resolved operator identity
/// (<c>Operations.GetMeta(op).Op</c> — never on raw token shapes).
/// </summary>
public static class Canonicalizer
{
    public static CanonExpr Canonicalize(TypedExpression expression, Substitution substitution) =>
        Walk(expression, substitution, ImmutableList<string>.Empty);

    private static CanonExpr Walk(TypedExpression e, Substitution s, ImmutableList<string> bindings) => e switch
    {
        // Quantifier bindings surface as TypedFieldRef; alpha-rename to nesting index
        // and shield them from substitution. LastIndexOf: an inner binding shadows
        // an outer one of the same name.
        TypedFieldRef f when bindings.Contains(f.FieldName) =>
            new CanonBinding(bindings.LastIndexOf(f.FieldName)),

        TypedFieldRef f => s.Lookup(f.FieldName) ?? new CanonFieldPre(f.FieldName),
        TypedArgRef a => new CanonArg(a.EventName, a.ArgName),
        TypedLiteral l => Literal(l),

        TypedBinaryOp b => Binary(b, s, bindings),

        TypedUnaryOp u => Operations.GetMeta(u.ResolvedOp).Op switch
        {
            OperatorKind.Negate => Canon.Neg(Walk(u.Operand, s, bindings)),
            OperatorKind.Not => Canon.Not(Walk(u.Operand, s, bindings)),
            var op => throw new NotSupportedException($"unary operator {op} has no canonical form"),
        },

        TypedPostfixOp p => p.IsNegated
            ? Canon.Not(Canon.IsSet(Walk(p.Operand, s, bindings)))
            : Canon.IsSet(Walk(p.Operand, s, bindings)),

        TypedConditional c => new CanonConditional(
            Walk(c.Condition, s, bindings),
            Walk(c.ThenBranch, s, bindings),
            Walk(c.ElseBranch, s, bindings)),

        TypedFunctionCall fc => new CanonCall(
            fc.ResolvedFunction.ToString(),
            [.. fc.Arguments.Select(a => Walk(a, s, bindings))]),

        TypedMemberAccess m => new CanonMember(
            Walk(m.Object, s, bindings),
            m.ResolvedAccessor.Name,
            m.Arguments.IsDefaultOrEmpty
                ? ImmutableArray<CanonExpr>.Empty
                : [.. m.Arguments.Select(a => Walk(a, s, bindings))]),

        TypedQuantifier q => new CanonQuantifier(
            Walk(q.Collection, s, bindings),
            Walk(q.Predicate, s, bindings.Add(q.BindingName))),

        TypedInterpolatedString i => new CanonInterp(
            [.. i.Segments.Select(seg => seg switch
            {
                TypedTextSegment t => (CanonExpr)new CanonString(t.Text),
                TypedHoleSegment h => Walk(h.Expression, s, bindings),
                _ => throw new NotSupportedException(seg.GetType().Name),
            })]),

        TypedTypedConstant t => new CanonTypedConst(
            t.ResultType.ToString(),
            t.ParsedValue is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : t.ParsedValue?.ToString() ?? t.RawText),

        // Interpolated typed constants key by result type + static parts
        // (text/magnitude/qualifier) + per-slot semantic kind: '{X} USD' and
        // '{X} EUR' are different constants even with identical hole expressions.
        InterpolatedTypedConstant itc => new CanonCall(
            InterpolatedConstantTag(itc),
            [.. itc.Slots.Select(slot => (CanonExpr)new CanonCall(
                "slot:" + slot.SlotKind,
                [Walk(slot.Expression, s, bindings)]))]),

        TypedListLiteral ll => new CanonList(
            [.. ll.Elements.Select(el => Walk(el, s, bindings))]),

        TypedErrorExpression => new CanonError(),

        _ => throw new NotSupportedException($"no canonical form for {e.GetType().Name}"),
    };

    private static string InterpolatedConstantTag(InterpolatedTypedConstant itc)
    {
        var tag = "iconst:" + itc.ResultType;
        if (itc.StaticText is not null)
            tag += ":text=" + itc.StaticText;
        if (itc.StaticMagnitude is not null)
            tag += ":mag=" + itc.StaticMagnitude.Value.ToString("G29", CultureInfo.InvariantCulture);
        if (itc.StaticQualifier is not null)
            tag += ":q=" + (itc.StaticQualifier switch
            {
                StaticCurrencyQualifier c => c.CurrencyCode,
                StaticUnitQualifier u => u.Unit.ToString(),
                StaticCurrencyAndUnitQualifier cu => cu.CurrencyCode + "/" + cu.Unit,
                StaticFromToCurrenciesQualifier ft => ft.FromCode + ">" + ft.ToCode,
                var q => q.ToString(),
            });
        return tag;
    }

    private static CanonExpr Literal(TypedLiteral l) => l.Value switch
    {
        bool b => new CanonBool(b),
        long i => new CanonNumber(i),
        int i => new CanonNumber(i),
        decimal d => new CanonNumber(d),
        double d => new CanonNumber((decimal)d),
        string t => new CanonString(t),
        null => new CanonUnset(),
        var v => throw new NotSupportedException($"literal value type {v.GetType().Name}"),
    };

    /// <summary>
    /// The type kinds whose values form an additive/multiplicative group for
    /// normalization purposes: subtraction rewrites to addition of the negation,
    /// and + / * commute and reassociate. <see cref="TypeKind.Number"/> is
    /// deliberately excluded: IEEE doubles commute under + and * but do NOT
    /// reassociate — (1e16 + -1e16) + 1 is 1 while 1e16 + (-1e16 + 1) is 0 —
    /// so reordering/flattening would equate spellings with different runtime
    /// values. Number arithmetic routes through the ordered residue, as do
    /// mixed-kind temporal arithmetic (date + period, …) and string concatenation.
    /// </summary>
    private static bool IsNumericLike(TypeKind kind) => kind is
        TypeKind.Integer or TypeKind.Decimal or
        TypeKind.Money or TypeKind.Quantity or TypeKind.Price or
        TypeKind.Duration or TypeKind.Period or TypeKind.ExchangeRate;

    private static CanonExpr Binary(TypedBinaryOp b, Substitution s, ImmutableList<string> bindings)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(b.ResolvedOp);
        var left = Walk(b.Left, s, bindings);
        var right = Walk(b.Right, s, bindings);
        bool groupArithmetic = IsNumericLike(meta.Lhs.Kind) && IsNumericLike(meta.Rhs.Kind);

        return meta.Op switch
        {
            OperatorKind.Plus when groupArithmetic => Canon.Add(left, right),
            OperatorKind.Minus when groupArithmetic => Canon.Add(left, Canon.Neg(right)),
            OperatorKind.Times when groupArithmetic => Canon.Mul(left, right),

            OperatorKind.Divide => new CanonBinary(CanonBinaryOp.Divide, left, right),
            OperatorKind.Modulo => new CanonBinary(CanonBinaryOp.Modulo, left, right),

            OperatorKind.And => Canon.And(left, right),
            OperatorKind.Or => Canon.Or(left, right),

            OperatorKind.Equals => Canon.Compare(CanonCompareOp.Eq, left, right),
            OperatorKind.NotEquals => Canon.Compare(CanonCompareOp.Ne, left, right),
            OperatorKind.CaseInsensitiveEquals => Canon.Compare(CanonCompareOp.EqCi, left, right),
            OperatorKind.CaseInsensitiveNotEquals => Canon.Compare(CanonCompareOp.NeCi, left, right),
            OperatorKind.LessThan => Canon.Compare(CanonCompareOp.Lt, left, right),
            OperatorKind.LessThanOrEqual => Canon.Compare(CanonCompareOp.Le, left, right),
            // Comparison-direction flip: a > b ≡ b < a, a >= b ≡ b <= a.
            OperatorKind.GreaterThan => Canon.Compare(CanonCompareOp.Lt, right, left),
            OperatorKind.GreaterThanOrEqual => Canon.Compare(CanonCompareOp.Le, right, left),

            OperatorKind.Contains => new CanonBinary(CanonBinaryOp.Contains, left, right),
            OperatorKind.LookupAccess => new CanonBinary(CanonBinaryOp.LookupAccess, left, right),

            // Non-group plus/minus/times (string concat, mixed temporal): keep the
            // operand order and key by the resolved operation identity.
            OperatorKind.Plus or OperatorKind.Minus or OperatorKind.Times =>
                new CanonCall("op:" + b.ResolvedOp, [left, right]),

            var op => throw new NotSupportedException($"binary operator {op} has no canonical form"),
        };
    }
}
