namespace Precept.Language;

public abstract record UcumExpression;

public sealed record UcumAtomNode(UcumAtom Atom) : UcumExpression;

public sealed record UcumPrefixedAtomNode(UcumPrefix Prefix, UcumAtom Atom) : UcumExpression;

public sealed record UcumProductNode(UcumExpression Left, UcumExpression Right) : UcumExpression;

public sealed record UcumQuotientNode(UcumExpression Left, UcumExpression Right) : UcumExpression;

public sealed record UcumExponentNode(UcumExpression Inner, int Exponent) : UcumExpression;

public sealed record UcumGroupNode(UcumExpression Inner) : UcumExpression;

public sealed record UcumAnnotatedNode(UcumExpression Inner, string Annotation) : UcumExpression;
