namespace Precept.Language;

/// <summary>Unary, binary, or postfix.</summary>
public enum Arity { Unary = 1, Binary = 2, Postfix = 3 }

/// <summary>
/// Broad operator family — used by the grammar generator, LS semantic tokens,
/// and MCP vocabulary to assign different scopes to operator groups.
/// </summary>
public enum OperatorFamily
{
    Arithmetic  = 1,
    Comparison  = 2,
    Logical     = 3,
    Membership  = 4,
    Presence    = 5,
}

/// <summary>Binding direction for the Pratt parser.</summary>
public enum Associativity { Left = 1, Right = 2, NonAssociative = 3 }

/// <summary>
/// Base metadata for a single operator. Subtypes carry the token structure:
/// <see cref="SingleTokenOp"/> for operators triggered by one token,
/// <see cref="MultiTokenOp"/> for keyword sequences like <c>is set</c> / <c>is not set</c>.
/// </summary>
public abstract record OperatorMeta(
    OperatorKind   Kind,
    string         Description,
    Arity          Arity,
    Associativity  Associativity,
    int            Precedence,
    OperatorFamily Family,
    bool           IsKeywordOperator = false,
    string?        HoverDescription  = null,
    string?        UsageExample      = null);

/// <summary>
/// Operator produced by a single lexer token.
/// For <see cref="OperatorKind.Negate"/> this is the same
/// <see cref="TokenKind.Minus"/> token used by <see cref="OperatorKind.Minus"/>.
/// </summary>
public sealed record SingleTokenOp(
    OperatorKind   Kind,
    TokenMeta      Token,
    string         Description,
    Arity          Arity,
    Associativity  Associativity,
    int            Precedence,
    OperatorFamily Family,
    bool           IsKeywordOperator = false,
    string?        HoverDescription  = null,
    string?        UsageExample      = null)
    : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family,
                   IsKeywordOperator, HoverDescription, UsageExample);

/// <summary>
/// Operator produced by a sequence of two or three keyword tokens (e.g. <c>is set</c>,
/// <c>is not set</c>). <see cref="LeadToken"/> is the first token in the sequence and
/// is used by the Pratt loop to detect the operator.
/// </summary>
public sealed record MultiTokenOp(
    OperatorKind              Kind,
    IReadOnlyList<TokenMeta>  Tokens,
    string                    Description,
    Arity                     Arity,
    Associativity             Associativity,
    int                       Precedence,
    OperatorFamily            Family,
    bool                      IsKeywordOperator = false,
    string?                   HoverDescription  = null,
    string?                   UsageExample      = null)
    : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family,
                   IsKeywordOperator, HoverDescription, UsageExample)
{
    public TokenMeta LeadToken => Tokens[0];
}
