namespace Precept.Language;

/// <summary>Unary or binary.</summary>
public enum Arity { Unary = 1, Binary = 2 }

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
}

/// <summary>Binding direction for the Pratt parser.</summary>
public enum Associativity { Left = 1, Right = 2, NonAssociative = 3 }

/// <summary>
/// Metadata for a single operator. Token is the lexer token that produces this
/// operator; for <see cref="OperatorKind.Negate"/> this is the same
/// <see cref="TokenKind.Minus"/> token used by <see cref="OperatorKind.Minus"/>.
/// </summary>
public record OperatorMeta(
    OperatorKind Kind,
    TokenMeta Token,
    string Description,
    Arity Arity,
    Associativity Associativity,
    int Precedence,
    OperatorFamily Family,
    bool IsKeywordOperator = false,
    string? HoverDescription = null,
    string? UsageExample = null);
